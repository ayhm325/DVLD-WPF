using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITestAppointmentService
{
    Task<Result<TestAppointmentDto>> GetByIdAsync(int id);

    Task<Result<List<TestAppointmentDto>>> GetAllAsync();

    Task<Result<List<TestAppointmentDto>>> GetByLocalDrivingLicenseApplicationIdAsync(
        int localAppId);

    Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(
        TestTypeEnum testType);

    Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(int userId);

    Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(int appointmentId);

    Task<Result<ScheduleTestDto>> GetSchedulePreparationAsync(
        int localAppId,
        int testTypeId);

    Task<decimal> GetTestTypeFeesAsync(int testTypeId);

    Task<int> GetTrialCountAsync(int localAppId, int testTypeId);

    Task<Result> AddAsync(CreateTestAppointmentDto dto);

    Task<Result> UpdateAsync(UpdateTestAppointmentDto dto);

    Task<Result> DeleteAsync(int id);

    Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId);

    Task<Result<ScheduleTestDto>> ScheduleAsync(
        int localAppId,
        int testTypeId,
        DateTime appointmentDate);
}