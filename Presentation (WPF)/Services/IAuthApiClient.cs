using DVLD.Contracts.Auth;
using DVLD.Contracts.User;
using Presentation.Services.Results;

namespace Presentation.Services;

public interface IAuthApiClient
{
    Task<ApiResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult<UserProfileResponse>> GetProfileAsync(
        CancellationToken cancellationToken = default);
}