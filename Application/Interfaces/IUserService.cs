using Application.DTOs;



namespace Application.Interfaces
{
    public interface IUserService
    {

        Task<List<UserDto>> GetAllUsersAsync();

        Task<UserDto?> GetUserByIdAsync(int id);

        Task<UserDto?> GetUserByPersonIdAsync(int id);

        Task<UserDto?> GetUserByUsernameAsync(string username);


        Task<int> AddUserAsync(CreateUserDto dto);

        Task<bool> UpdateUserAsync(int id, CreateUserDto dto);

        Task<bool> DeleteUserAsync(int id);

        Task<bool> IsUserExistsByIdAsync(int id);

        Task<bool> IsUsernameTakenForAnotherUserAsync(string username, int userId);

        Task<bool> AuthenticateUserAsync(string username, string password);

        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        Task<UserDto?> LoginAsync(string username, string password);
    }
}
