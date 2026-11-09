namespace Domain.Entities;

/// <summary>
/// A persisted refresh-token generation. A "session" is the whole rotation family;
/// each row represents one refresh token that was issued (and, after use, consumed).
/// Only the SHA-256 hash of the token is stored.
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Base64Url SHA-256 hash of the raw refresh token. Never the token itself.</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>Groups every generation produced by one successful login.</summary>
    public Guid TokenFamilyId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Absolute expiry of the token family (never extended by rotation).</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Last successful use; drives the idle timeout.</summary>
    public DateTime LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevocationReason { get; set; }

    /// <summary>The generation that replaced this one during rotation.</summary>
    public Guid? ReplacedBySessionId { get; set; }

    public virtual User User { get; set; } = null!;
}

public static class SessionRevocationReasons
{
    public const string Rotated = "Rotated";
    public const string Logout = "Logout";
    public const string LogoutAll = "LogoutAll";
    public const string PasswordChanged = "PasswordChanged";
    public const string SecurityStateChanged = "SecurityStateChanged";
    public const string ReuseDetected = "ReuseDetected";
    public const string Expired = "Expired";
    public const string AccountDisabled = "AccountDisabled";
}
