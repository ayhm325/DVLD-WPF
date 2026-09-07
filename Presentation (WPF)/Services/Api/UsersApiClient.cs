using DVLD.Contracts.User;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class UsersApiClient(
    IApiClient apiClient) : IUsersApiClient
{
    public Task<ApiResult<IReadOnlyList<UserResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<IReadOnlyList<UserResponse>>(
            "api/users",
            cancellationToken);

    public Task<ApiResult<UserResponse>> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<UserResponse>(
            $"api/users/{userId}",
            cancellationToken);

    public Task<ApiResult<UserResponse>> GetByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<UserResponse>(
            $"api/users/person/{personId}",
            cancellationToken);

    public Task<ApiResult<UserResponse>> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<UserResponse>(
            $"api/users/username/{Uri.EscapeDataString(username)}",
            cancellationToken);

    public Task<ApiResult<int>> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
        => apiClient.PostAsync<CreateUserRequest, int>(
            "api/users",
            request,
            cancellationToken);

    public Task<ApiResult> UpdateAsync(
        int userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
        => apiClient.PutAsync(
            $"api/users/{userId}",
            request,
            cancellationToken);

    public Task<ApiResult> DeleteAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => apiClient.DeleteAsync(
            $"api/users/{userId}",
            cancellationToken);
}