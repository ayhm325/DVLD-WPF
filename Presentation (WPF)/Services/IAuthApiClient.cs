using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;

namespace Presentation.Services;

public interface IAuthApiClient
{
    Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto);
}
