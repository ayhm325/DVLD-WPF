using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IApplicationTypeService
    {
        Task<Result<List<ApplicationTypeDto>>> GetAllApplicationTypesAsync(
            CancellationToken cancellationToken = default);

        Task<Result<ApplicationTypeDto>> GetApplicationTypeByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<Result> UpdateApplicationTypeAsync(
            int id,
            ApplicationTypeDto dto,
            CancellationToken cancellationToken = default);
    }
}