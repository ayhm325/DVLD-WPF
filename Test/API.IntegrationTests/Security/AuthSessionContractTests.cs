using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using API.IntegrationTests.Infrastructure;

using Application.Common.Results;
using Application.DTOs.AuthDTO;

using Domain.Enums;

using Moq;

namespace API.IntegrationTests.Security;

/// <summary>
/// Contract tests for the refresh-cookie transport and the authentication endpoints.
/// The application service is mocked, so these tests are database-free.
/// </summary>
public sealed class AuthSessionContractTests
{
    private const string CsrfHeader = "X-CSRF-Protection";
    private const string RefreshCookie = "dvld_rt";

    [Fact]
    public async Task Login_SetsHttpOnlyRefreshCookie_AndNeverReturnsRefreshTokenInJson()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(
                It.IsAny<Application.DTOs.UserDTO.LoginRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponseDto>.Success(Session("raw-refresh")));

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/Auth/login",
            new { UserName = "admin", Password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var setCookie = GetSetCookie(response);
        Assert.Contains($"{RefreshCookie}=raw-refresh", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.True(document.RootElement.TryGetProperty("accessToken", out _));
        Assert.False(document.RootElement.TryGetProperty("refreshToken", out _));
        Assert.False(document.RootElement.TryGetProperty("refreshTokenExpiresAtUtc", out _));
    }

    [Fact]
    public async Task Refresh_WithoutCsrfHeader_ReturnsValidationProblem()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookie}=old-refresh");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Refresh_WithCookie_RotatesCookieAndReturnsAccessToken()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.RefreshAsync("old-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponseDto>.Success(Session("new-refresh")));

        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookie}=old-refresh");
        request.Headers.Add(CsrfHeader, "1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var setCookie = GetSetCookie(response);
        Assert.Contains($"{RefreshCookie}=new-refresh", setCookie);

        var body = await response.Content
            .ReadFromJsonAsync<DVLD.Contracts.Auth.LoginResponse>();

        Assert.NotNull(body);
        Assert.Equal("access-token", body.AccessToken);

        factory.AuthServiceMock.Verify(
            x => x.RefreshAsync("old-refresh", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenUnauthorized_ClearsCookieAndReturns401ProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result<LoginResponseDto>.FromUnauthorized(
                    "The authentication session is invalid or has expired."));

        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookie}=stale");
        request.Headers.Add(CsrfHeader, "1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var setCookie = GetSetCookie(response);
        Assert.Contains($"{RefreshCookie}=", setCookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_WithCookie_RevokesAndClearsCookie()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.LogoutAsync("current-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout");
        request.Headers.Add("Cookie", $"{RefreshCookie}=current-refresh");
        request.Headers.Add(CsrfHeader, "1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var setCookie = GetSetCookie(response);
        Assert.Contains($"{RefreshCookie}=", setCookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", setCookie, StringComparison.OrdinalIgnoreCase);

        factory.AuthServiceMock.Verify(
            x => x.LogoutAsync("current-refresh", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAll_WithoutAuthentication_Returns401()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout-all");
        request.Headers.Add(CsrfHeader, "1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WhenAuthenticated_RevokesEverySession()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.LogoutAllAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        factory.CurrentUserServiceMock
            .SetupGet(x => x.UserId)
            .Returns(10);
        factory.CurrentUserServiceMock
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "10");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Staff");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Auth/logout-all");
        request.Headers.Add(CsrfHeader, "1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.AuthServiceMock.Verify(
            x => x.LogoutAllAsync(10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static LoginResponseDto Session(string refreshToken) =>
        new()
        {
            AccessToken = "access-token",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
            UserId = 10,
            UserName = "admin",
            PersonId = 20,
            FullName = "Admin User",
            Role = UserRole.Staff,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };

    private static string GetSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? string.Join(" | ", values)
            : string.Empty;
}
