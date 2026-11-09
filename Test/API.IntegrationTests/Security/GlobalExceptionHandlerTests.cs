using System.Text.Json;

using Application.Common.Exceptions;

using DVLD.Api.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.IntegrationTests.Security;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task UnexpectedException_ReturnsSafe500ProblemDetails()
    {
        var (handler, context) = CreateContext();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("Secret connection string Server=.;Database=DVLDf"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        var body = await ReadBodyAsync(context);

        Assert.DoesNotContain("DVLDf", body);
        Assert.DoesNotContain("Secret connection string", body);

        using var document = JsonDocument.Parse(body);
        Assert.Equal(500, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("trace-abc", document.RootElement.GetProperty("traceId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task ClientCancellation_Returns499_AndIsNotTreatedAsServerError()
    {
        var (handler, context) = CreateContext(clientAborted: true);

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, context.Response.StatusCode);
        Assert.Null(context.Response.ContentType);
    }

    [Fact]
    public async Task MalformedRequest_Returns400_WithoutLeakingFrameworkMessage()
    {
        var (handler, context) = CreateContext();

        var handled = await handler.TryHandleAsync(
            context,
            new BadHttpRequestException("Failed to read parameter from body.", StatusCodes.Status400BadRequest),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);

        Assert.DoesNotContain("Failed to read parameter", body);
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvalidJson_Returns400()
    {
        var (handler, context) = CreateContext();

        await handler.TryHandleAsync(context, new JsonException("Unexpected token"), CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task DuplicateUsername_Returns409_WithCuratedMessage()
    {
        var (handler, context) = CreateContext();

        await handler.TryHandleAsync(
            context,
            new DuplicateUsernameException(),
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        var body = await ReadBodyAsync(context);
        using var document = JsonDocument.Parse(body);

        Assert.Equal("Conflict", document.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "Username is already in use.",
            document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ResponseAlreadyStarted_ReturnsFalse()
    {
        var (handler, context) = CreateContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
    }

    private sealed class StartedResponseFeature : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }

    private static (GlobalExceptionHandler Handler, DefaultHttpContext Context) CreateContext(
        bool clientAborted = false)
    {
        var context = new DefaultHttpContext
        {
            RequestAborted = new CancellationTokenSource().Token
        };

        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.TraceIdentifier = "trace-abc";
        context.Response.Body = new MemoryStream();

        if (clientAborted)
        {
            var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            context.RequestAborted = cancellation.Token;
        }

        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        return (handler, context);
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;

        using var reader = new StreamReader(
            context.Response.Body,
            System.Text.Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024,
            leaveOpen: true);

        return await reader.ReadToEndAsync();
    }
}
