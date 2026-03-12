using Api_RestAPI_gradingTool.Contracts.Management;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[ApiController]
[Route("api/exams/{examId:int}/resources")]
public sealed class ExamResourcesController : ControllerBase
{
    private const long MaxCollectionSizeBytes = 10 * 1024 * 1024;
    private const long MaxDatabaseSizeBytes = 50 * 1024 * 1024;
    private readonly GradingDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ExamResourcesController(GradingDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost]
    [RequestSizeLimit(MaxDatabaseSizeBytes + MaxCollectionSizeBytes)]
    public async Task<ActionResult<ExamResourceDto>> Upload(
        int examId,
        [FromForm] IFormFile collection,
        [FromForm] IFormFile? database,
        CancellationToken cancellationToken = default)
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

    private static bool HasExtension(string fileName, string extension)
    {
        return string.Equals(Path.GetExtension(fileName), extension, StringComparison.OrdinalIgnoreCase);
    }
}
