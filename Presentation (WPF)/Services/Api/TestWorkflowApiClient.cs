using DVLD.Contracts.TestAppointment;
using DVLD.Contracts.TestWorkflow;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class TestWorkflowApiClient(
    IApiClient apiClient) : ITestWorkflowApiClient
{
    public Task<ApiResult<TestWorkflowResponse>> CanScheduleAsync(
        int localApplicationId,
        TestType testType,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<TestWorkflowResponse>(
            $"api/testworkflow/can-schedule?localAppId={localApplicationId}&testType={testType}",
            cancellationToken);

    public Task<ApiResult<TestWorkflowResponse>> GetNextTestAsync(
        int localApplicationId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<TestWorkflowResponse>(
            $"api/testworkflow/next-test/{localApplicationId}",
            cancellationToken);

    public Task<ApiResult<TestWorkflowResponse>> CanTakeAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
        => apiClient.GetAsync<TestWorkflowResponse>(
            $"api/testworkflow/can-take/{appointmentId}",
            cancellationToken);
}