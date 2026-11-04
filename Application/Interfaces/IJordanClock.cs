namespace Application.Interfaces;

/// <summary>
/// Single authoritative clock abstraction for the DVLD Backend.
/// Business workflows must obtain current Jordan local time/date or technical UTC instants
/// exclusively through this interface rather than accessing the system clock directly.
/// </summary>
public interface IJordanClock
{
    /// <summary>
    /// Current Jordan local wall-clock date and time (<see cref="DateTimeKind.Unspecified"/>).
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Current Jordan local calendar date.
    /// </summary>
    DateOnly Today { get; }

    /// <summary>
    /// Current UTC instant (<see cref="DateTimeKind.Utc"/>) for technical/global timestamps such as JWT expiry.
    /// </summary>
    DateTime UtcNow { get; }
}
