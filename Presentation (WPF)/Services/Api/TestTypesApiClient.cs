using DVLD.Contracts.TestType;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class TestTypesApiClient(
    IApiClient apiClient) : ITestTypesApiClient
{
    public Task<ApiResult<List<TestTypeResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<List<TestTypeResponse>>(
            "api/testtypes",
            cancellationToken);
    }

    public Task<ApiResult<TestTypeResponse>> GetByIdAsync(
        int testTypeId,
        CancellationToken cancellationToken = default)
    {
        return apiClient.GetAsync<TestTypeResponse>(
            $"api/testtypes/{testTypeId}",
            cancellationToken);
    }

    public Task<ApiResult> UpdateAsync(
        int testTypeId,
        UpdateTestTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        return apiClient.PutAsync(
            $"api/testtypes/{testTypeId}",
            request,
            cancellationToken);
    }
}