using Application.Common.Results;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IDriverService
    {
        // =========================
        // GET
        // =========================

        Task<Result<DriverDto>> GetByIdAsync(int id);

        Task<Result<List<DriverDto>>> GetAllAsync();

        Task<Result<DriverDto>> GetByPersonIdAsync(int personId);

        Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(int userId);

        // =========================
        // CHECKS
        // =========================

        Task<bool> ExistsByIdAsync(int driverId);

        Task<bool> ExistsByPersonIdAsync(int personId);

        // =========================
        // COMMANDS
        // =========================

        Task<Result<int>> AddAsync(DriverDto dto);

        Task<Result> UpdateAsync(DriverDto dto);

        Task<Result> DeleteAsync(int id);
    }
}