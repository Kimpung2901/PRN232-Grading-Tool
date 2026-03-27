using Api_RestAPI_gradingTool.Contracts.Management;
using Application.Contracts.Management;
using Application.Contracts.Common;
using Microsoft.AspNetCore.Mvc;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api/exams")]
public sealed class ExamsController : ApiControllerBase
{
    private readonly IExamService _service;

    public ExamsController(IExamService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ExamDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ListAsync(search, sort, order, page, pageSize, cancellationToken);
        var items = result.Items
            .Select(e => new ExamDto
            {
                ExamId = e.ExamId,
                ExamName = e.ExamName,
                SqlScriptPath = e.SqlScriptPath
            })
            .ToArray();

        return Ok(new PagedResult<ExamDto>
        {
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total,
            Items = items
        });
    }

    [HttpGet("{examId:int}")]
    public async Task<ActionResult<ExamDto>> GetById(
        int examId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _service.GetByIdAsync(examId, cancellationToken);

        if (entity is null)
        {
            return ProblemNotFound("Exam not found.");
        }

        return Ok(new ExamDto
        {
            ExamId = entity.ExamId,
            ExamName = entity.ExamName,
            SqlScriptPath = entity.SqlScriptPath
        });
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ExamDto>> Create(
        [FromForm] CreateExamRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SqlFile is not null && request.SqlFile.Length == 0)
        {
            return ProblemBadRequest("SQL script file is empty.");
        }

        await using var stream = request.SqlFile?.OpenReadStream();
        var result = await _service.CreateAsync(request.ExamName, stream, request.SqlFile?.FileName, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        var dto = new ExamDto
        {
            ExamId = result.Data!.ExamId,
            ExamName = result.Data!.ExamName,
            SqlScriptPath = result.Data!.SqlScriptPath
        };

        return CreatedAtAction(nameof(GetById), new { examId = result.Data!.ExamId }, dto);
    }

    [HttpPatch("{examId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ExamDto>> Patch(
        int examId,
        [FromForm] PatchExamRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SqlFile is not null && request.SqlFile.Length == 0)
        {
            return ProblemBadRequest("SQL script file is empty.");
        }

        await using var stream = request.SqlFile?.OpenReadStream();
        var result = await _service.PatchAsync(examId, request.ExamName, stream, request.SqlFile?.FileName, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new ExamDto
        {
            ExamId = result.Data!.ExamId,
            ExamName = result.Data!.ExamName,
            SqlScriptPath = result.Data!.SqlScriptPath
        });
    }

    [HttpDelete("{examId:int}")]
    public async Task<IActionResult> Delete(int examId, CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAsync(examId, cancellationToken);
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
