using Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Results;

public static class ResultHttpMapper
{
    public static IActionResult ToActionResult(
        this Result result,
        ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException(
                "A successful Result cannot be converted to a failure HTTP response.");
        }

        var statusCode = result.ErrorType switch
        {
            ErrorType.Validation =>
                StatusCodes.Status400BadRequest,

            ErrorType.NotFound =>
                StatusCodes.Status404NotFound,

            ErrorType.Conflict =>
                StatusCodes.Status409Conflict,

            ErrorType.Forbidden =>
                StatusCodes.Status403Forbidden,

            ErrorType.Failure =>
                StatusCodes.Status500InternalServerError,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        var title = result.ErrorType switch
        {
            ErrorType.Validation =>
                "Validation error",

            ErrorType.NotFound =>
                "Resource not found",

            ErrorType.Conflict =>
                "Conflict",

            ErrorType.Forbidden =>
                "Forbidden",

            ErrorType.Failure =>
                "An unexpected error occurred.",

            _ =>
                "An unexpected error occurred."
        };

        var detail = result.ErrorType switch
        {
            ErrorType.Validation =>
                result.Error,

            ErrorType.NotFound =>
                result.Error,

            ErrorType.Conflict =>
                result.Error,

            ErrorType.Forbidden =>
                result.Error,

            ErrorType.Failure =>
                "The server could not complete the request.",

            _ =>
                "The server could not complete the request."
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = controller.HttpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            controller.HttpContext.TraceIdentifier;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes =
            {
                "application/problem+json"
            }
        };
    }
}