using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_RestAPI_gradingTool.Validation;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api")]
public sealed class SubmissionsController : ApiControllerBase
{
    private const int MaxPageSize = 100;
    private const long MaxSubmissionSizeBytes = 200 * 1024 * 1024;
    private readonly GradingDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SubmissionsController(GradingDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("exams/{examId:int}/submissions")]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> ListByExam(
        int examId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Exams.AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return ProblemNotFound("Exam not found.");
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<Submission> query = _db.Submissions.AsNoTracking()
            .Where(s => s.ExamId == examId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s => EF.Functions.Like(s.StudentName, term) || EF.Functions.Like(s.FileName, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionDto
            {
                Id = s.Id,
                ExamId = s.ExamId,
                StudentName = s.StudentName,
                FileName = s.FileName,
                FilePath = s.FilePath,
                SubmittedAt = s.SubmittedAt,
                Status = s.Status,
                TotalScore = s.TotalScore,
                LastError = s.LastError
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<SubmissionDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("submissions/{id:int}")]
    public async Task<ActionResult<SubmissionDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SubmissionDto
            {
                Id = s.Id,
                ExamId = s.ExamId,
                StudentName = s.StudentName,
                FileName = s.FileName,
                FilePath = s.FilePath,
                SubmittedAt = s.SubmittedAt,
                Status = s.Status,
                TotalScore = s.TotalScore,
                LastError = s.LastError
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        return Ok(submission);
    }

    [HttpDelete("submissions/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        if (!string.IsNullOrWhiteSpace(submission.FilePath) && System.IO.File.Exists(submission.FilePath))
        {
            System.IO.File.Delete(submission.FilePath);
        }

        _db.Submissions.Remove(submission);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("exams/{examId:int}/submissions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxSubmissionSizeBytes)]
    public async Task<ActionResult<SubmissionDto>> Upload(
        int examId,
        [FromForm] SubmissionUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Exams.AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return ProblemNotFound("Exam not found.");
        }

        var studentNameError = NameRules.Validate(request.StudentName, 3, 200, "StudentName");
        if (studentNameError is not null)
        {
            return ProblemBadRequest(studentNameError);
        }

        if (request.File is null || request.File.Length == 0)
        {
            return ProblemBadRequest("Submission file is required.");
        }

        var ext = Path.GetExtension(request.File.FileName);
        if (!IsAllowedArchive(ext))
        {
            return ProblemBadRequest("Submission file must be .zip or .rar.");
        }

        if (request.File.Length > MaxSubmissionSizeBytes)
        {
            return ProblemBadRequest("Submission file is too large.");
        }

        var entity = new Submission
        {
            ExamId = examId,
            StudentName = request.StudentName.Trim(),
            Status = 0,
            TotalScore = 0,
            LastError = null,
            FileName = Path.GetFileName(request.File.FileName),
            FilePath = string.Empty
        };

        _db.Submissions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dataRoot = Path.Combine(_env.ContentRootPath, "data", "submissions", entity.Id.ToString());
        Directory.CreateDirectory(dataRoot);

        var filePath = Path.Combine(dataRoot, entity.FileName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await request.File.CopyToAsync(stream, cancellationToken);
        }

        entity.FilePath = filePath;
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new SubmissionDto
        {
            Id = entity.Id,
            ExamId = entity.ExamId,
            StudentName = entity.StudentName,
            FileName = entity.FileName,
            FilePath = entity.FilePath,
            SubmittedAt = entity.SubmittedAt,
            Status = entity.Status,
            TotalScore = entity.TotalScore,
            LastError = entity.LastError
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    private static IQueryable<Submission> ApplySort(IQueryable<Submission> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "submittedat").ToLowerInvariant() switch
        {
            "studentname" => isDesc ? query.OrderByDescending(s => s.StudentName) : query.OrderBy(s => s.StudentName),
            "submittedat" => isDesc ? query.OrderByDescending(s => s.SubmittedAt) : query.OrderBy(s => s.SubmittedAt),
            "status" => isDesc ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            "totalscore" => isDesc ? query.OrderByDescending(s => s.TotalScore) : query.OrderBy(s => s.TotalScore),
            _ => isDesc ? query.OrderByDescending(s => s.SubmittedAt) : query.OrderBy(s => s.SubmittedAt)
        };
    }

    private static bool IsAllowedArchive(string? extension)
    {
        return string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".rar", StringComparison.OrdinalIgnoreCase);
    }
}
