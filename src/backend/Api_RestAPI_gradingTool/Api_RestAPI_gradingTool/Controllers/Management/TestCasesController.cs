using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_RestAPI_gradingTool.Validation;
using System.Text.Json;
using System.Linq;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api/exams/{examId:int}/testcases")]
public sealed class TestCasesController : ApiControllerBase
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
            return ProblemNotFound("Exam not found.");
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
            return ProblemNotFound("TestCase not found.");
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
            if (validationError.StartsWith("TestCase name already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }
            if (validationError.StartsWith("PostmanItemId already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }

            if (validationError.StartsWith("TestCase name already exists", StringComparison.OrdinalIgnoreCase)
                || validationError.StartsWith("PostmanItemId already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }

            return ProblemBadRequest(validationError);
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

    [HttpPost("import-from-collection")]
    public async Task<ActionResult<ImportTestCaseResultDto>> ImportFromCollection(
        int examId,
        [FromQuery] decimal defaultScore = 1,
        [FromQuery] bool replace = false,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        if (string.IsNullOrWhiteSpace(exam.CollectionFilePath) || !System.IO.File.Exists(exam.CollectionFilePath))
        {
            return ProblemBadRequest("collection.json is missing.");
        }

        using var stream = System.IO.File.OpenRead(exam.CollectionFilePath);
        using var doc = JsonDocument.Parse(stream);

        if (!doc.RootElement.TryGetProperty("item", out var items))
        {
            return ProblemBadRequest("Invalid collection.json: missing item.");
        }

        var pairs = ExtractItems(items).ToList();
        var created = 0;
        var skipped = 0;

        if (replace)
        {
            var testCaseIds = await _db.TestCases
                .Where(t => t.ExamId == examId)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);

            if (testCaseIds.Count > 0)
            {
                var existingResults = _db.TestResults.Where(r => testCaseIds.Contains(r.TestCaseId));
                _db.TestResults.RemoveRange(existingResults);
            }

            var existing = _db.TestCases.Where(t => t.ExamId == examId);
            _db.TestCases.RemoveRange(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        foreach (var (id, name) in pairs)
        {
            var key = !string.IsNullOrWhiteSpace(id) ? id : name;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            var exists = await _db.TestCases
                .AnyAsync(t => t.ExamId == examId && t.PostmanItemId == key, cancellationToken);

            if (exists)
            {
                skipped++;
                continue;
            }

            _db.TestCases.Add(new TestCase
            {
                ExamId = examId,
                Name = name.Trim(),
                PostmanItemId = key.Trim(),
                Score = defaultScore
            });
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ImportTestCaseResultDto
        {
            ExamId = examId,
            Created = created,
            Skipped = skipped
        });
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
            return ProblemNotFound("TestCase not found.");
        }

        var validationError = await ValidateRequest(examId, request.Name, request.PostmanItemId, request.Score, request.DependencyTestCaseId, id, cancellationToken);
        if (validationError is not null)
        {
            if (validationError.StartsWith("TestCase name already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }
            if (validationError.StartsWith("PostmanItemId already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }

            if (validationError.StartsWith("TestCase name already exists", StringComparison.OrdinalIgnoreCase)
                || validationError.StartsWith("PostmanItemId already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }

            return ProblemBadRequest(validationError);
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
            return ProblemNotFound("TestCase not found.");
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
            if (validationError.StartsWith("TestCase name already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }
            if (validationError.StartsWith("PostmanItemId already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }

            if (validationError.StartsWith("TestCase name already exists", StringComparison.OrdinalIgnoreCase)
                || validationError.StartsWith("PostmanItemId already exists", StringComparison.OrdinalIgnoreCase))
            {
                return ProblemConflict(validationError);
            }

            return ProblemBadRequest(validationError);
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
            return ProblemNotFound("TestCase not found.");
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

        var nameError = NameRules.Validate(name, 3, 200);
        if (nameError is not null)
        {
            return nameError;
        }

        var trimmedName = name.Trim();

        if (string.IsNullOrWhiteSpace(postmanItemId))
        {
            return "PostmanItemId is required.";
        }

        var trimmedPostmanItemId = postmanItemId.Trim();
        if (trimmedPostmanItemId.Length < 1 || trimmedPostmanItemId.Length > 200)
        {
            return "PostmanItemId length must be between 1 and 200.";
        }

        if (score < 0)
        {
            return "Score must be >= 0.";
        }

        var normalizedName = trimmedName.ToLowerInvariant();
        IQueryable<TestCase> nameQuery = _db.TestCases.Where(t => t.ExamId == examId && t.Name.ToLower() == normalizedName);
        if (currentTestCaseId.HasValue)
        {
            nameQuery = nameQuery.Where(t => t.Id != currentTestCaseId.Value);
        }

        var nameExists = await nameQuery.AnyAsync(cancellationToken);
        if (nameExists)
        {
            return "TestCase name already exists in this exam.";
        }

        var normalizedPostmanItemId = trimmedPostmanItemId.ToLowerInvariant();
        IQueryable<TestCase> postmanQuery = _db.TestCases
            .Where(t => t.ExamId == examId && t.PostmanItemId.ToLower() == normalizedPostmanItemId);
        if (currentTestCaseId.HasValue)
        {
            postmanQuery = postmanQuery.Where(t => t.Id != currentTestCaseId.Value);
        }

        var postmanExists = await postmanQuery.AnyAsync(cancellationToken);
        if (postmanExists)
        {
            return "PostmanItemId already exists in this exam.";
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

    private static IEnumerable<(string id, string name)> ExtractItems(JsonElement items)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (item.TryGetProperty("item", out var childItems))
            {
                foreach (var child in ExtractItems(childItems))
                {
                    yield return child;
                }
                continue;
            }

            var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            yield return (id ?? string.Empty, name ?? string.Empty);
        }
    }
}
