using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Application.DTOs.AuthDTO;
using Application.Interfaces;
using Application.Options;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DVLD.Api.Security;

public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    IJordanClock clock) : IJwtTokenService
{
    private readonly JwtOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly IJordanClock _clock =
        clock ?? throw new ArgumentNullException(nameof(clock));

    public JwtTokenResult GenerateToken(JwtTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expiresAtUtc = _clock.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new Claim(ClaimTypes.Name, request.UserName),
            new Claim("FullName", request.FullName),
            new Claim("PersonId", request.PersonId.ToString()),
            new Claim(ClaimTypes.Role, request.Role.ToString()),

            // Binds the token to the user's current security state. Rotated on
            // password, role, or activation changes, which invalidates the token
            // on its very next request.
            new Claim("sst", request.SecurityStamp),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

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
