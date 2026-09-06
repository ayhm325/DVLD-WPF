using Application.Common.Results;
using Application.DTOs;

namespace Presentation.Services.Api;

public interface IApplicationTypesApiClient
{
    Task<Result<List<ApplicationTypeDto>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<ApplicationTypeDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        int id,
        ApplicationTypeDto dto,
        CancellationToken cancellationToken = default);
}