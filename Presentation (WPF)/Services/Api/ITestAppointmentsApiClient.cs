using DVLD.Contracts.TestAppointment;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ITestAppointmentsApiClient
{
    Task<ApiResult<List<TestAppointmentResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<TestAppointmentResponse>> GetByIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<List<TestAppointmentResponse>>> GetByLocalApplicationIdAsync(
        int localApplicationId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<List<TestAppointmentResponse>>> GetByTestTypeIdAsync(
        TestType testType,
        CancellationToken cancellationToken = default);

    Task<ApiResult<List<TestAppointmentResponse>>> GetByCreatedUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<ScheduleTestResponse>> GetScheduleInfoAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<ScheduleTestResponse>> GetSchedulePreparationAsync(
        int localApplicationId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<decimal>> GetTestTypeFeesAsync(
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<int>> GetTrialCountAsync(
        int localApplicationId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<bool>> IsAppointmentAlreadyScheduledAsync(
        int localApplicationId,
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> ScheduleAsync(
        ScheduleTestRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> CreateAsync(
        CreateTestAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateAsync(
        UpdateTestAppointmentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResult> DeleteAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);
}