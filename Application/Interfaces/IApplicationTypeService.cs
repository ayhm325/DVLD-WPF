using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IApplicationTypeService
    {
        Task<Result<List<ApplicationTypeDto>>> GetAllApplicationTypesAsync();

        Task<Result<ApplicationTypeDto>> GetApplicationTypeByIdAsync(int id);

        Task<Result> UpdateApplicationTypeAsync(int id, ApplicationTypeDto dto);
    }
}