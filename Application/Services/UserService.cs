using Application.DTOs;
using Application.Interfaces;
using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserRepository _userRepository;
       

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
           
        }

        // ================= GET ALL =================
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return [.. users.Select(MapToDto)];
        }

        // ================= GET BY ID =================
        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetUserByUserIdAsync(id);
            return u == null ? null : MapToDto(u);
        }

        public async Task<UserDto?> GetUserByPersonIdAsync(int id)
        {
            var u = await _userRepository.GetUserByPersonIdAsync(id);
            return u == null ? null : MapToDto(u);
        }

        public async Task<UserDto?> GetUserByUsernameAsync(string username)
        {
            var u = await _userRepository.GetUserByUsernameAsync(username);
            return u == null ? null : MapToDto(u);
        }

        //================= EXISTS =================
        public async Task<bool> IsUserExistsByIdAsync(int id)
        {
            return await _userRepository.IsUserExistsByIdAsync(id);
        }

        public  async Task<bool> IsUsernameTakenForAnotherUserAsync(string username, int userId)
        {
            return await _userRepository.IsUsernameTakenForAnotherUserAsync(username, userId);
        }


        // ================= ADD =================
        public async Task<int> AddUserAsync(CreateUserDto dto)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var user = MapToEntity(dto, hashedPassword);
            return await _userRepository.AddUserAsync(user);
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateUserAsync(int id, CreateUserDto dto)
        {
            var user = await _userRepository.GetUserByUserIdAsync(id); // جلبنا الكائن
            if (user == null) return false;

            // تعديل الخصائص على نفس الكائن
            user.UserName = dto.UserName;
            user.IsActive = dto.IsActive;
            user.PersonId = dto.PersonId;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            // إرسال الكائن نفسه للـ Repository
            return await _userRepository.UpdateUserAsync(user);
        }

        // ================= DELETE =================
        public async Task<bool> DeleteUserAsync(int id)
        {
            if (!await _userRepository.IsUserExistsByIdAsync(id))
                return false;

            return await _userRepository.DeleteUserAsync(id);
        }

        // ================= AUTHENTICATE =================
        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);
            if (user == null) return false;

            // استخدام BCrypt للتحقق من أن كلمة المرور المدخلة تطابق الـ Hash المخزن
            return BCrypt.Net.BCrypt.Verify(password, user.Password);
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
                FullName = u.Person != null ? $"{u.Person.FirstName} {u.Person.SecondName} {u.Person.ThirdName} {u.Person.LastName}" : string.Empty
            };
        }

        private User MapToEntity(CreateUserDto dto,string hashedPassword)
        {
            return new User
            {                
                PersonId = dto.PersonId,
                UserName = dto.UserName,
                Password = hashedPassword,
                IsActive = dto.IsActive
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            // 1. جلب المستخدم (تأكد أن الـ Repository يوفر GetUserByUserIdAsync)
            var user = await _userRepository.GetUserByUserIdAsync(userId);
            if (user == null) return false;

            // 2. التحقق من كلمة السر القديمة (استخدم خاصية Password التي تستخدمها في باقي الكود)
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                return false; // كلمة السر القديمة غير مطابقة
            }

            // 3. تشفير كلمة السر الجديدة وتحديثها
            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);

            // 4. حفظ التغييرات (تأكد أن Repository يدعم Update أو SaveChanges)
            return await _userRepository.UpdateUserAsync(user);
        }


    }
}
