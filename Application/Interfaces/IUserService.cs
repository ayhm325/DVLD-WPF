using Application.Common.Results;
using Application.DTOs.UserDTO;

namespace Application.Interfaces;

public interface IUserService
{
    Task<Result<List<UserDto>>> GetAllUsersAsync();


    Task<Result<UserDto>> GetUserByIdAsync(
    int id);

    Task<Result<UserDto>> GetUserByPersonIdAsync(
        int personId);

    Task<Result<UserDto>> GetUserByUsernameAsync(
        string username);

    Task<Result<int>> AddUserAsync(
        CreateUserDto dto);

    Task<Result> UpdateUserAsync(
        int id,
        UpdateUserDto dto);

    Task<Result> DeleteUserAsync(
        int id);

    Task<bool> IsUsernameTakenForAnotherUserAsync(
        string username,
        int userId);

    Task<Result> ChangePasswordAsync(
        int userId,
        ChangePasswordDto dto);
}
