using DVLD.Contracts.TestAppointment;
using DVLD.Contracts.TestWorkflow;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ITestWorkflowApiClient
{
    Task<ApiResult<TestWorkflowResponse>> CanScheduleAsync(
        int localApplicationId,
        TestType testType,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TestWorkflowResponse>> GetNextTestAsync(
        int localApplicationId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TestWorkflowResponse>> CanTakeAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);
}