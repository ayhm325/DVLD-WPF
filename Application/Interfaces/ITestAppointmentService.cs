using Application.DTOs;
using Domain.Enums;
using System.Data;

public interface ITestAppointmentService
{
    Task<TestAppointmentDto?> GetByIdAsync(int id);
    Task<List<TestAppointmentDto>> GetAllAsync();
    Task<List<TestAppointmentDto>> GetByApplicationIdAsync(int applicationId);
    Task<List<TestAppointmentDto>> GetByTestTypeIdAsync(TestTypeEnum testType);
    Task<List<TestAppointmentDto>> GetByCreatedUserIdAsync(int userId);

    Task<ScheduleTestDto?> GetScheduleInfoAsync(int testAppointmentId);

    Task<decimal> GetTestTypeFeesAsync(int testTypeId);
    Task<int> GetTrialCountAsync(int localAppId, int testTypeId);

    Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime);
    Task<bool> HasUserConflictAsync(int userId, DateTime dateTime);
    Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime);
    Task<bool> HasPassedAllTestsAsync(int appId);

    Task<bool> AddAsync(TestAppointmentDto dto);
    Task<bool> UpdateAsync(TestAppointmentDto dto);
    Task DeleteAsync(int id);

    Task<bool> SaveTestResultAsync(TestDto dto);

    Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId);
}