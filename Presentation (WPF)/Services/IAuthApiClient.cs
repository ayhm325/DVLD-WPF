using DVLD.Contracts.Auth;
using Presentation.Services.Results;

namespace Presentation.Services;

public interface IAuthApiClient
{
    Task<ApiResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}