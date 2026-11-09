using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using API.IntegrationTests.Infrastructure;

using Moq;

namespace API.IntegrationTests.Security;

/// <summary>
/// End-to-end contract tests for the RFC 7807 envelopes produced by middleware,
/// model binding, and the exception boundary — paths that execute outside controllers.
/// </summary>
public sealed class ProblemDetailsContractTests
{
    [Fact]
    public async Task Unauthenticated_Request_Returns401ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(401, problem.GetProperty("status").GetInt32());
        Assert.Equal("Unauthorized", problem.GetProperty("title").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task AuthenticatedButForbidden_Returns403ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = Authenticated(factory, userId: 10, role: "Staff");

        var response = await client.GetAsync("/api/Users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(403, problem.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", problem.GetProperty("title").GetString());
        Assert.Equal(
            "You do not have permission to perform this action.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnknownRoute_Returns404ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = Authenticated(factory, userId: 10, role: "Staff");

        var response = await client.GetAsync("/api/this-route-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(404, problem.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", problem.GetProperty("title").GetString());
        Assert.Equal(
            "The requested resource was not found.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UnsupportedMethod_Returns405ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = Authenticated(factory, userId: 10, role: "Staff");

        var response = await client.PostAsync("/api/Countries", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(405, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task MalformedJson_Returns400ValidationProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new StringContent("{not-json", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("errors", out var errors));
        Assert.NotEqual(0, errors.EnumerateObject().Count());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task UnexpectedException_ReturnsSafe500ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.PersonServiceMock
            .Setup(x => x.GetAllPeopleAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "leaked-internal-detail Server=.;Database=DVLDf"));

        using var client = Authenticated(factory, userId: 10, role: "Staff");

        var response = await client.GetAsync("/api/People");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(500, problem.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "The server could not complete the request.",
            problem.GetProperty("detail").GetString());

        var raw = problem.GetRawText();
        Assert.DoesNotContain("leaked-internal-detail", raw);
        Assert.DoesNotContain("DVLDf", raw);
    }

    [Fact]
    public async Task TraceId_IsUniquePerRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await ReadProblemAsync(await client.GetAsync("/api/Auth/me"));
        var second = await ReadProblemAsync(await client.GetAsync("/api/Auth/me"));

        var firstTraceId = first.GetProperty("traceId").GetString();
        var secondTraceId = second.GetProperty("traceId").GetString();

        Assert.False(string.IsNullOrWhiteSpace(firstTraceId));
        Assert.False(string.IsNullOrWhiteSpace(secondTraceId));
        Assert.NotEqual(firstTraceId, secondTraceId);
    }

    [Fact]
    public async Task SuccessfulResponse_IsNotProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = Authenticated(factory, userId: 10, role: "Staff");

        var response = await client.GetAsync("/api/Auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("userId", out _));
    }

    private static HttpClient Authenticated(
        ApiWebApplicationFactory factory,
        int userId,
        string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
