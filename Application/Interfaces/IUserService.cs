using Application.Common.Results;
using Application.DTOs.UserDTO;

namespace Application.Interfaces;

public interface IUserService
{
    Task<Result<List<UserDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    Task<Result<UserDetailsDto>> GetUserDetailsAsync(int userId, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<UserProfileDto>> GetCurrentProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> GetUserByPersonIdAsync(int personId, CancellationToken cancellationToken = default);

    Task<Result<UserDto>> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<Result<int>> AddUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task<Result> UpdateUserAsync(int id,UpdateUserDto dto, CancellationToken cancellationToken = default);

    Task<Result> DeleteUserAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsUsernameTakenForAnotherUserAsync(string username,int userId, CancellationToken cancellationToken = default);

    Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(int userId,ChangePasswordDto dto, CancellationToken cancellationToken = default);
}
