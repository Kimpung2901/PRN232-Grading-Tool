using Application.Contracts.Common;
using Application.Contracts.Grading;
using Application.Contracts.Management;
using Application.Validation;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class SubmissionService : ISubmissionService
{
    private const int MaxPageSize = 100;
    private const int PendingStatus = 0;
    private readonly IGradingDbContext _db;
    private readonly IRunnerStorage _runnerStorage;

    public SubmissionService(IGradingDbContext db, IRunnerStorage runnerStorage)
    {
        _db = db;
        _runnerStorage = runnerStorage;
    }

    public async Task<ServiceResult<PagedData<Submission>>> ListByExamAsync(int examId, string? search, string? sort, string? order, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await _db.Exams.AnyAsync(e => e.ExamId == examId, cancellationToken))
        {
            return ServiceResult<PagedData<Submission>>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<Submission> query = _db.Submissions.AsNoTracking()
            .Where(s => s.ExamId == examId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s =>
                EF.Functions.Like(s.StudentName, term) ||
                EF.Functions.Like(s.StudentCode, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return ServiceResult<PagedData<Submission>>.Ok(new PagedData<Submission>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    public Task<Submission?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyList<SubmissionGradeReport>>> GetReportsByExamAsync(int examId, CancellationToken cancellationToken)
    {
        if (!await _db.Exams.AnyAsync(e => e.ExamId == examId, cancellationToken))
        {
            return ServiceResult<IReadOnlyList<SubmissionGradeReport>>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        var reports = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.ExamId == examId)
            .OrderByDescending(s => s.Id)
            .Select(s => new SubmissionGradeReport
            {
                SubmissionId = s.Id,
                ExamId = s.ExamId,
                StudentName = s.StudentName,
                StudentCode = s.StudentCode,
                TotalScore = _db.TestResults
                    .Where(tr => tr.SubmissionId == s.Id)
                    .Select(tr => (decimal?)tr.Score)
                    .FirstOrDefault() ?? 0m,
                LastError = s.Status == PendingStatus ? "Pending grading." : null,
                Status = s.Status == PendingStatus ? "pending" : "graded",
                ReportPath = _db.TestResults
                    .Where(tr => tr.SubmissionId == s.Id)
                    .Select(tr => tr.ReportPath)
                    .FirstOrDefault()
            })
            .ToArrayAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<SubmissionGradeReport>>.Ok(reports);
    }

    public async Task<ServiceResult<Submission>> CreateAsync(int examId, string? studentName, string? studentCode, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(examId, studentName, studentCode, null, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult<Submission>.Fail(validation.Value.Type, validation.Value.Error);
        }

        var entity = new Submission
        {
            ExamId = examId,
            StudentName = studentName!.Trim(),
            StudentCode = studentCode!.Trim(),
            FilePath = string.Empty,
            Status = PendingStatus
        };

        _db.Submissions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<Submission>.Ok(entity);
    }

    public async Task<ServiceResult<Submission>> UploadAsync(int examId, string? studentName, string? studentCode, Stream fileStream, string originalFileName, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(examId, studentName, studentCode, null, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult<Submission>.Fail(validation.Value.Type, validation.Value.Error);
        }

        var extension = Path.GetExtension(originalFileName);
        if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<Submission>.Fail(ServiceErrorType.Validation, "Submission file must be .zip.");
        }

        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.ExamId == examId, cancellationToken);
        if (exam is null)
        {
            return ServiceResult<Submission>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        var entity = new Submission
        {
            ExamId = examId,
            StudentName = studentName!.Trim(),
            StudentCode = studentCode!.Trim(),
            FilePath = string.Empty,
            Status = PendingStatus
        };

        _db.Submissions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var saved = await _runnerStorage.SaveSubmissionAsync(exam.ExamName, entity.Id, entity.StudentCode, fileStream, originalFileName, cancellationToken);
        entity.FilePath = saved.RelativePath;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<Submission>.Ok(entity);
    }

    public async Task<ServiceResult<Submission>> PatchAsync(int id, string? studentName, string? studentCode, Stream? fileStream, string? originalFileName, CancellationToken cancellationToken)
    {
        var entity = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return ServiceResult<Submission>.Fail(ServiceErrorType.NotFound, "Submission not found.");
        }

        var newName = studentName ?? entity.StudentName;
        var newCode = studentCode ?? entity.StudentCode;

        var validation = await ValidateRequest(entity.ExamId, newName, newCode, id, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult<Submission>.Fail(validation.Value.Type, validation.Value.Error);
        }

        entity.StudentName = newName.Trim();
        entity.StudentCode = newCode.Trim();
        entity.Status = PendingStatus;

        if (fileStream is not null && originalFileName is not null)
        {
            var extension = Path.GetExtension(originalFileName);
            if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<Submission>.Fail(ServiceErrorType.Validation, "Submission file must be .zip.");
            }

            var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.ExamId == entity.ExamId, cancellationToken);
            if (exam is null)
            {
                return ServiceResult<Submission>.Fail(ServiceErrorType.NotFound, "Exam not found.");
            }

            var saved = await _runnerStorage.SaveSubmissionAsync(exam.ExamName, entity.Id, entity.StudentCode, fileStream, originalFileName, cancellationToken);
            entity.FilePath = saved.RelativePath;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<Submission>.Ok(entity);
    }

    public async Task<ServiceResult<Submission>> RequeueAsync(int id, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions
            .Include(s => s.TestResults)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (submission is null)
        {
            return ServiceResult<Submission>.Fail(ServiceErrorType.NotFound, "Submission not found.");
        }

        if (submission.TestResults.Count > 0)
        {
            _db.TestResults.RemoveRange(submission.TestResults);
        }

        submission.Status = PendingStatus;
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<Submission>.Ok(submission);
    }

    public async Task<ServiceResult<int>> RequeueByExamAsync(int examId, CancellationToken cancellationToken)
    {
        if (!await _db.Exams.AnyAsync(e => e.ExamId == examId, cancellationToken))
        {
            return ServiceResult<int>.Fail(ServiceErrorType.NotFound, "Exam not found.");
        }

        var submissions = await _db.Submissions
            .Include(s => s.TestResults)
            .Where(s => s.ExamId == examId)
            .ToListAsync(cancellationToken);

        if (submissions.Count == 0)
        {
            return ServiceResult<int>.Ok(0);
        }

        var testResults = submissions
            .SelectMany(s => s.TestResults)
            .ToList();

        if (testResults.Count > 0)
        {
            _db.TestResults.RemoveRange(testResults);
        }

        foreach (var submission in submissions)
        {
            submission.Status = PendingStatus;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Ok(submissions.Count);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var submission = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (submission is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Submission not found.");
        }

        _db.Submissions.Remove(submission);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }

    private static IQueryable<Submission> ApplySort(IQueryable<Submission> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "id").ToLowerInvariant() switch
        {
            "studentname" => isDesc ? query.OrderByDescending(s => s.StudentName) : query.OrderBy(s => s.StudentName),
            "studentcode" => isDesc ? query.OrderByDescending(s => s.StudentCode) : query.OrderBy(s => s.StudentCode),
            "id" => isDesc ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
            _ => isDesc ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id)
        };
    }

    private async Task<(ServiceErrorType Type, string Error)?> ValidateRequest(
        int examId,
        string? studentName,
        string? studentCode,
        int? currentId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Exams.AnyAsync(e => e.ExamId == examId, cancellationToken))
        {
            return (ServiceErrorType.NotFound, "Exam not found.");
        }

        var nameError = NameRules.Validate(studentName, 3, 200, "StudentName");
        if (nameError is not null)
        {
            return (ServiceErrorType.Validation, nameError);
        }

        if (string.IsNullOrWhiteSpace(studentCode))
        {
            return (ServiceErrorType.Validation, "StudentCode is required.");
        }

        var trimmedCode = studentCode.Trim();
        if (trimmedCode.Length > 50)
        {
            return (ServiceErrorType.Validation, "StudentCode length must be <= 50.");
        }

        IQueryable<Submission> query = _db.Submissions
            .Where(s => s.ExamId == examId && s.StudentCode == trimmedCode);
        if (currentId.HasValue)
        {
            query = query.Where(s => s.Id != currentId.Value);
        }

        var codeExists = await query.AnyAsync(cancellationToken);
        if (codeExists)
        {
            return (ServiceErrorType.Conflict, "StudentCode already exists in this exam.");
        }

        return null;
    }
}
