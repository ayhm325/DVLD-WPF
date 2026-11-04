namespace Application.Common.Time;

/// <summary>
/// Defines the official DVLD license-expiration rule across all local and international
/// license workflows, queries, repositories, and background synchronization.
/// </summary>
public static class LicenseExpirationPolicy
{
    /// <summary>
    /// A license remains valid throughout its Jordan expiration calendar date
    /// (<c>ExpirationDate.Date</c>) and becomes expired only at <c>00:00:00</c> Jordan time
    /// on the next calendar day (<c>jordanToday &gt; DateOnly.FromDateTime(expirationDate)</c>).
    /// </summary>
    public static bool IsExpired(DateTime expirationDate, DateOnly jordanToday) =>
        jordanToday > DateOnly.FromDateTime(expirationDate);

    /// <summary>
    /// Returns <see langword="true"/> when the license has not yet expired on <paramref name="jordanToday"/>
    /// (inclusive of the entire expiration calendar day).
    /// </summary>
    public static bool IsValidOn(DateTime expirationDate, DateOnly jordanToday) =>
        !IsExpired(expirationDate, jordanToday);
}
