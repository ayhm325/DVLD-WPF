using Application.DTOs.AuthDTO;

namespace Application.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(JwtTokenRequest request);
}
