using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IDetainedLicenseService
    {
        Task<DetainedLicense?> GetByIdAsync(int id);
        Task<List<DetainedLicense>> GetAllAsync();
        Task<DetainedLicense> AddAsync(DetainedLicense entity);
        Task UpdateAsync(DetainedLicense entity);
        Task<bool> IsLicenseDetainedAsync(int licenseId);
        Task ReleaseAsync(int detainId, int releasedByUserId, int applicationId);
    }
}