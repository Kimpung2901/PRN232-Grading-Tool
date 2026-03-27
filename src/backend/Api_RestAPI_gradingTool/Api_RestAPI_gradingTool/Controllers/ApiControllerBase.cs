using Microsoft.AspNetCore.Mvc;

namespace Api_RestAPI_gradingTool.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult ProblemBadRequest(string detail)
    {
        return Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest, title: "Bad Request");
    }

    protected ActionResult ProblemNotFound(string detail)
    {
        return Problem(detail: detail, statusCode: StatusCodes.Status404NotFound, title: "Not Found");
    }

    protected ActionResult ProblemConflict(string detail)
    {
        return Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");
    }
}
