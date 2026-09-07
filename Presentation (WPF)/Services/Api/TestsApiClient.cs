using DVLD.Contracts.Test;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class TestsApiClient(
    IApiClient apiClient) : ITestsApiClient
{
    public Task<ApiResult<List<TestResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<TestResponse>>(
            "api/tests",
            cancellationToken);

    public Task<ApiResult<TestResponse>> GetByIdAsync(
        int testId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<TestResponse>(
            $"api/tests/{testId}",
            cancellationToken);

    public Task<ApiResult<TestResponse>> GetByAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<TestResponse>(
            $"api/tests/appointment/{appointmentId}",
            cancellationToken);

    public Task<ApiResult<List<TestResponse>>> GetByCreatedUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<List<TestResponse>>(
            $"api/tests/created-by/{userId}",
            cancellationToken);

    public Task<ApiResult<int>> SaveResultAsync(
        SaveTestResultRequest request,
        CancellationToken cancellationToken = default)
        => apiClient.PostAsync<SaveTestResultRequest, int>(
            "api/tests",
            request,
            cancellationToken);
}