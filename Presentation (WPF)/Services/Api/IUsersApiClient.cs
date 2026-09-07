using DVLD.Contracts.User;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IUsersApiClient
{
    Task<ApiResult<IReadOnlyList<UserResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<UserResponse>> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<UserResponse>> GetByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<UserResponse>> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<ApiResult<int>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateAsync(
        int userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default);
}