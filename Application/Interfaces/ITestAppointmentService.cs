using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;

namespace Application.Interfaces;

public interface ITestAppointmentService
{
    Task<Result<TestAppointmentDto>> GetByIdAsync(int id);
    Task<Result<List<TestAppointmentDto>>> GetAllAsync();
    Task<Result<List<TestAppointmentDto>>> GetByLocalDrivingLicenseApplicationIdAsync(int localDrivingLicenseApplicationId);
    Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(TestTypeEnum testType);
    Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(int userId);
    Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(int testAppointmentId);


    Task<decimal> GetTestTypeFeesAsync(int testTypeId);
    Task<int> GetTrialCountAsync(int localAppId, int testTypeId);

    Task<Result> AddAsync(CreateTestAppointmentDto dto);
    Task<Result> UpdateAsync(UpdateTestAppointmentDto dto);
    Task<Result> DeleteAsync(int id);
    Task<Result> SaveTestResultAsync(SaveTestResultDto dto);

    Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId);
}
