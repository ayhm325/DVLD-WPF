using DVLD.Contracts.TestAppointment;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class TestAppointmentsApiClient(
    IApiClient apiClient) : ITestAppointmentsApiClient
{
    public Task<ApiResult<List<TestAppointmentResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<TestAppointmentResponse>>(
            "api/testappointments",
            cancellationToken);

    public Task<ApiResult<TestAppointmentResponse>> GetByIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<TestAppointmentResponse>(
            $"api/testappointments/{appointmentId}",
            cancellationToken);

    public Task<ApiResult<List<TestAppointmentResponse>>> GetByLocalApplicationIdAsync(
        int localApplicationId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<TestAppointmentResponse>>(
            $"api/testappointments/local-application/{localApplicationId}",
            cancellationToken);

    public Task<ApiResult<List<TestAppointmentResponse>>> GetByTestTypeIdAsync(
        TestType testType,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<TestAppointmentResponse>>(
            $"api/testappointments/test-type/{(int)testType}",
            cancellationToken);

    public Task<ApiResult<List<TestAppointmentResponse>>> GetByCreatedUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<TestAppointmentResponse>>(
            $"api/testappointments/created-by/{userId}",
            cancellationToken);

    public Task<ApiResult<ScheduleTestResponse>> GetScheduleInfoAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<ScheduleTestResponse>(
            $"api/testappointments/{appointmentId}/schedule-info",
            cancellationToken);

    public async Task<ApiResult<decimal>> GetTestTypeFeesAsync(
        int testTypeId,
        CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.GetAsync<TestTypeFeesResponse>(
                $"api/testappointments/fees/{testTypeId}",
                cancellationToken);

        if (result.IsFailure)
            return ApiResult<decimal>.Failure(result.Error);

        return ApiResult<decimal>.Success(result.Value!.Fees);
    }

    public async Task<ApiResult<int>> GetTrialCountAsync(
        int localApplicationId,
        int testTypeId,
        CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.GetAsync<TrialCountResponse>(
                $"api/testappointments/trial-count?localAppId={localApplicationId}&testTypeId={testTypeId}",
                cancellationToken);

        if (result.IsFailure)
            return ApiResult<int>.Failure(result.Error);

        return ApiResult<int>.Success(result.Value!.TrialCount);
    }

    public async Task<ApiResult<bool>> IsAppointmentAlreadyScheduledAsync(
        int localApplicationId,
        int testTypeId,
        CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.GetAsync<ScheduledResponse>(
                $"api/testappointments/scheduled?localAppId={localApplicationId}&testTypeId={testTypeId}",
                cancellationToken);

        if (result.IsFailure)
            return ApiResult<bool>.Failure(result.Error);

        return ApiResult<bool>.Success(result.Value!.Scheduled);
    }

    public async Task<ApiResult> CreateAsync(
        CreateTestAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result =
            await apiClient.PostAsync<CreateTestAppointmentRequest, object>(
                "api/testappointments",
                request,
                cancellationToken);

        return result.IsSuccess
            ? ApiResult.Success()
            : ApiResult.Failure(result.Error);
    }

    public Task<ApiResult> UpdateAsync(
        UpdateTestAppointmentRequest request,
        CancellationToken cancellationToken = default)
        => apiClient.PutAsync(
            $"api/testappointments/{request.TestAppointmentId}",
            request,
            cancellationToken);

    public Task<ApiResult> DeleteAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
        => apiClient.DeleteAsync(
            $"api/testappointments/{appointmentId}",
            cancellationToken);

    private sealed class TestTypeFeesResponse
    {
        public decimal Fees { get; init; }
    }

    private sealed class TrialCountResponse
    {
        public int TrialCount { get; init; }
    }

    private sealed class ScheduledResponse
    {
        public bool Scheduled { get; init; }
    }
}