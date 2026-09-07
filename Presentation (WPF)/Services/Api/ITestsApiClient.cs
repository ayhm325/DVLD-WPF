using DVLD.Contracts.Test;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ITestsApiClient
{
    Task<ApiResult<List<TestResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<TestResponse>> GetByIdAsync(
        int testId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<TestResponse>> GetByAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<List<TestResponse>>> GetByCreatedUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<ApiResult<int>> SaveResultAsync(
        SaveTestResultRequest request,
        CancellationToken cancellationToken = default);
}