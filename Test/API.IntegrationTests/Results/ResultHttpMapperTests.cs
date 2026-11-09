using Application.Common.Results;

using DVLD.Api.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace API.IntegrationTests.Results;

public sealed class ResultHttpMapperTests
{
    [Fact]
    public void ValidationFailure_MapsToValidationProblemDetails()
    {
        var (controller, httpContext) = CreateController();

        var actionResult = Result
            .ValidationFailure("First error.\nSecond error.")
            .ToActionResult(controller);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        var problem = Assert.IsType<ValidationProblemDetails>(objectResult.Value);

        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Validation error", problem.Title);
        Assert.Equal("First error.\nSecond error.", problem.Detail);
        Assert.Equal(ProblemTypes.Validation, problem.Type);
        Assert.Equal("/api/test", problem.Instance);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
        Assert.Contains(ApiProblemDetails.ContentType, objectResult.ContentTypes);

        var messages = problem.Errors[ResultHttpMapper.ValidationErrorKey];
        Assert.Equal(2, messages.Length);
        Assert.Contains("First error.", messages);
        Assert.Contains("Second error.", messages);
    }

    [Fact]
    public void ValidationFailure_WithoutMessage_StillProducesStructuredErrors()
    {
        var (controller, _) = CreateController();

        var actionResult = Result.ValidationFailure(" ").ToActionResult(controller);

        var problem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<ObjectResult>(actionResult).Value);

        var messages = problem.Errors[ResultHttpMapper.ValidationErrorKey];
        Assert.Single(messages);
        Assert.Equal("The request is invalid.", messages[0]);
    }

    [Fact]
    public void NotFound_MapsTo404()
    {
        var problem = Map(Result.NotFound("Application not found."), out var objectResult);

        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("Resource not found", problem.Title);
        Assert.Equal("Application not found.", problem.Detail);
        Assert.Equal(ProblemTypes.NotFound, problem.Type);
    }

    [Fact]
    public void Conflict_MapsTo409()
    {
        var problem = Map(Result.Conflict("Already completed."), out var objectResult);

        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal("Already completed.", problem.Detail);
        Assert.Equal(ProblemTypes.Conflict, problem.Type);
    }

    [Fact]
    public void Forbidden_MapsTo403()
    {
        var problem = Map(Result.Forbidden("Administrator privileges are required."), out var objectResult);

        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        Assert.Equal("Forbidden", problem.Title);
        Assert.Equal("Administrator privileges are required.", problem.Detail);
        Assert.Equal(ProblemTypes.Forbidden, problem.Type);
    }

    [Fact]
    public void Unauthorized_MapsTo401()
    {
        var problem = Map(Result.Unauthorized("Invalid username or password."), out var objectResult);

        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.Equal("Invalid username or password.", problem.Detail);
        Assert.Equal(ProblemTypes.Unauthorized, problem.Type);
    }

    [Fact]
    public void Failure_MapsTo500_WithoutLeakingInternalMessage()
    {
        var problem = Map(
            Result.Failure("SqlException: connection string Server=.;Database=DVLDf"),
            out var objectResult);

        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal("The server could not complete the request.", problem.Detail);
        Assert.DoesNotContain("DVLDf", problem.Detail);
        Assert.Equal(ProblemTypes.Internal, problem.Type);
    }

    [Fact]
    public void GenericUnauthorizedResult_MapsTo401()
    {
        var problem = Map(
            Result<int>.FromUnauthorized("Invalid credentials."),
            out var objectResult);

        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        Assert.Equal("Invalid credentials.", problem.Detail);
    }

    [Fact]
    public void SuccessfulResult_DoesNotThrow_AndFailsClosed()
    {
        var (controller, _) = CreateController();

        IActionResult? actionResult = null;

        var exception = Record.Exception(
            () => actionResult = Result.Success().ToActionResult(controller));

        Assert.Null(exception);

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);

        // A success passed to the failure mapper must never surface as a 2xx response.
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal("An unexpected error occurred.", problem.Title);
    }

    [Fact]
    public void Mapper_PreservesInstanceAndTraceIdForEveryCategory()
    {
        var (controller, _) = CreateController();

        foreach (var result in new[]
                 {
                     Result.ValidationFailure("v"),
                     Result.Unauthorized("u"),
                     Result.Forbidden("f"),
                     Result.NotFound("n"),
                     Result.Conflict("c"),
                     Result.Failure("x")
                 })
        {
            var objectResult = Assert.IsType<ObjectResult>(result.ToActionResult(controller));
            var problem = Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);

            Assert.Equal("/api/test", problem.Instance);
            Assert.Equal("trace-123", problem.Extensions["traceId"]);
            Assert.False(string.IsNullOrWhiteSpace(problem.Type));
            Assert.Contains(ApiProblemDetails.ContentType, objectResult.ContentTypes);
        }
    }

    private static ProblemDetails Map(Result result, out ObjectResult objectResult)
    {
        var (controller, _) = CreateController();
        objectResult = Assert.IsType<ObjectResult>(result.ToActionResult(controller));
        return Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);
    }

    private static (ControllerBase Controller, DefaultHttpContext HttpContext) CreateController()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        httpContext.Request.Path = "/api/test";
        httpContext.TraceIdentifier = "trace-123";

        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return (controller, httpContext);
    }

    private sealed class TestController : ControllerBase
    {
    }
}
