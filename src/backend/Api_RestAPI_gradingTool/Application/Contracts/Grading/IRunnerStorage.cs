namespace Application.Contracts.Grading;

public interface IRunnerStorage
{
    Task<RunnerFileInfo> SaveSubmissionAsync(string examName, int submissionId, string studentCode, Stream content, string originalFileName, CancellationToken cancellationToken);
    Task<RunnerFileInfo> SaveCollectionAsync(string examName, Stream content, string originalFileName, CancellationToken cancellationToken);
    Task<RunnerFileInfo> SaveSqlScriptAsync(string examName, Stream content, string originalFileName, CancellationToken cancellationToken);
}
