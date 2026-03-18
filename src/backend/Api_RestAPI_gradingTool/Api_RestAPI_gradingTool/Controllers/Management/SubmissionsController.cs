using Api_RestAPI_gradingTool.Contracts.Management;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Globalization;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_RestAPI_gradingTool.Validation;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api")]
public sealed class SubmissionsController : ApiControllerBase
{
    private const int MaxPageSize = 100;
    private const long MaxSubmissionSizeBytes = 200 * 1024 * 1024;
    private const int SubmissionStatusPending = 0;
    private const int SubmissionStatusQueued = 1;
    private const int SubmissionStatusRunning = 2;
    private const int SubmissionStatusCompleted = 3;
    private const int SubmissionStatusError = 4;
    private readonly GradingDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(GradingDbContext db, IWebHostEnvironment env, ILogger<SubmissionsController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet("exams/{examId:int}/submissions")]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> ListByExam(
        int examId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Exams.AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return ProblemNotFound("Exam not found.");
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<Submission> query = _db.Submissions.AsNoTracking()
            .Where(s => s.ExamId == examId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s => EF.Functions.Like(s.StudentName, term) || EF.Functions.Like(s.FileName, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionDto
            {
                Id = s.Id,
                ExamId = s.ExamId,
                StudentName = s.StudentName,
                FileName = s.FileName,
                FilePath = s.FilePath,
                SubmittedAt = s.SubmittedAt,
                Status = s.Status,
                TotalScore = s.TotalScore,
                LastError = s.LastError
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<SubmissionDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("submissions/{id:int}")]
    public async Task<ActionResult<SubmissionDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SubmissionDto
            {
                Id = s.Id,
                ExamId = s.ExamId,
                StudentName = s.StudentName,
                FileName = s.FileName,
                FilePath = s.FilePath,
                SubmittedAt = s.SubmittedAt,
                Status = s.Status,
                TotalScore = s.TotalScore,
                LastError = s.LastError
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        return Ok(submission);
    }

    [HttpDelete("submissions/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        if (!string.IsNullOrWhiteSpace(submission.FilePath) && System.IO.File.Exists(submission.FilePath))
        {
            System.IO.File.Delete(submission.FilePath);
        }

        _db.Submissions.Remove(submission);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("exams/{examId:int}/submissions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxSubmissionSizeBytes)]
    public async Task<ActionResult<SubmissionDto>> Upload(
        int examId,
        [FromForm] SubmissionUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Exams.AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return ProblemNotFound("Exam not found.");
        }

        var studentNameError = NameRules.Validate(request.StudentName, 3, 200, "StudentName");
        if (studentNameError is not null)
        {
            return ProblemBadRequest(studentNameError);
        }

        if (request.File is null || request.File.Length == 0)
        {
            return ProblemBadRequest("Submission file is required.");
        }

        var ext = Path.GetExtension(request.File.FileName);
        if (!IsAllowedArchive(ext))
        {
            return ProblemBadRequest("Submission file must be .zip or .rar.");
        }

        if (request.File.Length > MaxSubmissionSizeBytes)
        {
            return ProblemBadRequest("Submission file is too large.");
        }

        var entity = new Submission
        {
            ExamId = examId,
            StudentName = request.StudentName.Trim(),
            Status = SubmissionStatusPending,
            TotalScore = 0,
            LastError = null,
            FileName = Path.GetFileName(request.File.FileName),
            FilePath = string.Empty
        };

        _db.Submissions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dataRoot = Path.Combine(_env.ContentRootPath, "data", "submissions", entity.Id.ToString());
        Directory.CreateDirectory(dataRoot);

        var filePath = Path.Combine(dataRoot, entity.FileName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await request.File.CopyToAsync(stream, cancellationToken);
        }

        entity.FilePath = filePath;
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new SubmissionDto
        {
            Id = entity.Id,
            ExamId = entity.ExamId,
            StudentName = entity.StudentName,
            FileName = entity.FileName,
            FilePath = entity.FilePath,
            SubmittedAt = entity.SubmittedAt,
            Status = entity.Status,
            TotalScore = entity.TotalScore,
            LastError = entity.LastError
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    private static IQueryable<Submission> ApplySort(IQueryable<Submission> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "submittedat").ToLowerInvariant() switch
        {
            "studentname" => isDesc ? query.OrderByDescending(s => s.StudentName) : query.OrderBy(s => s.StudentName),
            "submittedat" => isDesc ? query.OrderByDescending(s => s.SubmittedAt) : query.OrderBy(s => s.SubmittedAt),
            "status" => isDesc ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            "totalscore" => isDesc ? query.OrderByDescending(s => s.TotalScore) : query.OrderBy(s => s.TotalScore),
            _ => isDesc ? query.OrderByDescending(s => s.SubmittedAt) : query.OrderBy(s => s.SubmittedAt)
        };
    }

    private static bool IsAllowedArchive(string? extension)
    {
        return string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".rar", StringComparison.OrdinalIgnoreCase);
    }

    [HttpPost("submissions/{id:int}/grade")]
    public async Task<ActionResult> Grade(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        var submissionFilePath = submission.FilePath;
        var sourceSubmissionFolderPath = string.IsNullOrWhiteSpace(submissionFilePath)
            ? null
            : Path.GetDirectoryName(submissionFilePath);
        var submissionFolderName = string.IsNullOrWhiteSpace(sourceSubmissionFolderPath)
            ? null
            : Path.GetFileName(sourceSubmissionFolderPath);

        var exam = await _db.Exams.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == submission.ExamId, cancellationToken);
        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        var testCaseFilePath = exam.CollectionFilePath;
        var testCases = await _db.TestCases
            .AsNoTracking()
            .Where(tc => tc.ExamId == submission.ExamId)
            .ToListAsync(cancellationToken);

        if (string.IsNullOrEmpty(submissionFilePath))
        {
            return NotFound(new { message = "Submission file not found." });
        }

        if (string.IsNullOrWhiteSpace(sourceSubmissionFolderPath) || !Directory.Exists(sourceSubmissionFolderPath))
        {
            return NotFound(new { message = "Submission folder not found." });
        }

        if (string.IsNullOrWhiteSpace(submissionFolderName))
        {
            return BadRequest(new { message = "Invalid submission folder name." });
        }

        if (string.IsNullOrEmpty(testCaseFilePath))
        {
            return NotFound(new { message = "Collection file not found for this exam." });
        }

        if (!System.IO.File.Exists(testCaseFilePath))
        {
            return NotFound(new { message = "Collection file path is invalid." });
        }

        if (testCases.Count == 0)
        {
            return BadRequest(new { message = "No test cases configured for this exam." });
        }

        submission.Status = SubmissionStatusQueued;
        submission.LastError = null;
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            submission.Status = SubmissionStatusRunning;
            submission.LastError = null;
            submission.TotalScore = 0;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Start grading submission {SubmissionId} for exam {ExamId}", submission.Id, submission.ExamId);

            var runnerRoot = Path.GetFullPath(Path.Combine(
                _env.ContentRootPath,
                "..",
                "..",
                "Runner"));
            var runnerSubmissionsRoot = Path.GetFullPath(Path.Combine(
                runnerRoot,
                "submissions"));
            var runnerCollectionsRoot = Path.GetFullPath(Path.Combine(
                runnerRoot,
                "collections"));

            Directory.CreateDirectory(runnerSubmissionsRoot);
            Directory.CreateDirectory(runnerCollectionsRoot);

            var destinationFolderPath = Path.Combine(runnerSubmissionsRoot, submissionFolderName);

            // Ensure destination is clean before copying latest submission package and metadata.
            if (Directory.Exists(destinationFolderPath))
            {
                Directory.Delete(destinationFolderPath, recursive: true);
            }

            CopyDirectory(sourceSubmissionFolderPath, destinationFolderPath);
            _logger.LogInformation("Copied submission folder {Source} -> {Destination}", sourceSubmissionFolderPath, destinationFolderPath);

            foreach (var rootZipFile in Directory.GetFiles(runnerSubmissionsRoot, "*.zip", SearchOption.TopDirectoryOnly))
            {
                System.IO.File.Delete(rootZipFile);
            }

            var submissionZipPath = Directory.GetFiles(sourceSubmissionFolderPath, "*.zip", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(submissionZipPath))
            {
                return BadRequest(new { message = "Submission folder does not contain a .zip file." });
            }

            var stagedSubmissionZipPath = Path.Combine(runnerSubmissionsRoot, Path.GetFileName(submissionZipPath));
            System.IO.File.Copy(submissionZipPath, stagedSubmissionZipPath, overwrite: true);
            _logger.LogInformation("Staged submission zip {ZipPath}", stagedSubmissionZipPath);

            foreach (var existingCollection in Directory.GetFiles(runnerCollectionsRoot, "*.postman_collection.json", SearchOption.TopDirectoryOnly))
            {
                System.IO.File.Delete(existingCollection);
            }

            var stagedCollectionPath = Path.Combine(runnerCollectionsRoot, "exam.postman_collection.json");
            System.IO.File.Copy(testCaseFilePath, stagedCollectionPath, overwrite: true);
            _logger.LogInformation("Copied collection file {CollectionSource} -> {CollectionDestination}", testCaseFilePath, stagedCollectionPath);

            var runnerConsole = await RunRunnerAndCaptureOutputAsync(runnerRoot, cancellationToken);
            var runnerOutput = ParseRunnerConsoleOutput(runnerConsole);
            await PersistRunnerRawLogAsync(submission.Id, runnerOutput, runnerConsole, cancellationToken);

            var rawResultByName = runnerOutput.Results
                .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.All(x => x.Passed),
                    StringComparer.OrdinalIgnoreCase);

            var testCasesById = testCases.ToDictionary(tc => tc.Id);
            var orderedTestCases = TopologicalSortByDependency(testCases);

            var computedResults = new Dictionary<int, (bool IsPassed, bool IsSkipped, decimal EarnedScore, string? Log)>();

            var existingResults = await _db.TestResults
                .Where(tr => tr.SubmissionId == submission.Id)
                .ToListAsync(cancellationToken);
            if (existingResults.Count > 0)
            {
                _db.TestResults.RemoveRange(existingResults);
            }

            decimal totalScore = 0;
            foreach (var testCase in orderedTestCases)
            {
                var skippedByDependency = false;
                if (testCase.DependencyTestCaseId.HasValue &&
                    computedResults.TryGetValue(testCase.DependencyTestCaseId.Value, out var parentResult) &&
                    (!parentResult.IsPassed || parentResult.IsSkipped))
                {
                    skippedByDependency = true;
                }

                var passed = false;
                var log = string.Empty;
                if (skippedByDependency)
                {
                    log = $"Skipped due to dependency test case {testCase.DependencyTestCaseId.GetValueOrDefault()} not passed.";
                }
                else if (rawResultByName.TryGetValue(testCase.Name, out var rawPassed))
                {
                    passed = rawPassed;
                    if (!passed)
                    {
                        log = "Failed by runner assertion result.";
                    }
                }
                else
                {
                    log = "No matching result returned by runner for this testcase.";
                }

                var earnedScore = passed ? testCase.Score : 0;
                totalScore += earnedScore;

                computedResults[testCase.Id] = (passed, skippedByDependency, earnedScore, log);

                _db.TestResults.Add(new TestResult
                {
                    SubmissionId = submission.Id,
                    TestCaseId = testCase.Id,
                    IsPassed = passed,
                    IsSkipped = skippedByDependency,
                    EarnedScore = earnedScore,
                    Log = log
                });
            }

            submission.TotalScore = decimal.Round(totalScore, 2);
            submission.Status = string.IsNullOrWhiteSpace(runnerOutput.Error)
                ? SubmissionStatusCompleted
                : SubmissionStatusError;
            submission.LastError = runnerOutput.ErrorMessage;

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Finish grading submission {SubmissionId}. Score={TotalScore}, MatchedCases={MatchedCases}, RunnerError={RunnerError}",
                submission.Id,
                submission.TotalScore,
                computedResults.Count,
                runnerOutput.Error);

            return Ok(new
            {
                submissionId = submission.Id,
                status = GetSubmissionStatusName(submission.Status),
                statusCode = submission.Status,
                totalScore = submission.TotalScore,
                matchedCases = computedResults.Count,
                totalRunnerResults = runnerOutput.Results.Count,
                runnerError = runnerOutput.Error,
                runnerErrorMessage = runnerOutput.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            submission.Status = SubmissionStatusError;
            submission.LastError = ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Error grading submission {SubmissionId}", id);
            return Problem(
                detail: "Failed to run grading pipeline.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Grading Failed");
        }
    }

    [HttpGet("submissions/{id:int}/report")]
    public async Task<ActionResult<SubmissionReportDto>> GetReport(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        var results = await _db.TestResults
            .AsNoTracking()
            .Where(r => r.SubmissionId == id)
            .Join(
                _db.TestCases.AsNoTracking(),
                tr => tr.TestCaseId,
                tc => tc.Id,
                (tr, tc) => new SubmissionTestResultDto
                {
                    TestCaseId = tc.Id,
                    TestCaseName = tc.Name,
                    Outcome = tr.IsSkipped
                        ? "Skipped"
                        : (tr.IsPassed ? "Passed" : "Failed"),
                    MaxScore = tc.Score,
                    EarnedScore = tr.EarnedScore,
                    Log = tr.Log
                })
            .OrderBy(x => x.TestCaseId)
            .ToListAsync(cancellationToken);

        return Ok(new SubmissionReportDto
        {
            SubmissionId = submission.Id,
            ExamId = submission.ExamId,
            TotalScore = submission.TotalScore,
            LastError = submission.LastError,
            Status = GetSubmissionStatusName(submission.Status),
            Results = results
        });
    }

    [HttpGet("submissions/{id:int}/status")]
    public async Task<ActionResult<SubmissionStatusDto>> GetStatus(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        return Ok(new SubmissionStatusDto
        {
            SubmissionId = submission.Id,
            StatusCode = submission.Status,
            Status = GetSubmissionStatusName(submission.Status),
            LastError = submission.LastError,
            SubmittedAt = submission.SubmittedAt
        });
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var destinationFilePath = Path.Combine(destinationDir, Path.GetFileName(filePath));
            System.IO.File.Copy(filePath, destinationFilePath, overwrite: true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destinationSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destinationSubDir);
        }
    }

    private async Task<string> RunRunnerAndCaptureOutputAsync(string runnerRoot, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project \"Runner.csproj\"",
            WorkingDirectory = runnerRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stdoutBuilder.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                stderrBuilder.AppendLine(args.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Unable to start Runner process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Runner exited with code {process.ExitCode}. {stderrBuilder}");
        }

        return stdoutBuilder.ToString();
    }

    private async Task PersistRunnerRawLogAsync(
        int submissionId,
        RunnerConsoleOutput output,
        string rawConsole,
        CancellationToken cancellationToken)
    {
        var logsRoot = Path.Combine(_env.ContentRootPath, "data", "grading-logs", submissionId.ToString(CultureInfo.InvariantCulture));
        Directory.CreateDirectory(logsRoot);

        var runnerOutputPath = Path.Combine(logsRoot, "runner-output.json");
        var runnerConsolePath = Path.Combine(logsRoot, "runner-console.log");
        var buildLogPath = Path.Combine(logsRoot, "build.log");
        var runLogPath = Path.Combine(logsRoot, "run.log");

        await System.IO.File.WriteAllTextAsync(
            runnerOutputPath,
            JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
        await System.IO.File.WriteAllTextAsync(runnerConsolePath, rawConsole, cancellationToken);
        await System.IO.File.WriteAllTextAsync(buildLogPath, output.BuildLog ?? string.Empty, cancellationToken);
        await System.IO.File.WriteAllTextAsync(runLogPath, output.RunLog ?? string.Empty, cancellationToken);
    }

    private static RunnerConsoleOutput ParseRunnerConsoleOutput(string runnerConsole)
    {
        if (string.IsNullOrWhiteSpace(runnerConsole))
        {
            throw new InvalidOperationException("Runner output is empty.");
        }

        var startIndex = runnerConsole.IndexOf('{');
        var endIndex = runnerConsole.LastIndexOf('}');
        if (startIndex < 0 || endIndex <= startIndex)
        {
            throw new InvalidOperationException("Runner output does not contain valid JSON.");
        }

        var json = runnerConsole[startIndex..(endIndex + 1)];
        var output = JsonSerializer.Deserialize<RunnerConsoleOutput>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return output ?? throw new InvalidOperationException("Unable to parse runner output JSON.");
    }

    private sealed class RunnerConsoleOutput
    {
        public string? Error { get; set; }

        public string? ErrorMessage { get; set; }

        public string? BuildLog { get; set; }

        public string? RunLog { get; set; }

        public List<RunnerTestOutcome> Results { get; set; } = [];
    }

    private sealed class RunnerTestOutcome
    {
        public string Name { get; set; } = string.Empty;

        public bool Passed { get; set; }
    }

    private static List<TestCase> TopologicalSortByDependency(List<TestCase> testCases)
    {
        var byId = testCases.ToDictionary(tc => tc.Id);
        var result = new List<TestCase>();
        var state = new Dictionary<int, int>();

        foreach (var testCase in testCases)
        {
            Visit(testCase, byId, state, result);
        }

        return result;
    }

    private static void Visit(
        TestCase testCase,
        Dictionary<int, TestCase> byId,
        Dictionary<int, int> state,
        List<TestCase> result)
    {
        if (state.TryGetValue(testCase.Id, out var mark))
        {
            if (mark == 2)
            {
                return;
            }

            if (mark == 1)
            {
                throw new InvalidOperationException("Circular testcase dependency detected.");
            }
        }

        state[testCase.Id] = 1;

        if (testCase.DependencyTestCaseId.HasValue && byId.TryGetValue(testCase.DependencyTestCaseId.Value, out var parent))
        {
            Visit(parent, byId, state, result);
        }

        state[testCase.Id] = 2;
        result.Add(testCase);
    }

    private static string GetSubmissionStatusName(int status)
    {
        return status switch
        {
            SubmissionStatusPending => "Uploaded",
            SubmissionStatusQueued => "Queued",
            SubmissionStatusRunning => "Running",
            SubmissionStatusCompleted => "Completed",
            SubmissionStatusError => "Error",
            _ => "Unknown"
        };
    }
}
