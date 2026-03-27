using Application.Contracts.Grading;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Runner;

public sealed class RunnerStorage : IRunnerStorage
{
    private readonly string _runnerRoot;

    public RunnerStorage(IHostEnvironment env)
    {
        _runnerRoot = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "Runner"));
    }

    public async Task<RunnerFileInfo> SaveSubmissionAsync(string examName, int submissionId, string studentCode, Stream content, string originalFileName, CancellationToken cancellationToken)
    {
        var submissionsRoot = Path.Combine(_runnerRoot, "submissions", SanitizePathSegment(examName));
        Directory.CreateDirectory(submissionsRoot);

        var extension = Path.GetExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"{submissionId}_{SanitizePathSegment(studentCode)}_{timestamp}{extension}";
        var fullPath = Path.Combine(submissionsRoot, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(stream, cancellationToken);
        }

        return new RunnerFileInfo
        {
            FileName = fileName,
            FullPath = fullPath,
            RelativePath = ToRelative(fullPath)
        };
    }

    public async Task<RunnerFileInfo> SaveCollectionAsync(string examName, Stream content, string originalFileName, CancellationToken cancellationToken)
    {
        var collectionsRoot = Path.Combine(_runnerRoot, "collections", SanitizePathSegment(examName));
        Directory.CreateDirectory(collectionsRoot);

        var fileName = "testcase.json";
        var fullPath = Path.Combine(collectionsRoot, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(stream, cancellationToken);
        }

        return new RunnerFileInfo
        {
            FileName = fileName,
            FullPath = fullPath,
            RelativePath = ToRelative(fullPath)
        };
    }

    public async Task<RunnerFileInfo> SaveSqlScriptAsync(string examName, Stream content, string originalFileName, CancellationToken cancellationToken)
    {
        var databaseRoot = Path.Combine(_runnerRoot, "database", SanitizePathSegment(examName));
        Directory.CreateDirectory(databaseRoot);

        var fileName = "seed.sql";
        var fullPath = Path.Combine(databaseRoot, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(stream, cancellationToken);
        }

        return new RunnerFileInfo
        {
            FileName = fileName,
            FullPath = fullPath,
            RelativePath = ToRelative(fullPath)
        };
    }


    private string ToRelative(string fullPath)
    {
        return Path.GetRelativePath(_runnerRoot, fullPath).Replace('\\', '/');
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var filtered = new string(value.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(filtered) ? "unknown" : filtered;
    }
}
