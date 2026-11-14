using System.Data;

using Application.Common.Exceptions;
using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Services;
using Application.UnitTests.Common;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<IUnitOfWorkTransaction> _transactionMock = new();
    private readonly FakeJordanClock _clock = FakeJordanClock.FromUtc(
        new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc));
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();

        _transactionMock
            .Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionMock
            .Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(repository => repository.AcquireAdminMutationLockAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(unit => unit.ExecuteInTransactionAsync<Result>(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                async (Func<IUnitOfWorkTransaction, Task<Result>> operation,
                    IsolationLevel _,
                    CancellationToken ct) =>
                {
                    try
                    {
                        var result = await operation(_transactionMock.Object);
                        if (result is { IsFailure: true })
                        {
                            await _transactionMock.Object.RollbackAsync(ct);
                            return result;
                        }

                        await _transactionMock.Object.CommitAsync(ct);
                        return result;
                    }
                    catch
                    {
                        await _transactionMock.Object.RollbackAsync(CancellationToken.None);
                        throw;
                    }
                });

        _sut = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _sessionRepositoryMock.Object,
            _clock);
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int userId = 10;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByUserIdAsync(userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserByUserIdAsync(userId, It.IsAny<CancellationToken>()),
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
                repository.GetUserByUserIdAsync(userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserByUserIdAsync(userId, It.IsAny<CancellationToken>()),
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserByPersonIdAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int personId = 20;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
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
                repository.GetUserByPersonIdAsync(personId, It.IsAny<CancellationToken>()),
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
                repository.GetUserByPersonIdAsync(personId, It.IsAny<CancellationToken>()))
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
                repository.GetUserByPersonIdAsync(personId, It.IsAny<CancellationToken>()),
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
                    It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const string username = "ayhm";

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserByUsernameAsync(username, It.IsAny<CancellationToken>()))
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
                repository.GetUserByUsernameAsync(username, It.IsAny<CancellationToken>()),
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
                    normalizedUsername, It.IsAny<CancellationToken>()))
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
                    normalizedUsername, It.IsAny<CancellationToken>()),
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
                repository.GetUserByUsernameAsync(username, It.IsAny<CancellationToken>()))
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
                repository.GetUserByUsernameAsync(username, It.IsAny<CancellationToken>()),
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                    userId, It.IsAny<CancellationToken>()))
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
                    userId, It.IsAny<CancellationToken>()),
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
                    userId, It.IsAny<CancellationToken>()))
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
                    userId, It.IsAny<CancellationToken>()),
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
                repository.GetAllUsersAsync(It.IsAny<CancellationToken>()))
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
                repository.GetAllUsersAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_NoUsers_ReturnsEmptyList()
    {
        // Arrange
        _userRepositoryMock
            .Setup(repository =>
                repository.GetAllUsersAsync(It.IsAny<CancellationToken>()))
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
                repository.GetAllUsersAsync(It.IsAny<CancellationToken>()),
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
                    It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>(), It.IsAny<CancellationToken>()),
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
                    It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>(), It.IsAny<CancellationToken>()),
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
                    dto.UserName, It.IsAny<CancellationToken>()))
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
                    dto.UserName, It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>(), It.IsAny<CancellationToken>()),
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
                    dto.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId, It.IsAny<CancellationToken>()))
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
                    dto.UserName, It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId, It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.AddUserAsync(
                    It.IsAny<User>(), It.IsAny<CancellationToken>()),
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
                    dto.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.AddUserAsync(
                    It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) =>
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
                    It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_WhenSaveThrowsDuplicateUsernameException_ReturnsConflict()
    {
        // Arrange
        var dto = CreateValidCreateUserDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenAsync(dto.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(dto.PersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateUsernameException());

        // Act
        Result<int> result = await _sut.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Username is already in use.", result.Error);

        _userRepositoryMock.Verify(
            repository => repository.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_WhenSaveThrowsDuplicatePersonUserException_ReturnsConflict()
    {
        // Arrange
        var dto = CreateValidCreateUserDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenAsync(dto.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(dto.PersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicatePersonUserException());

        // Act
        Result<int> result = await _sut.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("This person is already associated with a user account.", result.Error);

        _userRepositoryMock.Verify(
            repository => repository.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_WhenSaveThrowsUnrelatedException_PropagatesException()
    {
        // Arrange
        var dto = CreateValidCreateUserDto();

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenAsync(dto.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(dto.PersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Unrelated database error."));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.AddUserAsync(dto));

        _userRepositoryMock.Verify(
            repository => repository.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()),
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
                    dto.UserName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.AddUserAsync(
                    It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) =>
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
                    It.IsAny<User>(), It.IsAny<CancellationToken>()),
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()),
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId, It.IsAny<CancellationToken>()))
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
                    userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId, It.IsAny<CancellationToken>()))
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
                    dto.PersonId, It.IsAny<CancellationToken>()),
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId, It.IsAny<CancellationToken>()))
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserExistsByPersonIdAsync(
                    dto.PersonId, It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenNoPropertiesChanged_ReturnsSuccessWithoutSaving()
    {
        // Arrange
        const int userId = 10;

        var user = CreateUser(
            userId,
            personId: 20,
            userName: "ayhm");

        var dto = new UpdateUserDto
        {
            UserName = "  ayhm  ",
            PersonId = 20,
            IsActive = user.IsActive
        };

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    "ayhm",
                    userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result result =
            await _sut.UpdateUserAsync(
                userId,
                dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_SaveThrowsDbUpdateException_PropagatesException()
    {
        // Arrange
        const int userId = 10;

        var dto = CreateValidUpdateUserDto();

        var user = CreateUser(
            userId,
            personId: dto.PersonId);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new Microsoft.EntityFrameworkCore.DbUpdateException(
                    "Simulated user persistence exception."));

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => _sut.UpdateUserAsync(
                userId,
                dto));
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        const int userId = 10;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()),
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()),
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

    [Fact]
    public async Task UpdateUserAsync_PrimaryAdminAccount_CannotBeDeactivated_ReturnsForbidden()
    {
        // Arrange
        const int userId = 2;
        var dto = CreateValidUpdateUserDto();
        dto.IsActive = false;
        var user = CreateUser(userId, role: UserRole.Admin);

        _userRepositoryMock
            .Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("The primary administrator account cannot be deactivated.", result.Error);
    }

    [Fact]
    public async Task UpdateUserAsync_CurrentAdminAccount_CannotDeactivateSelf_ReturnsForbidden()
    {
        // Arrange
        const int userId = 5;
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(c => c.IsLoggedIn).Returns(true);
        currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
        currentUserServiceMock.Setup(c => c.Role).Returns(UserRole.Admin);

        var service = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _sessionRepositoryMock.Object,
            _clock,
            currentUserServiceMock.Object);

        var dto = CreateValidUpdateUserDto();
        dto.IsActive = false;
        var user = CreateUser(userId, role: UserRole.Admin);

        _userRepositoryMock
            .Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result result = await service.UpdateUserAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("You cannot deactivate your own administrator account.", result.Error);
    }

    [Fact]
    public async Task UpdateUserAsync_LastActiveAdmin_CannotBeDeactivated_ReturnsConflict()
    {
        // Arrange
        const int userId = 99;
        var dto = CreateValidUpdateUserDto();
        dto.IsActive = false;
        var user = CreateUser(userId, role: UserRole.Admin);

        _userRepositoryMock
            .Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.GetActiveAdminCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Cannot deactivate the last active administrator.", result.Error);
    }

    [Fact]
    public async Task DeleteUserAsync_PrimaryAdminAccount_CannotBeDeleted_ReturnsForbidden()
    {
        // Arrange
        const int userId = 2;
        var user = CreateUser(userId, role: UserRole.Admin);

        _userRepositoryMock
            .Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result result = await _sut.DeleteUserAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("The primary administrator account cannot be deleted.", result.Error);
    }

    [Fact]
    public async Task DeleteUserAsync_CurrentAdminAccount_CannotDeleteSelf_ReturnsForbidden()
    {
        // Arrange
        const int userId = 7;
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(c => c.IsLoggedIn).Returns(true);
        currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
        currentUserServiceMock.Setup(c => c.Role).Returns(UserRole.Admin);

        var service = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _sessionRepositoryMock.Object,
            _clock,
            currentUserServiceMock.Object);

        var user = CreateUser(userId, role: UserRole.Admin);

        _userRepositoryMock
            .Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result result = await service.DeleteUserAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("You cannot delete your own administrator account.", result.Error);
    }

    [Fact]
    public async Task DeleteUserAsync_LastActiveAdmin_CannotBeDeleted_ReturnsConflict()
    {
        // Arrange
        const int userId = 99;
        var user = CreateUser(userId, role: UserRole.Admin);

        _userRepositoryMock
            .Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.GetActiveAdminCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result result = await _sut.DeleteUserAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Cannot delete the last administrator account.", result.Error);
    }

    [Fact]
    public async Task UpdateUserAsync_PrimaryAdminAccount_CannotBeRenamedOrRelinked_ReturnsForbidden()
    {
        // Arrange
        const int userId = 2;
        var user = CreateUser(userId, personId: 100, userName: "ayhm", role: UserRole.Admin);
        var dto = new UpdateUserDto
        {
            PersonId = 100,
            UserName = "renamed_admin",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("The primary administrator account cannot be modified.", result.Error);
    }

    [Fact]
    public async Task AddUserAsync_WhenCallerIsStaff_ReturnsForbidden()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(c => c.IsLoggedIn).Returns(true);
        currentUserServiceMock.Setup(c => c.UserId).Returns(15);
        currentUserServiceMock.Setup(c => c.Role).Returns(UserRole.Staff);

        var service = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _sessionRepositoryMock.Object,
            _clock,
            currentUserServiceMock.Object);

        var dto = new CreateUserDto
        {
            PersonId = 200,
            UserName = "escalated_admin",
            Password = "Password123!",
            IsActive = true,
            Role = UserRole.Admin
        };

        // Act
        Result<int> result = await service.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _userRepositoryMock.Verify(r => r.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddUserAsync_InvalidRole_ReturnsValidationFailure()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            PersonId = 200,
            UserName = "valid_admin",
            Password = "Password123!",
            IsActive = true,
            Role = (UserRole)99
        };

        // Act
        Result<int> result = await _sut.AddUserAsync(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains("Invalid user role.", result.Error);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Staff)]
    public async Task AddUserAsync_AdminCaller_PersistsSelectedRole(UserRole selectedRole)
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(c => c.IsLoggedIn).Returns(true);
        currentUserServiceMock.Setup(c => c.UserId).Returns(2);
        currentUserServiceMock.Setup(c => c.Role).Returns(UserRole.Admin);

        var service = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _sessionRepositoryMock.Object,
            _clock,
            currentUserServiceMock.Object);

        User? addedUser = null;

        _userRepositoryMock
            .Setup(r => r.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) =>
            {
                u.UserId = 501;
                addedUser = u;
            })
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new CreateUserDto
        {
            PersonId = 300,
            UserName = "role_select",
            Password = "Password123!",
            IsActive = true,
            Role = selectedRole
        };

        // Act
        Result<int> result = await service.AddUserAsync(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(501, result.Value);
        Assert.NotNull(addedUser);
        Assert.Equal(selectedRole, addedUser.Role);
        Assert.NotEqual("Password123!", addedUser.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123!", addedUser.Password));
    }

    [Fact]
    public async Task UpdateUserAsync_AdminPromotesStaffToAdmin_PersistsRole()
    {
        // Arrange
        const int userId = 40;
        var user = CreateUser(userId, personId: 40, userName: "staff_one", role: UserRole.Staff);

        _userRepositoryMock.Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.IsUsernameTakenForAnotherUserAsync("staff_one", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dto = new UpdateUserDto { PersonId = 40, UserName = "staff_one", IsActive = true, Role = UserRole.Admin };

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_AdminDemotesAdminWhenOthersExist_PersistsStaffRole()
    {
        // Arrange
        const int userId = 41;
        var user = CreateUser(userId, personId: 41, userName: "second_admin", role: UserRole.Admin);

        _userRepositoryMock.Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.GetActiveAdminCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _userRepositoryMock.Setup(r => r.IsUsernameTakenForAnotherUserAsync("second_admin", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dto = new UpdateUserDto { PersonId = 41, UserName = "second_admin", IsActive = true, Role = UserRole.Staff };

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Staff, user.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_NullRole_KeepsExistingRole()
    {
        // Arrange
        const int userId = 42;
        var user = CreateUser(userId, personId: 42, userName: "keeps_role", role: UserRole.Admin);

        _userRepositoryMock.Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.IsUsernameTakenForAnotherUserAsync("renamed_keep", userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dto = new UpdateUserDto { PersonId = 42, UserName = "renamed_keep", IsActive = true, Role = null };

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Admin, user.Role);
        _userRepositoryMock.Verify(r => r.GetActiveAdminCountAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_SelfDemotion_ReturnsForbidden()
    {
        // Arrange
        const int userId = 43;
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(c => c.IsLoggedIn).Returns(true);
        currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
        currentUserServiceMock.Setup(c => c.Role).Returns(UserRole.Admin);

        var service = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _sessionRepositoryMock.Object,
            _clock,
            currentUserServiceMock.Object);

        var user = CreateUser(userId, personId: 43, userName: "self_admin", role: UserRole.Admin);
        _userRepositoryMock.Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.GetActiveAdminCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);

        var dto = new UpdateUserDto { PersonId = 43, UserName = "self_admin", IsActive = true, Role = UserRole.Staff };

        // Act
        Result result = await service.UpdateUserAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("You cannot change your own role.", result.Error);
        Assert.Equal(UserRole.Admin, user.Role);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_DemotingLastActiveAdmin_ReturnsConflict()
    {
        // Arrange
        const int userId = 44;
        var user = CreateUser(userId, personId: 44, userName: "only_admin", role: UserRole.Admin);

        _userRepositoryMock.Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.GetActiveAdminCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dto = new UpdateUserDto { PersonId = 44, UserName = "only_admin", IsActive = true, Role = UserRole.Staff };

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Cannot change the role of the last active administrator.", result.Error);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_PrimaryAdminRoleChange_ReturnsForbidden()
    {
        // Arrange
        const int userId = 2;
        var user = CreateUser(userId, personId: 100, userName: "ayhm", role: UserRole.Admin);
        _userRepositoryMock.Setup(r => r.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var dto = new UpdateUserDto { PersonId = 100, UserName = "ayhm", IsActive = true, Role = UserRole.Staff };

        // Act
        Result result = await _sut.UpdateUserAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("The primary administrator account role cannot be changed.", result.Error);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_StaffCallerAttemptsRoleChange_ReturnsForbidden()
    {
        // Arrange
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(c => c.IsLoggedIn).Returns(true);
        currentUserServiceMock.Setup(c => c.UserId).Returns(15);
        currentUserServiceMock.Setup(c => c.Role).Returns(UserRole.Staff);

        var service = new UserService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _sessionRepositoryMock.Object,
            _clock,
            currentUserServiceMock.Object);

        var dto = new UpdateUserDto { PersonId = 15, UserName = "staff_self", IsActive = true, Role = UserRole.Admin };

        // Act
        Result result = await service.UpdateUserAsync(15, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        _userRepositoryMock.Verify(r => r.GetUserForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_InvalidRole_ReturnsValidationFailure()
    {
        var dto = new UpdateUserDto { PersonId = 15, UserName = "valid_name", IsActive = true, Role = (UserRole)9 };

        Result result = await _sut.UpdateUserAsync(15, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains("Invalid user role.", result.Error);
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                    It.IsAny<int>(), It.IsAny<CancellationToken>()),
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _sessionRepositoryMock.Verify(
            sessionRepo =>
                sessionRepo.RevokeAllForUserAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsValidationFailure()
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
        var originalPasswordHash = user.Password;
        var originalSecurityStamp = user.SecurityStamp;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Current password is incorrect.",
            result.Error);
        Assert.Equal(originalPasswordHash, user.Password);
        Assert.Equal(originalSecurityStamp, user.SecurityStamp);

        _sessionRepositoryMock.Verify(
            sessionRepo =>
                sessionRepo.RevokeAllForUserAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
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
        var oldSecurityStamp = user.SecurityStamp;

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result result =
            await _sut.ChangePasswordAsync(
                userId,
                dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);

        Assert.NotEqual(oldHash, user.Password);
        Assert.NotEqual(oldSecurityStamp, user.SecurityStamp);
        Assert.False(string.IsNullOrWhiteSpace(user.SecurityStamp));

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
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);

        _sessionRepositoryMock.Verify(
            sessionRepo =>
                sessionRepo.RevokeAllForUserAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    SessionRevocationReasons.PasswordChanged,
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenSessionRevocationThrows_RollsBackTransactionAndPropagates()
    {
        // Arrange
        const int userId = 10;
        var dto = CreateValidChangePasswordDto();
        var user = CreateUser(userId, password: dto.CurrentPassword);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _sessionRepositoryMock
            .Setup(sessionRepo =>
                sessionRepo.RevokeAllForUserAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    SessionRevocationReasons.PasswordChanged,
                    It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Session revocation failed."));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ChangePasswordAsync(userId, dto));

        Assert.Equal("Session revocation failed.", ex.Message);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCommitThrows_RollsBackTransactionAndPropagates()
    {
        // Arrange
        const int userId = 10;
        var dto = CreateValidChangePasswordDto();
        var user = CreateUser(userId, password: dto.CurrentPassword);

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _transactionMock
            .Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database write failure."));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ChangePasswordAsync(userId, dto));

        Assert.Equal("Database write failure.", ex.Message);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCancelledDuringCommit_RollsBackTransactionAndPropagates()
    {
        // Arrange
        const int userId = 10;
        var dto = CreateValidChangePasswordDto();
        var user = CreateUser(userId, password: dto.CurrentPassword);

        using var cts = new CancellationTokenSource();

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, cts.Token))
            .ReturnsAsync(user);

        _sessionRepositoryMock
            .Setup(sessionRepo =>
                sessionRepo.RevokeAllForUserAsync(
                    userId,
                    It.IsAny<DateTime>(),
                    SessionRevocationReasons.PasswordChanged,
                    cts.Token))
            .ReturnsAsync(1);

        _transactionMock
            .Setup(t => t.CommitAsync(cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.ChangePasswordAsync(userId, dto, cts.Token));

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCancelled_PropagatesCancellationWithoutCommit()
    {
        // Arrange
        const int userId = 10;
        var dto = CreateValidChangePasswordDto();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _userRepositoryMock
            .Setup(repository =>
                repository.GetUserForUpdateAsync(userId, cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _sut.ChangePasswordAsync(userId, dto, cts.Token));

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // =========================================================
    // IsUserActiveAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IsUserActiveAsync_InvalidUserId_ReturnsFalseWithoutCallingRepository(
        int invalidUserId)
    {
        bool result = await _sut.IsUserActiveAsync(invalidUserId);

        Assert.False(result);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserActiveAsync(
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsUserActiveAsync_ValidUserId_ReturnsRepositoryResult(
        bool expectedActive)
    {
        const int userId = 10;

        _userRepositoryMock
            .Setup(repository =>
                repository.IsUserActiveAsync(
                    userId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedActive);

        bool result = await _sut.IsUserActiveAsync(userId);

        Assert.Equal(expectedActive, result);

        _userRepositoryMock.Verify(
            repository =>
                repository.IsUserActiveAsync(
                    userId,
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
        string password = "Password123",
        UserRole role = UserRole.Staff)
    {
        return new User
        {
            UserId = userId,
            PersonId = personId,
            UserName = userName,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            Role = role,

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

    [Fact]
    public async Task GetAllUsersAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        _userRepositoryMock
            .Setup(r => r.GetAllUsersAsync(cts.Token))
            .ReturnsAsync(new List<User>());

        await _sut.GetAllUsersAsync(cts.Token);

        _userRepositoryMock.Verify(
            r => r.GetAllUsersAsync(cts.Token),
            Times.Once);
    }
}
