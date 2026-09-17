using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Domain.Enums;
using DVLD.Contracts.User;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Me_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("api/Auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("api/Auth/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_WhenAuthenticated_ReturnsCurrentUserProfile()
    {
        await using var factory = new ApiWebApplicationFactory();

        SetupCurrentUser(factory, 10, "testuser", "Test User", UserRole.Staff);

        factory.UserServiceMock
            .Setup(x => x.GetCurrentProfileAsync(10))
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

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync("api/Auth/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<UserProfileResponse>();

        Assert.NotNull(body);
        Assert.Equal(10, body!.UserId);
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
            x => x.GetCurrentProfileAsync(10), Times.Once);
    }

    [Fact]
    public async Task Profile_WhenUserNotFound_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        const string error = "User was not found.";

        factory.UserServiceMock
            .Setup(x => x.GetCurrentProfileAsync(10))
            .ReturnsAsync(Result<UserProfileDto>.FromNotFound(error));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync("api/Auth/profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);

        factory.UserServiceMock.Verify(
            x => x.GetCurrentProfileAsync(10), Times.Once);
    }

    [Fact]
    public async Task Me_WhenAuthenticated_ReturnsOkWithUserInformation()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10, "testuser", "Test User", UserRole.Staff);

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.GetAsync("api/Auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.NotNull(body);
        Assert.Equal(10, body!.UserId);
        Assert.Equal("testuser", body.Username);
        Assert.Equal("Test User", body.FullName);
        Assert.Equal("Staff", body.Role);
    }

    [Fact]
    public async Task Login_WhenAuthServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        const string error = "Username is required.\nPassword is required.";

        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(Result<LoginResponseDto>.FromValidationFailure(error));

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "api/Auth/login", new { UserName = "", Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);
    }

    [Fact]
    public async Task Login_WhenAuthServiceReturnsFailure_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        const string error = "Invalid username or password.";

        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(Result<LoginResponseDto>.FromFailure(error));

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "api/Auth/login",
            new { UserName = "wronguser", Password = "wrongpassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);
    }

    [Fact]
    public async Task Login_WhenAuthServiceSucceeds_ReturnsOkWithCompleteResponse()
    {
        await using var factory = new ApiWebApplicationFactory();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        factory.AuthServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
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

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "api/Auth/login",
            new { UserName = "testuser", Password = "Password123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<DVLD.Contracts.Auth.LoginResponse>();

        Assert.NotNull(body);
        Assert.Equal("test-access-token", body!.AccessToken);
        Assert.Equal(expiresAt, body.ExpiresAtUtc);
        Assert.Equal(10, body.UserId);
        Assert.Equal("testuser", body.UserName);
        Assert.Equal(20, body.PersonId);
        Assert.Equal("Test User", body.FullName);
        Assert.Equal("Staff", body.Role);

        factory.AuthServiceMock.Verify(
            x => x.LoginAsync(It.Is<LoginRequestDto>(dto =>
                dto.UserName == "testuser" &&
                dto.Password == "Password123")),
            Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "api/Auth/change-password",
            new { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        factory.UserServiceMock.Verify(
            x => x.ChangePasswordAsync(
                It.IsAny<int>(), It.IsAny<ChangePasswordDto>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceSucceeds_ReturnsNoContent()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(10, It.IsAny<ChangePasswordDto>()))
            .ReturnsAsync(Result.Success());

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.PostAsJsonAsync(
            "api/Auth/change-password",
            new { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.UserServiceMock.Verify(
            x => x.ChangePasswordAsync(
                10,
                It.Is<ChangePasswordDto>(dto =>
                    dto.CurrentPassword == "OldPassword123" &&
                    dto.NewPassword == "NewPassword123")),
            Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        const string error = "Current password is required.";

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(10, It.IsAny<ChangePasswordDto>()))
            .ReturnsAsync(Result.ValidationFailure(error));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.PostAsJsonAsync(
            "api/Auth/change-password",
            new { CurrentPassword = "", NewPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceReturnsNotFound_ReturnsNotFound()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        const string error = "User was not found.";

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(10, It.IsAny<ChangePasswordDto>()))
            .ReturnsAsync(Result.NotFound(error));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.PostAsJsonAsync(
            "api/Auth/change-password",
            new { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceReturnsConflict_ReturnsConflict()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        const string error = "Password change conflict.";

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(10, It.IsAny<ChangePasswordDto>()))
            .ReturnsAsync(Result.Conflict(error));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.PostAsJsonAsync(
            "api/Auth/change-password",
            new { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceReturnsForbidden_ReturnsForbidden()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        const string error = "You are not allowed to change this password.";

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(10, It.IsAny<ChangePasswordDto>()))
            .ReturnsAsync(Result.Forbidden(error));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.PostAsJsonAsync(
            "api/Auth/change-password",
            new { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);
    }

    [Fact]
    public async Task ChangePassword_WhenServiceReturnsFailure_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();
        SetupCurrentUser(factory, 10);

        const string error = "Unexpected password change failure.";

        factory.UserServiceMock
            .Setup(x => x.ChangePasswordAsync(10, It.IsAny<ChangePasswordDto>()))
            .ReturnsAsync(Result.Failure(error));

        using var client = CreateAuthenticatedClient(factory, 10);
        var response = await client.PostAsJsonAsync(
            "api/Auth/change-password",
            new { CurrentPassword = "OldPassword123", NewPassword = "NewPassword123" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);
        Assert.Equal(error, body!.Error);
    }

    private static void SetupCurrentUser(
        ApiWebApplicationFactory factory,
        int userId,
        string username = "testuser",
        string fullName = "Test User",
        UserRole role = UserRole.Staff)
    {
        factory.CurrentUserServiceMock.SetupGet(x => x.UserId).Returns(userId);
        factory.CurrentUserServiceMock.SetupGet(x => x.Username).Returns(username);
        factory.CurrentUserServiceMock.SetupGet(x => x.FullName).Returns(fullName);
        factory.CurrentUserServiceMock.SetupGet(x => x.Role).Returns(role);
    }

    private static HttpClient CreateAuthenticatedClient(
        ApiWebApplicationFactory factory,
        int userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        return client;
    }

    private sealed class ErrorResponse
    {
        public string Error { get; init; } = string.Empty;
    }

    private sealed class MeResponse
    {
        public int UserId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }
}