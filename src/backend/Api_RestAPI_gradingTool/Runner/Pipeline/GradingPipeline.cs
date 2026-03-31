using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using Runner.Models;

namespace Runner.Pipeline;

public sealed class GradingPipeline
{
    private const int DefaultApiPort = 5000;

    private readonly UnzipService _unzipService = new();
    private readonly BuildService _buildService = new();
    private readonly RunApiService _runApiService = new();
    private readonly HealthCheckService _healthCheckService = new();
    private readonly NewmanService _newmanService = new();
    private readonly ReportParser _reportParser = new();
    private readonly CleanupService _cleanupService = new();
    private readonly EnvironmentSetupService _environmentSetupService = new();
    private readonly DatabaseSetupService _databaseSetupService = new();

    public async Task RunPipeline(CancellationToken cancellationToken = default)
    {
        var runnerRoot = RunnerPathResolver.ResolveRunnerRoot();
        var workspaceRoot = Path.Combine(runnerRoot, "workspace");
        var submissionsRoot = Path.Combine(runnerRoot, "submissions");
        var collectionsRoot = Path.Combine(runnerRoot, "collections");
        var reportsRoot = Path.Combine(runnerRoot, "reports");

        Directory.CreateDirectory(workspaceRoot);
        Directory.CreateDirectory(submissionsRoot);
        Directory.CreateDirectory(collectionsRoot);
        Directory.CreateDirectory(reportsRoot);

        await LogBatchAsync($"Runner root: {runnerRoot}", cancellationToken);
        await LogBatchAsync("Preparing grading batch.", cancellationToken);

        var batchResultPath = Path.Combine(reportsRoot, "result.json");
        var batchOutput = new BatchPipelineOutput();

        try
        {
            var probeLogPath = Path.Combine(reportsRoot, "newman.setup.log");
            await LogBatchAsync("Resolving Newman CLI.", cancellationToken);
            var newmanCommand = await _environmentSetupService.EnsureNewmanInstalledAsync(runnerRoot, probeLogPath, cancellationToken);
            await LogBatchAsync($"Newman CLI ready: {newmanCommand}", cancellationToken);

            var databaseRoot = Path.Combine(runnerRoot, "database");
            var seedScript = DatabaseSetupService.FindSeedScript(databaseRoot);
            var runnerConnStr = DatabaseSetupService.ReadRunnerConnectionString(databaseRoot);
            if (seedScript is not null && runnerConnStr is not null)
            {
                var dbLogPath = Path.Combine(reportsRoot, "db.log");
                await LogBatchAsync($"Seeding database from {Path.GetFileName(seedScript)}.", cancellationToken);
                await _databaseSetupService.SeedAsync(runnerConnStr, seedScript, dbLogPath, cancellationToken);
                await LogBatchAsync("Database seed completed.", cancellationToken);
            }
            else
            {
                await LogBatchAsync("Skipping database seed because config.json or .sql seed script was not found.", cancellationToken);
            }

            var collectionPath = ResolveSingleFile(collectionsRoot, "*.postman_collection.json", "Postman collection");
            var submissionZipPaths = Directory.GetFiles(submissionsRoot, "*.zip", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName)
                .ToList();

            await LogBatchAsync($"Using collection: {Path.GetFileName(collectionPath)}", cancellationToken);
            await LogBatchAsync($"Found {submissionZipPaths.Count} submission(s) to grade.", cancellationToken);

            if (submissionZipPaths.Count == 0)
            {
                throw new PipelineException("TEST_RUN_FAILED", $"Could not find submission zip in {submissionsRoot}.");
            }

            await LogBatchAsync("Starting grading tasks sequentially.", cancellationToken);

            for (var index = 0; index < submissionZipPaths.Count; index++)
            {
                var zipPath = submissionZipPaths[index];
                var submissionName = Path.GetFileNameWithoutExtension(zipPath);
                await LogBatchAsync($"[{index + 1}/{submissionZipPaths.Count}] Grading {submissionName}.", cancellationToken);

                var submissionResult = await GradeSingleSubmissionAsync(
                    runnerRoot,
                    workspaceRoot,
                    reportsRoot,
                    zipPath,
                    collectionPath,
                    newmanCommand,
                    DefaultApiPort,
                    cancellationToken);

                batchOutput.Submissions.Add(submissionResult);
                RecalculateBatchTotals(batchOutput);
                batchOutput.Status = batchOutput.FailedSubmissions > 0 ? "completed_with_failures" : "running";

                await LogBatchAsync($"Deleting graded submission artifact: {Path.GetFileName(zipPath)}", cancellationToken);
                await _cleanupService.DeleteSubmissionArtifactAsync(zipPath, cancellationToken);
                await LogBatchAsync($"Deleted graded submission artifact: {Path.GetFileName(zipPath)}", cancellationToken);

                await WriteBatchResultFileAsync(batchOutput, batchResultPath, cancellationToken);
                await LogBatchAsync($"[{index + 1}/{submissionZipPaths.Count}] Finished {submissionName} with status={submissionResult.Status}.", cancellationToken);
            }

            RecalculateBatchTotals(batchOutput);
            batchOutput.Status = batchOutput.FailedSubmissions > 0 ? "completed_with_failures" : "completed";
            await LogBatchAsync($"Batch finished with status={batchOutput.Status}.", cancellationToken);
        }
        catch (PipelineException ex)
        {
            await LogBatchAsync($"Batch failed with pipeline error {ex.ErrorCode}: {ex.Message}", cancellationToken);
            batchOutput.Status = "failed";
            batchOutput.Submissions.Add(new PipelineConsoleOutput
            {
                Status = "failed",
                Error = ex.ErrorCode,
                ErrorMessage = ex.Message
            });
            batchOutput.TotalSubmissions = batchOutput.Submissions.Count;
            batchOutput.FailedSubmissions = batchOutput.Submissions.Count;
        }
        catch (Exception ex)
        {
            await LogBatchAsync($"Batch failed with unexpected error: {ex.Message}", cancellationToken);
            batchOutput.Status = "failed";
            batchOutput.Submissions.Add(new PipelineConsoleOutput
            {
                Status = "failed",
                Error = "TEST_RUN_FAILED",
                ErrorMessage = ex.Message
            });
            batchOutput.TotalSubmissions = batchOutput.Submissions.Count;
            batchOutput.FailedSubmissions = batchOutput.Submissions.Count;
        }

        await LogBatchAsync($"Writing batch result file: {batchResultPath}", cancellationToken);
        await WriteBatchResultFileAsync(batchOutput, batchResultPath, cancellationToken);
        await WriteBatchSummaryAsync(batchOutput, cancellationToken);
    }

    private static string ResolveSingleFile(string directoryPath, string searchPattern, string displayName)
    {
        var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            throw new PipelineException("TEST_RUN_FAILED", $"Could not find {displayName} in {directoryPath}.");
        }

        return files[0];
    }


    private static string ResolveStartupProject(IReadOnlyList<(string Path, string Guid)> projects, string startupGuid)
    {
        var match = projects.FirstOrDefault(p => p.Guid.Equals(startupGuid, StringComparison.OrdinalIgnoreCase));
        if (match.Path is null)
        {
            throw new PipelineException("API_START_FAILED", $"Startup project GUID {startupGuid} not found in solution.");
        }

        return match.Path;
    }

    private static string FindBestProject(IReadOnlyList<string> projectPaths)
    {
        if (projectPaths.Count == 0)
        {
            throw new PipelineException("BUILD_FAILED", "No .csproj files found in the solution.");
        }

        var candidates = projectPaths
            .Select(CreateProjectCandidate)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Path.Length)
            .ToList();

        var bestCandidate = candidates.FirstOrDefault();
        if (bestCandidate is null || bestCandidate.Score <= 0)
        {
            throw new PipelineException("API_START_FAILED", "Could not determine a runnable API project from the solution.");
        }

        return bestCandidate.Path;
    }

    private static ProjectCandidate CreateProjectCandidate(string projectPath)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            var projectElement = document.Root;

            var sdk = projectElement?.Attribute("Sdk")?.Value ?? string.Empty;
            var propertyGroups = projectElement?.Elements("PropertyGroup").ToList() ?? [];
            var outputType = propertyGroups
                .Elements("OutputType")
                .Select(x => x.Value.Trim())
                .FirstOrDefault() ?? string.Empty;
            var assemblyName = propertyGroups
                .Elements("AssemblyName")
                .Select(x => x.Value.Trim())
                .FirstOrDefault() ?? Path.GetFileNameWithoutExtension(projectPath);

            var score = 0;

            if (sdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
            {
                score += 1000;
            }

            if (outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase) ||
                outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase))
            {
                score += 500;
            }

            if (File.Exists(Path.Combine(Path.GetDirectoryName(projectPath)!, "Program.cs")))
            {
                score += 200;
            }

            var nameSignals = new[] { "api", "web", "service", "host", "presentation" };
            if (nameSignals.Any(signal => assemblyName.Contains(signal, StringComparison.OrdinalIgnoreCase)) ||
                nameSignals.Any(signal => projectPath.Contains(signal, StringComparison.OrdinalIgnoreCase)))
            {
                score += 100;
            }

            return new ProjectCandidate(projectPath, score);
        }
        catch
        {
            return new ProjectCandidate(projectPath, 0);
        }
    }

    private static async Task<string> SafeReadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private async Task<List<TestOutcome>> TryParseResultsAsync(string reportPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(reportPath))
        {
            return [];
        }

        try
        {
            return await _reportParser.ParseAsync(reportPath, cancellationToken);
        }
        catch
        {
            return [];
        }
    }

    private static async Task StopApiProcessAsync(Process? apiProcess)
    {
        if (apiProcess is null)
        {
            return;
        }

        try
        {
            if (!apiProcess.HasExited)
            {
                apiProcess.Kill(entireProcessTree: true);
                await apiProcess.WaitForExitAsync();
            }
        }
        catch
        {
        }
        finally
        {
            apiProcess.Dispose();
        }
    }

    private static async Task WriteBatchSummaryAsync(BatchPipelineOutput batch, CancellationToken cancellationToken)
    {
        var sep = new string('─', 60);
        var lines = new System.Text.StringBuilder();
        lines.AppendLine();
        lines.AppendLine(sep);
        lines.AppendLine($"  Grading complete  |  Total: {batch.TotalSubmissions}  Completed: {batch.CompletedSubmissions}  Failed: {batch.FailedSubmissions}");
        lines.AppendLine(sep);
        lines.AppendLine($"  {"Student",-40} {"Score",7}  {"Build",-6}  {"Time",7}  Status");
        lines.AppendLine($"  {new string('·', 40)} {"·······",7}  {"······",-6}  {"·······",7}  ──────");

        foreach (var s in batch.Submissions.OrderBy(x => x.SubmissionName))
        {
            var score = $"{s.Summary.ScorePercent:F2}%";
            var build = s.BuildSucceeded ? "ok" : "FAIL";
            var elapsed = $"{s.ElapsedSeconds:F1}s";
            lines.AppendLine($"  {s.SubmissionName,-40} {score,7}  {build,-6}  {elapsed,7}  {s.Status}");
        }

        lines.AppendLine(sep);
        await Console.Out.WriteLineAsync(lines.ToString().AsMemory(), cancellationToken);
    }

    private static void RecalculateBatchTotals(BatchPipelineOutput batchOutput)
    {
        batchOutput.TotalSubmissions = batchOutput.Submissions.Count;
        batchOutput.CompletedSubmissions = batchOutput.Submissions.Count(x => x.BuildSucceeded && x.ApiStarted && x.TestRunCompleted);
        batchOutput.FailedSubmissions = batchOutput.Submissions.Count(x => x.Status == "failed");
    }

    private static Task LogBatchAsync(string message, CancellationToken cancellationToken = default)
    {
        return Console.Out.WriteLineAsync($"[Batch] {message}".AsMemory(), cancellationToken);
    }

    private static Task LogSubmissionAsync(string submissionName, string step, string message, CancellationToken cancellationToken = default)
    {
        return Console.Out.WriteLineAsync($"[Submission:{submissionName}] [{step}] {message}".AsMemory(), cancellationToken);
    }

    private static async Task WriteResultFileAsync(PipelineConsoleOutput output, string resultJsonPath, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(resultJsonPath, SerializeOutput(output), cancellationToken);
    }

    private static async Task WriteBatchResultFileAsync(BatchPipelineOutput output, string resultJsonPath, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(resultJsonPath, SerializeBatchOutput(output), cancellationToken);
    }

    private static string SerializeOutput(PipelineConsoleOutput output)
    {
        return JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static string SerializeBatchOutput(BatchPipelineOutput output)
    {
        return JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static PipelineConsoleOutput CreateInitialOutput(
        string submissionName,
        string submissionFilePath,
        string buildLogPath,
        string runLogPath,
        string newmanLogPath,
        string reportPath,
        string resultPath)
    {
        return new PipelineConsoleOutput
        {
            SubmissionName = submissionName,
            SubmissionFilePath = submissionFilePath,
            BuildLogPath = buildLogPath,
            RunLogPath = runLogPath,
            NewmanLogPath = newmanLogPath,
            ReportPath = reportPath,
            ResultPath = resultPath
        };
    }

    private async Task<PipelineConsoleOutput> GradeSingleSubmissionAsync(
        string runnerRoot,
        string workspaceRoot,
        string reportsRoot,
        string submissionZipPath,
        string collectionPath,
        string newmanCommand,
        int apiPort,
        CancellationToken cancellationToken)
    {
        var submissionName = Path.GetFileNameWithoutExtension(submissionZipPath);
        var reportDirectory = Path.Combine(reportsRoot, SanitizeFileName(submissionName));
        Directory.CreateDirectory(reportDirectory);

        var buildLogPath = Path.Combine(reportDirectory, "build.log");
        var runLogPath = Path.Combine(reportDirectory, "run.log");
        var newmanLogPath = Path.Combine(reportDirectory, "newman.log");
        var reportJsonPath = Path.Combine(reportDirectory, "report.json");
        var resultJsonPath = Path.Combine(reportDirectory, "result.json");

        await File.WriteAllTextAsync(buildLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(runLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(newmanLogPath, string.Empty, cancellationToken);
        if (File.Exists(reportJsonPath)) File.Delete(reportJsonPath);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Process? apiProcess = null;
        string? extractedWorkspace = null;
        var output = CreateInitialOutput(
            submissionName,
            submissionZipPath,
            buildLogPath,
            runLogPath,
            newmanLogPath,
            reportJsonPath,
            resultJsonPath);

        try
        {
            await LogSubmissionAsync(submissionName, "START", "Preparing report files and workspace.", cancellationToken);

            await LogSubmissionAsync(submissionName, "UNZIP", $"Extracting archive {Path.GetFileName(submissionZipPath)}.", cancellationToken);
            extractedWorkspace = await _unzipService.ExtractAsync(submissionZipPath, workspaceRoot, cancellationToken);
            await LogSubmissionAsync(submissionName, "UNZIP", $"Extracted to {extractedWorkspace}.", cancellationToken);

            await LogSubmissionAsync(submissionName, "DISCOVER", "Searching for solution and startup project.", cancellationToken);
            var solutionPath = SolutionParser.FindSolutionFile(extractedWorkspace);
            var solutionProjects = SolutionParser.ExtractProjects(solutionPath);

            var startupGuid = SolutionParser.TryReadStartupProjectGuid(solutionPath);
            var projectPath = startupGuid is not null
                ? ResolveStartupProject(solutionProjects, startupGuid)
                : FindBestProject(solutionProjects.Select(p => p.Path).ToList());

            await LogSubmissionAsync(
                submissionName,
                "DISCOVER",
                $"Selected project {Path.GetFileName(projectPath)} from solution {Path.GetFileName(solutionPath)}.",
                cancellationToken);

            await LogSubmissionAsync(submissionName, "BUILD", "Starting dotnet build.", cancellationToken);
            output.BuildLog = await _buildService.BuildAsync(solutionPath, buildLogPath, cancellationToken);
            output.BuildSucceeded = true;
            await LogSubmissionAsync(submissionName, "BUILD", "Build completed successfully.", cancellationToken);

            await LogSubmissionAsync(submissionName, "API", $"Starting API on port {apiPort}.", cancellationToken);
            apiProcess = await _runApiService.StartAsync(projectPath, apiPort, runLogPath, cancellationToken);
            await LogSubmissionAsync(submissionName, "API", $"API process started with PID {apiProcess.Id}.", cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            if (apiProcess.HasExited)
            {
                throw new PipelineException("API_START_FAILED", "API process exited before health check completed.");
            }

            await LogSubmissionAsync(submissionName, "HEALTH", "Waiting for API health check.", cancellationToken);
            await _healthCheckService.EnsureApiReadyAsync(apiPort, cancellationToken);
            output.ApiStarted = true;
            await LogSubmissionAsync(submissionName, "HEALTH", "API is reachable.", cancellationToken);

            await LogSubmissionAsync(submissionName, "NEWMAN", "Running Postman/Newman test collection.", cancellationToken);
            var newmanResult = await _newmanService.RunAsync(newmanCommand, collectionPath, reportJsonPath, newmanLogPath, apiPort, cancellationToken);
            output.NewmanLog = newmanResult.LogContent;
            output.TestRunCompleted = true;
            await LogSubmissionAsync(submissionName, "NEWMAN", $"Newman finished with exit code {newmanResult.ExitCode}.", cancellationToken);

            await LogSubmissionAsync(submissionName, "REPORT", "Parsing Newman report.", cancellationToken);
            output.Results = await _reportParser.ParseAsync(reportJsonPath, cancellationToken);
            output.Summary = ResultSummaryFactory.Create(output.Results);
            output.HasFailedAssertions = output.Summary.AssertionsFailed > 0;
            output.RunLog = await SafeReadFileAsync(runLogPath, cancellationToken);
            output.Status = output.HasFailedAssertions ? "completed_with_failures" : "completed";
            await LogSubmissionAsync(
                submissionName,
                "REPORT",
                $"Summary: passed={output.Summary.AssertionsPassed}/{output.Summary.AssertionsTotal}, failed={output.Summary.AssertionsFailed}, score={output.Summary.ScorePercent:F2}%.",
                cancellationToken);

            if (newmanResult.ExitCode != 0)
            {
                output.Error = "TEST_RUN_FAILED";
                output.ErrorMessage = "Newman completed with failed assertions.";
                await LogSubmissionAsync(submissionName, "DONE", "Completed with failed assertions.", cancellationToken);
            }
            else
            {
                await LogSubmissionAsync(submissionName, "DONE", "Completed successfully.", cancellationToken);
            }
        }
        catch (PipelineException ex)
        {
            output.Status = "failed";
            output.Error = ex.ErrorCode;
            output.ErrorMessage = ex.Message;
            output.BuildLog = await SafeReadFileAsync(buildLogPath, cancellationToken);
            output.RunLog = await SafeReadFileAsync(runLogPath, cancellationToken);
            output.NewmanLog = await SafeReadFileAsync(newmanLogPath, cancellationToken);
            output.Results = await TryParseResultsAsync(reportJsonPath, cancellationToken);
            output.Summary = ResultSummaryFactory.Create(output.Results);
            output.HasFailedAssertions = output.Summary.AssertionsFailed > 0;
            await LogSubmissionAsync(submissionName, "ERROR", $"{ex.ErrorCode}: {ex.Message}", cancellationToken);
        }
        catch (Exception ex)
        {
            output.Status = "failed";
            output.Error = "TEST_RUN_FAILED";
            output.ErrorMessage = ex.Message;
            output.BuildLog = await SafeReadFileAsync(buildLogPath, cancellationToken);
            output.RunLog = await SafeReadFileAsync(runLogPath, cancellationToken);
            output.NewmanLog = await SafeReadFileAsync(newmanLogPath, cancellationToken);
            output.Results = await TryParseResultsAsync(reportJsonPath, cancellationToken);
            output.Summary = ResultSummaryFactory.Create(output.Results);
            output.HasFailedAssertions = output.Summary.AssertionsFailed > 0;
            await LogSubmissionAsync(submissionName, "ERROR", $"Unexpected failure: {ex.Message}", cancellationToken);
        }
        finally
        {
            await LogSubmissionAsync(submissionName, "CLEANUP", "Stopping API process.", CancellationToken.None);
            await StopApiProcessAsync(apiProcess);
            await Task.Delay(500, CancellationToken.None); // allow OS to release file locks after process kill

            if (!string.IsNullOrWhiteSpace(extractedWorkspace))
            {
                try
                {
                    await LogSubmissionAsync(submissionName, "CLEANUP", "Deleting extracted workspace.", CancellationToken.None);
                    await _cleanupService.DeleteWorkspaceAsync(extractedWorkspace, cancellationToken);
                    await LogSubmissionAsync(submissionName, "CLEANUP", "Workspace deleted.", CancellationToken.None);
                }
                catch
                {
                    await LogSubmissionAsync(submissionName, "CLEANUP", "Workspace cleanup failed and was ignored.", CancellationToken.None);
                    // cleanup failure must not affect grading results or other submissions
                }
            }
        }

        stopwatch.Stop();
        output.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;

        await LogSubmissionAsync(submissionName, "RESULT", $"Writing result file after {output.ElapsedSeconds:F1}s.", cancellationToken);
        await WriteResultFileAsync(output, resultJsonPath, cancellationToken);
        await LogSubmissionAsync(submissionName, "RESULT", "Posting test result to grading API.", cancellationToken);
        await PostTestResultAsync(output, resultJsonPath, cancellationToken);
        return output;
    }

    private const string GradingApiBaseUrl = "http://localhost:5069";

    private static readonly HttpClient _httpClient = new();

    private static async Task PostTestResultAsync(
        PipelineConsoleOutput output, string resultFilePath, CancellationToken cancellationToken)
    {
        var payload = new TestResultPayload
        {
            StudentName = output.SubmissionName,
            Score = (int)Math.Round(output.Summary.ScorePercent),
            Status = output.BuildSucceeded ? "build ok" : "build false",
            ReportFilePath = resultFilePath
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{GradingApiBaseUrl}/api/testresults",
                payload,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                cancellationToken);

            var statusLabel = response.IsSuccessStatusCode ? "OK" : $"HTTP {(int)response.StatusCode}";
            await Console.Out.WriteLineAsync(
                $"[TestResult] {output.SubmissionName} ({output.ElapsedSeconds:F1}s) → score={payload.Score}% status={payload.Status} api={statusLabel}".AsMemory(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync(
                $"[TestResult] {output.SubmissionName} ({output.ElapsedSeconds:F1}s) → score={payload.Score}% status={payload.Status} api=ERROR({ex.Message})".AsMemory(),
                cancellationToken);
        }
    }

    private static string SanitizeDbName(string name)
    {
        var sanitized = new string(name.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray());
        return sanitized.Length > 50 ? sanitized[..50] : sanitized;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "submission" : sanitized;
    }

    private sealed record ProjectCandidate(string Path, int Score);
}
