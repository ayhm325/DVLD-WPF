using Domain.Enums;

namespace Application.DTOs.AuthDTO;

public sealed class LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }

    public int UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public int PersonId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    /// <summary>
    /// Raw refresh token. Transported only through an HttpOnly cookie by the API and
    /// never serialized into a JSON response body.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Absolute expiry of the refresh-token family (cookie expiry).</summary>
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}