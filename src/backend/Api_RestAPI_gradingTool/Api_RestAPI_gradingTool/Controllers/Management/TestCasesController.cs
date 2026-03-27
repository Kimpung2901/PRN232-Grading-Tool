using Api_RestAPI_gradingTool.Contracts.Management;
using Application.Contracts.Common;
using Application.Contracts.Management;
using Microsoft.AspNetCore.Mvc;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api/exams/{examId:int}/testcases")]
public sealed class TestCasesController : ApiControllerBase
{
    private readonly ITestCaseService _service;

    public TestCasesController(ITestCaseService service)
    {
        _service = service;
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
        var result = await _service.ListAsync(examId, search, sort, order, page, pageSize, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        var items = result.Data!.Items
            .Select(t => new TestCaseDto
            {
                Id = t.Id,
                ExamId = t.ExamId,
                FilePath = t.FilePath
            })
            .ToArray();

        return Ok(new PagedResult<TestCaseDto>
        {
            Page = result.Data!.Page,
            PageSize = result.Data!.PageSize,
            Total = result.Data!.Total,
            Items = items
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TestCaseDto>> GetById(
        int examId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _service.GetByIdAsync(examId, id, cancellationToken);

        if (entity is null)
        {
            return ProblemNotFound("TestCase not found.");
        }

        return Ok(new TestCaseDto
        {
            Id = entity.Id,
            ExamId = entity.ExamId,
            FilePath = entity.FilePath
        });
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TestCaseDto>> UploadCollection(
        int examId,
        [FromForm] TestCaseCollectionUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return ProblemBadRequest("Collection file is required.");
        }

        await using var stream = request.File.OpenReadStream();
        var result = await _service.UploadCollectionAsync(examId, stream, request.File.FileName, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new TestCaseDto
        {
            Id = result.Data!.Id,
            ExamId = result.Data!.ExamId,
            FilePath = result.Data!.FilePath
        });
    }

    [HttpPatch("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TestCaseDto>> Patch(
        int examId,
        int id,
        [FromForm] PatchTestCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.File is not null && request.File.Length == 0)
        {
            return ProblemBadRequest("Collection file is empty.");
        }

        await using var stream = request.File?.OpenReadStream();
        var result = await _service.PatchAsync(examId, id, request.FilePath, stream, request.File?.FileName, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new TestCaseDto
        {
            Id = result.Data!.Id,
            ExamId = result.Data!.ExamId,
            FilePath = result.Data!.FilePath
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int examId,
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAsync(examId, id, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        return NoContent();
    }

    private ActionResult MapError<T>(ServiceResult<T> result)
    {
        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => ProblemNotFound(result.Error ?? "Not found."),
            ServiceErrorType.Conflict => ProblemConflict(result.Error ?? "Conflict."),
            _ => ProblemBadRequest(result.Error ?? "Bad request.")
        };
    }
}
