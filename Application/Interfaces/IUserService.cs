using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUserService
    {
        Task<Result<List<UserDto>>> GetAllUsersAsync();

        Task<Result<UserDto>> GetUserByIdAsync(int id);

        Task<Result<UserDto>> GetUserByPersonIdAsync(int id);

        Task<Result<UserDto>> GetUserByUsernameAsync(string username);

        Task<Result<int>> AddUserAsync(CreateUserDto dto);

        Task<Result> UpdateUserAsync(int id, CreateUserDto dto);

        Task<Result> DeleteUserAsync(int id);

        // بقيت كما هي لأنها فحوصات بسيطة (Checks)
        Task<bool> IsUserExistsByIdAsync(int id);
        Task<bool> IsUsernameTakenForAnotherUserAsync(string username, int userId);

        // بقيت bool لأسباب أمنية (لمنع تسريب معلومات المستخدمين)
        Task<bool> AuthenticateUserAsync(string username, string password);

        Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        Task<Result<UserDto>> LoginAsync(string username, string password);
    }
}