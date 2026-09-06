using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DVLD.Api.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
    : IJwtTokenService
{
    private readonly JwtOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));

    public JwtTokenResult GenerateToken(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var expiresAtUtc =
            DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim("FullName", user.FullName),
            new Claim("PersonId", user.PersonId.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SecretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtTokenResult
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc
        };
    }
}