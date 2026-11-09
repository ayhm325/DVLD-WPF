using Application.Common.Pagination;
using Application.DTOs.DriverDTO;
using Domain.Entities;

namespace Application.Interfaces;

public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Driver?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<Driver?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<List<Driver>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<Driver>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<Driver>> GetAllAsync(DriverQueryRequest request, CancellationToken cancellationToken = default);

    Task<Driver?> GetByPersonIdAsync(int personId, CancellationToken cancellationToken = default);

    Task<List<Driver>> GetByCreatedUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAsync(int driverId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByPersonIdAsync(int personId, CancellationToken cancellationToken = default);

    Task AddAsync(Driver driver, CancellationToken cancellationToken = default);

    void Delete(Driver driver);
}
