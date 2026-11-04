namespace Application.Common.Time;

/// <summary>
/// Resolves and exposes the canonical Jordan timezone across Windows, Linux, and
/// minimal container environments without ever falling back to <see cref="TimeZoneInfo.Local"/>.
/// </summary>
public static class JordanTimeZoneProvider
{
    public const string WindowsTimeZoneId = "Jordan Standard Time";
    public const string IanaTimeZoneId = "Asia/Amman";
    public static readonly TimeSpan FixedUtcOffset = TimeSpan.FromHours(3);

    /// <summary>
    /// Singleton resolved Jordan <see cref="TimeZoneInfo"/> instance.
    /// </summary>
    public static TimeZoneInfo TimeZone { get; } =
        ResolveTimeZone(TimeZoneInfo.FindSystemTimeZoneById);

    /// <summary>
    /// Resolves the Jordan timezone using Windows ("Jordan Standard Time"),
    /// Linux/IANA ("Asia/Amman"), or an explicit fixed UTC+03:00 timezone when
    /// OS timezone data (tzdata/ICU) is unavailable. Never falls back to machine-local timezone.
    /// </summary>
    public static TimeZoneInfo ResolveTimeZone(
        Func<string, TimeZoneInfo> systemTimeZoneFinder)
    {
        ArgumentNullException.ThrowIfNull(systemTimeZoneFinder);

        if (TryFindTimeZone(systemTimeZoneFinder, WindowsTimeZoneId, out var windowsTz))
        {
            return windowsTz;
        }

        if (TryFindTimeZone(systemTimeZoneFinder, IanaTimeZoneId, out var ianaTz))
        {
            return ianaTz;
        }

        // Jordan permanently abolished Daylight Saving Time in October 2022 and remains
        // on permanent UTC+03:00 year-round. In minimal containers lacking OS tzdata,
        // use an explicit fixed UTC+03:00 zone rather than TimeZoneInfo.Local.
        return TimeZoneInfo.CreateCustomTimeZone(
            id: WindowsTimeZoneId,
            baseUtcOffset: FixedUtcOffset,
            displayName: "(UTC+03:00) Amman",
            standardDisplayName: WindowsTimeZoneId);
    }

    /// <summary>
    /// Converts a UTC instant (<see cref="DateTimeKind.Utc"/>) to Jordan wall-clock time
    /// with <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    public static DateTime ConvertUtcToJordan(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Input DateTime must have DateTimeKind.Utc.",
                nameof(utcDateTime));
        }

        var jordanTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZone);
        return DateTime.SpecifyKind(jordanTime, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Converts a Jordan wall-clock timestamp (<see cref="DateTimeKind.Unspecified"/>)
    /// to its corresponding UTC instant (<see cref="DateTimeKind.Utc"/>).
    /// </summary>
    public static DateTime ConvertJordanToUtc(DateTime jordanLocalDateTime)
    {
        if (jordanLocalDateTime.Kind == DateTimeKind.Utc)
        {
            return jordanLocalDateTime;
        }

        var unspecified = DateTime.SpecifyKind(jordanLocalDateTime, DateTimeKind.Unspecified);
        var offset = TimeZone.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset).UtcDateTime;
    }

    private static bool TryFindTimeZone(
        Func<string, TimeZoneInfo> finder,
        string timeZoneId,
        out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = finder(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = null!;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = null!;
            return false;
        }
    }
}
