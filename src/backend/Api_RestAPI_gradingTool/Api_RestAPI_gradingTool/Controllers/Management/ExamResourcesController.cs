using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api/exams")]
public sealed class ExamResourcesController : ApiControllerBase
{
    private const long MaxCollectionSizeBytes = 10 * 1024 * 1024;
    private const long MaxSpecSizeBytes = 2 * 1024 * 1024;
    private const long MaxEnvSizeBytes = 2 * 1024 * 1024;
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
                EndpointSpecFilePath = e.EndpointSpecFilePath,
                EnvironmentFilePath = e.EnvironmentFilePath,
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
    [RequestSizeLimit(MaxDatabaseSizeBytes + MaxCollectionSizeBytes + MaxSpecSizeBytes)]
    public Task<ActionResult<ExamResourceDto>> Create(
        int examId,
        [FromForm] ExamResourceUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        return UploadInternal(examId, request.Collection, request.Database, request.EndpointSpec, request.Environment, cancellationToken);
    }

    [HttpPut("{examId:int}/resources")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDatabaseSizeBytes + MaxCollectionSizeBytes + MaxSpecSizeBytes)]
    public Task<ActionResult<ExamResourceDto>> Replace(
        int examId,
        [FromForm] ExamResourceUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        return UploadInternal(examId, request.Collection, request.Database, request.EndpointSpec, request.Environment, cancellationToken);
    }

    [HttpGet("{examId:int}/resources")]
    public async Task<ActionResult<ExamResourceDto>> Get(
        int examId,
        CancellationToken cancellationToken = default)
    {
        var exam = await _db.Exams.AsNoTracking().FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        if (string.IsNullOrWhiteSpace(exam.CollectionFilePath))
        {
            return ProblemNotFound("Resources not found.");
        }

            return Ok(new ExamResourceDto
        {
            ExamId = exam.Id,
            CollectionFilePath = exam.CollectionFilePath,
            DatabaseFilePath = exam.DatabaseFilePath,
            EndpointSpecFilePath = exam.EndpointSpecFilePath,
            EnvironmentFilePath = exam.EnvironmentFilePath,
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
            return ProblemNotFound("Exam not found.");
        }

        if (!string.IsNullOrWhiteSpace(exam.CollectionFilePath) && System.IO.File.Exists(exam.CollectionFilePath))
        {
            System.IO.File.Delete(exam.CollectionFilePath);
        }

        if (!string.IsNullOrWhiteSpace(exam.DatabaseFilePath) && System.IO.File.Exists(exam.DatabaseFilePath))
        {
            System.IO.File.Delete(exam.DatabaseFilePath);
        }

        if (!string.IsNullOrWhiteSpace(exam.EndpointSpecFilePath) && System.IO.File.Exists(exam.EndpointSpecFilePath))
        {
            System.IO.File.Delete(exam.EndpointSpecFilePath);
        }

        if (!string.IsNullOrWhiteSpace(exam.EnvironmentFilePath) && System.IO.File.Exists(exam.EnvironmentFilePath))
        {
            System.IO.File.Delete(exam.EnvironmentFilePath);
        }

        exam.CollectionFilePath = null;
        exam.DatabaseFilePath = null;
        exam.EndpointSpecFilePath = null;
        exam.EnvironmentFilePath = null;

        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<ActionResult<ExamResourceDto>> UploadInternal(
        int examId,
        IFormFile collection,
        IFormFile? database,
        IFormFile? endpointSpec,
        IFormFile? environment,
        CancellationToken cancellationToken)
    {
        if (collection is null || collection.Length == 0)
        {
            return ProblemBadRequest("collection.json is required.");
        }

        if (!HasExtension(collection.FileName, ".json"))
        {
            return ProblemBadRequest("collection must be a .json file.");
        }

        if (collection.Length > MaxCollectionSizeBytes)
        {
            return ProblemBadRequest("collection.json is too large.");
        }

        if (database is not null)
        {
            if (!HasExtension(database.FileName, ".sql"))
            {
                return ProblemBadRequest("database must be a .sql file.");
            }

            if (database.Length > MaxDatabaseSizeBytes)
            {
                return ProblemBadRequest("database.sql is too large.");
            }
        }

        if (endpointSpec is not null)
        {
            if (!HasExtension(endpointSpec.FileName, ".json"))
            {
                return ProblemBadRequest("endpoint-spec must be a .json file.");
            }

            if (endpointSpec.Length > MaxSpecSizeBytes)
            {
                return ProblemBadRequest("endpoint-spec.json is too large.");
            }
        }

        if (environment is not null)
        {
            if (!HasExtension(environment.FileName, ".json"))
            {
                return ProblemBadRequest("environment must be a .json file.");
            }

            if (environment.Length > MaxEnvSizeBytes)
            {
                return ProblemBadRequest("environment.json is too large.");
            }
        }

        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);
        if (exam is null)
        {
            return ProblemNotFound("Exam not found.");
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

        string? endpointSpecPath = null;
        if (endpointSpec is not null)
        {
            endpointSpecPath = Path.Combine(dataRoot, "endpoint-spec.json");
            await using var stream = System.IO.File.Create(endpointSpecPath);
            await endpointSpec.CopyToAsync(stream, cancellationToken);
        }

        string? environmentPath = null;
        if (environment is not null)
        {
            environmentPath = Path.Combine(dataRoot, "environment.json");
            await using var stream = System.IO.File.Create(environmentPath);
            await environment.CopyToAsync(stream, cancellationToken);
        }

        exam.CollectionFilePath = collectionPath;
        if (databasePath is not null)
        {
            exam.DatabaseFilePath = databasePath;
        }
        if (endpointSpecPath is not null)
        {
            exam.EndpointSpecFilePath = endpointSpecPath;
        }
        if (environmentPath is not null)
        {
            exam.EnvironmentFilePath = environmentPath;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new ExamResourceDto
        {
            ExamId = exam.Id,
            CollectionFilePath = exam.CollectionFilePath ?? collectionPath,
            DatabaseFilePath = exam.DatabaseFilePath,
            EndpointSpecFilePath = exam.EndpointSpecFilePath,
            EnvironmentFilePath = exam.EnvironmentFilePath,
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
