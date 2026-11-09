using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILicenseClassService
    {
        Task<Result<List<LicenseClassDto>>> GetAllLicenseClassesAsync(CancellationToken cancellationToken = default);

        Task<Result<LicenseClassDto>> GetLicenseClassByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Result> UpdateLicenseClassAsync(int id, LicenseClassDto dto, CancellationToken cancellationToken = default);
    }
}
