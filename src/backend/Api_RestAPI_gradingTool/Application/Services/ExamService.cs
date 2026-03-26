using Application.Contracts.Common;
using Application.Contracts.Grading;
using Application.Contracts.Management;
using Application.Validation;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class ExamService : IExamService
{
    private const int MaxPageSize = 100;
    private readonly IGradingDbContext _db;
    private readonly IRunnerStorage _runnerStorage;

    public ExamService(IGradingDbContext db, IRunnerStorage runnerStorage)
    {
        _db = db;
        _runnerStorage = runnerStorage;
    }

    public async Task<PagedData<Exam>> ListAsync(string? search, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<Exam> query = _db.Exams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(e => EF.Functions.Like(e.ExamName, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedData<Exam>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    public Task<Exam?> GetByIdAsync(int examId, CancellationToken cancellationToken)
    {
        return _db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
    }

    public async Task<ServiceResult<Exam>> CreateAsync(string? examName, Stream? sqlStream, string? sqlFileName, CancellationToken cancellationToken)
    {
        var nameError = NameRules.Validate(examName, 3, 200, "ExamName");
        if (nameError is not null)
        {
            return ServiceResult<Exam>.Fail(ServiceErrorType.Validation, nameError);
        }

        var sqlError = ValidateSqlFile(sqlStream, sqlFileName);
        if (sqlError is not null)
        {
            return ServiceResult<Exam>.Fail(ServiceErrorType.Validation, sqlError);
        }

        var normalizedName = examName!.Trim().ToLowerInvariant();
        var nameExists = await _db.Exams.AnyAsync(e => e.ExamName.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ServiceResult<Exam>.Fail(ServiceErrorType.Conflict, "Exam name already exists.");
        }

        var entity = new Exam { ExamName = examName.Trim() };
        _db.Exams.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (sqlStream is not null && sqlFileName is not null)
        {
            var saved = await _runnerStorage.SaveSqlScriptAsync(entity.ExamName, sqlStream, sqlFileName, cancellationToken);
            entity.SqlScriptPath = saved.RelativePath;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<Exam>.Ok(entity);
    }

    public async Task<ServiceResult<Exam>> PatchAsync(int examId, string? examName, Stream? sqlStream, string? sqlFileName, CancellationToken cancellationToken)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        if (exam is null)
        {
            return ServiceResult<Exam>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        var newName = examName ?? exam.ExamName;
        var nameError = NameRules.Validate(newName, 3, 200, "ExamName");
        if (nameError is not null)
        {
            return ServiceResult<Exam>.Fail(ServiceErrorType.Validation, nameError);
        }

        var sqlError = ValidateSqlFile(sqlStream, sqlFileName);
        if (sqlError is not null)
        {
            return ServiceResult<Exam>.Fail(ServiceErrorType.Validation, sqlError);
        }

        var normalizedName = newName.Trim().ToLowerInvariant();
        var nameExists = await _db.Exams.AnyAsync(e => e.ExamId != examId && e.ExamName.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ServiceResult<Exam>.Fail(ServiceErrorType.Conflict, "Exam name already exists.");
        }

        exam.ExamName = newName.Trim();

        if (sqlStream is not null && sqlFileName is not null)
        {
            var saved = await _runnerStorage.SaveSqlScriptAsync(exam.ExamName, sqlStream, sqlFileName, cancellationToken);
            exam.SqlScriptPath = saved.RelativePath;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<Exam>.Ok(exam);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int examId, CancellationToken cancellationToken)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        if (exam is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        var hasSubmissions = await _db.Submissions.AnyAsync(s => s.ExamId == examId, cancellationToken);
        var hasTestCases = await _db.TestCases.AnyAsync(t => t.ExamId == examId, cancellationToken);
        if (hasSubmissions || hasTestCases)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.Conflict, "Cannot delete exam with submissions or test cases.");
        }

        _db.Exams.Remove(exam);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }

    private static IQueryable<Exam> ApplySort(IQueryable<Exam> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "examid").ToLowerInvariant() switch
        {
            "examname" => isDesc ? query.OrderByDescending(e => e.ExamName) : query.OrderBy(e => e.ExamName),
            "examid" => isDesc ? query.OrderByDescending(e => e.ExamId) : query.OrderBy(e => e.ExamId),
            _ => isDesc ? query.OrderByDescending(e => e.ExamId) : query.OrderBy(e => e.ExamId)
        };
    }

    private static string? ValidateSqlFile(Stream? sqlStream, string? sqlFileName)
    {
        if (sqlStream is null || sqlFileName is null)
        {
            return null;
        }

        var extension = Path.GetExtension(sqlFileName);
        if (!string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase))
        {
            return "SQL script must be .sql.";
        }

        return null;
    }
}
