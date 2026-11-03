using Application.Interfaces;
using Application.Options;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace Application.Services;

public sealed class NoShowProcessorService : INoShowProcessorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITestAppointmentRepository _appointmentRepository;
    private readonly ITestRepository _testRepository;
    private readonly IJordanClock _clock;
    private readonly IOptions<NoShowProcessorOptions> _options;
    private readonly ILogger<NoShowProcessorService> _logger;

    public NoShowProcessorService(
        IUnitOfWork unitOfWork,
        ITestAppointmentRepository appointmentRepository,
        ITestRepository testRepository,
        IJordanClock clock,
        IOptions<NoShowProcessorOptions> options,
        ILogger<NoShowProcessorService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
        _testRepository = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NoShowBatchResult> ProcessPendingNoShowsAsync(
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        var batchSize = options.BatchSize > 0 ? options.BatchSize : 50;

        var totalCandidates = 0;
        var processedCount = 0;
        var skippedIneligibleCount = 0;
        var lockFailedCount = 0;
        var errorCount = 0;
        var allItems = new List<NoShowItemResult>();

        const int maxBatches = 20;
        var batchIndex = 0;

        while (!cancellationToken.IsCancellationRequested && batchIndex < maxBatches)
        {
            batchIndex++;
            var batchResult = await ProcessBatchAsync(batchSize, cancellationToken);

            totalCandidates += batchResult.TotalCandidatesFound;
            processedCount += batchResult.ProcessedCount;
            skippedIneligibleCount += batchResult.SkippedIneligibleCount;
            lockFailedCount += batchResult.LockFailedCount;
            errorCount += batchResult.ErrorCount;
            allItems.AddRange(batchResult.Items);

            if (batchResult.TotalCandidatesFound < batchSize)
                break;
        }

        if (totalCandidates > 0)
        {
            _logger.LogInformation(
                "Completed No-Show processing run. Candidates: {TotalCandidates}, Processed: {Processed}, Ineligible: {Skipped}, LockFailed: {LockFailed}, Errors: {Errors}",
                totalCandidates, processedCount, skippedIneligibleCount, lockFailedCount, errorCount);
        }

        return new NoShowBatchResult(
            totalCandidates,
            processedCount,
            skippedIneligibleCount,
            lockFailedCount,
            errorCount,
            allItems);
    }

    public async Task<NoShowBatchResult> ProcessBatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            batchSize = 50;

        var options = _options.Value;
        var nowJordan = _clock.Now;
        var gracePeriod = Math.Max(0, options.GracePeriodMinutes);
        var cutoff = nowJordan.AddMinutes(-gracePeriod);

        List<int> candidateIds;
        try
        {
            candidateIds = await _appointmentRepository.GetPendingNoShowAppointmentIdsAsync(
                cutoff, batchSize, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query pending No-Show candidates.");
            return new NoShowBatchResult(0, 0, 0, 0, 1, []);
        }

        if (candidateIds.Count == 0)
        {
            return new NoShowBatchResult(0, 0, 0, 0, 0, []);
        }

        var processedCount = 0;
        var skippedIneligibleCount = 0;
        var lockFailedCount = 0;
        var errorCount = 0;
        var items = new List<NoShowItemResult>(candidateIds.Count);

        foreach (var id in candidateIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var itemResult = await ProcessSingleAppointmentAsync(
                id, nowJordan, cutoff, options.LockTimeoutMs, cancellationToken);
            items.Add(itemResult);

            switch (itemResult.Status)
            {
                case NoShowProcessStatus.Processed:
                    processedCount++;
                    break;
                case NoShowProcessStatus.Ineligible:
                    skippedIneligibleCount++;
                    break;
                case NoShowProcessStatus.LockFailed:
                    lockFailedCount++;
                    break;
                case NoShowProcessStatus.Failed:
                    errorCount++;
                    break;
            }
        }

        return new NoShowBatchResult(
            candidateIds.Count,
            processedCount,
            skippedIneligibleCount,
            lockFailedCount,
            errorCount,
            items);
    }

    private async Task<NoShowItemResult> ProcessSingleAppointmentAsync(
        int appointmentId,
        DateTime nowJordan,
        DateTime cutoff,
        int lockTimeoutMs,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async tx =>
            {
                var lockAcquired = await _appointmentRepository.AcquireAppointmentLockAsync(
                    appointmentId, lockTimeoutMs, cancellationToken);

                if (!lockAcquired)
                {
                    await tx.RollbackAsync(cancellationToken);
                    _logger.LogWarning(
                        "Could not acquire exclusive lock for appointment {AppointmentId}. Skipping.",
                        appointmentId);
                    return new NoShowItemResult(appointmentId, NoShowProcessStatus.LockFailed, "Lock could not be acquired");
                }

                var appointment = await _appointmentRepository.GetForUpdateAsync(appointmentId);
                if (appointment is null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return new NoShowItemResult(appointmentId, NoShowProcessStatus.Ineligible, "Appointment not found");
                }

                if (appointment.Status != AppointmentStatus.Scheduled ||
                    appointment.IsLocked ||
                    appointment.AppointmentDate > cutoff)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return new NoShowItemResult(appointmentId, NoShowProcessStatus.Ineligible, "State or cutoff condition changed");
                }

                var testExists = await _testRepository.IsTestAlreadyTakenAsync(appointmentId);
                if (testExists)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return new NoShowItemResult(appointmentId, NoShowProcessStatus.Ineligible, "Test record exists");
                }

                appointment.Status = AppointmentStatus.NoShow;
                appointment.NoShowDate = nowJordan;
                appointment.IsLocked = true;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                return new NoShowItemResult(appointmentId, NoShowProcessStatus.Processed);
            }, IsolationLevel.ReadCommitted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process No-Show for appointment {AppointmentId}.", appointmentId);
            return new NoShowItemResult(appointmentId, NoShowProcessStatus.Failed, ex.Message);
        }
    }
}
