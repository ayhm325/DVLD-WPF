namespace Application.Interfaces;

public enum NoShowProcessStatus
{
    Processed,
    Ineligible,
    LockFailed,
    Failed
}

public sealed record NoShowItemResult(
    int AppointmentId,
    NoShowProcessStatus Status,
    string? Message = null);

public sealed record NoShowBatchResult(
    int TotalCandidatesFound,
    int ProcessedCount,
    int SkippedIneligibleCount,
    int LockFailedCount,
    int ErrorCount,
    IReadOnlyList<NoShowItemResult> Items);

public interface INoShowProcessorService
{
    Task<NoShowBatchResult> ProcessPendingNoShowsAsync(
        CancellationToken cancellationToken = default);

    Task<NoShowBatchResult> ProcessBatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}
