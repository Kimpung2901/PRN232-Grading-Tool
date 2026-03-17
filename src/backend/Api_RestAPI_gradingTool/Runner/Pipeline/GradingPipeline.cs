using System.Diagnostics;
using System.Text.Json;
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
        var runnerRoot = Directory.GetCurrentDirectory();
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

        await File.WriteAllTextAsync(buildLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(runLogPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(newmanLogPath, string.Empty, cancellationToken);

        Process? apiProcess = null;
        string? extractedWorkspace = null;
        var output = new PipelineConsoleOutput();

        try
        {
            var newmanCommand = await _environmentSetupService.EnsureNewmanInstalledAsync(newmanLogPath, cancellationToken);

            var submissionZipPath = ResolveSingleFile(submissionsRoot, "*.zip", "submission zip");
            var collectionPath = ResolveSingleFile(collectionsRoot, "*.postman_collection.json", "Postman collection");

            extractedWorkspace = await _unzipService.ExtractAsync(submissionZipPath, workspaceRoot, cancellationToken);
            var projectPath = FindProjectFile(extractedWorkspace);

            output.BuildLog = await _buildService.BuildAsync(projectPath, buildLogPath, cancellationToken);

            apiProcess = await _runApiService.StartAsync(projectPath, DefaultApiPort, runLogPath, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            if (apiProcess.HasExited)
            {
                throw new PipelineException("API_START_FAILED", "API process exited before health check completed.");
            }

            await _healthCheckService.EnsureApiReadyAsync(DefaultApiPort, cancellationToken);
            _ = await _newmanService.RunAsync(newmanCommand, collectionPath, reportJsonPath, newmanLogPath, cancellationToken);

            output.Results = await _reportParser.ParseAsync(reportJsonPath, cancellationToken);
            output.RunLog = await SafeReadFileAsync(runLogPath, cancellationToken);

            await WriteConsoleOutputAsync(output, cancellationToken);
        }
        catch (PipelineException ex)
        {
            output.Error = ex.ErrorCode;
            output.ErrorMessage = ex.Message;
            output.BuildLog = await SafeReadFileAsync(buildLogPath, cancellationToken);
            output.RunLog = await SafeReadFileAsync(runLogPath, cancellationToken);
            await WriteConsoleOutputAsync(output, cancellationToken);
        }
        catch (Exception)
        {
            output.Error = "TEST_RUN_FAILED";
            output.ErrorMessage = "Unexpected pipeline failure.";
            output.BuildLog = await SafeReadFileAsync(buildLogPath, cancellationToken);
            output.RunLog = await SafeReadFileAsync(runLogPath, cancellationToken);
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

        return projectFiles[0];
    }

    private static async Task<string> SafeReadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        return File.Exists(filePath)
            ? await File.ReadAllTextAsync(filePath, cancellationToken)
            : string.Empty;
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
        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await Console.Out.WriteLineAsync(json.AsMemory(), cancellationToken);
    }
}
