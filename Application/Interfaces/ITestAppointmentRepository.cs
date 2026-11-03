using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITestAppointmentRepository
{
    Task<TestAppointment?> GetByIdAsync(int id);

    Task<TestAppointment?> GetForUpdateAsync(int id);

    Task<List<TestAppointment>> GetAllAsync();

    Task<List<TestAppointment>>
        GetByLocalDrivingLicenseApplicationIdAsync(
            int localAppId);

    Task<List<TestAppointment>>
        GetByTestTypeIdAsync(TestTypeEnum testType);

    Task<List<TestAppointment>>
        GetByCreatedUserIdAsync(int userId);

    Task<TestAppointment?> GetScheduleInfoAsync(
        int appointmentId);

    Task<HashSet<int>> GetPassedTestTypeIdsAsync(
        int localAppId);

    Task<bool> HasUserConflictAsync(
        int userId,
        DateTime dateTime,
        int? excludeAppointmentId = null);

    Task<bool> HasLocalApplicationConflictAsync(
        int localAppId,
        DateTime dateTime,
        int? excludeAppointmentId = null);

    Task<bool> IsSlotOccupiedAsync(
        int testTypeId,
        DateTime appointmentDate,
        int? excludeAppointmentId = null);

    Task<bool> HasApplicantSameDayConflictAsync(
        int applicantPersonId,
        DateTime appointmentDate,
        int? excludeAppointmentId = null);

    Task<int?> GetApplicantPersonIdByLocalAppIdAsync(
        int localAppId);

    Task<bool> AcquireSchedulingLocksAsync(
        int applicantPersonId,
        int testTypeId,
        DateTime appointmentDate,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default);

    Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId);

    Task<AppStatus?> GetApplicationStatusAsync(
        int localAppId);

    Task<int> GetTrialCountAsync(
        int localAppId,
        int testTypeId);

    Task AddAsync(TestAppointment appointment);

    void Delete(TestAppointment appointment);

    Task<bool> LockLocalApplicationForSchedulingAsync(
        int localAppId);

    Task<(HashSet<DateTime> OccupiedSlots, HashSet<DateTime> ApplicantAppointmentDates, DateTime? LastTestDate)>
        GetAvailabilityDataAsync(
            int testTypeId,
            int applicantPersonId,
            int localAppId,
            DateTime rangeStart,
            DateTime rangeEnd);

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
