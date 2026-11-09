using Application.Common.Results;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DVLD.Api.Results;

/// <summary>
/// Converts application-layer <see cref="Result"/> outcomes into RFC 7807 HTTP responses.
/// This is the only place where a <see cref="ErrorType"/> becomes an HTTP status code.
/// </summary>
public static class ResultHttpMapper
{
    /// <summary>
    /// Error key used for validation messages that are not bound to a specific field.
    /// </summary>
    public const string ValidationErrorKey = "request";

    public static IActionResult ToActionResult(
        this Result result,
        ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);

        var httpContext = controller.HttpContext;

        if (result.IsSuccess)
        {
            // A successful Result must never be converted into a failure response by
            // accident, and it must never throw. Treat it as a programming error:
            // log it and return a generic 500 instead of leaking the success payload
            // or crashing the request pipeline.
            LogSuccessPassedToFailureMapper(controller);

            return Failure(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "The server could not complete the request.",
                ProblemTypes.Internal);
        }

        return result.ErrorType switch
        {
            ErrorType.Validation => ValidationFailure(httpContext, result.Error),

            ErrorType.Unauthorized => Failure(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                result.Error,
                ProblemTypes.Unauthorized),

            ErrorType.Forbidden => Failure(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                result.Error,
                ProblemTypes.Forbidden),

            ErrorType.NotFound => Failure(
                httpContext,
                StatusCodes.Status404NotFound,
                "Resource not found",
                result.Error,
                ProblemTypes.NotFound),

            ErrorType.Conflict => Failure(
                httpContext,
                StatusCodes.Status409Conflict,
                "Conflict",
                result.Error,
                ProblemTypes.Conflict),

            // ErrorType.Failure (and any undefined value) is an unexpected server-side
            // condition. Log the internal reason; never echo it to the client.
            _ => UnexpectedFailure(httpContext, result.Error)
        };
    }

    private static IActionResult UnexpectedFailure(
        HttpContext httpContext,
        string internalMessage)
    {
        GetLogger(httpContext)?.LogError(
            "A use case returned an unrecoverable failure with no client-safe detail: {Error}",
            internalMessage);

        return Failure(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.",
            ProblemTypes.Internal);
    }

    private static IActionResult ValidationFailure(
        HttpContext httpContext,
        string error)
    {
        var messages = SplitValidationMessages(error);

        var problemDetails = ApiProblemDetails.CreateValidation(
            httpContext,
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [ValidationErrorKey] = messages
            },
            title: "Validation error");

        // Preserve the aggregated message for clients that display a single detail,
        // while also exposing the structured, field-addressable errors collection.
        problemDetails.Detail = error;

        return ApiProblemDetails.ToActionResult(httpContext, problemDetails);
    }

    private static IActionResult Failure(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string type) =>
        ApiProblemDetails.ToActionResult(
            httpContext,
            ApiProblemDetails.Create(httpContext, statusCode, title, detail, type));

    private static string[] SplitValidationMessages(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return ["The request is invalid."];
        }

        var messages = error
            .Split(
                ["\r\n", "\n"],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return messages.Length == 0
            ? [error.Trim()]
            : messages;
    }

    private static void LogSuccessPassedToFailureMapper(
        ControllerBase controller)
    {
        GetLogger(controller.HttpContext)?.LogError(
            "A successful Result was passed to ResultHttpMapper.ToActionResult by controller {Controller}. " +
            "A generic HTTP 500 response was returned instead of throwing.",
            controller.GetType().Name);
    }

    private static ILogger? GetLogger(HttpContext httpContext) =>
        (httpContext.RequestServices.GetService(typeof(ILoggerFactory))
            as ILoggerFactory)
        ?.CreateLogger("DVLD.Api.Results.ResultHttpMapper");
}
