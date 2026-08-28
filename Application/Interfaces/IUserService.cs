
using Application.Common.Results;
using Application.DTOs.UserDTO;

namespace Application.Interfaces;

public interface IUserService
{
    // =========================================================
    // GET
    // =========================================================

    Task<Result<List<UserDto>>>
        GetAllUsersAsync();

    Task<Result<UserDto>>
        GetUserByIdAsync(
            int id);

    Task<Result<UserDto>>
        GetUserByPersonIdAsync(
            int personId);

    Task<Result<UserDto>>
        GetUserByUsernameAsync(
            string username);


    // =========================================================
    // CREATE
    // =========================================================

    Task<Result<int>>
        AddUserAsync(
            CreateUserDto dto);


    // =========================================================
    // UPDATE
    // =========================================================

    Task<Result>
        UpdateUserAsync(
            int id,
            UpdateUserDto dto);


    // =========================================================
    // DELETE
    // =========================================================

    Task<Result>
        DeleteUserAsync(
            int id);


    // =========================================================
    // CHECKS
    // =========================================================

    Task<bool>
        IsUserExistsByIdAsync(
            int id);

    Task<bool>
        IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId);


    // =========================================================
    // AUTHENTICATION
    // =========================================================

    Task<bool>
        AuthenticateUserAsync(
            string username,
            string password);


    // =========================================================
    // CHANGE PASSWORD
    // =========================================================

    Task<Result>
        ChangePasswordAsync(
            int userId,
            ChangePasswordDto dto);


    // =========================================================
    // LOGIN
    // =========================================================

    Task<Result<UserDto>>
        LoginAsync(
            LoginRequestDto dto);
}
