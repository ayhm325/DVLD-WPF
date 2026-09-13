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
}