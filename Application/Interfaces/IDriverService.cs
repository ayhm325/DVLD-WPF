using Application.DTOs;

namespace Application.Interfaces
{
    public interface IDriverService
    {
        // =========================
        // GET
        // =========================

        Task<DriverDto?> GetByIdAsync(int id);

        Task<List<DriverDto>> GetAllAsync();

        Task<DriverDto?> GetByPersonIdAsync(int personId);

        Task<List<DriverDto>> GetByCreatedUserIdAsync(int userId);

        // =========================
        // CHECKS
        // =========================

        Task<bool> ExistsByIdAsync(int driverId);

        Task<bool> ExistsByPersonIdAsync(int personId);

        // =========================
        // COMMANDS
        // =========================

        Task AddAsync(DriverDto dto);

        Task UpdateAsync(DriverDto dto);

        Task DeleteAsync(int id);
    }
}