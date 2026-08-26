using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Mappings;
using Application.Validators;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    // Get All
    public async Task<Result<List<UserDto>>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return Result<List<UserDto>>.Success(users.Select(UserMapper.ToDto).ToList());
    }

    // Get By Id
    public async Task<Result<UserDto>> GetUserByIdAsync(int id)
    {
        if (id <= 0) return Result<UserDto>.FromFailure("Invalid user ID.");

        var user = await _userRepository.GetUserByUserIdAsync(id);
        return user is null
            ? Result<UserDto>.FromFailure("User not found.")
            : Result<UserDto>.Success(UserMapper.ToDto(user));
    }

    // Get By Person Id
    public async Task<Result<UserDto>> GetUserByPersonIdAsync(int personId)
    {
        if (personId <= 0) return Result<UserDto>.FromFailure("Invalid person ID.");

        var user = await _userRepository.GetUserByPersonIdAsync(personId);
        return user is null
            ? Result<UserDto>.FromFailure("No user is associated with this person.")
            : Result<UserDto>.Success(UserMapper.ToDto(user));
    }

    // Get By Username
    public async Task<Result<UserDto>> GetUserByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<UserDto>.FromFailure("Username is required.");

        var user = await _userRepository.GetUserByUsernameAsync(username.Trim());
        return user is null
            ? Result<UserDto>.FromFailure("User not found.")
            : Result<UserDto>.Success(UserMapper.ToDto(user));
    }

    // Checks
    public async Task<bool> IsUserExistsByIdAsync(int id) =>
        id > 0 && await _userRepository.IsUserExistsByIdAsync(id);

    public async Task<bool> IsUsernameTakenForAnotherUserAsync(string username, int userId) =>
        !string.IsNullOrWhiteSpace(username) &&
        await _userRepository.IsUsernameTakenForAnotherUserAsync(username.Trim(), userId);

    // Create
    public async Task<Result<int>> AddUserAsync(CreateUserDto dto)
    {
        if (dto is null) return Result<int>.FromFailure("User data is required.");

        var validation = UserValidator.ValidateCreateUser(dto);
        if (validation.IsFailure) return Result<int>.FromFailure(validation.Error);

        // التحقق من تفرد اسم المستخدم
        if (await _userRepository.IsUsernameTakenAsync(dto.UserName))
            return Result<int>.FromFailure("Username is already in use.");

        // التحقق من أن لكل شخص حساب مستخدم واحد فقط
        if (await _userRepository.IsUserExistsByPersonIdAsync(dto.PersonId))
            return Result<int>.FromFailure("This person is already associated with a user account.");

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var user = UserMapper.ToEntity(dto, hashedPassword);

        var userId = await _userRepository.AddUserAsync(user);
        return Result<int>.Success(userId);
    }

    // Update
    public async Task<Result> UpdateUserAsync(int id, UpdateUserDto dto)
    {
        if (id <= 0) return Result.Failure("Invalid user ID.");
        if (dto is null) return Result.Failure("User data is required.");

        var validation = UserValidator.ValidateUpdateUser(dto);
        if (validation.IsFailure) return Result.Failure(validation.Error);

        var user = await _userRepository.GetUserByUserIdAsync(id);
        if (user is null) return Result.Failure("User not found.");

        // التحقق من تفرد اسم المستخدم لغير المستخدم الحالي
        if (await _userRepository.IsUsernameTakenForAnotherUserAsync(dto.UserName, id))
            return Result.Failure("Username is already in use by another user.");

        // التحقق من تفرد الشخص في حال تم تغييره
        if (user.PersonId != dto.PersonId && await _userRepository.IsUserExistsByPersonIdAsync(dto.PersonId))
            return Result.Failure("This person is already associated with another user account.");

        // تحديث البيانات
        user.UserName = dto.UserName.Trim();
        user.PersonId = dto.PersonId;
        user.IsActive = dto.IsActive;

        // تحديث كلمة المرور اختيارياً
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var success = await _userRepository.UpdateUserAsync(user);
        return success ? Result.Success() : Result.Failure("Failed to update user data.");
    }

    // Delete
    public async Task<Result> DeleteUserAsync(int id)
    {
        if (id <= 0) return Result.Failure("Invalid user ID.");
        if (!await _userRepository.IsUserExistsByIdAsync(id))
            return Result.Failure("User not found.");

        var success = await _userRepository.DeleteUserAsync(id);
        return success ? Result.Success() : Result.Failure("Failed to delete user.");
    }

    // Authenticate
    public async Task<bool> AuthenticateUserAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        var user = await _userRepository.GetUserByUsernameAsync(username.Trim());
        if (user is null || !user.IsActive) return false;

        return BCrypt.Net.BCrypt.Verify(password, user.Password);
    }

    // Change Password
    public async Task<Result> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        if (userId <= 0) return Result.Failure("Invalid user ID.");
        if (dto is null) return Result.Failure("Change password data is required.");

        var validation = UserValidator.ValidateChangePassword(dto);
        if (validation.IsFailure) return Result.Failure(validation.Error);

        var user = await _userRepository.GetUserByUserIdAsync(userId);
        if (user is null) return Result.Failure("User not found.");

        // التحقق من كلمة المرور الحالية
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
            return Result.Failure("Current password is incorrect.");

        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        var success = await _userRepository.UpdateUserAsync(user);
        return success ? Result.Success() : Result.Failure("Failed to update password.");
    }

    // Login
    public async Task<Result<UserDto>> LoginAsync(LoginRequestDto dto)
    {
        if (dto is null) return Result<UserDto>.FromFailure("Login data is required.");

        var validation = UserValidator.ValidateLogin(dto);
        if (validation.IsFailure) return Result<UserDto>.FromFailure(validation.Error);

        var user = await _userRepository.GetUserByUsernameAsync(dto.UserName.Trim());

        if (user is null)
            return Result<UserDto>.FromFailure("Invalid username or password.");
        if (!user.IsActive)
            return Result<UserDto>.FromFailure("This user account is inactive.");
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return Result<UserDto>.FromFailure("Invalid username or password.");

        return Result<UserDto>.Success(UserMapper.ToDto(user));
    }
}