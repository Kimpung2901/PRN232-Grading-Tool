using Application.Contracts.Common;
using Application.Contracts.Grading;
using Application.Contracts.Management;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class TestCaseService : ITestCaseService
{
    private const int MaxPageSize = 100;
    private readonly IGradingDbContext _db;
    private readonly IRunnerStorage _runnerStorage;

    public TestCaseService(IGradingDbContext db, IRunnerStorage runnerStorage)
    {
        _db = db;
        _runnerStorage = runnerStorage;
    }

    public async Task<ServiceResult<PagedData<TestCase>>> ListAsync(int examId, string? search, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await _db.Exams.AnyAsync(e => e.ExamId == examId, cancellationToken))
        {
            return ServiceResult<PagedData<TestCase>>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<TestCase> query = _db.TestCases.AsNoTracking().Where(t => t.ExamId == examId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(t => EF.Functions.Like(t.FilePath, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return ServiceResult<PagedData<TestCase>>.Ok(new PagedData<TestCase>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    public Task<TestCase?> GetByIdAsync(int examId, int id, CancellationToken cancellationToken)
    {
        return _db.TestCases.AsNoTracking().FirstOrDefaultAsync(t => t.ExamId == examId && t.Id == id, cancellationToken);
    }

    public async Task<ServiceResult<TestCase>> CreateAsync(int examId, string? filePath, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(examId, filePath, null, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult<TestCase>.Fail(validation.Value.Type, validation.Value.Error);
        }

        var entity = new TestCase
        {
            ExamId = examId,
            FilePath = filePath!.Trim()
        };

        _db.TestCases.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<TestCase>.Ok(entity);
    }

    public async Task<ServiceResult<TestCase>> UploadCollectionAsync(int examId, Stream fileStream, string originalFileName, CancellationToken cancellationToken)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        if (exam is null)
        {
            return ServiceResult<TestCase>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        var saved = await _runnerStorage.SaveCollectionAsync(exam.ExamName, fileStream, originalFileName, cancellationToken);

        var existing = await _db.TestCases.FirstOrDefaultAsync(t => t.ExamId == examId, cancellationToken);
        if (existing is null)
        {
            existing = new TestCase
            {
                ExamId = examId,
                FilePath = saved.RelativePath
            };
            _db.TestCases.Add(existing);
        }
        else
        {
            existing.FilePath = saved.RelativePath;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<TestCase>.Ok(existing);
    }

    public async Task<ServiceResult<TestCase>> PatchAsync(int examId, int id, string? filePath, Stream? fileStream, string? originalFileName, CancellationToken cancellationToken)
    {
        var entity = await _db.TestCases.FirstOrDefaultAsync(t => t.ExamId == examId && t.Id == id, cancellationToken);
        if (entity is null)
        {
            return ServiceResult<TestCase>.Fail(ServiceErrorType.NotFound, "TestCase not found.");
        }

        if (fileStream is not null && originalFileName is not null)
        {
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
            if (exam is null)
            {
                return ServiceResult<TestCase>.Fail(ServiceErrorType.NotFound, "Exam not found.");
            }

            var saved = await _runnerStorage.SaveCollectionAsync(exam.ExamName, fileStream, originalFileName, cancellationToken);
            entity.FilePath = saved.RelativePath;
        }
        else if (filePath is not null)
        {
            var newPath = filePath ?? entity.FilePath;
            var validation = await ValidateRequest(examId, newPath, id, cancellationToken);
            if (validation is not null)
            {
                return ServiceResult<TestCase>.Fail(validation.Value.Type, validation.Value.Error);
            }

            entity.FilePath = newPath.Trim();
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<TestCase>.Ok(entity);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int examId, int id, CancellationToken cancellationToken)
    {
        var entity = await _db.TestCases.FirstOrDefaultAsync(t => t.ExamId == examId && t.Id == id, cancellationToken);
        if (entity is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "TestCase not found.");
        }

        _db.TestCases.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }

    private static IQueryable<TestCase> ApplySort(IQueryable<TestCase> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "id").ToLowerInvariant() switch
        {
            "filepath" => isDesc ? query.OrderByDescending(t => t.FilePath) : query.OrderBy(t => t.FilePath),
            "id" => isDesc ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id),
            _ => isDesc ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id)
        };
    }

    private async Task<(ServiceErrorType Type, string Error)?> ValidateRequest(
        int examId,
        string? filePath,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Exams.AnyAsync(e => e.ExamId == examId, cancellationToken))
        {
            return (ServiceErrorType.NotFound, "Exam not found.");
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return (ServiceErrorType.Validation, "FilePath is required.");
        }

        var trimmed = filePath.Trim();
        if (trimmed.Length > 500)
        {
            return (ServiceErrorType.Validation, "FilePath length must be <= 500.");
        }

        IQueryable<TestCase> query = _db.TestCases.Where(t => t.ExamId == examId && t.FilePath == trimmed);
        if (currentId.HasValue)
        {
            query = query.Where(t => t.Id != currentId.Value);
        }

        var exists = await query.AnyAsync(cancellationToken);
        if (exists)
        {
            return (ServiceErrorType.Conflict, "FilePath already exists in this exam.");
        }

        return null;
    }
}
