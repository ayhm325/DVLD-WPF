using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.UnitTests.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();

    private AuthService CreateService() =>
        new(_userRepository.Object, _jwtTokenService.Object);

    private static LoginRequestDto Request(
        string userName = "admin",
        string password = "Password123!") =>
        new() { UserName = userName, Password = password };

    private static User CreateUser(
        int id = 1,
        string userName = "admin",
        bool active = true,
        string password = "Password123!") =>
        new()
        {
            UserId = id,
            UserName = userName,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = active,
            PersonId = 10
        };

    [Fact]
    public void Constructor_WhenUserRepositoryIsNull_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new AuthService(null!, _jwtTokenService.Object));
        Assert.Equal("userRepository", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenJwtTokenServiceIsNull_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new AuthService(_userRepository.Object, null!));
        Assert.Equal("jwtTokenService", ex.ParamName);
    }

    [Fact]
    public async Task Login_WhenDtoIsNull_ReturnsValidationFailure()
    {
        var result = await CreateService().LoginAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _userRepository.Verify(
            x => x.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_WhenUsernameIsBlank_ReturnsValidationFailure()
    {
        var result = await CreateService().LoginAsync(Request("   "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Login_WhenPasswordIsBlank_ReturnsValidationFailure()
    {
        var result = await CreateService().LoginAsync(Request(password: "   "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Login_TrimsUsernameBeforeRepositoryLookup()
    {
        _userRepository.Setup(x => x.GetUserByUsernameAsync("admin"))
            .ReturnsAsync((User?)null);

        var result = await CreateService().LoginAsync(Request("  admin  "));

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Invalid username or password.", result.Error);
        _userRepository.Verify(
            x => x.GetUserByUsernameAsync("admin"), Times.Once);
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ReturnsInvalidCredentials()
    {
        _userRepository.Setup(x => x.GetUserByUsernameAsync("admin"))
            .ReturnsAsync((User?)null);

        var result = await CreateService().LoginAsync(Request());

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Invalid username or password.", result.Error);
        _jwtTokenService.Verify(
            x => x.GenerateToken(It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_WhenUserInactive_ReturnsInvalidCredentials()
    {
        _userRepository.Setup(x => x.GetUserByUsernameAsync("admin"))
            .ReturnsAsync(CreateUser(active: false));

        var result = await CreateService().LoginAsync(Request());

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Invalid username or password.", result.Error);
        _jwtTokenService.Verify(
            x => x.GenerateToken(It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ReturnsInvalidCredentials()
    {
        _userRepository.Setup(x => x.GetUserByUsernameAsync("admin"))
            .ReturnsAsync(CreateUser());

        var result = await CreateService().LoginAsync(
            Request(password: "WrongPassword"));

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Invalid username or password.", result.Error);
        _jwtTokenService.Verify(
            x => x.GenerateToken(It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsTokenAndUserData()
    {
        var user = CreateUser(id: 5, userName: "admin");
        var expires = DateTime.UtcNow.AddHours(1);

        _userRepository.Setup(x => x.GetUserByUsernameAsync("admin"))
            .ReturnsAsync(user);

        _jwtTokenService.Setup(x => x.GenerateToken(It.IsAny<UserDto>()))
            .Returns(new JwtTokenResult
            {
                AccessToken = "jwt-token",
                ExpiresAtUtc = expires
            });

        var result = await CreateService().LoginAsync(Request());

        Assert.True(result.IsSuccess);
        var response = result.Value
            ?? throw new Xunit.Sdk.XunitException("Expected login response.");

        Assert.Equal("jwt-token", response.AccessToken);
        Assert.Equal(expires, response.ExpiresAtUtc);
        Assert.Equal(5, response.UserId);
        Assert.Equal("admin", response.UserName);
        Assert.Equal(10, response.PersonId);
        _jwtTokenService.Verify(
            x => x.GenerateToken(It.IsAny<UserDto>()), Times.Once);
    }

    [Fact]
    public async Task Login_WhenRepositoryThrows_PropagatesException()
    {
        _userRepository.Setup(x => x.GetUserByUsernameAsync("admin"))
            .ThrowsAsync(new InvalidOperationException("database error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().LoginAsync(Request()));

        Assert.Equal("database error", ex.Message);
    }

    [Fact]
    public async Task Login_WhenJwtServiceThrows_PropagatesException()
    {
        _userRepository.Setup(x => x.GetUserByUsernameAsync("admin"))
            .ReturnsAsync(CreateUser());

        _jwtTokenService.Setup(x => x.GenerateToken(It.IsAny<UserDto>()))
            .Throws(new InvalidOperationException("token error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().LoginAsync(Request()));

        Assert.Equal("token error", ex.Message);
    }
}
