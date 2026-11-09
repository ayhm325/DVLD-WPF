using Microsoft.AspNetCore.Mvc;

namespace DVLD.Api.Results;

/// <summary>
/// Stable RFC 7807 <c>type</c> identifiers exposed by the DVLD API.
/// </summary>
public static class ProblemTypes
{
    public const string Validation = "urn:dvld:error:validation";
    public const string Unauthorized = "urn:dvld:error:unauthorized";
    public const string Forbidden = "urn:dvld:error:forbidden";
    public const string NotFound = "urn:dvld:error:not-found";
    public const string Conflict = "urn:dvld:error:conflict";
    public const string MethodNotAllowed = "urn:dvld:error:method-not-allowed";
    public const string UnsupportedMediaType = "urn:dvld:error:unsupported-media-type";
    public const string TooManyRequests = "urn:dvld:error:too-many-requests";
    public const string Internal = "urn:dvld:error:internal";

    public static string ForStatusCode(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => Validation,
            StatusCodes.Status401Unauthorized => Unauthorized,
            StatusCodes.Status403Forbidden => Forbidden,
            StatusCodes.Status404NotFound => NotFound,
            StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
            StatusCodes.Status409Conflict => Conflict,
            StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
            StatusCodes.Status429TooManyRequests => TooManyRequests,
            >= 500 => Internal,
            _ => $"urn:dvld:error:http-{statusCode}"
        };
}

/// <summary>
/// Single construction point for every <see cref="ProblemDetails"/> payload the API emits.
/// </summary>
public static class ApiProblemDetails
{
    public const string ContentType = "application/problem+json";

    public static ProblemDetails Create(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string? type = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = type ?? ProblemTypes.ForStatusCode(statusCode)
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        return problem;
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext context,
        IDictionary<string, string[]> errors,
        string title = "One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(errors);

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title,
            Instance = context.Request.Path,
            Type = ProblemTypes.Validation
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        return problem;
    }

    /// <summary>
    /// Wraps a <see cref="ProblemDetails"/> in an MVC result that always serializes as
    /// <c>application/problem+json</c>.
    /// </summary>
    public static IActionResult ToActionResult(
        HttpContext context,
        ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(problem);

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ContentType }
        };
    }

    /// <summary>
    /// Safe, non-revealing detail text for responses that have no domain message,
    /// such as middleware-generated 401/403/404 responses.
    /// </summary>
    public static string DefaultDetailForStatusCode(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status401Unauthorized =>
                "Authentication is required to access this resource.",
            StatusCodes.Status403Forbidden =>
                "You do not have permission to perform this action.",
            StatusCodes.Status404NotFound =>
                "The requested resource was not found.",
            StatusCodes.Status405MethodNotAllowed =>
                "The HTTP method is not supported for this resource.",
            StatusCodes.Status415UnsupportedMediaType =>
                "The request media type is not supported.",
            StatusCodes.Status429TooManyRequests =>
                "Too many requests were received. Please retry later.",
            _ => "The request could not be completed."
        };
}
