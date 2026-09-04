using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    {
        _userRepository =
            userRepository
            ?? throw new ArgumentNullException(
                nameof(userRepository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    public async Task<Result<List<UserDto>>>
        GetAllUsersAsync()
    {
        var users =
            await _userRepository
                .GetAllUsersAsync();

        var dtos =
            users
                .Select(UserMapper.ToDto)
                .ToList();

        return Result<List<UserDto>>
            .Success(dtos);
    }

    public async Task<Result<UserDto>>
        GetUserByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Result<UserDto>
                .FromValidationFailure(
                    "Invalid user ID.");
        }

        var user =
            await _userRepository
                .GetUserByUserIdAsync(id);

        if (user is null)
        {
            return Result<UserDto>
                .FromNotFound(
                    "User not found.");
        }

        return Result<UserDto>
            .Success(
                UserMapper.ToDto(user));
    }

    public async Task<Result<UserDto>>
        GetUserByPersonIdAsync(int personId)
    {
        if (personId <= 0)
        {
            return Result<UserDto>
                .FromValidationFailure(
                    "Invalid person ID.");
        }

        var user =
            await _userRepository
                .GetUserByPersonIdAsync(personId);

        if (user is null)
        {
            return Result<UserDto>
                .FromNotFound(
                    "No user is associated with this person.");
        }

        return Result<UserDto>
            .Success(
                UserMapper.ToDto(user));
    }

    public async Task<Result<UserDto>>
        GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result<UserDto>
                .FromValidationFailure(
                    "Username is required.");
        }

        var normalizedUsername =
            username.Trim();

        var user =
            await _userRepository
                .GetUserByUsernameAsync(
                    normalizedUsername);

        if (user is null)
        {
            return Result<UserDto>
                .FromNotFound(
                    "User not found.");
        }

        return Result<UserDto>
            .Success(
                UserMapper.ToDto(user));
    }

    public Task<bool>
        IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            userId <= 0)
        {
            return Task.FromResult(false);
        }

        return _userRepository
            .IsUsernameTakenForAnotherUserAsync(
                username.Trim(),
                userId);
    }

    public async Task<Result<int>>
        AddUserAsync(CreateUserDto dto)
    {
        if (dto is null)
        {
            return Result<int>
                .FromValidationFailure(
                    "User data is required.");
        }

        var validation =
            UserValidator
                .ValidateCreateUser(dto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    validation.Error);
        }

        var username =
            dto.UserName.Trim();

        if (await _userRepository
                .IsUsernameTakenAsync(username))
        {
            return Result<int>
                .FromConflict(
                    "Username is already in use.");
        }

        if (await _userRepository
                .IsUserExistsByPersonIdAsync(
                    dto.PersonId))
        {
            return Result<int>
                .FromConflict(
                    "This person is already associated with a user account.");
        }

        var hashedPassword =
            BCrypt.Net.BCrypt
                .HashPassword(
                    dto.Password);

        var user =
            UserMapper.ToEntity(
                dto,
                hashedPassword);

        await _userRepository
            .AddUserAsync(user);

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0 ||
            user.UserId <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create user.");
        }

        return Result<int>
            .Success(
                user.UserId);
    }

    public async Task<Result>
        UpdateUserAsync(
            int id,
            UpdateUserDto dto)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid user ID.");
        }

        if (dto is null)
        {
            return Result.ValidationFailure(
                "User data is required.");
        }

        var validation =
            UserValidator
                .ValidateUpdateUser(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var user =
            await _userRepository
                .GetUserForUpdateAsync(id);

        if (user is null)
        {
            return Result.NotFound(
                "User not found.");
        }

        var username =
            dto.UserName.Trim();

        if (await _userRepository
                .IsUsernameTakenForAnotherUserAsync(
                    username,
                    id))
        {
            return Result.Conflict(
                "Username is already in use by another user.");
        }

        if (user.PersonId != dto.PersonId &&
            await _userRepository
                .IsUserExistsByPersonIdAsync(
                    dto.PersonId))
        {
            return Result.Conflict(
                "This person is already associated with another user account.");
        }

        user.UserName =
            username;

        user.PersonId =
            dto.PersonId;

        user.IsActive =
            dto.IsActive;

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result.Failure(
                "No user changes were saved.");
        }

        return Result.Success();
    }

    public async Task<Result>
        DeleteUserAsync(int id)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid user ID.");
        }

        var user =
            await _userRepository
                .GetUserForUpdateAsync(id);

        if (user is null)
        {
            return Result.NotFound(
                "User not found.");
        }

        _userRepository
            .DeleteUser(user);

        try
        {
            var saved =
                await _unitOfWork
                    .SaveChangesAsync();

            if (saved <= 0)
            {
                return Result.Failure(
                    "Failed to save user deletion.");
            }

            return Result.Success();
        }
        catch
        {
            return Result.Conflict(
                "This user cannot be deleted because it is referenced by existing records. Deactivate the user instead.");
        }
    }

    public async Task<Result>
        ChangePasswordAsync(
            int userId,
            ChangePasswordDto dto)
    {
        if (userId <= 0)
        {
            return Result.ValidationFailure(
                "Invalid user ID.");
        }

        if (dto is null)
        {
            return Result.ValidationFailure(
                "Change password data is required.");
        }

        var validation =
            UserValidator
                .ValidateChangePassword(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var user =
            await _userRepository
                .GetUserForUpdateAsync(userId);

        if (user is null)
        {
            return Result.NotFound(
                "User not found.");
        }

        var currentPasswordValid =
            BCrypt.Net.BCrypt
                .Verify(
                    dto.CurrentPassword,
                    user.Password);

        if (!currentPasswordValid)
        {
            return Result.Failure(
                "Current password is incorrect.");
        }

        user.Password =
            BCrypt.Net.BCrypt
                .HashPassword(
                    dto.NewPassword);

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result.Failure(
                "Failed to save password change.");
        }

        return Result.Success();
    }

}
