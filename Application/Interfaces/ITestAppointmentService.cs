using Application.Common.Results;
using Application.DTOs;
using Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ITestAppointmentService
{
    Task<Result<TestAppointmentDto>> GetByIdAsync(int id);

    Task<Result<List<TestAppointmentDto>>> GetAllAsync();

    Task<Result<List<TestAppointmentDto>>> GetByApplicationIdAsync(int applicationId);

    Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(TestTypeEnum testType);

    Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(int userId);

    Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(int testAppointmentId);

    Task<decimal> GetTestTypeFeesAsync(int testTypeId);

    Task<int> GetTrialCountAsync(int localAppId, int testTypeId);

    // فحوصات بسيطة (تبقى bool)
    Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime);
    Task<bool> HasUserConflictAsync(int userId, DateTime dateTime);
    Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime);
    Task<bool> HasPassedAllTestsAsync(int appId);
    Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId);

    // أوامر (Commands)
    Task<Result> AddAsync(TestAppointmentDto dto);
    Task<Result> UpdateAsync(TestAppointmentDto dto);
    Task<Result> DeleteAsync(int id);

    Task<Result> SaveTestResultAsync(TestDto dto);
}