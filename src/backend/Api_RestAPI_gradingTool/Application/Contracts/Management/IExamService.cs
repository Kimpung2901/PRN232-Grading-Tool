using Application.Contracts.Common;
using Infrastructure;

namespace Application.Contracts.Management;

public interface IExamService
{
    Task<PagedData<Exam>> ListAsync(string? search, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken);
    Task<Exam?> GetByIdAsync(int examId, CancellationToken cancellationToken);
    Task<ServiceResult<Exam>> CreateAsync(string? examName, Stream? sqlStream, string? sqlFileName, CancellationToken cancellationToken);
    Task<ServiceResult<Exam>> PatchAsync(int examId, string? examName, Stream? sqlStream, string? sqlFileName, CancellationToken cancellationToken);
    Task<ServiceResult<bool>> DeleteAsync(int examId, CancellationToken cancellationToken);
}
