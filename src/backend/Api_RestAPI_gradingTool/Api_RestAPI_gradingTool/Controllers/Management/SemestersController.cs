using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_RestAPI_gradingTool.Validation;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api/semesters")]
public sealed class SemestersController : ApiControllerBase
{
    private const int MaxPageSize = 100;
    private readonly GradingDbContext _db;

    public SemestersController(GradingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SemesterDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<Semester> query = _db.Semesters.AsNoTracking();

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
            .Select(s => new SemesterDto
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                CreatedAt = s.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<SemesterDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SemesterDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var semester = await _db.Semesters.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SemesterDto
            {
                Id = s.Id,
                Name = s.Name,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (semester is null)
        {
            return ProblemNotFound("Semester not found.");
        }

        return Ok(semester);
    }

    [HttpPost]
    public async Task<ActionResult<SemesterDto>> Create(
        [FromBody] CreateSemesterRequest request,
        CancellationToken cancellationToken = default)
    {
        var nameError = NameRules.Validate(request.Name, 3, 100);
        if (nameError is not null)
        {
            return ProblemBadRequest(nameError);
        }

        var validationError = ValidateDates(request.StartDate, request.EndDate);
        if (validationError is not null)
        {
            return ProblemBadRequest(validationError);
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameExists = await _db.Semesters
            .AnyAsync(s => s.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ProblemConflict("Semester name already exists.");
        }

        var entity = new Semester
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _db.Semesters.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new SemesterDto
        {
            Id = entity.Id,
            Name = entity.Name,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SemesterDto>> Update(
        int id,
        [FromBody] UpdateSemesterRequest request,
        CancellationToken cancellationToken = default)
    {
        var semester = await _db.Semesters.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (semester is null)
        {
            return ProblemNotFound("Semester not found.");
        }

        var nameError = NameRules.Validate(request.Name, 3, 100);
        if (nameError is not null)
        {
            return ProblemBadRequest(nameError);
        }

        var validationError = ValidateDates(request.StartDate, request.EndDate);
        if (validationError is not null)
        {
            return ProblemBadRequest(validationError);
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameExists = await _db.Semesters
            .AnyAsync(s => s.Id != id && s.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ProblemConflict("Semester name already exists.");
        }

        semester.Name = request.Name.Trim();
        semester.StartDate = request.StartDate;
        semester.EndDate = request.EndDate;

        await _db.SaveChangesAsync(cancellationToken);

        var dto = new SemesterDto
        {
            Id = semester.Id,
            Name = semester.Name,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            CreatedAt = semester.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<SemesterDto>> Patch(
        int id,
        [FromBody] PatchSemesterRequest request,
        CancellationToken cancellationToken = default)
    {
        var semester = await _db.Semesters.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (semester is null)
        {
            return ProblemNotFound("Semester not found.");
        }

        var newName = request.Name ?? semester.Name;
        var nameError = NameRules.Validate(newName, 3, 100);
        if (nameError is not null)
        {
            return ProblemBadRequest(nameError);
        }

        var newStart = request.StartDate ?? semester.StartDate;
        var newEnd = request.EndDate ?? semester.EndDate;
        var validationError = ValidateDates(newStart, newEnd);
        if (validationError is not null)
        {
            return ProblemBadRequest(validationError);
        }

        var normalizedName = newName.Trim().ToLowerInvariant();
        var nameExists = await _db.Semesters
            .AnyAsync(s => s.Id != id && s.Name.ToLower() == normalizedName, cancellationToken);
        if (nameExists)
        {
            return ProblemConflict("Semester name already exists.");
        }

        semester.Name = newName.Trim();
        semester.StartDate = newStart;
        semester.EndDate = newEnd;

        await _db.SaveChangesAsync(cancellationToken);

        var dto = new SemesterDto
        {
            Id = semester.Id,
            Name = semester.Name,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            CreatedAt = semester.CreatedAt
        };

        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var semester = await _db.Semesters.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (semester is null)
        {
            return ProblemNotFound("Semester not found.");
        }

        var hasSessions = await _db.ExamSessions.AnyAsync(s => s.SemesterId == id, cancellationToken);
        if (hasSessions)
        {
            return ProblemConflict("Cannot delete semester with exam sessions.");
        }

        _db.Semesters.Remove(semester);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static string? ValidateDates(DateOnly? start, DateOnly? end)
    {
        if (start.HasValue && end.HasValue && end.Value < start.Value)
        {
            return "EndDate must be >= StartDate.";
        }

        return null;
    }

    private static IQueryable<Semester> ApplySort(IQueryable<Semester> query, string? sort, string? order)
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
}
