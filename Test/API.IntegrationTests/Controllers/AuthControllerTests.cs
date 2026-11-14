using API.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Domain.Enums;
using DVLD.Contracts.User;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.IntegrationTests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Me_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        var response = await factory.CreateClient().GetAsync("/api/Auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        var response = await factory.CreateClient().GetAsync("/api/Auth/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenAuthenticated_ReturnsCurrentUserProfile()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        factory.UserServiceMock
            .Setup(x => x.GetCurrentProfileAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserProfileDto>.Success(new()
            {
                UserId = 10,
                PersonId = 20,
                UserName = "testuser",
                IsActive = true,
                FullName = "Test User",
                NationalNo = "N123",
                FirstName = "Test",
                SecondName = "User",
                LastName = "User",
                DateOfBirth = new DateTime(1993, 1, 1),
                Gender = 1,
                Address = "Test Address",
                Phone = "0790000000",
                Email = "test@example.com",
                NationalityCountryID = 1,
                CountryName = "Jordan"
            }));

        var response = await Authenticated(factory, 10)
            .GetAsync("/api/Auth/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<UserProfileResponse>();

        Assert.NotNull(body);
        Assert.Equal(10, body.UserId);
        Assert.Equal(20, body.PersonId);
        Assert.Equal("testuser", body.UserName);
        Assert.True(body.IsActive);
        Assert.Equal("Test User", body.FullName);
        Assert.Equal("N123", body.NationalNo);
        Assert.Equal("Test Address", body.Address);
        Assert.Equal("0790000000", body.Phone);
        Assert.Equal("test@example.com", body.Email);
        Assert.Equal("Jordan", body.CountryName);

        factory.UserServiceMock.Verify(
            x => x.GetCurrentProfileAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Profile_WhenUserNotFound_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        factory.UserServiceMock
            .Setup(x => x.GetCurrentProfileAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result<UserProfileDto>.FromNotFound("User was not found."));

        var response = await Authenticated(factory, 10)
            .GetAsync("/api/Auth/profile");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.NotFound,
            "Resource not found",
            "User was not found.");
    }

    [Fact]
    public async Task Me_WhenAuthenticated_ReturnsOkWithUserInformation()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        var response = await Authenticated(factory, 10)
            .GetAsync("/api/Auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.NotNull(body);
        Assert.Equal(10, body.UserId);
        Assert.Equal("testuser", body.Username);
        Assert.Equal("Test User", body.FullName);
        Assert.Equal("Staff", body.Role);
    }

    [Fact]
    public async Task Login_WhenAuthServiceReturnsValidationFailure_ReturnsValidationProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        const string error = "Username is required.\nPassword is required.";

        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result<LoginResponseDto>.FromValidationFailure(error));

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/Auth/login",
            new { UserName = "", Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("Validation error", body.GetProperty("title").GetString());
        Assert.Equal(error, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("instance").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));

        var errors = body.GetProperty("errors").GetProperty("request");
        var messages = errors.EnumerateArray().Select(e => e.GetString()).ToList();

        Assert.Equal(2, messages.Count);
        Assert.Contains("Username is required.", messages);
        Assert.Contains("Password is required.", messages);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorizedProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result<LoginResponseDto>.FromUnauthorized("Invalid username or password."));

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/Auth/login",
            new { UserName = "wronguser", Password = "wrongpassword" });

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.Unauthorized,
            "Unauthorized",
            "Invalid username or password.");
    }

    [Fact]
    public async Task Login_WhenAuthServiceSucceeds_ReturnsOkWithCompleteResponse()
    {
        await using var factory = new ApiWebApplicationFactory();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponseDto>.Success(new()
            {
                AccessToken = "test-access-token",
                ExpiresAtUtc = expiresAt,
                UserId = 10,
                UserName = "testuser",
                PersonId = 20,
                FullName = "Test User",
                Role = UserRole.Staff
            }));

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/Auth/login",
            new { UserName = "testuser", Password = "Password123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<DVLD.Contracts.Auth.LoginResponse>();

        Assert.NotNull(body);
        Assert.Equal("test-access-token", body.AccessToken);
        Assert.Equal(expiresAt, body.ExpiresAtUtc);
        Assert.Equal(10, body.UserId);
        Assert.Equal("testuser", body.UserName);
        Assert.Equal(20, body.PersonId);
        Assert.Equal("Test User", body.FullName);
        Assert.Equal("Staff", body.Role);

        factory.AuthServiceMock.Verify(
            x => x.LoginAsync(It.Is<LoginRequestDto>(d =>
                d.UserName == "testuser" &&
                d.Password == "Password123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WhenRateLimitIsExceeded_ReturnsTooManyRequests()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();
        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponseDto>.FromUnauthorized("Invalid username or password."));

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    UserName = "testuser",
                    Password = "WrongPassword123"
                });

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        var sixthResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                UserName = "testuser",
                Password = "WrongPassword123"
            });

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            sixthResponse.StatusCode);
    }

    [Fact]
    public async Task LoginRateLimit_DoesNotAffectAuthenticatedEndpoints()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        var client = Authenticated(factory, 10);

        for (var attempt = 1; attempt <= 6; attempt++)
        {
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    UserName = "testuser",
                    Password = "WrongPassword123"
                });
        }

        var response = await client.GetAsync("/api/Auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        factory.UserServiceMock.Verify(
            x => x.ChangePasswordAsync(
                It.IsAny<int>(),
                It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceSucceeds_ReturnsFreshSession()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(
                10,
                It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        factory.AuthServiceMock
            .Setup(x => x.StartSessionAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponseDto>.Success(new LoginResponseDto
            {
                AccessToken = "fresh-access-token",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),
                UserId = 10,
                UserName = "testuser",
                PersonId = 20,
                FullName = "Test User",
                Role = UserRole.Staff,
                RefreshToken = "fresh-refresh-token",
                RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(14)
            }));

        var response = await Authenticated(factory, 10).PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<DVLD.Contracts.Auth.LoginResponse>();

        Assert.NotNull(body);
        Assert.Equal("fresh-access-token", body.AccessToken);

        factory.UserServiceMock.Verify(
            x => x.ChangePasswordAsync(
                10,
                It.Is<ChangePasswordDto>(d =>
                    d.CurrentPassword == "OldPassword123" &&
                    d.NewPassword == "NewPassword123"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(
        "Current password is required.",
        400,
        "Validation error")]
    [InlineData(
        "Current password is incorrect.",
        400,
        "Validation error")]
    [InlineData(
        "User was not found.",
        404,
        "Resource not found")]
    [InlineData(
        "Password change conflict.",
        409,
        "Conflict")]
    [InlineData(
        "You are not allowed to change this password.",
        403,
        "Forbidden")]
    public async Task ChangePassword_WhenServiceReturnsFailure_ReturnsExpectedProblemDetails(
        string error,
        int status,
        string title)
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        var result = status switch
        {
            400 => Result.ValidationFailure(error),
            404 => Result.NotFound(error),
            409 => Result.Conflict(error),
            403 => Result.Forbidden(error),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(
                10,
                It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        var response = await Authenticated(factory, 10).PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            });

        await AssertProblemDetailsAsync(
            response,
            (HttpStatusCode)status,
            title,
            error);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceReturnsFailure_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(
                10,
                It.IsAny<ChangePasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                Result.Failure("Unexpected password change failure."));

        var response = await Authenticated(factory, 10).PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            });

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task ChangePassword_WhenStartSessionFails_ReturnsSessionEstablishmentFailureAndClearsCookie()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(
                10,
                It.IsAny<ChangePasswordDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        factory.AuthServiceMock
            .Setup(x => x.StartSessionAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponseDto>.FromFailure("Internal session database error."));

        var response = await Authenticated(factory, 10).PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            });

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "Session establishment failed",
            "Your password was changed successfully and all existing sessions were revoked, but establishing a new session failed. Please log in with your new password.");

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Internal session database error", content);
        Assert.DoesNotContain("AccessToken", content);
        Assert.DoesNotContain("RefreshToken", content);

        AssertRefreshCookieDeleted(response);
    }

    [Fact]
    public async Task ChangePassword_WhenStartSessionThrows_ReturnsSessionEstablishmentFailureAndClearsCookie()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(
                10,
                It.IsAny<ChangePasswordDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        factory.AuthServiceMock
            .Setup(x => x.StartSessionAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal database down exception."));

        var response = await Authenticated(factory, 10).PostAsJsonAsync(
            "/api/Auth/change-password",
            new
            {
                CurrentPassword = "OldPassword123",
                NewPassword = "NewPassword123"
            });

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "Session establishment failed",
            "Your password was changed successfully and all existing sessions were revoked, but establishing a new session failed. Please log in with your new password.");

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Fatal database down exception", content);

        AssertRefreshCookieDeleted(response);
    }

    [Fact]
    public async Task JwtBearer_WhenTokenIsValidAndUserIsActive_AllowsAccessViaUserService()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.IsPrincipalCurrentAsync(
                10, "stamp-jwt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupCurrentUser(factory, 10, "jwtuser", "Jwt User", UserRole.Staff);

        await using var jwtFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme =
                        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme =
                        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                });
            });
        });

        using var scope = jwtFactory.Services.CreateScope();
        var jwtTokenService =
            scope.ServiceProvider.GetRequiredService<Application.Interfaces.IJwtTokenService>();

        var tokenResult = jwtTokenService.GenerateToken(new JwtTokenRequest
        {
            UserId = 10,
            PersonId = 20,
            UserName = "jwtuser",
            FullName = "Jwt User",
            Role = UserRole.Staff,
            SecurityStamp = "stamp-jwt"
        });

        var client = jwtFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

        var response = await client.GetAsync("/api/Auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.AuthServiceMock.Verify(
            x => x.IsPrincipalCurrentAsync(
                10, "stamp-jwt", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JwtBearer_WhenUserIsInactiveOrMissing_ReturnsUnauthorizedViaUserService()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.AuthServiceMock
            .Setup(x => x.IsPrincipalCurrentAsync(
                10, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await using var jwtFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme =
                        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme =
                        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                });
            });
        });

        using var scope = jwtFactory.Services.CreateScope();
        var jwtTokenService =
            scope.ServiceProvider.GetRequiredService<Application.Interfaces.IJwtTokenService>();

        var tokenResult = jwtTokenService.GenerateToken(new JwtTokenRequest
        {
            UserId = 10,
            PersonId = 20,
            UserName = "inactiveuser",
            FullName = "Inactive User",
            Role = UserRole.Staff,
            SecurityStamp = "stamp-inactive"
        });

        var client = jwtFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

        var response = await client.GetAsync("/api/Auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.AuthServiceMock.Verify(
            x => x.IsPrincipalCurrentAsync(
                10, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static void SetupCurrentUser(
        ApiWebApplicationFactory factory,
        int userId,
        string username = "testuser",
        string fullName = "Test User",
        UserRole role = UserRole.Staff)
    {
        factory.CurrentUserServiceMock
            .SetupGet(x => x.UserId)
            .Returns(userId);

        factory.CurrentUserServiceMock
            .SetupGet(x => x.Username)
            .Returns(username);

        factory.CurrentUserServiceMock
            .SetupGet(x => x.FullName)
            .Returns(fullName);

        factory.CurrentUserServiceMock
            .SetupGet(x => x.Role)
            .Returns(role);
    }

    private static HttpClient Authenticated(
        ApiWebApplicationFactory factory,
        int userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id",
            userId.ToString());

        return client;
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

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.GetProperty("instance").GetString()));

        Assert.True(
            body.TryGetProperty("traceId", out var traceId));

        Assert.False(
            string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    private sealed class MeResponse
    {
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }

    [Fact]
    public async Task Login_WhenAuthServiceReturnsNullResult_Returns500InternalServerErrorWithoutThrowing()
    {
        await using var factory = new ApiWebApplicationFactory();
        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Result<LoginResponseDto>)null! );

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/Auth/login",
            new { UserName = "testuser", Password = "Password123" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenRequestIsNull_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        var client = factory.CreateClient();
        var content = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static void AssertRefreshCookieDeleted(HttpResponseMessage response)
    {
        Assert.True(
            response.Headers.TryGetValues("Set-Cookie", out var setCookies),
            "Expected Set-Cookie header to be present on the response.");

        var cookie = Assert.Single(setCookies, c => c.StartsWith("dvld_rt="));
        Assert.Contains("dvld_rt=;", cookie);
        Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/Auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
