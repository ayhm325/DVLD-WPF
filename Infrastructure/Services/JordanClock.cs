using Application.Common.Time;
using Application.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IJordanClock"/> backed by the system UTC clock
/// and <see cref="JordanTimeZoneProvider"/>.
/// </summary>
public sealed class JordanClock : IJordanClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now =>
        JordanTimeZoneProvider.ConvertUtcToJordan(DateTime.UtcNow);

    public DateOnly Today =>
        DateOnly.FromDateTime(Now);
}
