using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.UserDTO;
using Domain.Enums;
using DVLD.Contracts.User;
using Moq;

namespace API.IntegrationTests.Controllers;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        factory.UserServiceMock.Verify(x => x.GetAllUsersAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenAuthenticatedAsStaff_ReturnsForbidden()
    {
        await using var factory = new ApiWebApplicationFactory();
        using var client = CreateClient(factory, 10, "Staff");

        var response = await client.GetAsync("/api/Users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        factory.UserServiceMock.Verify(x => x.GetAllUsersAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenAuthenticatedAsAdmin_ReturnsOkWithMappedUsers()
    {
        await using var factory = new ApiWebApplicationFactory();

        var users = new List<UserDto>
        {
            new() { UserId = 1, PersonId = 101, UserName = "admin", IsActive = true, FullName = "Admin User", Role = UserRole.Admin },
            new() { UserId = 2, PersonId = 102, UserName = "staff", IsActive = true, FullName = "Staff User", Role = UserRole.Staff }
        };

        factory.UserServiceMock.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(Result<List<UserDto>>.Success(users));

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync("/api/Users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<UserResponse>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);

        Assert.Equal(1, body[0].UserId);
        Assert.Equal(101, body[0].PersonId);
        Assert.Equal("admin", body[0].UserName);
        Assert.True(body[0].IsActive);
        Assert.Equal("Admin User", body[0].PersonFullName);

        Assert.Equal(2, body[1].UserId);
        Assert.Equal(102, body[1].PersonId);
        Assert.Equal("staff", body[1].UserName);
        Assert.True(body[1].IsActive);
        Assert.Equal("Staff User", body[1].PersonFullName);

        factory.UserServiceMock.Verify(x => x.GetAllUsersAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceReturnsFailure_ReturnsInternalServerError()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.UserServiceMock.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(Result<List<UserDto>>.FromFailure("Unexpected user retrieval failure."));

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync("/api/Users");

        await AssertProblemDetailsAsync(
            response,
            HttpStatusCode.InternalServerError,
            "An unexpected error occurred.",
            "The server could not complete the request.");
    }

    [Fact]
    public async Task GetById_WhenUserExists_ReturnsOkWithMappedUser()
    {
        await using var factory = new ApiWebApplicationFactory();
        var user = new UserDto
        {
            UserId = 25,
            PersonId = 125,
            UserName = "john",
            IsActive = true,
            FullName = "John Doe",
            Role = UserRole.Staff
        };

        factory.UserServiceMock.Setup(x => x.GetUserByIdAsync(25))
            .ReturnsAsync(Result<UserDto>.Success(user));

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync("/api/Users/25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        Assert.Equal(user.UserId, body.UserId);
        Assert.Equal(user.PersonId, body.PersonId);
        Assert.Equal(user.UserName, body.UserName);
        Assert.Equal(user.IsActive, body.IsActive);
        Assert.Equal(user.FullName, body.PersonFullName);

        factory.UserServiceMock.Verify(x => x.GetUserByIdAsync(25), Times.Once);
    }

    [Theory]
    [InlineData(999, "User was not found.", 404, "Resource not found")]
    [InlineData(0, "Invalid user id.", 400, "Validation error")]
    public async Task GetById_WhenServiceReturnsError_ReturnsProblemDetails(
        int id, string detail, int status, string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = status == 404
            ? Result<UserDto>.FromNotFound(detail)
            : Result<UserDto>.FromValidationFailure(detail);

        factory.UserServiceMock.Setup(x => x.GetUserByIdAsync(id))
            .ReturnsAsync(result);

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync($"/api/Users/{id}");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task GetByPersonId_WhenUserExists_ReturnsOkWithMappedUser()
    {
        await using var factory = new ApiWebApplicationFactory();
        var user = new UserDto
        {
            UserId = 30,
            PersonId = 300,
            UserName = "person300",
            IsActive = true,
            FullName = "Person Three Hundred",
            Role = UserRole.Staff
        };

        factory.UserServiceMock.Setup(x => x.GetUserByPersonIdAsync(300))
            .ReturnsAsync(Result<UserDto>.Success(user));

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync("/api/Users/person/300");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        Assert.Equal(user.UserId, body.UserId);
        Assert.Equal(user.PersonId, body.PersonId);
        Assert.Equal(user.UserName, body.UserName);
        Assert.Equal(user.IsActive, body.IsActive);
        Assert.Equal(user.FullName, body.PersonFullName);

        factory.UserServiceMock.Verify(x => x.GetUserByPersonIdAsync(300), Times.Once);
    }

    [Fact]
    public async Task GetByPersonId_WhenUserDoesNotExist_ReturnsNotFoundProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        const string detail = "No user exists for this person.";

        factory.UserServiceMock.Setup(x => x.GetUserByPersonIdAsync(500))
            .ReturnsAsync(Result<UserDto>.FromNotFound(detail));

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync("/api/Users/person/500");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.NotFound, "Resource not found", detail);

        factory.UserServiceMock.Verify(x => x.GetUserByPersonIdAsync(500), Times.Once);
    }

    [Fact]
    public async Task GetByUsername_WhenUserExists_ReturnsOkWithMappedUser()
    {
        await using var factory = new ApiWebApplicationFactory();
        var user = new UserDto
        {
            UserId = 40,
            PersonId = 400,
            UserName = "ayhm",
            IsActive = true,
            FullName = "Ayhm Obeidat",
            Role = UserRole.Staff
        };

        factory.UserServiceMock.Setup(x => x.GetUserByUsernameAsync("ayhm"))
            .ReturnsAsync(Result<UserDto>.Success(user));

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync("/api/Users/username/ayhm");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(body);
        Assert.Equal(user.UserId, body.UserId);
        Assert.Equal(user.PersonId, body.PersonId);
        Assert.Equal(user.UserName, body.UserName);
        Assert.Equal(user.IsActive, body.IsActive);
        Assert.Equal(user.FullName, body.PersonFullName);

        factory.UserServiceMock.Verify(
            x => x.GetUserByUsernameAsync("ayhm"), Times.Once);
    }

    [Fact]
    public async Task GetByUsername_WhenUserDoesNotExist_ReturnsNotFoundProblemDetails()
    {
        await using var factory = new ApiWebApplicationFactory();
        const string detail = "User was not found.";

        factory.UserServiceMock.Setup(x => x.GetUserByUsernameAsync("missing"))
            .ReturnsAsync(Result<UserDto>.FromNotFound(detail));

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.GetAsync("/api/Users/username/missing");

        await AssertProblemDetailsAsync(
            response, HttpStatusCode.NotFound, "Resource not found", detail);
    }

    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedWithUserId()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.UserServiceMock.Setup(x => x.AddUserAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(Result<int>.Success(77));

        using var client = CreateClient(factory, 10, "Admin");

        var response = await client.PostAsJsonAsync("/api/Users", new
        {
            PersonId = 700,
            UserName = "newuser",
            Password = "Password123",
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(77, await response.Content.ReadFromJsonAsync<int>());
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith("/api/Users/77", response.Headers.Location.ToString());

        factory.UserServiceMock.Verify(x => x.AddUserAsync(
            It.Is<CreateUserDto>(d =>
                d.PersonId == 700 &&
                d.UserName == "newuser" &&
                d.Password == "Password123" &&
                d.IsActive)), Times.Once);
    }

    [Theory]
    [InlineData("Username is required.", 400, "Validation error")]
    [InlineData("Username already exists.", 409, "Conflict")]
    public async Task Create_WhenServiceReturnsError_ReturnsProblemDetails(
        string detail, int status, string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = status == 400
            ? Result<int>.FromValidationFailure(detail)
            : Result<int>.FromConflict(detail);

        factory.UserServiceMock.Setup(x => x.AddUserAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(result);

        using var client = CreateClient(factory, 10, "Admin");

        var response = await client.PostAsJsonAsync("/api/Users", new
        {
            PersonId = 700,
            UserName = status == 400 ? "" : "existing",
            Password = "Password123",
            IsActive = true
        });

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsNoContent()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.UserServiceMock.Setup(x =>
                x.UpdateUserAsync(55, It.IsAny<UpdateUserDto>()))
            .ReturnsAsync(Result.Success());

        using var client = CreateClient(factory, 10, "Admin");

        var response = await client.PutAsJsonAsync("/api/Users/55", new
        {
            PersonId = 555,
            UserName = "updateduser",
            IsActive = false
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        factory.UserServiceMock.Verify(x => x.UpdateUserAsync(
            55,
            It.Is<UpdateUserDto>(d =>
                d.PersonId == 555 &&
                d.UserName == "updateduser" &&
                !d.IsActive)), Times.Once);
    }

    [Theory]
    [InlineData("User was not found.", 404, "Resource not found")]
    [InlineData("Username already exists.", 409, "Conflict")]
    public async Task Update_WhenServiceReturnsError_ReturnsProblemDetails(
        string detail, int status, string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = status == 404
            ? Result.NotFound(detail)
            : Result.Conflict(detail);

        factory.UserServiceMock.Setup(x =>
                x.UpdateUserAsync(55, It.IsAny<UpdateUserDto>()))
            .ReturnsAsync(result);

        using var client = CreateClient(factory, 10, "Admin");

        var response = await client.PutAsJsonAsync("/api/Users/55", new
        {
            PersonId = 555,
            UserName = status == 404 ? "updateduser" : "existing",
            IsActive = true
        });

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);
    }

    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsNoContent()
    {
        await using var factory = new ApiWebApplicationFactory();

        factory.UserServiceMock.Setup(x => x.DeleteUserAsync(66))
            .ReturnsAsync(Result.Success());

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.DeleteAsync("/api/Users/66");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        factory.UserServiceMock.Verify(x => x.DeleteUserAsync(66), Times.Once);
    }

    [Theory]
    [InlineData("User was not found.", 404, "Resource not found")]
    [InlineData("You are not allowed to delete this user.", 403, "Forbidden")]
    public async Task Delete_WhenServiceReturnsError_ReturnsProblemDetails(
        string detail, int status, string title)
    {
        await using var factory = new ApiWebApplicationFactory();

        var result = status == 404
            ? Result.NotFound(detail)
            : Result.Forbidden(detail);

        factory.UserServiceMock.Setup(x => x.DeleteUserAsync(66))
            .ReturnsAsync(result);

        using var client = CreateClient(factory, 10, "Admin");
        var response = await client.DeleteAsync("/api/Users/66");

        await AssertProblemDetailsAsync(
            response, (HttpStatusCode)status, title, detail);

        factory.UserServiceMock.Verify(x => x.DeleteUserAsync(66), Times.Once);
    }

    private static HttpClient CreateClient(
        ApiWebApplicationFactory factory, int userId, string role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Username", "testuser");
        client.DefaultRequestHeaders.Add("X-Test-FullName", "Test User");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string title,
        string detail)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        var body = document.RootElement;

        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.Equal(detail, body.GetProperty("detail").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            body.GetProperty("instance").GetString()));

        Assert.True(body.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
