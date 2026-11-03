namespace Application.Options;

public sealed class NoShowProcessorOptions
{
    public const string SectionName = "NoShowProcessor";

    public bool IsEnabled { get; set; } = true;
    public int PollingIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 50;
    public int LockTimeoutMs { get; set; } = 5000;
    public int GracePeriodMinutes { get; set; } = 40;
}
