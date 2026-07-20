using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Interfaces
{
    public interface IDriverRepository
    {
        // GET

        Task<Driver?> GetByIdAsync(int id);

        Task<List<Driver>> GetAllAsync();

        Task<Driver?> GetByPersonIdAsync(int personId);

        Task<List<Driver>> GetByCreatedUserIdAsync(int userId);



        // CHECK

        Task<bool> ExistsAsync(
            Expression<Func<Driver, bool>> predicate);


        Task<bool> ExistsByIdAsync(int driverId);


        Task<bool> ExistsByPersonIdAsync(int personId);



        // COMMAND

        Task AddAsync(Driver driver);


        Task UpdateAsync(Driver driver);


        Task DeleteAsync(int id);
    }
}