using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // ================= GET ALL =================
        public async Task<Result<List<UserDto>>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();

            return Result<List<UserDto>>.Success(
                [.. users.Select(MapToDto)]);
        }

        // ================= GET BY ID =================
        public async Task<Result<UserDto>> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetUserByUserIdAsync(id);

            if (u == null)
                return Result<UserDto>.Fail("المستخدم غير موجود.");

            return Result<UserDto>.Success(MapToDto(u));
        }

        public async Task<Result<UserDto>> GetUserByPersonIdAsync(int id)
        {
            var u = await _userRepository.GetUserByPersonIdAsync(id);

            if (u == null)
                return Result<UserDto>.Fail("لا يوجد مستخدم مرتبط بهذا الشخص.");

            return Result<UserDto>.Success(MapToDto(u));
        }

        public async Task<Result<UserDto>> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Result<UserDto>.Fail("اسم المستخدم مطلوب.");

            var u = await _userRepository.GetUserByUsernameAsync(username);

            if (u == null)
                return Result<UserDto>.Fail("المستخدم غير موجود.");

            return Result<UserDto>.Success(MapToDto(u));
        }

        // ================= EXISTS & CHECKS =================
        public async Task<bool> IsUserExistsByIdAsync(int id)
        {
            return await _userRepository.IsUserExistsByIdAsync(id);
        }

        public async Task<bool> IsUsernameTakenForAnotherUserAsync(string username, int userId)
        {
            return await _userRepository.IsUsernameTakenForAnotherUserAsync(username, userId);
        }

        // ================= ADD =================
        public async Task<Result<int>> AddUserAsync(CreateUserDto dto)
        {
            if (dto == null)
                return Result<int>.Fail("بيانات المستخدم مطلوبة.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return Result<int>.Fail("كلمة المرور مطلوبة.");

            // التأكد من عدم تكرار اسم المستخدم
            if (await _userRepository.IsUsernameTakenForAnotherUserAsync(dto.UserName, 0))
                return Result<int>.Fail("اسم المستخدم مستخدم بالفعل.");

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = MapToEntity(dto, hashedPassword);

            int id = await _userRepository.AddUserAsync(user);

            return Result<int>.Success(id);
        }

        // ================= UPDATE =================
        public async Task<Result> UpdateUserAsync(int id, CreateUserDto dto)
        {
            if (id <= 0)
                return Result.Failure("معرف المستخدم غير صالح.");

            if (dto == null)
                return Result.Failure("بيانات المستخدم مطلوبة.");

            var user = await _userRepository.GetUserByUserIdAsync(id);

            if (user is null)
                return Result.Failure("المستخدم غير موجود.");

            // التأكد من عدم تكرار اسم المستخدم لمستخدم آخر
            if (await _userRepository.IsUsernameTakenForAnotherUserAsync(dto.UserName, id))
                return Result.Failure("اسم المستخدم مستخدم من قبل مستخدم آخر.");

            user.UserName = dto.UserName;
            user.IsActive = dto.IsActive;
            user.PersonId = dto.PersonId;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            var isSuccess = await _userRepository.UpdateUserAsync(user);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث بيانات المستخدم.");
        }

        // ================= DELETE =================
        public async Task<Result> DeleteUserAsync(int id)
        {
            if (!await _userRepository.IsUserExistsByIdAsync(id))
                return Result.Failure("المستخدم غير موجود.");

            var isSuccess = await _userRepository.DeleteUserAsync(id);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في حذف المستخدم.");
        }

        // ================= AUTHENTICATE =================
        // بقيت Task<bool> لأسباب أمنية حتى لا نعطي رسائل تساعد على معرفة إذا كان المستخدم موجوداً أم لا
        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null) return false;

            return BCrypt.Net.BCrypt.Verify(password, user.Password);
        }

        // ================= CHANGE PASSWORD =================
        public async Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return Result.Failure("كلمة المرور الجديدة مطلوبة.");

            var user = await _userRepository.GetUserByUserIdAsync(userId);

            if (user == null)
                return Result.Failure("المستخدم غير موجود.");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
                return Result.Failure("كلمة المرور الحالية غير صحيحة.");

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            var isSuccess = await _userRepository.UpdateUserAsync(user);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث كلمة المرور.");
        }

        // ================= LOGIN =================
        public async Task<Result<UserDto>> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Result<UserDto>.Fail("اسم المستخدم وكلمة المرور مطلوبان.");

            var isAuthenticated = await AuthenticateUserAsync(username, password);

            if (!isAuthenticated)
                return Result<UserDto>.Fail("اسم المستخدم أو كلمة المرور غير صحيحة.");

            var userResult = await GetUserByUsernameAsync(username);

            if (userResult.IsFailure)
                return Result<UserDto>.Fail("حدث خطأ أثناء جلب بيانات المستخدم.");

            return Result<UserDto>.Success(userResult.Value);
        }

        // ================= MAPPING =================
        private UserDto MapToDto(User u)
        {
            return new UserDto
            {
                UserId = u.UserId,
                PersonId = u.PersonId,
                UserName = u.UserName,
                Password = u.Password,
                IsActive = u.IsActive,
                FullName = u.Person != null
                    ? $"{u.Person.FirstName} {u.Person.SecondName} {u.Person.ThirdName} {u.Person.LastName}"
                    : string.Empty
            };
        }

        private User MapToEntity(CreateUserDto dto, string hashedPassword)
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