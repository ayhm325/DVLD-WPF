namespace Application.Options;

/// <summary>
/// Refresh-session and token-transport configuration. Validated at startup.
/// </summary>
public sealed class AuthSessionOptions
{
    public const string SectionName = "AuthSession";

    /// <summary>Absolute lifetime of a refresh-token family, in days.</summary>
    public int RefreshTokenLifetimeDays { get; set; } = 14;

    /// <summary>Maximum idle time between refreshes, in days.</summary>
    public int IdleTimeoutDays { get; set; } = 7;

    /// <summary>
    /// Window after a rotation during which presenting the consumed token is treated
    /// as a benign concurrent refresh (409 retry) instead of confirmed reuse.
    /// </summary>
    public int RotationGraceSeconds { get; set; } = 30;

    public string RefreshCookieName { get; set; } = "dvld_rt";

    public string RefreshCookiePath { get; set; } = "/api/auth";
}
