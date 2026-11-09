using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;

namespace Application.Interfaces;

public interface ITestAppointmentService
{
    Task<Result<TestAppointmentDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<List<TestAppointmentDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<List<TestAppointmentDto>>> GetByLocalDrivingLicenseApplicationIdAsync(
        int localAppId,
        CancellationToken cancellationToken = default);

    Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(
        int testType,
        CancellationToken cancellationToken = default);

    Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduleTestDto>> GetSchedulePreparationAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetTestTypeFeesAsync(
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<int> GetTrialCountAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<Result> AddAsync(
        CreateTestAppointmentDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        UpdateTestAppointmentDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> CancelAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduleTestDto>> ScheduleAsync(
        int localAppId,
        int testTypeId,
        DateTime appointmentDate,
        CancellationToken cancellationToken = default);

    Task<Result<AppointmentAvailabilityDto>> GetAvailabilityAsync(
        int localAppId,
        int testTypeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<ExaminationAttemptStats> GetAttemptStatsAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default);
}
