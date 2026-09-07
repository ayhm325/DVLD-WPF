using Application.DTOs;
using Presentation.Services.Results;

namespace Presentation.Services.Api;

public interface IApplicationTypesApiClient
{
    Task<ApiResult<List<ApplicationTypeDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResult<ApplicationTypeDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ApiResult> UpdateAsync(
        int id,
        ApplicationTypeDto dto,
        CancellationToken cancellationToken = default);
}