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

    public async Task DeleteSubmissionArtifactAsync(string artifactPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (File.Exists(artifactPath))
                {
                    File.Delete(artifactPath);
                    return;
                }

                if (Directory.Exists(artifactPath))
                {
                    Directory.Delete(artifactPath, recursive: true);
                    return;
                }

                return;
            }
            catch (IOException) when (attempt < 3)
            {
                await Task.Delay(500, cancellationToken);
            }
            catch (UnauthorizedAccessException) when (attempt < 3)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        if (File.Exists(artifactPath))
        {
            File.Delete(artifactPath);
            return;
        }

        if (Directory.Exists(artifactPath))
        {
            Directory.Delete(artifactPath, recursive: true);
        }
    }
}
