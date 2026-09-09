using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _sut = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // =========================================================
    // GetUserByIdAsync
    // =========================================================

    [Fact]
    public async Task GetUserByIdAsync_InvalidId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidUserId = 0;

        // Act
        Result<UserDto> result =
            await _sut.GetUserByIdAsync(invalidUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid user ID.", result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByUserIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int userId = 10;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByUserIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        Result<UserDto> result =
            await _sut.GetUserByIdAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("User not found.", result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByUserIdAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserExists_ReturnsSuccess()
    {
        // Arrange
        const int userId = 10;

        var user = CreateUser(userId);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByUserIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        Result<UserDto> result =
            await _sut.GetUserByIdAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.NotNull(result.Value);

        Assert.Equal(user.UserId, result.Value!.UserId);
        Assert.Equal(user.PersonId, result.Value.PersonId);
        Assert.Equal(user.UserName, result.Value.UserName);
        Assert.Equal(user.IsActive, result.Value.IsActive);
        Assert.Equal(user.Role, result.Value.Role);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByUserIdAsync(userId),
            Times.Once);
    }

    // =========================================================
    // GetUserByPersonIdAsync
    // =========================================================

    [Fact]
    public async Task GetUserByPersonIdAsync_InvalidId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidPersonId = 0;

        // Act
        Result<UserDto> result =
            await _sut.GetUserByPersonIdAsync(
                invalidPersonId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid person ID.", result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByPersonIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserByPersonIdAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int personId = 20;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByPersonIdAsync(personId))
            .ReturnsAsync((User?)null);

        // Act
        Result<UserDto> result =
            await _sut.GetUserByPersonIdAsync(personId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "No user is associated with this person.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByPersonIdAsync(personId),
            Times.Once);
    }

    [Fact]
    public async Task GetUserByPersonIdAsync_UserExists_ReturnsSuccess()
    {
        // Arrange
        const int personId = 20;

        var user = CreateUser(
            userId: 10,
            personId: personId);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByPersonIdAsync(personId))
            .ReturnsAsync(user);

        // Act
        Result<UserDto> result =
            await _sut.GetUserByPersonIdAsync(personId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(personId, result.Value!.PersonId);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByPersonIdAsync(personId),
            Times.Once);
    }

    // =========================================================
    // GetUserByUsernameAsync
    // =========================================================

    [Fact]
    public async Task GetUserByUsernameAsync_EmptyUsername_ReturnsValidationFailure()
    {
        // Arrange
        const string username = "   ";

        // Act
        Result<UserDto> result =
            await _sut.GetUserByUsernameAsync(username);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Username is required.", result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByUsernameAsync(
                    It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const string username = "ayhm";

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByUsernameAsync(username))
            .ReturnsAsync((User?)null);

        // Act
        Result<UserDto> result =
            await _sut.GetUserByUsernameAsync(username);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("User not found.", result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByUsernameAsync(username),
            Times.Once);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_UsernameHasWhitespace_TrimsBeforeRepositoryCall()
    {
        // Arrange
        const string inputUsername = "  ayhm  ";
        const string normalizedUsername = "ayhm";

        var user = CreateUser(
            userId: 10,
            userName: normalizedUsername);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByUsernameAsync(
                    normalizedUsername))
            .ReturnsAsync(user);

        // Act
        Result<UserDto> result =
            await _sut.GetUserByUsernameAsync(
                inputUsername);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(
            normalizedUsername,
            result.Value!.UserName);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByUsernameAsync(
                    normalizedUsername),
            Times.Once);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_UserExists_ReturnsSuccess()
    {
        // Arrange
        const string username = "ayhm";

        var user = CreateUser(
            userId: 10,
            userName: username);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByUsernameAsync(username))
            .ReturnsAsync(user);

        // Act
        Result<UserDto> result =
            await _sut.GetUserByUsernameAsync(username);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.NotNull(result.Value);
        Assert.Equal(username, result.Value!.UserName);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserByUsernameAsync(username),
            Times.Once);
    }

    // =========================================================
    // IsUsernameTakenForAnotherUserAsync
    // =========================================================

    [Fact]
    public async Task IsUsernameTakenForAnotherUserAsync_InvalidInput_ReturnsFalse()
    {
        // Arrange
        const string username = "ayhm";

        // Act
        bool result =
            await _sut.IsUsernameTakenForAnotherUserAsync(
                username,
                0);

        // Assert
        Assert.False(result);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IsUsernameTakenForAnotherUserAsync_ValidInput_DelegatesToRepository()
    {
        // Arrange
        const string username = "ayhm";
        const int userId = 10;

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    username,
                    userId))
            .ReturnsAsync(true);

        // Act
        bool result =
            await _sut.IsUsernameTakenForAnotherUserAsync(
                username,
                userId);

        // Assert
        Assert.True(result);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    username,
                    userId),
            Times.Once);
    }

    [Fact]
    public async Task IsUsernameTakenForAnotherUserAsync_UsernameHasWhitespace_TrimsBeforeRepositoryCall()
    {
        // Arrange
        const string inputUsername = "  ayhm  ";
        const string normalizedUsername = "ayhm";
        const int userId = 10;

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    normalizedUsername,
                    userId))
            .ReturnsAsync(true);

        // Act
        bool result =
            await _sut.IsUsernameTakenForAnotherUserAsync(
                inputUsername,
                userId);

        // Assert
        Assert.True(result);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    normalizedUsername,
                    userId),
            Times.Once);
    }

    // =========================================================
    // GetAllUsersAsync
    // =========================================================

    [Fact]
    public async Task GetAllUsersAsync_UsersExist_ReturnsMappedUsers()
    {
        // Arrange
        var users = new List<User>
        {
            CreateUser(1, 10, "ayhm1"),
            CreateUser(2, 20, "ayhm2")
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetAllUsersAsync())
            .ReturnsAsync(users);

        // Act
        Result<List<UserDto>> result =
            await _sut.GetAllUsersAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.NotNull(result.Value);

        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(
            users[0].UserId,
            result.Value[0].UserId);

        Assert.Equal(
            users[0].UserName,
            result.Value[0].UserName);

        Assert.Equal(
            users[1].UserId,
            result.Value[1].UserId);

        Assert.Equal(
            users[1].UserName,
            result.Value[1].UserName);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetAllUsersAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_NoUsers_ReturnsEmptyList()
    {
        // Arrange
        _userRepositoryMock
            .Setup(repository =>
                repository.GetAllUsersAsync())
            .ReturnsAsync(new List<User>());

        // Act
        Result<List<UserDto>> result =
            await _sut.GetAllUsersAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetAllUsersAsync(),
            Times.Once);
    }

    // =========================================================
    // AddUserAsync
    // =========================================================

    [Fact]
    public async Task AddUserAsync_NullDto_ReturnsValidationFailure()
    {
        // Arrange
        CreateUserDto? dto = null;

        // Act
        Result<int> result =
            await _sut.AddUserAsync(dto!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "User data is required.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenAsync(
                    It.IsAny<string>()),
            Times.Never);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddUserAsync_InvalidDto_ReturnsValidationFailure()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            UserName = "a",
            Password = "123",
            IsActive = true,
            PersonId = 0
        };

        // Act
        Result<int> result =
            await _sut.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenAsync(
                    It.IsAny<string>()),
            Times.Never);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddUserAsync_DuplicateUsername_ReturnsConflict()
    {
        // Arrange
        var dto = CreateValidCreateUserDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenAsync(
                    dto.UserName))
            .ReturnsAsync(true);

        // Act
        Result<int> result =
            await _sut.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Username is already in use.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenAsync(
                    dto.UserName),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    It.IsAny<int>()),
            Times.Never);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddUserAsync_PersonAlreadyHasUser_ReturnsConflict()
    {
        // Arrange
        var dto = CreateValidCreateUserDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenAsync(
                    dto.UserName))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId))
            .ReturnsAsync(true);

        // Act
        Result<int> result =
            await _sut.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "This person is already associated with a user account.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenAsync(
                    dto.UserName),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddUserAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        var dto = CreateValidCreateUserDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenAsync(
                    dto.UserName))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()))
            .Callback<User>(user =>
                user.UserId = 1)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result<int> result =
            await _sut.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to create user.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_ValidDto_ReturnsCreatedUserIdAndHashesPassword()
    {
        // Arrange
        var dto = CreateValidCreateUserDto();

        User? addedUser = null;

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenAsync(
                    dto.UserName))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()))
            .Callback<User>(user =>
            {
                addedUser = user;
                user.UserId = 25;
            })
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<int> result =
            await _sut.AddUserAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(25, result.Value);

        Assert.NotNull(addedUser);
        Assert.NotEqual(dto.Password, addedUser!.Password);
        Assert.True(
            BCrypt.Net.BCrypt.Verify(
                dto.Password,
                addedUser.Password));

        Assert.Equal(
            dto.UserName,
            addedUser.UserName);

        Assert.Equal(
            dto.PersonId,
            addedUser.PersonId);

        Assert.Equal(
            dto.IsActive,
            addedUser.IsActive);

        Assert.Equal(
            UserRole.Staff,
            addedUser.Role);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // UpdateUserAsync
    // =========================================================

    [Fact]
    public async Task UpdateUserAsync_InvalidId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidUserId = 0;
        var dto = CreateValidUpdateUserDto();

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                invalidUserId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid user ID.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_NullDto_ReturnsValidationFailure()
    {
        // Arrange
        const int userId = 10;
        UpdateUserDto? dto = null;

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "User data is required.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_InvalidDto_ReturnsValidationFailure()
    {
        // Arrange
        const int userId = 10;

        var dto = new UpdateUserDto
        {
            UserName = "a",
            IsActive = true,
            PersonId = 0
        };

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int userId = 10;
        var dto = CreateValidUpdateUserDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "User not found.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_DuplicateUsername_ReturnsConflict()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidUpdateUserDto();

        var user = CreateUser(
            userId,
            personId: 20);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId))
            .ReturnsAsync(true);

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Username is already in use by another user.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    It.IsAny<int>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_PersonAlreadyHasAnotherUser_ReturnsConflict()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidUpdateUserDto();
        dto.PersonId = 30;

        var user = CreateUser(
            userId,
            personId: 20);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId))
            .ReturnsAsync(true);

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "This person is already associated with another user account.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidUpdateUserDto();

        var user = CreateUser(
            userId,
            personId: dto.PersonId);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "No user changes were saved.",
            result.Error);

        Assert.Equal(
            dto.UserName,
            user.UserName);

        Assert.Equal(
            dto.IsActive,
            user.IsActive);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_ValidDto_ReturnsSuccess()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidUpdateUserDto();
        dto.PersonId = 30;

        var user = CreateUser(
            userId,
            personId: 20);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);

        Assert.Equal(
            dto.UserName,
            user.UserName);

        Assert.Equal(
            dto.PersonId,
            user.PersonId);

        Assert.Equal(
            dto.IsActive,
            user.IsActive);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(userId),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // DeleteUserAsync
    // =========================================================

    [Fact]
    public async Task DeleteUserAsync_InvalidId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidUserId = 0;

        // Act
        Result result =
            await _sut.DeleteUserAsync(invalidUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid user ID.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int userId = 10;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        Result result =
            await _sut.DeleteUserAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "User not found.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(userId),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.DeleteUser(
                    It.IsAny<User>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        const int userId = 10;

        var user = CreateUser(userId);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result result =
            await _sut.DeleteUserAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to save user deletion.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.DeleteUser(user),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        const int userId = 10;

        var user = CreateUser(userId);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result =
            await _sut.DeleteUserAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(userId),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.DeleteUser(user),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // ChangePasswordAsync
    // =========================================================

    [Fact]
    public async Task ChangePasswordAsync_InvalidUserId_ReturnsValidationFailure()
    {
        // Arrange
        const int invalidUserId = 0;

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123"
        };

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                invalidUserId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid user ID.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_NullDto_ReturnsValidationFailure()
    {
        // Arrange
        const int userId = 10;
        ChangePasswordDto? dto = null;

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto!);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Change password data is required.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_InvalidDto_ReturnsValidationFailure()
    {
        // Arrange
        const int userId = 10;

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "",
            NewPassword = "123"
        };

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(
                    It.IsAny<int>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidChangePasswordDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "User not found.",
            result.Error);

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsFailure()
    {
        // Arrange
        const int userId = 10;

        const string currentPassword = "CorrectPassword123";
        const string wrongPassword = "WrongPassword123";

        var dto = new ChangePasswordDto
        {
            CurrentPassword = wrongPassword,
            NewPassword = "NewPassword123"
        };

        var user = CreateUser(
            userId,
            password: currentPassword);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Current password is incorrect.",
            result.Error);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidChangePasswordDto();

        var user = CreateUser(
            userId,
            password: dto.CurrentPassword);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to save password change.",
            result.Error);

        Assert.NotEqual(
            dto.CurrentPassword,
            user.Password);

        Assert.True(
            BCrypt.Net.BCrypt.Verify(
                dto.NewPassword,
                user.Password));

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidData_ReturnsSuccessAndUpdatesPassword()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidChangePasswordDto();

        var user = CreateUser(
            userId,
            password: dto.CurrentPassword);

        var oldHash = user.Password;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);

        Assert.NotEqual(oldHash, user.Password);

        Assert.True(
            BCrypt.Net.BCrypt.Verify(
                dto.NewPassword,
                user.Password));

        Assert.False(
            BCrypt.Net.BCrypt.Verify(
                dto.CurrentPassword,
                user.Password));

        _userRepositoryMock.Verify(
            repository =>
                repository.GetUserForUpdateAsync(userId),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // Test Data Helpers
    // =========================================================

    private static User CreateUser(
        int userId = 10,
        int personId = 20,
        string userName = "ayhm",
        string password = "Password123")
    {
        return new User
        {
            UserId = userId,
            PersonId = personId,
            UserName = userName,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            Role = UserRole.Staff,

            Person = new Person
            {
                PersonId = personId,
                FirstName = "Ayhm",
                SecondName = "Mohammed",
                ThirdName = null,
                LastName = "Obeidat"
            }
        };
    }

    private static CreateUserDto CreateValidCreateUserDto()
    {
        return new CreateUserDto
        {
            UserName = "newuser",
            Password = "Password123",
            IsActive = true,
            PersonId = 20
        };
    }

    private static UpdateUserDto CreateValidUpdateUserDto()
    {
        return new UpdateUserDto
        {
            UserName = "updateduser",
            IsActive = false,
            PersonId = 20
        };
    }

    private static ChangePasswordDto CreateValidChangePasswordDto()
    {
        return new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123"
        };
    }
}