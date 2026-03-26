using Application.Contracts.Common;
using Infrastructure;

namespace Application.Contracts.Management;

public interface ISubmissionService
{
    Task<ServiceResult<PagedData<Submission>>> ListByExamAsync(int examId, string? search, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken);
    Task<Submission?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ServiceResult<Submission>> CreateAsync(int examId, string? studentName, string? studentCode, CancellationToken cancellationToken);
    Task<ServiceResult<Submission>> UploadAsync(int examId, string? studentName, string? studentCode, Stream fileStream, string originalFileName, CancellationToken cancellationToken);
    Task<ServiceResult<Submission>> PatchAsync(int id, string? studentName, string? studentCode, Stream? fileStream, string? originalFileName, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken);
}
