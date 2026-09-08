using DVLD.Contracts.TestType;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface ITestTypesApiClient
{
    Task<ApiResult<List<TestTypeResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<TestTypeResponse>> GetByIdAsync(
        int testTypeId,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateAsync(
        int testTypeId,
        UpdateTestTypeRequest request,
        CancellationToken cancellationToken = default);
}