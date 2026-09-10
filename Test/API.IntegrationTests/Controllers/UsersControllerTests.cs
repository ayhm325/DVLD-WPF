using API.IntegrationTests.Infrastructure;
using Application.Common.Results;
using Application.DTOs.UserDTO;
using Domain.Enums;
using DVLD.Contracts.User;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace API.IntegrationTests.Controllers;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync("api/Users");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        factory.UserServiceMock.Verify(
            x => x.GetAllUsersAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenAuthenticatedAsStaff_ReturnsForbidden()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Staff");

        var response =
            await client.GetAsync("api/Users");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        factory.UserServiceMock.Verify(
            x => x.GetAllUsersAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenAuthenticatedAsAdmin_ReturnsOkWithMappedUsers()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        var users = new List<UserDto>
        {
            new()
            {
                UserId = 1,
                PersonId = 101,
                UserName = "admin",
                IsActive = true,
                FullName = "Admin User",
                Role = UserRole.Admin
            },
            new()
            {
                UserId = 2,
                PersonId = 102,
                UserName = "staff",
                IsActive = true,
                FullName = "Staff User",
                Role = UserRole.Staff
            }
        };

        factory.UserServiceMock
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(
                Result<List<UserDto>>.Success(users));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync("api/Users");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<List<UserResponse>>();

        Assert.NotNull(body);

        Assert.Equal(
            2,
            body!.Count);

        Assert.Equal(
            1,
            body[0].UserId);

        Assert.Equal(
            101,
            body[0].PersonId);

        Assert.Equal(
            "admin",
            body[0].UserName);

        Assert.True(
            body[0].IsActive);

        Assert.Equal(
            "Admin User",
            body[0].PersonFullName);

        Assert.Equal(
            2,
            body[1].UserId);

        Assert.Equal(
            102,
            body[1].PersonId);

        Assert.Equal(
            "staff",
            body[1].UserName);

        Assert.True(
            body[1].IsActive);

        Assert.Equal(
            "Staff User",
            body[1].PersonFullName);

        factory.UserServiceMock.Verify(
            x => x.GetAllUsersAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenServiceReturnsFailure_ReturnsInternalServerError()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Unexpected user retrieval failure.";

        factory.UserServiceMock
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(
                Result<List<UserDto>>.FromFailure(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync("api/Users");

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task GetById_WhenUserExists_ReturnsOkWithMappedUser()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        var user = new UserDto
        {
            UserId = 25,
            PersonId = 125,
            UserName = "john",
            IsActive = true,
            FullName = "John Doe",
            Role = UserRole.Staff
        };

        factory.UserServiceMock
            .Setup(x =>
                x.GetUserByIdAsync(25))
            .ReturnsAsync(
                Result<UserDto>.Success(user));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync("api/Users/25");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            25,
            body!.UserId);

        Assert.Equal(
            125,
            body.PersonId);

        Assert.Equal(
            "john",
            body.UserName);

        Assert.True(
            body.IsActive);

        Assert.Equal(
            "John Doe",
            body.PersonFullName);

        factory.UserServiceMock.Verify(
            x =>
                x.GetUserByIdAsync(25),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenUserDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "User was not found.";

        factory.UserServiceMock
            .Setup(x =>
                x.GetUserByIdAsync(999))
            .ReturnsAsync(
                Result<UserDto>.FromNotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync("api/Users/999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);

        factory.UserServiceMock.Verify(
            x =>
                x.GetUserByIdAsync(999),
            Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Invalid user id.";

        factory.UserServiceMock
            .Setup(x =>
                x.GetUserByIdAsync(0))
            .ReturnsAsync(
                Result<UserDto>.FromValidationFailure(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync("api/Users/0");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task GetByPersonId_WhenUserExists_ReturnsOkWithMappedUser()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        var user = new UserDto
        {
            UserId = 30,
            PersonId = 300,
            UserName = "person300",
            IsActive = true,
            FullName = "Person Three Hundred",
            Role = UserRole.Staff
        };

        factory.UserServiceMock
            .Setup(x =>
                x.GetUserByPersonIdAsync(300))
            .ReturnsAsync(
                Result<UserDto>.Success(user));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync(
                "api/Users/person/300");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            30,
            body!.UserId);

        Assert.Equal(
            300,
            body.PersonId);

        Assert.Equal(
            "person300",
            body.UserName);

        Assert.True(
            body.IsActive);

        Assert.Equal(
            "Person Three Hundred",
            body.PersonFullName);

        factory.UserServiceMock.Verify(
            x =>
                x.GetUserByPersonIdAsync(300),
            Times.Once);
    }

    [Fact]
    public async Task GetByPersonId_WhenUserDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "No user exists for this person.";

        factory.UserServiceMock
            .Setup(x =>
                x.GetUserByPersonIdAsync(500))
            .ReturnsAsync(
                Result<UserDto>.FromNotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync(
                "api/Users/person/500");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task GetByUsername_WhenUserExists_ReturnsOkWithMappedUser()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        var user = new UserDto
        {
            UserId = 40,
            PersonId = 400,
            UserName = "ayhm",
            IsActive = true,
            FullName = "Ayhm Obeidat",
            Role = UserRole.Staff
        };

        factory.UserServiceMock
            .Setup(x =>
                x.GetUserByUsernameAsync("ayhm"))
            .ReturnsAsync(
                Result<UserDto>.Success(user));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync(
                "api/Users/username/ayhm");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            40,
            body!.UserId);

        Assert.Equal(
            400,
            body.PersonId);

        Assert.Equal(
            "ayhm",
            body.UserName);

        Assert.True(
            body.IsActive);

        Assert.Equal(
            "Ayhm Obeidat",
            body.PersonFullName);

        factory.UserServiceMock.Verify(
            x =>
                x.GetUserByUsernameAsync("ayhm"),
            Times.Once);
    }

    [Fact]
    public async Task GetByUsername_WhenUserDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "User was not found.";

        factory.UserServiceMock
            .Setup(x =>
                x.GetUserByUsernameAsync("missing"))
            .ReturnsAsync(
                Result<UserDto>.FromNotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.GetAsync(
                "api/Users/username/missing");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedWithUserId()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        factory.UserServiceMock
            .Setup(x =>
                x.AddUserAsync(
                    It.IsAny<CreateUserDto>()))
            .ReturnsAsync(
                Result<int>.Success(77));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.PostAsJsonAsync(
                "api/Users",
                new
                {
                    PersonId = 700,
                    UserName = "newuser",
                    Password = "Password123",
                    IsActive = true
                });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<int>();

        Assert.Equal(
            77,
            body);

        Assert.NotNull(
            response.Headers.Location);

        Assert.EndsWith(
            "/api/Users/77",
            response.Headers.Location!.ToString());

        factory.UserServiceMock.Verify(
            x =>
                x.AddUserAsync(
                    It.Is<CreateUserDto>(
                        dto =>
                            dto.PersonId == 700 &&
                            dto.UserName == "newuser" &&
                            dto.Password == "Password123" &&
                            dto.IsActive)),
            Times.Once);
    }

    [Fact]
    public async Task Create_WhenServiceReturnsValidationFailure_ReturnsBadRequest()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Username is required.";

        factory.UserServiceMock
            .Setup(x =>
                x.AddUserAsync(
                    It.IsAny<CreateUserDto>()))
            .ReturnsAsync(
                Result<int>.FromValidationFailure(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.PostAsJsonAsync(
                "api/Users",
                new
                {
                    PersonId = 700,
                    UserName = "",
                    Password = "Password123",
                    IsActive = true
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Create_WhenUsernameConflicts_ReturnsConflict()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Username already exists.";

        factory.UserServiceMock
            .Setup(x =>
                x.AddUserAsync(
                    It.IsAny<CreateUserDto>()))
            .ReturnsAsync(
                Result<int>.FromConflict(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.PostAsJsonAsync(
                "api/Users",
                new
                {
                    PersonId = 700,
                    UserName = "existing",
                    Password = "Password123",
                    IsActive = true
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsNoContent()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        factory.UserServiceMock
            .Setup(x =>
                x.UpdateUserAsync(
                    55,
                    It.IsAny<UpdateUserDto>()))
            .ReturnsAsync(
                Result.Success());

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.PutAsJsonAsync(
                "api/Users/55",
                new
                {
                    PersonId = 555,
                    UserName = "updateduser",
                    IsActive = false
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        factory.UserServiceMock.Verify(
            x =>
                x.UpdateUserAsync(
                    55,
                    It.Is<UpdateUserDto>(
                        dto =>
                            dto.PersonId == 555 &&
                            dto.UserName == "updateduser" &&
                            dto.IsActive == false)),
            Times.Once);
    }

    [Fact]
    public async Task Update_WhenUserDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "User was not found.";

        factory.UserServiceMock
            .Setup(x =>
                x.UpdateUserAsync(
                    55,
                    It.IsAny<UpdateUserDto>()))
            .ReturnsAsync(
                Result.NotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.PutAsJsonAsync(
                "api/Users/55",
                new
                {
                    PersonId = 555,
                    UserName = "updateduser",
                    IsActive = true
                });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Update_WhenUsernameConflicts_ReturnsConflict()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "Username already exists.";

        factory.UserServiceMock
            .Setup(x =>
                x.UpdateUserAsync(
                    55,
                    It.IsAny<UpdateUserDto>()))
            .ReturnsAsync(
                Result.Conflict(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.PutAsJsonAsync(
                "api/Users/55",
                new
                {
                    PersonId = 555,
                    UserName = "existing",
                    IsActive = true
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsNoContent()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        factory.UserServiceMock
            .Setup(x =>
                x.DeleteUserAsync(66))
            .ReturnsAsync(
                Result.Success());

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.DeleteAsync(
                "api/Users/66");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        factory.UserServiceMock.Verify(
            x =>
                x.DeleteUserAsync(66),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenUserDoesNotExist_ReturnsNotFound()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "User was not found.";

        factory.UserServiceMock
            .Setup(x =>
                x.DeleteUserAsync(66))
            .ReturnsAsync(
                Result.NotFound(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.DeleteAsync(
                "api/Users/66");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);

        factory.UserServiceMock.Verify(
            x =>
                x.DeleteUserAsync(66),
            Times.Once);
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsForbidden_ReturnsForbidden()
    {
        await using var factory =
            new ApiWebApplicationFactory();

        const string error =
            "You are not allowed to delete this user.";

        factory.UserServiceMock
            .Setup(x =>
                x.DeleteUserAsync(66))
            .ReturnsAsync(
                Result.Forbidden(error));

        using var client =
            CreateAuthenticatedClient(
                factory,
                userId: 10,
                role: "Admin");

        var response =
            await client.DeleteAsync(
                "api/Users/66");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            error,
            body!.Error);
    }

    private static HttpClient CreateAuthenticatedClient(
        ApiWebApplicationFactory factory,
        int userId,
        string role)
    {
        var client =
            factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User-Id",
            userId.ToString());

        client.DefaultRequestHeaders.Add(
            "X-Test-Role",
            role);

        return client;
    }

    private sealed class ErrorResponse
    {
        public string Error { get; init; } =
            string.Empty;
    }
}