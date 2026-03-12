using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[ApiController]
[Route("api")]
public sealed class ExamSessionsController : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly GradingDbContext _db;

    public ExamSessionsController(GradingDbContext db)
    {
        _db = db;
    }

    [HttpGet("semesters/{semesterId:int}/exam-sessions")]
    public async Task<ActionResult<PagedResult<ExamSessionDto>>> ListBySemester(
        int semesterId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Semesters.AnyAsync(s => s.Id == semesterId, cancellationToken))
        {
            return NotFound(new { message = "Semester not found." });
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<ExamSession> query = _db.ExamSessions.AsNoTracking()
            .Where(s => s.SemesterId == semesterId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s => EF.Functions.Like(s.Name, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ExamSessionDto
            {
                Id = s.Id,
                SemesterId = s.SemesterId,
                Name = s.Name,
                CreatedAt = s.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<ExamSessionDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("exam-sessions/{id:int}")]
    public async Task<ActionResult<ExamSessionDto>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ExamSessionDto
            {
                Id = s.Id,
                SemesterId = s.SemesterId,
                Name = s.Name,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return NotFound(new { message = "Exam session not found." });
        }

        return Ok(session);
    }

    [HttpPost("semesters/{semesterId:int}/exam-sessions")]
    public async Task<ActionResult<ExamSessionDto>> Create(
        int semesterId,
        [FromBody] CreateExamSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Semesters.AnyAsync(s => s.Id == semesterId, cancellationToken))
        {
            return NotFound(new { message = "Semester not found." });
        }

        var nameError = ValidateName(request.Name);
        if (nameError is not null)
        {
            return BadRequest(new { message = nameError });
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameExists = await _db.ExamSessions
            .AnyAsync(s => s.SemesterId == semesterId && s.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return Conflict(new { message = "Exam session name already exists in this semester." });
        }

        var entity = new ExamSession
        {
            SemesterId = semesterId,
            Name = request.Name.Trim()
        };

        _db.ExamSessions.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ExamSessionDto
        {
            Id = entity.Id,
            SemesterId = entity.SemesterId,
            Name = entity.Name,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("exam-sessions/{id:int}")]
    public async Task<ActionResult<ExamSessionDto>> Update(
        int id,
        [FromBody] UpdateExamSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Exam session not found." });
        }

        var nameError = ValidateName(request.Name);
        if (nameError is not null)
        {
            return BadRequest(new { message = nameError });
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameExists = await _db.ExamSessions
            .AnyAsync(s => s.SemesterId == session.SemesterId && s.Id != id && s.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return Conflict(new { message = "Exam session name already exists in this semester." });
        }

        session.Name = request.Name.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ExamSessionDto
        {
            Id = session.Id,
            SemesterId = session.SemesterId,
            Name = session.Name,
            CreatedAt = session.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPatch("exam-sessions/{id:int}")]
    public async Task<ActionResult<ExamSessionDto>> Patch(
        int id,
        [FromBody] PatchExamSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Exam session not found." });
        }

        var newName = request.Name ?? session.Name;
        var nameError = ValidateName(newName);
        if (nameError is not null)
        {
            return BadRequest(new { message = nameError });
        }

        var normalizedName = newName.Trim().ToLowerInvariant();
        var nameExists = await _db.ExamSessions
            .AnyAsync(s => s.SemesterId == session.SemesterId && s.Id != id && s.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return Conflict(new { message = "Exam session name already exists in this semester." });
        }

        session.Name = newName.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ExamSessionDto
        {
            Id = session.Id,
            SemesterId = session.SemesterId,
            Name = session.Name,
            CreatedAt = session.CreatedAt
        };

        return Ok(dto);
    }

    [HttpDelete("exam-sessions/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var session = await _db.ExamSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null)
        {
            return NotFound(new { message = "Exam session not found." });
        }

        var hasExams = await _db.Exams.AnyAsync(e => e.SessionId == id, cancellationToken);
        if (hasExams)
        {
            return Conflict(new { message = "Cannot delete exam session with exams." });
        }

        _db.ExamSessions.Remove(session);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static IQueryable<ExamSession> ApplySort(IQueryable<ExamSession> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "id").ToLowerInvariant() switch
        {
            "name" => isDesc ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "createdat" => isDesc ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            "id" => isDesc ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
            _ => isDesc ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id)
        };
    }

    private static string? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        var trimmed = name.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 100)
        {
            return "Name length must be between 3 and 100.";
        }

        return null;
    }
}
