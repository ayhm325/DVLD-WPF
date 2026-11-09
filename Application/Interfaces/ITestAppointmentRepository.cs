using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITestAppointmentRepository
{
    Task<TestAppointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TestAppointment?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<List<TestAppointment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<TestAppointment>>
        GetByLocalDrivingLicenseApplicationIdAsync(
            int localAppId,
            CancellationToken cancellationToken = default);

    Task<List<TestAppointment>>
        GetByTestTypeIdAsync(
            TestTypeEnum testType,
            CancellationToken cancellationToken = default);

    Task<List<TestAppointment>>
        GetByCreatedUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default);

    Task<TestAppointment?> GetScheduleInfoAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetPassedTestTypeIdsAsync(
        int localAppId,
        CancellationToken cancellationToken = default);

    Task<bool> HasUserConflictAsync(
        int userId,
        DateTime dateTime,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasLocalApplicationConflictAsync(
        int localAppId,
        DateTime dateTime,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsSlotOccupiedAsync(
        int testTypeId,
        DateTime appointmentDate,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicantSameDayConflictAsync(
        int applicantPersonId,
        DateTime appointmentDate,
        int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    Task<int?> GetApplicantPersonIdByLocalAppIdAsync(
        int localAppId,
        CancellationToken cancellationToken = default);

    Task<bool> AcquireSchedulingLocksAsync(
        int applicantPersonId,
        int testTypeId,
        DateTime appointmentDate,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default);

    Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<AppStatus?> GetApplicationStatusAsync(
        int localAppId,
        CancellationToken cancellationToken = default);

    Task<int> GetTrialCountAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TestAppointment appointment,
        CancellationToken cancellationToken = default);

    void Delete(TestAppointment appointment);

    Task<bool> LockLocalApplicationForSchedulingAsync(
        int localAppId,
        CancellationToken cancellationToken = default);

    Task<(HashSet<DateTime> OccupiedSlots, HashSet<DateTime> ApplicantAppointmentDates, DateTime? LastTestDate)>
        GetAvailabilityDataAsync(
            int testTypeId,
            int applicantPersonId,
            int localAppId,
            DateTime rangeStart,
            DateTime rangeEnd,
            CancellationToken cancellationToken = default);

    Task<List<int>> GetPendingNoShowAppointmentIdsAsync(
        DateTime cutoffJordanTime,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<bool> AcquireAppointmentLockAsync(
        int appointmentId,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default);

    Task<Application.DTOs.TestAppointmentDTO.ExaminationAttemptStats> GetAttemptStatsAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<List<TestAppointment>> GetActiveScheduledByLocalAppIdForUpdateAsync(
        int localAppId,
        CancellationToken cancellationToken = default);
}
