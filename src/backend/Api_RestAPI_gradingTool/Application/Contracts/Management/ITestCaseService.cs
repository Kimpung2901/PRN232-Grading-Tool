using Application.Contracts.Common;
using Infrastructure;

namespace Application.Contracts.Management;

public interface ITestCaseService
{
    Task<ServiceResult<PagedData<TestCase>>> ListAsync(int examId, string? search, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken);
    Task<TestCase?> GetByIdAsync(int examId, int id, CancellationToken cancellationToken);
    Task<ServiceResult<TestCase>> CreateAsync(int examId, string? filePath, CancellationToken cancellationToken);
    Task<ServiceResult<TestCase>> UploadCollectionAsync(int examId, Stream fileStream, string originalFileName, CancellationToken cancellationToken);
    Task<ServiceResult<TestCase>> PatchAsync(int examId, int id, string? filePath, Stream? fileStream, string? originalFileName, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(int examId, int id, CancellationToken cancellationToken);
}
