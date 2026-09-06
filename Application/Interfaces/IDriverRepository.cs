using Domain.Entities;

namespace Application.Interfaces;

public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(int id);

    Task<Driver?> GetForDeleteAsync(int id);

    Task<List<Driver>> GetAllAsync();

    Task<Driver?> GetByPersonIdAsync(int personId);

    Task<List<Driver>> GetByCreatedUserIdAsync(int userId);

    Task<bool> ExistsByIdAsync(int driverId);

    Task<bool> ExistsByPersonIdAsync(int personId);

    Task AddAsync(Driver driver);

    void Delete(Driver driver);
}