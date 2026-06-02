using Application.DTOs;


namespace Application.Interfaces
{
    public interface IUserService
    {

        Task<List<UserDto>> GetAllUsersAsync();

        Task<UserDto?> GetUserByIdAsync(int id);

        Task<int> AddUserAsync(CreateUserDto dto);

        Task<bool> UpdateUserAsync(int id, CreateUserDto dto);

        Task<bool> DeleteUserAsync(int id);
    }
}
