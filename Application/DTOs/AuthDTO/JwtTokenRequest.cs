using Domain.Enums;

namespace Application.DTOs.AuthDTO;

/// <summary>
/// Minimal, non-sensitive data required to issue an access token.
/// </summary>
public sealed class JwtTokenRequest
{
    public int UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public int PersonId { get; init; }

    public UserRole Role { get; init; }

    /// <summary>Security stamp the token is bound to; changes invalidate the token.</summary>
    public string SecurityStamp { get; init; } = string.Empty;
}

/// <summary>
/// Current security state of a user, read server-side on every authenticated request.
/// </summary>
public sealed class UserSecurityState
{
    public bool IsActive { get; init; }

    public string SecurityStamp { get; init; } = string.Empty;
}
