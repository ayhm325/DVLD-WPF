using Domain.Entities;
using Domain.Enums;
using System.Linq.Expressions;

namespace Application.Interfaces;

public interface ITestAppointmentRepository
{
    // ===== GET =====

    Task<TestAppointment?> GetByIdAsync(int id);
    Task<List<TestAppointment>> GetAllAsync();
    Task<List<TestAppointment>> GetByLocalDrivingLicenseApplicationIdAsync(int localDrivingLicenseApplicationId);
    Task<List<TestAppointment>> GetByTestTypeIdAsync(TestTypeEnum testType);
    Task<List<TestAppointment>> GetByCreatedUserIdAsync(int userId);
    Task<TestAppointment?> GetScheduleInfoAsync(int testAppointmentId);

    // ===== CHECKS =====

    Task<bool> ExistsAsync(Expression<Func<TestAppointment, bool>> predicate);
    Task<bool> HasConflictAsync(int localAppId, int testTypeId, DateTime dateTime, int? excludeAppointmentId = null);
    Task<bool> HasUserConflictAsync(int userId, DateTime dateTime, int? excludeAppointmentId = null);
    Task<bool> HasLocalApplicationConflictAsync(int localAppId, DateTime dateTime, int? excludeAppointmentId = null);
    Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId);
    Task<AppStatus?> GetApplicationStatusAsync(int localAppId);

    // ===== CREATE / UPDATE / DELETE =====

    Task<bool> AddAsync(TestAppointment appointment);
    Task<bool> UpdateAsync(TestAppointment appointment);
    Task DeleteAsync(int id);
}