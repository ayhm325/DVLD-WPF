using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.CountryDTO;
using DVLD.Contracts.Country;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class CountriesControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CountriesControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CountryServiceMock.Reset();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Countries");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _factory.CountryServiceMock.Verify(
            x => x.GetAllCountriesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        var countries = new List<CountryDto>
        {
            new() { CountryId = 1, CountryName = "Jordan" },
            new() { CountryId = 2, CountryName = "United States" }
        };

        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(Result<List<CountryDto>>.Success(countries));

        var response = await _client.SendAsync(CreateAuthenticatedRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<CountryResponse>>();

        Assert.NotNull(result);
        Assert.Collection(
            result,
            country =>
            {
                Assert.Equal(1, country.CountryId);
                Assert.Equal("Jordan", country.CountryName);
            },
            country =>
            {
                Assert.Equal(2, country.CountryId);
                Assert.Equal("United States", country.CountryName);
            });

        _factory.CountryServiceMock.Verify(
            x => x.GetAllCountriesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFails_ReturnsBadRequest()
    {
        SetupFailure(Result<List<CountryDto>>.FromValidationFailure("validation error"));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(CreateAuthenticatedRequest()),
            HttpStatusCode.BadRequest,
            "Validation error",
            "validation error");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        SetupFailure(Result<List<CountryDto>>.FromNotFound("countries not found"));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(CreateAuthenticatedRequest()),
            HttpStatusCode.NotFound,
            "Resource not found",
            "countries not found");
    }

    [Fact]
    public async Task GetAll_WhenConflictOccurs_ReturnsConflict()
    {
        SetupFailure(Result<List<CountryDto>>.FromConflict("conflict"));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(CreateAuthenticatedRequest()),
            HttpStatusCode.Conflict,
            "Conflict",
            "conflict");
    }

    [Fact]
    public async Task GetAll_WhenForbidden_ReturnsForbidden()
    {
        SetupFailure(Result<List<CountryDto>>.FromForbidden("access denied"));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(CreateAuthenticatedRequest()),
            HttpStatusCode.Forbidden,
            "Forbidden",
            "access denied");
    }

    [Fact]
    public async Task GetAll_WhenUnexpectedFailureOccurs_ReturnsInternalServerError()
    {
        SetupFailure(Result<List<CountryDto>>.FromFailure("unexpected failure"));

        await AssertProblemDetailsAsync(
            await _client.SendAsync(CreateAuthenticatedRequest()),
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetAll_WhenSuccessfulWithEmptyList_ReturnsEmptyArray()
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(Result<List<CountryDto>>.Success([]));

        var response = await _client.SendAsync(CreateAuthenticatedRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<List<CountryResponse>>();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private void SetupFailure(Result<List<CountryDto>> result)
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(result);
    }

    private static HttpRequestMessage CreateAuthenticatedRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Countries");

        request.Headers.Add("X-Test-User-Id", "1");
        request.Headers.Add("X-Test-Username", "testuser");
        request.Headers.Add("X-Test-FullName", "Test User");
        request.Headers.Add("X-Test-Role", "Staff");

        return request;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedTitle,
        string expectedDetail)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal(
            (int)expectedStatus,
            body.GetProperty("status").GetInt32());

        Assert.Equal(
            expectedTitle,
            body.GetProperty("title").GetString());

        Assert.Equal(
            expectedDetail,
            body.GetProperty("detail").GetString());

        Assert.Equal(
            "/api/Countries",
            body.GetProperty("instance").GetString());

        Assert.True(
            body.TryGetProperty("traceId", out var traceId));

        Assert.False(
            string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}