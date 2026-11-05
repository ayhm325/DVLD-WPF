using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService? _currentUserService;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
        : this(userRepository, unitOfWork, null)
    {
    }

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService? currentUserService)
    {
        _userRepository =
            userRepository
            ?? throw new ArgumentNullException(
                nameof(userRepository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        _currentUserService = currentUserService;
    }

    public async Task<Result<List<UserDto>>>
        GetAllUsersAsync()
    {
        if (IsNonAdminAuthenticatedCaller())
        {
            return Result<List<UserDto>>.FromForbidden(
                "Administrator privileges are required.");
        }

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

    public async Task<Result<UserDetailsDto>> GetUserDetailsAsync(int userId)
    {
        if (IsNonAdminAuthenticatedCaller())
        {
            return Result<UserDetailsDto>.FromForbidden(
                "Administrator privileges are required.");
        }

        if (userId <= 0)
        {
            return Result<UserDetailsDto>.FromValidationFailure(
                "User ID must be greater than zero.");
        }

        var user = await _userRepository
            .GetUserDetailsByIdAsync(userId);

        if (user is null)
        {
            return Result<UserDetailsDto>.FromNotFound(
                $"User with ID {userId} was not found.");
        }

        return Result<UserDetailsDto>.Success(user);
    }

    public async Task<Result<UserProfileDto>>
    GetCurrentProfileAsync(int userId)
    {
        if (userId <= 0)
        {
            return Result<UserProfileDto>
                .FromValidationFailure(
                    "Invalid user ID.");
        }

        var user =
            await _userRepository
                .GetUserProfileAsync(userId);

        if (user is null)
        {
            return Result<UserProfileDto>
                .FromNotFound(
                    "User not found.");
        }

        return Result<UserProfileDto>
            .Success(
                UserMapper.ToProfileDto(user));
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

    public Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return Task.FromResult(false);

        return _userRepository.IsUserActiveAsync(
            userId,
            cancellationToken);
    }

    public async Task<Result<int>>
        AddUserAsync(CreateUserDto dto)
    {
        if (IsNonAdminAuthenticatedCaller())
        {
            return Result<int>.FromForbidden(
                "Administrator privileges are required.");
        }

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
        if (IsNonAdminAuthenticatedCaller())
        {
            return Result.Forbidden(
                "Administrator privileges are required.");
        }

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

        // Account safety rules
        var targetRole = dto.Role ?? user.Role;
        var isRoleChange = targetRole != user.Role;

        if (id == 2 && user.Role == UserRole.Admin)
        {
            if (!dto.IsActive)
            {
                return Result.Forbidden(
                    "The primary administrator account cannot be deactivated.");
            }

            if (isRoleChange)
            {
                return Result.Forbidden(
                    "The primary administrator account role cannot be changed.");
            }

            if (string.Equals(user.UserName, "ayhm", StringComparison.OrdinalIgnoreCase) &&
                (!string.Equals(user.UserName, dto.UserName.Trim(), StringComparison.Ordinal) ||
                 user.PersonId != dto.PersonId))
            {
                return Result.Forbidden(
                    "The primary administrator account cannot be modified.");
            }
        }

        var isSelf =
            _currentUserService is { IsLoggedIn: true } &&
            _currentUserService.UserId == id;

        if (isSelf && !dto.IsActive)
        {
            return Result.Forbidden(
                "You cannot deactivate your own administrator account.");
        }

        if (isSelf && isRoleChange)
        {
            return Result.Forbidden(
                "You cannot change your own role.");
        }

        var removesActiveAdmin =
            user.Role == UserRole.Admin &&
            user.IsActive &&
            (!dto.IsActive || targetRole != UserRole.Admin);

        if (removesActiveAdmin)
        {
            var activeAdminCount = await _userRepository.GetActiveAdminCountAsync();
            if (activeAdminCount <= 1)
            {
                return Result.Conflict(
                    !dto.IsActive
                        ? "Cannot deactivate the last active administrator."
                        : "Cannot change the role of the last active administrator.");
            }
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

        bool hasChanges =
            !string.Equals(user.UserName, username, StringComparison.Ordinal) ||
            user.PersonId != dto.PersonId ||
            user.IsActive != dto.IsActive ||
            isRoleChange;

        if (!hasChanges)
        {
            return Result.Success();
        }

        user.UserName =
            username;

        user.PersonId =
            dto.PersonId;

        user.IsActive =
            dto.IsActive;

        user.Role =
            targetRole;

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
        if (IsNonAdminAuthenticatedCaller())
        {
            return Result.Forbidden(
                "Administrator privileges are required.");
        }

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

        // Account safety rules
        if (id == 2 && user.Role == UserRole.Admin)
        {
            return Result.Forbidden(
                "The primary administrator account cannot be deleted.");
        }

        if (_currentUserService != null && _currentUserService.IsLoggedIn && _currentUserService.UserId == id)
        {
            return Result.Forbidden(
                "You cannot delete your own administrator account.");
        }

        if (user.Role == UserRole.Admin)
        {
            var activeAdminCount = await _userRepository.GetActiveAdminCountAsync();
            if (activeAdminCount <= 1)
            {
                return Result.Conflict(
                    "Cannot delete the last administrator account.");
            }
        }

        _userRepository
            .DeleteUser(user);

        var saved = await _unitOfWork.SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure("Failed to save user deletion.");
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
            return Result.ValidationFailure("Current password is incorrect.");
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

    private bool IsNonAdminAuthenticatedCaller() =>
        _currentUserService is { IsLoggedIn: true } &&
        _currentUserService.Role != UserRole.Admin;
}
