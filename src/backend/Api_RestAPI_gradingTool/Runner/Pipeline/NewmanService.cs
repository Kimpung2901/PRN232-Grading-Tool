namespace Runner.Pipeline;

public sealed class NewmanService
{
    public async Task<NewmanExecutionResult> RunAsync(
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

        return new NewmanExecutionResult
        {
            ExitCode = result.ExitCode,
            LogContent = await File.ReadAllTextAsync(newmanLogPath, cancellationToken)
        };
    }
}
