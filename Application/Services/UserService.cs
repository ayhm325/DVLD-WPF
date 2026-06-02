using Application.DTOs;
using Application.Interfaces;
using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Infrastructure.Repositories;
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
            return users.Select(MapToDto).ToList();
        }

        // ================= GET BY ID =================
        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetUserByUserIdAsync(id);
            return u == null ? null : MapToDto(u);
        }

        // ================= ADD =================
        public async Task<int> AddUserAsync(CreateUserDto dto)
        {
            var user = MapToEntity(dto);
            return await _userRepository.AddUserAsync(user);
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateUserAsync(int id, CreateUserDto dto)
        {
            // 1. جلب المستخدم الحالي من الـ Repository
            var user = await _userRepository.GetUserByUserIdAsync(id);
            if (user == null) return false;

            // 2. تحديث البيانات (يفضل استخدام AutoMapper لاحقاً بدلاً من اليدوي)
            user.Username = dto.Username;
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.Password = dto.Password; // يفضل إضافة التشفير (Hashing) هنا
            }
            user.IsActive = dto.IsActive;
            user.PersonId = dto.PersonId;

            // 3. استدعاء الـ Repository لحفظ التغييرات
            return await _userRepository.UpdateUserAsync(user);
        }

        // ================= DELETE =================
        public async Task<bool> DeleteUserAsync(int id)
        {
            if (!await _userRepository.IsUserExistsByIdAsync(id))
                return false;

            return await _userRepository.DeleteUserAsync(id);
        }

        // ================= MAPPING =================
        private UserDto MapToDto(User u)
        {
            return new UserDto
            {
                UserId = u.UserId,
                PersonId = u.PersonId,
                Username = u.Username,
                IsActive = u.IsActive
            };
        }

        private User MapToEntity(CreateUserDto dto)
        {
            return new User
            {                
                PersonId = dto.PersonId,
                Username = dto.Username,
                Password = dto.Password,
                IsActive = dto.IsActive
            };
        }


    }
}
