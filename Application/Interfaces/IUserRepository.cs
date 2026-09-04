using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByUserIdAsync(int id);


    Task<User?> GetUserByPersonIdAsync(int personId);

    Task<User?> GetUserByUsernameAsync(string username);

    Task<List<User>> GetAllUsersAsync();

    Task<User?> GetUserForUpdateAsync(int id);

    Task<bool> IsUsernameTakenAsync(string username);

    Task<bool> IsUsernameTakenForAnotherUserAsync(
        string username,
        int userId);

    Task<bool> IsUserExistsByPersonIdAsync(int personId);

    Task AddUserAsync(User user);

    void DeleteUser(User user);

}
