using System.IO.Compression;

namespace Runner.Pipeline;

public sealed class UnzipService
{
    public Task<string> ExtractAsync(string zipFilePath, string workspaceRoot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(zipFilePath))
        {
            throw new FileNotFoundException("Submission zip file was not found.", zipFilePath);
        }

        Directory.CreateDirectory(workspaceRoot);

        var extractionDirectory = Path.Combine(
            workspaceRoot,
            $"submission-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

        ZipFile.ExtractToDirectory(zipFilePath, extractionDirectory);
        return Task.FromResult(extractionDirectory);
    }
}
