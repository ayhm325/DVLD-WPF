using Application.DTOs;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public sealed class ApplicationTypesApiClient(
    IApiClient apiClient) : IApplicationTypesApiClient
{
    private readonly IApiClient _apiClient =
        apiClient
        ?? throw new ArgumentNullException(nameof(apiClient));

    public Task<ApiResult<List<ApplicationTypeDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _apiClient.GetAsync<List<ApplicationTypeDto>>(
            "api/applicationtypes",
            cancellationToken);
    }

    public Task<ApiResult<ApplicationTypeDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult<ApplicationTypeDto>.Failure(
                    "Application type ID must be greater than zero."));
        }

        return _apiClient.GetAsync<ApplicationTypeDto>(
            $"api/applicationtypes/{id}",
            cancellationToken);
    }

    public Task<ApiResult> UpdateAsync(
        int id,
        ApplicationTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (id <= 0)
        {
            return Task.FromResult(
                ApiResult.Failure(
                    "Application type ID must be greater than zero."));
        }

        return _apiClient.PutAsync(
            $"api/applicationtypes/{id}",
            dto,
            cancellationToken);
    }
}