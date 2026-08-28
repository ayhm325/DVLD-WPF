using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;


    public UserService(
        IUserRepository userRepository)
    {
        _userRepository =
            userRepository
            ?? throw new ArgumentNullException(
                nameof(userRepository));
    }


    // =========================================================
    // GET ALL
    // =========================================================

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


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<UserDto>>
        GetUserByIdAsync(
            int id)
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


    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    public async Task<Result<UserDto>>
        GetUserByPersonIdAsync(
            int personId)
    {
        if (personId <= 0)
        {
            return Result<UserDto>
                .FromValidationFailure(
                    "Invalid person ID.");
        }

        var user =
            await _userRepository
                .GetUserByPersonIdAsync(
                    personId);

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


    // =========================================================
    // GET BY USERNAME
    // =========================================================

    public async Task<Result<UserDto>>
        GetUserByUsernameAsync(
            string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Result<UserDto>
                .FromValidationFailure(
                    "Username is required.");
        }

        username =
            username.Trim();

        var user =
            await _userRepository
                .GetUserByUsernameAsync(
                    username);

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


    // =========================================================
    // CHECK USER EXISTS
    // =========================================================

    public async Task<bool>
        IsUserExistsByIdAsync(
            int id)
    {
        if (id <= 0)
            return false;

        return await _userRepository
            .IsUserExistsByIdAsync(id);
    }


    // =========================================================
    // CHECK USERNAME
    // =========================================================

    public async Task<bool>
        IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            userId <= 0)
        {
            return false;
        }

        return await _userRepository
            .IsUsernameTakenForAnotherUserAsync(
                username.Trim(),
                userId);
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<int>>
        AddUserAsync(
            CreateUserDto dto)
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


        // -----------------------------------------------------
        // Username uniqueness
        // -----------------------------------------------------

        if (await _userRepository
            .IsUsernameTakenAsync(username))
        {
            return Result<int>
                .FromConflict(
                    "Username is already in use.");
        }


        // -----------------------------------------------------
        // One user per person
        // -----------------------------------------------------

        if (await _userRepository
            .IsUserExistsByPersonIdAsync(
                dto.PersonId))
        {
            return Result<int>
                .FromConflict(
                    "This person is already associated with a user account.");
        }


        // -----------------------------------------------------
        // Hash password
        // -----------------------------------------------------

        var hashedPassword =
            BCrypt.Net.BCrypt
                .HashPassword(dto.Password);


        var user =
            UserMapper.ToEntity(
                dto,
                hashedPassword);


        var userId =
            await _userRepository
                .AddUserAsync(user);


        if (userId <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create user.");
        }

        return Result<int>
            .Success(userId);
    }


    // =========================================================
    // UPDATE
    // =========================================================

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
                .GetUserByUserIdAsync(id);

        if (user is null)
        {
            return Result.NotFound(
                "User not found.");
        }

        var username =
            dto.UserName.Trim();


        // -----------------------------------------------------
        // Username uniqueness
        // -----------------------------------------------------

        if (await _userRepository
            .IsUsernameTakenForAnotherUserAsync(
                username,
                id))
        {
            return Result.Conflict(
                "Username is already in use by another user.");
        }


        // -----------------------------------------------------
        // Person uniqueness
        // -----------------------------------------------------

        if (user.PersonId != dto.PersonId &&
            await _userRepository
                .IsUserExistsByPersonIdAsync(
                    dto.PersonId))
        {
            return Result.Conflict(
                "This person is already associated with another user account.");
        }


        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------

        user.UserName =
            username;

        user.PersonId =
            dto.PersonId;

        user.IsActive =
            dto.IsActive;


        // IMPORTANT:
        // Password is intentionally NOT changed here.


        var success =
            await _userRepository
                .UpdateUserAsync(user);

        return success
            ? Result.Success()
            : Result.Failure(
                "Failed to update user data.");
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteUserAsync(
            int id)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid user ID.");
        }

        var exists =
            await _userRepository
                .IsUserExistsByIdAsync(id);

        if (!exists)
        {
            return Result.NotFound(
                "User not found.");
        }


        var success =
            await _userRepository
                .DeleteUserAsync(id);

        if (!success)
        {
            return Result.Conflict(
                "This user cannot be deleted because it is referenced by existing records. Deactivate the user instead.");
        }

        return Result.Success();
    }


    // =========================================================
    // AUTHENTICATE
    // =========================================================

    public async Task<bool>
        AuthenticateUserAsync(
            string username,
            string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        username =
            username.Trim();

        var user =
            await _userRepository
                .GetUserByUsernameAsync(
                    username);

        if (user is null ||
            !user.IsActive)
        {
            return false;
        }

        return BCrypt.Net.BCrypt
            .Verify(
                password,
                user.Password);
    }


    // =========================================================
    // CHANGE PASSWORD
    // =========================================================

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
                .GetUserByUserIdAsync(
                    userId);

        if (user is null)
        {
            return Result.NotFound(
                "User not found.");
        }


        // -----------------------------------------------------
        // Verify current password
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // Hash new password
        // -----------------------------------------------------

        user.Password =
            BCrypt.Net.BCrypt
                .HashPassword(
                    dto.NewPassword);


        var success =
            await _userRepository
                .UpdateUserAsync(user);

        return success
            ? Result.Success()
            : Result.Failure(
                "Failed to update password.");
    }


    // =========================================================
    // LOGIN
    // =========================================================

    public async Task<Result<UserDto>>
        LoginAsync(
            LoginRequestDto dto)
    {
        if (dto is null)
        {
            return Result<UserDto>
                .FromValidationFailure(
                    "Login data is required.");
        }

        var validation =
            UserValidator
                .ValidateLogin(dto);

        if (validation.IsFailure)
        {
            return Result<UserDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var username =
            dto.UserName.Trim();

        var user =
            await _userRepository
                .GetUserByUsernameAsync(
                    username);

        if (user is null)
        {
            return Result<UserDto>
                .FromFailure(
                    "Invalid username or password.");
        }

        if (!user.IsActive)
        {
            return Result<UserDto>
                .FromFailure(
                    "This user account is inactive.");
        }

        var passwordValid =
            BCrypt.Net.BCrypt
                .Verify(
                    dto.Password,
                    user.Password);

        if (!passwordValid)
        {
            return Result<UserDto>
                .FromFailure(
                    "Invalid username or password.");
        }

        return Result<UserDto>
            .Success(
                UserMapper.ToDto(user));
    }
}
