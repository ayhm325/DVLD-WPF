using Application.Interfaces;

namespace Infrastructure.Services;

public sealed class JordanClock : IJordanClock
{
    private static readonly TimeZoneInfo JordanTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");

    public DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, JordanTimeZone);
}
