using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ILicenseClassService
    {
        Task<Result<List<LicenseClassDto>>> GetAllLicenseClassesAsync();

        Task<Result<LicenseClassDto>> GetLicenseClassByIdAsync(int id);
    }
}