using Api_RestAPI_gradingTool.Contracts.Management;
using Application.Contracts.Common;
using Application.Contracts.Management;
using Microsoft.AspNetCore.Mvc;

namespace Api_RestAPI_gradingTool.Controllers.Management;

[Route("api")]
public sealed class SubmissionsController : ApiControllerBase
{
    private readonly ISubmissionService _service;

    public SubmissionsController(ISubmissionService service)
    {
        _service = service;
    }

    [HttpGet("exams/{examId:int}/submissions")]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> ListByExam(
        int examId,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.ListByExamAsync(examId, search, sort, order, page, pageSize, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        var items = result.Data!.Items
            .Select(s => new SubmissionDto
            {
                Id = s.Id,
                ExamId = s.ExamId,
                StudentName = s.StudentName,
                StudentCode = s.StudentCode,
                FilePath = s.FilePath,
                Status = s.Status
            })
            .ToArray();

        return Ok(new PagedResult<SubmissionDto>
        {
            Page = result.Data!.Page,
            PageSize = result.Data!.PageSize,
            Total = result.Data!.Total,
            Items = items
        });
    }

    [HttpGet("submissions/{id:int}")]
    public async Task<ActionResult<SubmissionDto>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var submission = await _service.GetByIdAsync(id, cancellationToken);

        if (submission is null)
        {
            return ProblemNotFound("Submission not found.");
        }

        return Ok(new SubmissionDto
        {
            Id = submission.Id,
            ExamId = submission.ExamId,
            StudentName = submission.StudentName,
            StudentCode = submission.StudentCode,
            FilePath = submission.FilePath,
            Status = submission.Status
        });
    }

    [HttpPost("exams/{examId:int}/submissions")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<SubmissionDto>> Upload(
        int examId,
        [FromForm] SubmissionUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return ProblemBadRequest("Submission file is required.");
        }

        await using var stream = request.File.OpenReadStream();
        var result = await _service.UploadAsync(examId, request.StudentName, request.StudentCode, stream, request.File.FileName, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, new SubmissionDto
        {
            Id = result.Data!.Id,
            ExamId = result.Data!.ExamId,
            StudentName = result.Data!.StudentName,
            StudentCode = result.Data!.StudentCode,
            FilePath = result.Data!.FilePath,
            Status = result.Data!.Status
        });
    }

    [HttpPatch("submissions/{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<SubmissionDto>> Patch(
        int id,
        [FromForm] PatchSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.File is not null && request.File.Length == 0)
        {
            return ProblemBadRequest("Submission file is empty.");
        }

        await using var stream = request.File?.OpenReadStream();
        var result = await _service.PatchAsync(id, request.StudentName, request.StudentCode, stream, request.File?.FileName, cancellationToken);
        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new SubmissionDto
        {
            Id = result.Data!.Id,
            ExamId = result.Data!.ExamId,
            StudentName = result.Data!.StudentName,
            StudentCode = result.Data!.StudentCode,
            FilePath = result.Data!.FilePath,
            Status = result.Data!.Status
        });
    }

    [HttpDelete("submissions/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
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
