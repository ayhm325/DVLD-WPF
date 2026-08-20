using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUserRepository
    {
        // =========================
        // GET OPERATIONS
        // =========================

        Task<User?> GetUserByUserIdAsync(int id);

        Task<User?> GetUserByPersonIdAsync(int personId);

        Task<User?> GetUserByUsernameAsync(string username);

        Task<List<User>> GetAllUsersAsync();


        // =========================
        // CHECK OPERATIONS
        // =========================

        Task<bool> IsUsernameTakenAsync(string username);

        Task<bool> IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId);

        Task<bool> IsUserExistsByIdAsync(int id);

        Task<bool> IsUserExistsByPersonIdAsync(int personId);


        // =========================
        // CREATE
        // =========================

        Task<int> AddUserAsync(User user);


        // =========================
        // UPDATE
        // =========================

        Task<bool> UpdateUserAsync(User user);


        // =========================
        // DELETE
        // =========================

        Task<bool> DeleteUserAsync(int id);
    }
}