using Application.DTOs.UserDTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByUserIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserProfileAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<UserDetailsDto?> GetUserDetailsByIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<List<User>> GetAllUsersAsync(
        CancellationToken cancellationToken = default);

    Task<User?> GetUserForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> IsUsernameTakenAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<bool> IsUsernameTakenForAnotherUserAsync(
        string username,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUserExistsByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<int> GetActiveAdminCountAsync(
        CancellationToken cancellationToken = default);

    Task AddUserAsync(
        User user,
        CancellationToken cancellationToken = default);

    void DeleteUser(User user);
}
