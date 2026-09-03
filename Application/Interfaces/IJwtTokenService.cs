using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;

namespace Application.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(UserDto user);
}