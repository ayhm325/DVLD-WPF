namespace Application.Options;

public sealed class LicenseExpirationOptions
{
    public const string SectionName = "LicenseExpiration";

    public bool IsEnabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 60;
}
