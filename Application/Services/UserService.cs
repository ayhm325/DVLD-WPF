using Application.Common.Results;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));
        }

        // =========================================================
        // GET ALL
        // =========================================================

        public async Task<Result<List<UserDto>>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();

            var userDtos = users
                .Select(MapToDto)
                .ToList();

            return Result<List<UserDto>>.Success(userDtos);
        }

        // =========================================================
        // GET BY ID
        // =========================================================

        public async Task<Result<UserDto>> GetUserByIdAsync(int id)
        {
            if (id <= 0)
            {
                return Result<UserDto>.Fail(
                    "Invalid user ID.");
            }

            var user =
                await _userRepository.GetUserByUserIdAsync(id);

            if (user is null)
            {
                return Result<UserDto>.Fail(
                    "User not found.");
            }

            return Result<UserDto>.Success(
                MapToDto(user));
        }

        // =========================================================
        // GET BY PERSON ID
        // =========================================================

        public async Task<Result<UserDto>> GetUserByPersonIdAsync(
            int personId)
        {
            if (personId <= 0)
            {
                return Result<UserDto>.Fail(
                    "Invalid person ID.");
            }

            var user =
                await _userRepository.GetUserByPersonIdAsync(personId);

            if (user is null)
            {
                return Result<UserDto>.Fail(
                    "No user is associated with this person.");
            }

            return Result<UserDto>.Success(
                MapToDto(user));
        }

        // =========================================================
        // GET BY USERNAME
        // =========================================================

        public async Task<Result<UserDto>> GetUserByUsernameAsync(
            string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Result<UserDto>.Fail(
                    "Username is required.");
            }

            var user =
                await _userRepository.GetUserByUsernameAsync(username);

            if (user is null)
            {
                return Result<UserDto>.Fail(
                    "User not found.");
            }

            return Result<UserDto>.Success(
                MapToDto(user));
        }

        // =========================================================
        // CHECKS
        // =========================================================

        public async Task<bool> IsUserExistsByIdAsync(int id)
        {
            if (id <= 0)
                return false;

            return await _userRepository
                .IsUserExistsByIdAsync(id);
        }

        public async Task<bool> IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return await _userRepository
                .IsUsernameTakenForAnotherUserAsync(
                    username,
                    userId);
        }

        // =========================================================
        // ADD USER
        // =========================================================

        public async Task<Result<int>> AddUserAsync(
            CreateUserDto dto)
        {
            if (dto is null)
            {
                return Result<int>.Fail(
                    "User data is required.");
            }

            var validation =
                UserValidator.ValidateCreateUser(dto);

            if (!validation.IsValid)
            {
                return Result<int>.Fail(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            // Username must be unique
            if (await _userRepository
                .IsUsernameTakenAsync(dto.UserName))
            {
                return Result<int>.Fail(
                    "Username is already in use.");
            }

            // One person = one user account
            if (await _userRepository
                .IsUserExistsByPersonIdAsync(dto.PersonId))
            {
                return Result<int>.Fail(
                    "This person is already associated with a user account.");
            }

            // Hash password
            var hashedPassword =
                BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user =
                MapToEntity(dto, hashedPassword);

            var userId =
                await _userRepository.AddUserAsync(user);

            return Result<int>.Success(userId);
        }

        // =========================================================
        // UPDATE USER
        // =========================================================

        public async Task<Result> UpdateUserAsync(
            int id,
            UpdateUserDto dto)
        {
            if (id <= 0)
            {
                return Result.Failure(
                    "Invalid user ID.");
            }

            if (dto is null)
            {
                return Result.Failure(
                    "User data is required.");
            }

            var validation =
                UserValidator.ValidateUpdateUser(dto);

            if (!validation.IsValid)
            {
                return Result.Failure(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            var user =
                await _userRepository
                    .GetUserByUserIdAsync(id);

            if (user is null)
            {
                return Result.Failure(
                    "User not found.");
            }

            // Username uniqueness
            if (await _userRepository
                .IsUsernameTakenForAnotherUserAsync(
                    dto.UserName,
                    id))
            {
                return Result.Failure(
                    "Username is already in use by another user.");
            }

            // Person uniqueness
            if (user.PersonId != dto.PersonId &&
                await _userRepository
                    .IsUserExistsByPersonIdAsync(dto.PersonId))
            {
                return Result.Failure(
                    "This person is already associated with another user account.");
            }

            user.UserName = dto.UserName;
            user.PersonId = dto.PersonId;
            user.IsActive = dto.IsActive;

            // Password is optional during update
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.Password =
                    BCrypt.Net.BCrypt.HashPassword(
                        dto.Password);
            }

            var success =
                await _userRepository.UpdateUserAsync(user);

            return success
                ? Result.Success()
                : Result.Failure(
                    "Failed to update user data.");
        }

        // =========================================================
        // DELETE USER
        // =========================================================

        public async Task<Result> DeleteUserAsync(int id)
        {
            if (id <= 0)
            {
                return Result.Failure(
                    "Invalid user ID.");
            }

            var exists =
                await _userRepository
                    .IsUserExistsByIdAsync(id);

            if (!exists)
            {
                return Result.Failure(
                    "User not found.");
            }

            var success =
                await _userRepository.DeleteUserAsync(id);

            return success
                ? Result.Success()
                : Result.Failure(
                    "Failed to delete user.");
        }

        // =========================================================
        // AUTHENTICATE
        // =========================================================

        public async Task<bool> AuthenticateUserAsync(
            string username,
            string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var user =
                await _userRepository
                    .GetUserByUsernameAsync(username);

            if (user is null)
                return false;

            if (!user.IsActive)
                return false;

            return BCrypt.Net.BCrypt.Verify(
                password,
                user.Password);
        }

        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        public async Task<Result> ChangePasswordAsync(
            int userId,
            ChangePasswordDto dto)
        {
            if (userId <= 0)
            {
                return Result.Failure(
                    "Invalid user ID.");
            }

            if (dto is null)
            {
                return Result.Failure(
                    "Change password data is required.");
            }

            var validation =
                UserValidator.ValidateChangePassword(dto);

            if (!validation.IsValid)
            {
                return Result.Failure(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            var user =
                await _userRepository
                    .GetUserByUserIdAsync(userId);

            if (user is null)
            {
                return Result.Failure(
                    "User not found.");
            }

            // Verify current password
            var currentPasswordValid =
                BCrypt.Net.BCrypt.Verify(
                    dto.CurrentPassword,
                    user.Password);

            if (!currentPasswordValid)
            {
                return Result.Failure(
                    "Current password is incorrect.");
            }

            // Hash new password
            user.Password =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.NewPassword);

            var success =
                await _userRepository.UpdateUserAsync(user);

            return success
                ? Result.Success()
                : Result.Failure(
                    "Failed to update password.");
        }

        // =========================================================
        // LOGIN
        // =========================================================

        public async Task<Result<UserDto>> LoginAsync(
            LoginRequestDto dto)
        {
            if (dto is null)
            {
                return Result<UserDto>.Fail(
                    "Login data is required.");
            }

            var validation =
                UserValidator.ValidateLogin(dto);

            if (!validation.IsValid)
            {
                return Result<UserDto>.Fail(
                    string.Join(
                        Environment.NewLine,
                        validation.Errors));
            }

            var user =
                await _userRepository
                    .GetUserByUsernameAsync(dto.UserName);

            // Don't reveal whether username exists
            if (user is null)
            {
                return Result<UserDto>.Fail(
                    "Invalid username or password.");
            }

            if (!user.IsActive)
            {
                return Result<UserDto>.Fail(
                    "This user account is inactive.");
            }

            var passwordValid =
                BCrypt.Net.BCrypt.Verify(
                    dto.Password,
                    user.Password);

            if (!passwordValid)
            {
                return Result<UserDto>.Fail(
                    "Invalid username or password.");
            }

            return Result<UserDto>.Success(
                MapToDto(user));
        }

        // =========================================================
        // MAPPING ENTITY -> DTO
        // =========================================================

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,

                PersonId = user.PersonId,

                UserName = user.UserName,

                IsActive = user.IsActive,

                FullName = user.Person is null
                    ? string.Empty
                    : string.Join(
                        " ",
                        new[]
                        {
                            user.Person.FirstName,
                            user.Person.SecondName,
                            user.Person.ThirdName,
                            user.Person.LastName
                        }
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x)))
            };
        }

        // =========================================================
        // MAPPING CREATE DTO -> ENTITY
        // =========================================================

        private static User MapToEntity(
            CreateUserDto dto,
            string hashedPassword)
        {
            return new User
            {
                PersonId = dto.PersonId,
                UserName = dto.UserName,
                Password = hashedPassword,
                IsActive = dto.IsActive
            };
        }
    }
}