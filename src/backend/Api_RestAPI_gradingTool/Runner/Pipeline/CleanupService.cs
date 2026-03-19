namespace Runner.Pipeline;

public sealed class CleanupService
{
    public Task DeleteWorkspaceAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Directory.Exists(workspacePath))
        {
            Directory.Delete(workspacePath, recursive: true);
        }

        return Task.CompletedTask;
    }
}
