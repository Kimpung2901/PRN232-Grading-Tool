using System.Diagnostics;

namespace Runner.Pipeline;

public sealed class RunApiService
{
    public Task<Process> StartAsync(string projectPath, int port, string runLogPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var workingDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("Unable to resolve project directory.");

        var environmentVariables = new Dictionary<string, string?>
        {
            ["ASPNETCORE_URLS"] = $"http://localhost:{port}",
            ["DOTNET_ENVIRONMENT"] = "Development",
            ["ASPNETCORE_ENVIRONMENT"] = "Development"
        };

        try
        {
            var process = ProcessRunner.StartLongRunningProcess(
                "dotnet",
                $"run --no-build --no-launch-profile --project \"{projectPath}\"",
                workingDirectory,
                runLogPath,
                environmentVariables);

            return Task.FromResult(process);
        }
        catch (Exception ex)
        {
            throw new PipelineException("API_START_FAILED", "Failed to start API process.", ex);
        }
    }
}
