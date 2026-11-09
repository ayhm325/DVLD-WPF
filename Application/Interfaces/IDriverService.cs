using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;

namespace Application.Interfaces;

public interface IDriverService
{
    Task<Result<DriverDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<List<DriverDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<PagedResult<DriverDto>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<DriverDto>>> GetAllAsync(DriverQueryRequest request, CancellationToken cancellationToken = default);
    Task<Result<DriverDto>> GetByPersonIdAsync(int personId, CancellationToken cancellationToken = default);
    Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByIdAsync(int driverId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPersonIdAsync(int personId, CancellationToken cancellationToken = default);

    Task<Result<int>> AddAsync(CreateDriverDto dto, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(UpdateDriverDto dto, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
