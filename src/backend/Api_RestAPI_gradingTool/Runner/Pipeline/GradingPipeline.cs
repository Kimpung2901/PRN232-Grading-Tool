using System.Diagnostics;
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

        var buildLogPath = Path.Combine(reportsRoot, "build.log");
        var runLogPath = Path.Combine(reportsRoot, "run.log");
        var newmanLogPath = Path.Combine(reportsRoot, "newman.log");
        var reportJsonPath = Path.Combine(reportsRoot, "report.json");
        var resultJsonPath = Path.Combine(reportsRoot, "result.json");

        await File.WriteAllTextAsync(buildLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(runLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(newmanLogPath, string.Empty, cancellationToken);

        Process? apiProcess = null;
        string? extractedWorkspace = null;
        var output = CreateInitialOutput(buildLogPath, runLogPath, newmanLogPath, reportJsonPath, resultJsonPath);

        try
        {
            var newmanCommand = await _environmentSetupService.EnsureNewmanInstalledAsync(runnerRoot, newmanLogPath, cancellationToken);

            var submissionZipPath = ResolveSingleFile(submissionsRoot, "*.zip", "submission zip");
            var collectionPath = ResolveSingleFile(collectionsRoot, "*.postman_collection.json", "Postman collection");

            extractedWorkspace = await _unzipService.ExtractAsync(submissionZipPath, workspaceRoot, cancellationToken);
            var projectPath = FindProjectFile(extractedWorkspace);

            output.BuildLog = await _buildService.BuildAsync(projectPath, buildLogPath, cancellationToken);
            output.BuildSucceeded = true;

            apiProcess = await _runApiService.StartAsync(projectPath, DefaultApiPort, runLogPath, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            if (apiProcess.HasExited)
            {
                throw new PipelineException("API_START_FAILED", "API process exited before health check completed.");
            }

            await _healthCheckService.EnsureApiReadyAsync(DefaultApiPort, cancellationToken);
            output.ApiStarted = true;

            var newmanResult = await _newmanService.RunAsync(newmanCommand, collectionPath, reportJsonPath, newmanLogPath, cancellationToken);
            output.NewmanLog = newmanResult.LogContent;
            output.TestRunCompleted = true;

            output.Results = await _reportParser.ParseAsync(reportJsonPath, cancellationToken);
            output.Summary = ResultSummaryFactory.Create(output.Results);
            output.HasFailedAssertions = output.Summary.AssertionsFailed > 0;
            output.RunLog = await SafeReadFileAsync(runLogPath, cancellationToken);
            output.Status = output.HasFailedAssertions ? "completed_with_failures" : "completed";

            if (newmanResult.ExitCode != 0)
            {
                output.Error = "TEST_RUN_FAILED";
                output.ErrorMessage = "Newman completed with failed assertions.";
            }

            await WriteResultFileAsync(output, resultJsonPath, cancellationToken);
            await WriteConsoleOutputAsync(output, cancellationToken);
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
            await WriteResultFileAsync(output, resultJsonPath, cancellationToken);
            await WriteConsoleOutputAsync(output, cancellationToken);
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
            await WriteResultFileAsync(output, resultJsonPath, cancellationToken);
            await WriteConsoleOutputAsync(output, cancellationToken);
        }
        finally
        {
            await StopApiProcessAsync(apiProcess);

            if (!string.IsNullOrWhiteSpace(extractedWorkspace))
            {
                await _cleanupService.DeleteWorkspaceAsync(extractedWorkspace, cancellationToken);
            }
        }
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

    private static string FindProjectFile(string extractedWorkspace)
    {
        var projectFiles = Directory.GetFiles(extractedWorkspace, "*.csproj", SearchOption.AllDirectories);
        if (projectFiles.Length == 0)
        {
            throw new PipelineException("BUILD_FAILED", "No .csproj file found in submission.");
        }

        var candidates = projectFiles
            .Select(CreateProjectCandidate)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Path.Length)
            .ToList();

        var bestCandidate = candidates.FirstOrDefault();
        if (bestCandidate is null || bestCandidate.Score <= 0)
        {
            throw new PipelineException("API_START_FAILED", "Could not determine a runnable API project from the submission.");
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

    private static async Task WriteConsoleOutputAsync(PipelineConsoleOutput output, CancellationToken cancellationToken)
    {
        var json = SerializeOutput(output);
        await Console.Out.WriteLineAsync(json.AsMemory(), cancellationToken);
    }

    private static async Task WriteResultFileAsync(PipelineConsoleOutput output, string resultJsonPath, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(resultJsonPath, SerializeOutput(output), cancellationToken);
    }

    private static string SerializeOutput(PipelineConsoleOutput output)
    {
        return JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private static PipelineConsoleOutput CreateInitialOutput(
        string buildLogPath,
        string runLogPath,
        string newmanLogPath,
        string reportPath,
        string resultPath)
    {
        return new PipelineConsoleOutput
        {
            BuildLogPath = buildLogPath,
            RunLogPath = runLogPath,
            NewmanLogPath = newmanLogPath,
            ReportPath = reportPath,
            ResultPath = resultPath
        };
    }

    private sealed record ProjectCandidate(string Path, int Score);
}
