using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.CountryDTO;
using Application.Interfaces;
using DVLD.Contracts.Country;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class CountriesControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CountriesControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;

        _factory.CountryServiceMock.Reset();

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync("/api/Countries");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        _factory.CountryServiceMock.Verify(
            x => x.GetAllCountriesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenSuccessful_ReturnsMappedResponses()
    {
        var countries = new List<CountryDto>
        {
            new()
            {
                CountryId = 1,
                CountryName = "Jordan"
            },
            new()
            {
                CountryId = 2,
                CountryName = "United States"
            }
        };

        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
                Result<List<CountryDto>>.Success(
                    countries));

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<CountryResponse>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal(
            1,
            result[0].CountryId);

        Assert.Equal(
            "Jordan",
            result[0].CountryName);

        Assert.Equal(
            2,
            result[1].CountryId);

        Assert.Equal(
            "United States",
            result[1].CountryName);

        _factory.CountryServiceMock.Verify(
            x => x.GetAllCountriesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenValidationFails_ReturnsBadRequest()
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
                Result<List<CountryDto>>
                    .FromValidationFailure(
                        "validation error"));

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.BadRequest,
            "validation error");
    }

    [Fact]
    public async Task GetAll_WhenNotFound_ReturnsNotFound()
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
                Result<List<CountryDto>>
                    .FromNotFound(
                        "countries not found"));

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.NotFound,
            "countries not found");
    }

    [Fact]
    public async Task GetAll_WhenConflictOccurs_ReturnsConflict()
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
                Result<List<CountryDto>>
                    .FromConflict(
                        "conflict"));

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.Conflict,
            "conflict");
    }

    [Fact]
    public async Task GetAll_WhenForbidden_ReturnsForbidden()
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
                Result<List<CountryDto>>
                    .FromForbidden(
                        "access denied"));

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.Forbidden,
            "access denied");
    }

    [Fact]
    public async Task GetAll_WhenUnexpectedFailureOccurs_ReturnsInternalServerError()
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
                Result<List<CountryDto>>
                    .FromFailure(
                        "unexpected failure"));

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        await AssertErrorResponseAsync(
            response,
            HttpStatusCode.InternalServerError,
            "unexpected failure");
    }

    [Fact]
    public async Task GetAll_WhenSuccessfulWithEmptyList_ReturnsEmptyArray()
    {
        _factory.CountryServiceMock
            .Setup(x => x.GetAllCountriesAsync())
            .ReturnsAsync(
                Result<List<CountryDto>>.Success(
                    new List<CountryDto>()));

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    List<CountryResponse>>();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private static HttpRequestMessage
        CreateAuthenticatedRequest()
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/Countries");

        request.Headers.Add(
            "X-Test-User-Id",
            "1");

        request.Headers.Add(
            "X-Test-Username",
            "testuser");

        request.Headers.Add(
            "X-Test-FullName",
            "Test User");

        request.Headers.Add(
            "X-Test-Role",
            "Staff");

        return request;
    }

    private static async Task
        AssertErrorResponseAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatus,
            string expectedError)
    {
        Assert.Equal(
            expectedStatus,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            expectedError,
            body.Error);
    }

    private sealed record ErrorResponse(
        string? Error);
}