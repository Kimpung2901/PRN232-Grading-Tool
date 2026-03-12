using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[ApiController]
[Route("api/exams")]
public sealed class ExamResourcesController : ControllerBase
{
    private const long MaxCollectionSizeBytes = 10 * 1024 * 1024;
    private const long MaxDatabaseSizeBytes = 200 * 1024 * 1024;
    private const int MaxPageSize = 100;
    private readonly GradingDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ExamResourcesController(GradingDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet("resources")]
    public async Task<ActionResult<PagedResult<ExamResourceListItemDto>>> List(
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

        IQueryable<Exam> query = _db.Exams.AsNoTracking()
            .Where(e => e.CollectionFilePath != null);

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
            .Select(e => new ExamResourceListItemDto
            {
                ExamId = e.Id,
                ExamName = e.Name,
                CollectionFilePath = e.CollectionFilePath!,
                DatabaseFilePath = e.DatabaseFilePath,
                ExamCreatedAt = e.CreatedAt
            })
            .ToArrayAsync(cancellationToken);

        return Ok(new PagedResult<ExamResourceListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpPost("{examId:int}/resources")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDatabaseSizeBytes + MaxCollectionSizeBytes)]
    public Task<ActionResult<ExamResourceDto>> Create(
        int examId,
        [FromForm] ExamResourceUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        return UploadInternal(examId, request.Collection, request.Database, cancellationToken);
    }

    [HttpPut("{examId:int}/resources")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDatabaseSizeBytes + MaxCollectionSizeBytes)]
    public Task<ActionResult<ExamResourceDto>> Replace(
        int examId,
        [FromForm] ExamResourceUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        return UploadInternal(examId, request.Collection, request.Database, cancellationToken);
    }

    [HttpGet("{examId:int}/resources")]
    public async Task<ActionResult<ExamResourceDto>> Get(
        int examId,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound(new { message = "Exam not found." });
        }

        if (string.IsNullOrWhiteSpace(exam.CollectionFilePath))
        {
            return NotFound(new { message = "Resources not found." });
        }

        return Ok(new ExamResourceDto
        {
            ExamId = exam.Id,
            CollectionFilePath = exam.CollectionFilePath,
            DatabaseFilePath = exam.DatabaseFilePath,
            UpdatedAt = DateTime.UtcNow
        });
    }

    [HttpDelete("{examId:int}/resources")]
    public async Task<IActionResult> Delete(
        int examId,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound(new { message = "Exam not found." });
        }

        if (!string.IsNullOrWhiteSpace(exam.CollectionFilePath) && System.IO.File.Exists(exam.CollectionFilePath))
        {
            System.IO.File.Delete(exam.CollectionFilePath);
        }

        if (!string.IsNullOrWhiteSpace(exam.DatabaseFilePath) && System.IO.File.Exists(exam.DatabaseFilePath))
        {
            System.IO.File.Delete(exam.DatabaseFilePath);
        }

        exam.CollectionFilePath = null;
        exam.DatabaseFilePath = null;

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<ActionResult<ExamResourceDto>> UploadInternal(
        int examId,
        IFormFile collection,
        IFormFile? database,
        CancellationToken cancellationToken)
    {
        if (collection is null || collection.Length == 0)
        {
            return BadRequest(new { message = "collection.json is required." });
        }

        if (!HasExtension(collection.FileName, ".json"))
        {
            return BadRequest(new { message = "collection must be a .json file." });
        }

        if (collection.Length > MaxCollectionSizeBytes)
        {
            return BadRequest(new { message = "collection.json is too large." });
        }

        if (database is not null)
        {
            if (!HasExtension(database.FileName, ".sql"))
            {
                return BadRequest(new { message = "database must be a .sql file." });
            }

            if (database.Length > MaxDatabaseSizeBytes)
            {
                return BadRequest(new { message = "database.sql is too large." });
            }
        }

        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return NotFound(new { message = "Exam not found." });
        }

        var dataRoot = Path.Combine(_env.ContentRootPath, "data", "exams", examId.ToString());
        Directory.CreateDirectory(dataRoot);

        var collectionPath = Path.Combine(dataRoot, "collection.json");
        await using (var stream = System.IO.File.Create(collectionPath))
        {
            await collection.CopyToAsync(stream, cancellationToken);
        }

        string? databasePath = null;
        if (database is not null)
        {
            databasePath = Path.Combine(dataRoot, "database.sql");
            await using var stream = System.IO.File.Create(databasePath);
            await database.CopyToAsync(stream, cancellationToken);
        }

        exam.CollectionFilePath = collectionPath;
        if (databasePath is not null)
        {
            exam.DatabaseFilePath = databasePath;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ExamResourceDto
        {
            ExamId = exam.Id,
            CollectionFilePath = exam.CollectionFilePath ?? collectionPath,
            DatabaseFilePath = exam.DatabaseFilePath,
            UpdatedAt = DateTime.UtcNow
        });
    }

    private static IQueryable<Exam> ApplySort(IQueryable<Exam> query, string? sort, string? order)
    {
        var isDesc = string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase);
        return (sort ?? "examId").ToLowerInvariant() switch
        {
            "name" => isDesc ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            "createdat" => isDesc ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
            "examid" => isDesc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id),
            _ => isDesc ? query.OrderByDescending(e => e.Id) : query.OrderBy(e => e.Id)
        };
    }

    private static bool HasExtension(string fileName, string extension)
    {
        return string.Equals(Path.GetExtension(fileName), extension, StringComparison.OrdinalIgnoreCase);
    }
}
