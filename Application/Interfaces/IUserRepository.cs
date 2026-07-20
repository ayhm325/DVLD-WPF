using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Interfaces
{
    public interface IUserRepository
    {
        // =========================
        // GET OPERATIONS
        // =========================

        Task<User?> GetUserByUserIdAsync(int id);

        Task<User?> GetUserByPersonIdAsync(int id);

        Task<User?> GetUserByUsernameAsync(string username);

        Task<List<User>> GetAllUsersAsync();


        // =========================
        // CHECK OPERATIONS
        // =========================

        Task<bool> IsUserExistsAsync(
            Expression<Func<User, bool>> predicate);

        Task<bool> CheckUserCredentialsAsync(
            string username,
            string password);

        Task<bool> IsUsernameTakenAsync(
            string username);

        Task<bool> IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId);

        Task<bool> IsUserExistsByIdAsync(
            int id);

        Task<bool> IsUserExistsByPersonIdAsync(
            int personId);

        Task<bool> CheckUserExistsAsync(
            string username,
            string password);


        // =========================
        // CREATE
        // =========================

        Task<int> AddUserAsync(
            User user);


        // =========================
        // UPDATE
        // =========================

        Task<bool> UpdateUserAsync(
            User user);


        // =========================
        // DELETE
        // =========================

        Task<bool> DeleteUserAsync(
            int id);
    }
}