namespace Runner.Pipeline;

public sealed class NewmanService
{
    public async Task<string> RunAsync(
        string newmanCommand,
        string collectionPath,
        string reportPath,
        string newmanLogPath,
        CancellationToken cancellationToken = default)
    {
        var workingDirectory = Path.GetDirectoryName(collectionPath)
            ?? throw new InvalidOperationException("Unable to resolve collection directory.");

        var result = await ProcessRunner.RunAsync(
            newmanCommand,
            $"run \"{collectionPath}\" -r json --reporter-json-export \"{reportPath}\"",
            workingDirectory,
            newmanLogPath,
            cancellationToken: cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new PipelineException("TEST_RUN_FAILED", $"Newman test run failed. {result.CombinedOutput}");
        }

        return await File.ReadAllTextAsync(newmanLogPath, cancellationToken);
    }
}
