namespace Runner.Pipeline;

public sealed class NewmanService
{
    public async Task<NewmanExecutionResult> RunAsync(
        string newmanCommand,
        string collectionPath,
        string reportPath,
        string newmanLogPath,
        int apiPort,
        CancellationToken cancellationToken = default)
    {
        var workingDirectory = Path.GetDirectoryName(collectionPath)
            ?? throw new InvalidOperationException("Unable to resolve collection directory.");

        var baseUrl = $"http://localhost:{apiPort}";
        var result = await ProcessRunner.RunAsync(
            newmanCommand,
            $"run \"{collectionPath}\" -r json --reporter-json-export \"{reportPath}\" --env-var \"baseUrl={baseUrl}\"",
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
