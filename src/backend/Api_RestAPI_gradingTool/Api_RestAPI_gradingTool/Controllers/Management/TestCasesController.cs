using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[ApiController]
[Route("api/exams/{examId:int}/testcases")]
public sealed class TestCasesController : ControllerBase
{
    private const int MaxPageSize = 100;
    private readonly GradingDbContext _db;

    public TestCasesController(GradingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TestCaseDto>>> GetList(
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
            return NotFound(new { message = "Exam not found." });
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        IQueryable<TestCase> query = _db.TestCases.Where(t => t.ExamId == examId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Name, term) ||
                EF.Functions.Like(t.PostmanItemId, term));
        }

        query = ApplySort(query, sort, order);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TestCaseDto
            {
                Id = t.Id,
                ExamId = t.ExamId,
                Name = t.Name,
                PostmanItemId = t.PostmanItemId,
                Score = t.Score,
                DependencyTestCaseId = t.DependencyTestCaseId,
                CreatedAt = t.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<TestCaseDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TestCaseDto>> GetById(
        int examId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.TestCases
            .Where(t => t.ExamId == examId && t.Id == id)
            .Select(t => new TestCaseDto
            {
                Id = t.Id,
                ExamId = t.ExamId,
                Name = t.Name,
                PostmanItemId = t.PostmanItemId,
                Score = t.Score,
                DependencyTestCaseId = t.DependencyTestCaseId,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            return NotFound(new { message = "TestCase not found." });
        }

        return Ok(entity);
    }

    [HttpPost]
    public async Task<ActionResult<TestCaseDto>> Create(
        int examId,
        [FromBody] CreateTestCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateRequest(examId, request.Name, request.PostmanItemId, request.Score, request.DependencyTestCaseId, null, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var entity = new TestCase
        {
            ExamId = examId,
            Name = request.Name.Trim(),
            PostmanItemId = request.PostmanItemId.Trim(),
            Score = request.Score,
            DependencyTestCaseId = request.DependencyTestCaseId
        };

        _db.TestCases.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new TestCaseDto
        {
            Id = entity.Id,
            ExamId = entity.ExamId,
            Name = entity.Name,
            PostmanItemId = entity.PostmanItemId,
            Score = entity.Score,
            DependencyTestCaseId = entity.DependencyTestCaseId,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { examId, id = entity.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TestCaseDto>> Update(
        int examId,
        int id,
        [FromBody] UpdateTestCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.TestCases.FirstOrDefaultAsync(t => t.ExamId == examId && t.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "TestCase not found." });
        }

        var validationError = await ValidateRequest(examId, request.Name, request.PostmanItemId, request.Score, request.DependencyTestCaseId, id, cancellationToken);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        entity.Name = request.Name.Trim();
        entity.PostmanItemId = request.PostmanItemId.Trim();
        entity.Score = request.Score;
        entity.DependencyTestCaseId = request.DependencyTestCaseId;

        await _db.SaveChangesAsync(cancellationToken);

        var dto = new TestCaseDto
        {
            Id = entity.Id,
            ExamId = entity.ExamId,
            Name = entity.Name,
            PostmanItemId = entity.PostmanItemId,
            Score = entity.Score,
            DependencyTestCaseId = entity.DependencyTestCaseId,
            CreatedAt = entity.CreatedAt
        };

        return Ok(dto);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult<TestCaseDto>> Patch(
        int examId,
        int id,
        [FromBody] PatchTestCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.TestCases.FirstOrDefaultAsync(t => t.ExamId == examId && t.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "TestCase not found." });
        }

        var newName = request.Name ?? entity.Name;
        var newPostmanItemId = request.PostmanItemId ?? entity.PostmanItemId;
        var newScore = request.Score ?? entity.Score;
        var newDependencyId = request.DependencyTestCaseId ?? entity.DependencyTestCaseId;

        var validationError = await ValidateRequest(
            examId,
            newName,
            newPostmanItemId,
            newScore,
            newDependencyId,
            id,
            cancellationToken);

        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        entity.Name = newName.Trim();
        entity.PostmanItemId = newPostmanItemId.Trim();
        entity.Score = newScore;
        entity.DependencyTestCaseId = newDependencyId;

        await _db.SaveChangesAsync(cancellationToken);

        var dto = new TestCaseDto
        {
            Id = entity.Id,
            ExamId = entity.ExamId,
            Name = entity.Name,
            PostmanItemId = entity.PostmanItemId,
            Score = entity.Score,
            DependencyTestCaseId = entity.DependencyTestCaseId,
            CreatedAt = entity.CreatedAt
        };

        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int examId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.TestCases.FirstOrDefaultAsync(t => t.ExamId == examId && t.Id == id, cancellationToken);
        if (entity is null)
        {
            return NotFound(new { message = "TestCase not found." });
        }

        _db.TestCases.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static IQueryable<TestCase> ApplySort(IQueryable<TestCase> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "createdAt").ToLowerInvariant() switch
        {
            "name" => isDesc ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            "score" => isDesc ? query.OrderByDescending(t => t.Score) : query.OrderBy(t => t.Score),
            "postmanitemid" => isDesc ? query.OrderByDescending(t => t.PostmanItemId) : query.OrderBy(t => t.PostmanItemId),
            "createdat" => isDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _ => isDesc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt)
        };
    }

    private async Task<string?> ValidateRequest(
        int examId,
        string? name,
        string? postmanItemId,
        decimal score,
        int? dependencyTestCaseId,
        int? currentTestCaseId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Exams.AnyAsync(e => e.Id == examId, cancellationToken))
        {
            return "Exam not found.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (string.IsNullOrWhiteSpace(postmanItemId))
        {
            return "PostmanItemId is required.";
        }

        if (score < 0)
        {
            return "Score must be >= 0.";
        }

        if (dependencyTestCaseId.HasValue)
        {
            if (currentTestCaseId.HasValue && dependencyTestCaseId.Value == currentTestCaseId.Value)
            {
                return "DependencyTestCaseId cannot reference itself.";
            }

            var dependencyExists = await _db.TestCases
                .AnyAsync(t => t.ExamId == examId && t.Id == dependencyTestCaseId.Value, cancellationToken);

            if (!dependencyExists)
            {
                return "DependencyTestCaseId does not exist in this exam.";
            }
        }

        return null;
    }
}
