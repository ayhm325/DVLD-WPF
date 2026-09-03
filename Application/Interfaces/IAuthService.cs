using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDto>> LoginAsync(
        LoginRequestDto dto);
}