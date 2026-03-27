namespace Runner.Pipeline;

public sealed class BuildService
{
    public async Task<string> BuildAsync(string projectPath, string buildLogPath, CancellationToken cancellationToken = default)
    {
        var workingDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException("Unable to resolve project directory.");

        var result = await ProcessRunner.RunAsync(
            "dotnet",
            $"build \"{projectPath}\"",
            workingDirectory,
            buildLogPath,
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new PipelineException("BUILD_FAILED", "dotnet build failed.");
        }

        return await File.ReadAllTextAsync(buildLogPath, cancellationToken);
    }
}
