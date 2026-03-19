using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_RestAPI_gradingTool.Validation;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api")]
public sealed class ExamsController : ApiControllerBase
{
    private const int MaxPageSize = 100;
    private readonly GradingDbContext _db;

    public ExamsController(GradingDbContext db)
    {
        _db = db;
    }

    [HttpGet("exam-sessions/{sessionId:int}/exams")]
    public async Task<ActionResult<PagedResult<ExamDto>>> ListBySession(
        int sessionId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamSessions.AnyAsync(s => s.Id == sessionId, cancellationToken))
        {
            return ProblemNotFound("Exam session not found.");
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<Exam> query = _db.Exams.AsNoTracking()
            .Where(e => e.SessionId == sessionId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(e => EF.Functions.Like(e.Name, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExamDto
            {
                Id = e.Id,
                SessionId = e.SessionId,
                Name = e.Name,
                DatabaseFilePath = e.DatabaseFilePath,
                CollectionFilePath = e.CollectionFilePath,
                EndpointSpecFilePath = e.EndpointSpecFilePath,
                EnvironmentFilePath = e.EnvironmentFilePath,
                StudentDbConnection = e.StudentDbConnection,
                HealthPath = e.HealthPath,
                SwaggerPath = e.SwaggerPath,
                SqlCmdServer = e.SqlCmdServer,
                SqlCmdUser = e.SqlCmdUser,
                SqlCmdPassword = e.SqlCmdPassword,
                NewmanExtraArgs = e.NewmanExtraArgs,
                CreatedAt = e.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<ExamDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("exams/{id:int}")]
    public async Task<ActionResult<ExamDto>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new ExamDto
            {
                Id = e.Id,
                SessionId = e.SessionId,
                Name = e.Name,
                DatabaseFilePath = e.DatabaseFilePath,
                CollectionFilePath = e.CollectionFilePath,
                EndpointSpecFilePath = e.EndpointSpecFilePath,
                EnvironmentFilePath = e.EnvironmentFilePath,
                StudentDbConnection = e.StudentDbConnection,
                HealthPath = e.HealthPath,
                SwaggerPath = e.SwaggerPath,
                SqlCmdServer = e.SqlCmdServer,
                SqlCmdUser = e.SqlCmdUser,
                SqlCmdPassword = e.SqlCmdPassword,
                NewmanExtraArgs = e.NewmanExtraArgs,
                CreatedAt = e.CreatedAt,
                TestCasesCount = e.TestCases.Count,
                SubmissionsCount = e.Submissions.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        return Ok(exam);
    }

    [HttpPost("exam-sessions/{sessionId:int}/exams")]
    public async Task<ActionResult<ExamDto>> Create(
        int sessionId,
        [FromBody] CreateExamRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.ExamSessions.AnyAsync(s => s.Id == sessionId, cancellationToken))
        {
            return ProblemNotFound("Exam session not found.");
        }

        var nameError = NameRules.Validate(request.Name, 3, 200);
        if (nameError is not null)
        {
            return ProblemBadRequest(nameError);
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameExists = await _db.Exams
            .AnyAsync(e => e.SessionId == sessionId && e.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ProblemConflict("Exam name already exists in this session.");
        }

        var entity = new Exam
        {
            SessionId = sessionId,
            Name = request.Name.Trim()
        };

        _db.Exams.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ExamDto
        {
            Id = entity.Id,
            SessionId = entity.SessionId,
            Name = entity.Name,
            DatabaseFilePath = entity.DatabaseFilePath,
            CollectionFilePath = entity.CollectionFilePath,
            EndpointSpecFilePath = entity.EndpointSpecFilePath,
            EnvironmentFilePath = entity.EnvironmentFilePath,
            StudentDbConnection = entity.StudentDbConnection,
            HealthPath = entity.HealthPath,
            SwaggerPath = entity.SwaggerPath,
            SqlCmdServer = entity.SqlCmdServer,
            SqlCmdUser = entity.SqlCmdUser,
            SqlCmdPassword = entity.SqlCmdPassword,
            NewmanExtraArgs = entity.NewmanExtraArgs,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("exams/{id:int}")]
    public async Task<ActionResult<ExamDto>> Update(
        int id,
        [FromBody] UpdateExamRequest request,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        var nameError = NameRules.Validate(request.Name, 3, 200);
        if (nameError is not null)
        {
            return ProblemBadRequest(nameError);
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameExists = await _db.Exams
            .AnyAsync(e => e.SessionId == exam.SessionId && e.Id != id && e.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ProblemConflict("Exam name already exists in this session.");
        }

        exam.Name = request.Name.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ExamDto
        {
            Id = exam.Id,
            SessionId = exam.SessionId,
            Name = exam.Name,
            DatabaseFilePath = exam.DatabaseFilePath,
            CollectionFilePath = exam.CollectionFilePath,
            EndpointSpecFilePath = exam.EndpointSpecFilePath,
            EnvironmentFilePath = exam.EnvironmentFilePath,
            StudentDbConnection = exam.StudentDbConnection,
            HealthPath = exam.HealthPath,
            SwaggerPath = exam.SwaggerPath,
            SqlCmdServer = exam.SqlCmdServer,
            SqlCmdUser = exam.SqlCmdUser,
            SqlCmdPassword = exam.SqlCmdPassword,
            NewmanExtraArgs = exam.NewmanExtraArgs,
            CreatedAt = exam.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPatch("exams/{id:int}")]
    public async Task<ActionResult<ExamDto>> Patch(
        int id,
        [FromBody] PatchExamRequest request,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        var newName = request.Name ?? exam.Name;
        var nameError = NameRules.Validate(newName, 3, 200);
        if (nameError is not null)
        {
            return ProblemBadRequest(nameError);
        }

        var normalizedName = newName.Trim().ToLowerInvariant();
        var nameExists = await _db.Exams
            .AnyAsync(e => e.SessionId == exam.SessionId && e.Id != id && e.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ProblemConflict("Exam name already exists in this session.");
        }

        exam.Name = newName.Trim();
        if (request.StudentDbConnection is not null) exam.StudentDbConnection = request.StudentDbConnection.Trim();
        if (request.HealthPath is not null) exam.HealthPath = request.HealthPath.Trim();
        if (request.SwaggerPath is not null) exam.SwaggerPath = request.SwaggerPath.Trim();
        if (request.SqlCmdServer is not null) exam.SqlCmdServer = request.SqlCmdServer.Trim();
        if (request.SqlCmdUser is not null) exam.SqlCmdUser = request.SqlCmdUser.Trim();
        if (request.SqlCmdPassword is not null) exam.SqlCmdPassword = request.SqlCmdPassword.Trim();
        if (request.NewmanExtraArgs is not null) exam.NewmanExtraArgs = request.NewmanExtraArgs.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ExamDto
        {
            Id = exam.Id,
            SessionId = exam.SessionId,
            Name = exam.Name,
            DatabaseFilePath = exam.DatabaseFilePath,
            CollectionFilePath = exam.CollectionFilePath,
            EndpointSpecFilePath = exam.EndpointSpecFilePath,
            EnvironmentFilePath = exam.EnvironmentFilePath,
            StudentDbConnection = exam.StudentDbConnection,
            HealthPath = exam.HealthPath,
            SwaggerPath = exam.SwaggerPath,
            SqlCmdServer = exam.SqlCmdServer,
            SqlCmdUser = exam.SqlCmdUser,
            SqlCmdPassword = exam.SqlCmdPassword,
            NewmanExtraArgs = exam.NewmanExtraArgs,
            CreatedAt = exam.CreatedAt
        };

        return Ok(dto);
    }

    [HttpDelete("exams/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        var hasSubmissions = await _db.Submissions.AnyAsync(s => s.ExamId == id, cancellationToken);
        var hasTestCases = await _db.TestCases.AnyAsync(t => t.ExamId == id, cancellationToken);
        if (hasSubmissions || hasTestCases)
        {
            return ProblemConflict("Cannot delete exam with submissions or test cases.");
        }

        _db.Exams.Remove(exam);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static IQueryable<Exam> ApplySort(IQueryable<Exam> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "id").ToLowerInvariant() switch
        {
            "name" => isDesc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            "createdat" => isDesc ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
            "id" => isDesc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id),
            _ => isDesc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id)
        };
    }

}
