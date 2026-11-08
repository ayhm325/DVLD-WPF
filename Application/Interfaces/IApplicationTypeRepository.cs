using Domain.Entities;

namespace Application.Interfaces
{
    public interface IApplicationTypeRepository
    {
        Task<List<ApplicationType>> GetAllApplicationTypesAsync(
            CancellationToken cancellationToken = default);

        Task<ApplicationType?> GetApplicationTypeByIdAsync(
            int id,
            CancellationToken cancellationToken = default);

        Task<bool> UpdateApplicationTypeAsync(
            ApplicationType appType,
            CancellationToken cancellationToken = default);
    }
}