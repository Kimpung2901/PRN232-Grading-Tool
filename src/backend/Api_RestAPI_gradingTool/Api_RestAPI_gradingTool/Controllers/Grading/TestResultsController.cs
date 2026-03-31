using Api_RestAPI_gradingTool.Contracts.Grading;
using Api_RestAPI_gradingTool.Hubs;
using Application.Contracts.Grading;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;

namespace Api_RestAPI_gradingTool.Controllers.Grading;

[Route("api")]
public sealed class TestResultsController : ApiControllerBase
{
    private const int PendingStatus = 0;
    private const int GradedStatus = 1;
    private static readonly object RunnerStateLock = new();
    private static bool _isRunnerRunning;
    private static int? _currentProcessId;
    private static DateTimeOffset? _startedAtUtc;
    private static DateTimeOffset? _lastCompletedAtUtc;
    private static int? _lastExitCode;
    private static string? _lastError;

    private readonly IGradingDbContext _db;
    private readonly IHubContext<TestHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TestResultsController> _logger;

    public TestResultsController(
        IGradingDbContext db,
        IHubContext<TestHub> hubContext,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<TestResultsController> logger)
    {
        _db = db;
        _hubContext = hubContext;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("grading-runs")]
    [HttpGet("testrunner")]
    public async Task<ActionResult> StartRunner(CancellationToken cancellationToken = default)
    {
        try
        {
            await StartRunnerCoreAsync(null, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ProblemConflict(ex.Message);
        }

        return Ok(new
        {
            message = "Runner started."
        });
    }

    [HttpPost("exams/{examId:int}/grading-runs")]
    [HttpGet("exams/{examId:int}/testrunner")]
    public async Task<ActionResult> StartRunnerForExam(int examId, CancellationToken cancellationToken = default)
    {
        try
        {
            await StartRunnerCoreAsync(examId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return ProblemConflict(ex.Message);
        }

        return Ok(new
        {
            message = $"Runner started for exam {examId}."
        });
    }

    [HttpGet("grading-runs/current")]
    [HttpGet("testrunner/status")]
    public ActionResult GetRunnerStatus()
    {
        lock (RunnerStateLock)
        {
            return Ok(new
            {
                isRunning = _isRunnerRunning,
                processId = _currentProcessId,
                startedAtUtc = _startedAtUtc,
                lastCompletedAtUtc = _lastCompletedAtUtc,
                lastExitCode = _lastExitCode,
                lastError = _lastError
            });
        }
    }

    [HttpPost("grading-results")]
    [HttpPost("testresults")]
    public async Task<ActionResult<TestResultRequest>> Post(
        [FromBody] TestResultRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Received POST /api/testresults for SubmissionId={SubmissionId}, StudentName={StudentName}, Score={Score}, ReportFilePath={ReportFilePath}",
            request.SubmissionId,
            request.StudentName,
            request.Score,
            request.ReportFilePath);

        if (string.IsNullOrWhiteSpace(request.StudentName))
        {
            return ProblemBadRequest("studentName is required.");
        }

        var submission = await ResolveSubmissionAsync(request.SubmissionId, request.StudentName, cancellationToken);
        if (submission is null)
        {
            return ProblemNotFound($"Submission not found for submissionId '{request.SubmissionId}' or studentName '{request.StudentName}'.");
        }

        var testResult = await _db.TestResults.FirstOrDefaultAsync(
            x => x.SubmissionId == submission.Id,
            cancellationToken);

        if (testResult is null)
        {
            testResult = new Infrastructure.TestResult
            {
                SubmissionId = submission.Id
            };
            _db.TestResults.Add(testResult);
        }

        testResult.Score = request.Score;
        testResult.ReportPath = request.ReportFilePath;
        submission.Status = GradedStatus;

        await _db.SaveChangesAsync(cancellationToken);
        await _hubContext.Clients.All.SendAsync("ReceiveTestResult", request, cancellationToken);

        return Ok(request);
    }

    private string ResolveRunnerPath()
    {
        var configuredPath = _configuration["Runner:ExecutablePath"]
            ?? Environment.GetEnvironmentVariable("RUNNER_EXECUTABLE_PATH");

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("Runner path is missing. Configure Runner:ExecutablePath or RUNNER_EXECUTABLE_PATH.");
        }

        var fullPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(_environment.ContentRootPath, configuredPath));

        if (!System.IO.File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Runner executable was not found at '{fullPath}'.");
        }

        return fullPath;
    }

    private static ProcessStartInfo BuildStartInfo(string runnerExecutablePath)
    {
        var extension = Path.GetExtension(runnerExecutablePath);
        if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(runnerExecutablePath);
            return startInfo;
        }

        if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(runnerExecutablePath);
            return startInfo;
        }

        return new ProcessStartInfo
        {
            FileName = runnerExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    private async Task<Infrastructure.Submission?> ResolveSubmissionAsync(int? submissionId, string studentName, CancellationToken cancellationToken)
    {
        if (submissionId.HasValue)
        {
            var byId = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == submissionId.Value, cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        var normalized = studentName.Trim();

        var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var submissionIdText = parts.Length >= 3 ? parts[^3] : parts.FirstOrDefault();
        if (int.TryParse(submissionIdText, out var parsedSubmissionId))
        {
            var byId = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == parsedSubmissionId, cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        var byStudentCode = await _db.Submissions
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => s.StudentCode == normalized, cancellationToken);
        if (byStudentCode is not null)
        {
            return byStudentCode;
        }

        var byStudentName = await _db.Submissions
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => s.StudentName == normalized, cancellationToken);
        if (byStudentName is not null)
        {
            return byStudentName;
        }

        return await _db.Submissions
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => EF.Functions.Like(s.FilePath, $"%{normalized}%"), cancellationToken);
    }

    private async Task StartRunnerCoreAsync(int? examId, CancellationToken cancellationToken)
    {
        DateTimeOffset startedAtUtc;
        lock (RunnerStateLock)
        {
            if (_isRunnerRunning)
            {
                throw new InvalidOperationException("Runner is already running.");
            }

            _isRunnerRunning = true;
            _currentProcessId = null;
            startedAtUtc = DateTimeOffset.UtcNow;
            _startedAtUtc = startedAtUtc;
            _lastCompletedAtUtc = null;
            _lastExitCode = null;
            _lastError = null;
        }

        try
        {
            var runnerExecutablePath = ResolveRunnerPath();
            var manifestPath = await WritePendingManifestAsync(runnerExecutablePath, examId, cancellationToken);
            var processStartInfo = BuildStartInfo(runnerExecutablePath);
            processStartInfo.ArgumentList.Add("--manifest");
            processStartInfo.ArgumentList.Add(manifestPath);
            StartRunnerInBackground(processStartInfo, startedAtUtc);
        }
        catch
        {
            lock (RunnerStateLock)
            {
                _isRunnerRunning = false;
                _currentProcessId = null;
                _startedAtUtc = null;
                _lastCompletedAtUtc = DateTimeOffset.UtcNow;
                _lastExitCode = -1;
            }

            throw;
        }
    }

    private async Task<string> WritePendingManifestAsync(string runnerExecutablePath, int? examId, CancellationToken cancellationToken)
    {
        var runnerRoot = Path.GetDirectoryName(runnerExecutablePath)
            ?? throw new InvalidOperationException("Runner root could not be resolved.");
        var reportsRoot = Path.Combine(runnerRoot, "reports");
        Directory.CreateDirectory(reportsRoot);

        var pendingQuery = _db.Submissions
            .AsNoTracking()
            .Where(s => s.Status == PendingStatus && !string.IsNullOrWhiteSpace(s.FilePath));

        if (examId.HasValue)
        {
            pendingQuery = pendingQuery.Where(s => s.ExamId == examId.Value);
        }

        var pendingSubmissions = await pendingQuery
            .OrderBy(s => s.ExamId)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        if (pendingSubmissions.Count == 0)
        {
            throw new InvalidOperationException(examId.HasValue
                ? $"No pending submissions to grade for exam {examId.Value}."
                : "No pending submissions to grade.");
        }

        var examIds = pendingSubmissions.Select(s => s.ExamId).Distinct().ToArray();
        if (examIds.Length != 1)
        {
            throw new InvalidOperationException("Runner can only grade one exam per batch. Resolve pending submissions from other exams first.");
        }

        var targetExamId = examIds[0];
        var collection = await _db.TestCases
            .AsNoTracking()
            .Where(t => t.ExamId == targetExamId)
            .OrderByDescending(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (collection is null || string.IsNullOrWhiteSpace(collection.FilePath))
        {
            throw new InvalidOperationException($"Exam {targetExamId} does not have a Postman collection.");
        }

        var exam = await _db.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ExamId == targetExamId, cancellationToken)
            ?? throw new InvalidOperationException($"Exam {targetExamId} was not found.");

        var manifest = new
        {
            CollectionPath = Path.GetFullPath(Path.Combine(runnerRoot, collection.FilePath.Replace('/', Path.DirectorySeparatorChar))),
            SeedScriptPath = string.IsNullOrWhiteSpace(exam.SqlScriptPath)
                ? null
                : Path.GetFullPath(Path.Combine(runnerRoot, exam.SqlScriptPath.Replace('/', Path.DirectorySeparatorChar))),
            Submissions = pendingSubmissions.Select(s => new
            {
                SubmissionId = s.Id,
                SubmissionName = Path.GetFileNameWithoutExtension(s.FilePath),
                SubmissionFilePath = Path.GetFullPath(Path.Combine(runnerRoot, s.FilePath.Replace('/', Path.DirectorySeparatorChar)))
            }).ToArray()
        };

        var manifestPath = Path.Combine(reportsRoot, "pending-submissions.json");
        await System.IO.File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);

        return manifestPath;
    }

    private void StartRunnerInBackground(ProcessStartInfo startInfo, DateTimeOffset startedAtUtc)
    {
        _ = Task.Run(async () =>
        {
            int? processId = null;
            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();
                processId = process.Id;

                lock (RunnerStateLock)
                {
                    _isRunnerRunning = true;
                    _currentProcessId = processId;
                    _startedAtUtc = startedAtUtc;
                    _lastCompletedAtUtc = null;
                    _lastExitCode = null;
                    _lastError = null;
                }

                await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                {
                    isRunning = true,
                    processId,
                    startedAtUtc = _startedAtUtc
                });

                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var stdOut = await stdOutTask;
                var stdErr = await stdErrTask;

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Runner finished successfully.");

                    lock (RunnerStateLock)
                    {
                        _isRunnerRunning = false;
                        _currentProcessId = null;
                        _lastCompletedAtUtc = DateTimeOffset.UtcNow;
                        _lastExitCode = process.ExitCode;
                        _lastError = null;
                    }

                    await _hubContext.Clients.All.SendAsync("RunnerCompleted", new
                    {
                        status = "ok",
                        output = stdOut
                    });

                    await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                    {
                        isRunning = false,
                        processId = (int?)null,
                        lastCompletedAtUtc = _lastCompletedAtUtc,
                        lastExitCode = _lastExitCode,
                        lastError = _lastError
                    });
                }
                else
                {
                    _logger.LogWarning("Runner failed. ExitCode: {ExitCode}. Error: {Error}", process.ExitCode, stdErr);

                    lock (RunnerStateLock)
                    {
                        _isRunnerRunning = false;
                        _currentProcessId = null;
                        _lastCompletedAtUtc = DateTimeOffset.UtcNow;
                        _lastExitCode = process.ExitCode;
                        _lastError = stdErr;
                    }

                    await _hubContext.Clients.All.SendAsync("RunnerCompleted", new
                    {
                        status = "build false",
                        error = stdErr
                    });

                    await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                    {
                        isRunning = false,
                        processId = (int?)null,
                        lastCompletedAtUtc = _lastCompletedAtUtc,
                        lastExitCode = _lastExitCode,
                        lastError = _lastError
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute runner.");

                lock (RunnerStateLock)
                {
                    _isRunnerRunning = false;
                    _currentProcessId = null;
                    _lastCompletedAtUtc = DateTimeOffset.UtcNow;
                    _lastExitCode = -1;
                    _lastError = ex.Message;
                }

                await _hubContext.Clients.All.SendAsync("RunnerStatusChanged", new
                {
                    isRunning = false,
                    processId = (int?)null,
                    lastCompletedAtUtc = _lastCompletedAtUtc,
                    lastExitCode = _lastExitCode,
                    lastError = _lastError
                });
            }
        });
    }
}
