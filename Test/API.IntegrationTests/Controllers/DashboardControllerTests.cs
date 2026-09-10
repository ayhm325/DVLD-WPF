using API.IntegrationTests.Infrastructure;
using Application.DTOs;
using Application.Interfaces;
using DVLD.Contracts.Dashboard;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class DashboardControllerTests
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DashboardControllerTests(
        ApiWebApplicationFactory factory)
    {
        _factory = factory;

        _factory.DashboardServiceMock.Reset();

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatistics_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync(
                "/api/Dashboard/statistics");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        _factory.DashboardServiceMock.Verify(
            x => x.GetStatisticsAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetStatistics_WhenSuccessful_ReturnsMappedResponse()
    {
        var dto = new DashboardDto
        {
            TotalPeople = 100,
            TotalDrivers = 80,
            ActiveLicenses = 65,
            PendingApplications = 12,
            LocalDrivingLicenseApplications = 40,
            InternationalLicenses = 15,
            DetainedLicenses = 5,
            UpcomingTests = 7
        };

        _factory.DashboardServiceMock
            .Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(dto);

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<DashboardResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            dto.TotalPeople,
            result.TotalPeople);

        Assert.Equal(
            dto.TotalDrivers,
            result.TotalDrivers);

        Assert.Equal(
            dto.ActiveLicenses,
            result.ActiveLicenses);

        Assert.Equal(
            dto.PendingApplications,
            result.PendingApplications);

        Assert.Equal(
            dto.LocalDrivingLicenseApplications,
            result.LocalDrivingLicenseApplications);

        Assert.Equal(
            dto.InternationalLicenses,
            result.InternationalLicenses);

        Assert.Equal(
            dto.DetainedLicenses,
            result.DetainedLicenses);

        Assert.Equal(
            dto.UpcomingTests,
            result.UpcomingTests);

        _factory.DashboardServiceMock.Verify(
            x => x.GetStatisticsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetStatistics_WhenStatisticsAreZero_ReturnsZeroValues()
    {
        var dto = new DashboardDto
        {
            TotalPeople = 0,
            TotalDrivers = 0,
            ActiveLicenses = 0,
            PendingApplications = 0,
            LocalDrivingLicenseApplications = 0,
            InternationalLicenses = 0,
            DetainedLicenses = 0,
            UpcomingTests = 0
        };

        _factory.DashboardServiceMock
            .Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(dto);

        using var request =
            CreateAuthenticatedRequest();

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<DashboardResponse>();

        Assert.NotNull(result);

        Assert.Equal(0, result.TotalPeople);
        Assert.Equal(0, result.TotalDrivers);
        Assert.Equal(0, result.ActiveLicenses);
        Assert.Equal(0, result.PendingApplications);
        Assert.Equal(
            0,
            result.LocalDrivingLicenseApplications);
        Assert.Equal(
            0,
            result.InternationalLicenses);
        Assert.Equal(
            0,
            result.DetainedLicenses);
        Assert.Equal(
            0,
            result.UpcomingTests);
    }

    private static HttpRequestMessage
        CreateAuthenticatedRequest()
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "/api/Dashboard/statistics");

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
}