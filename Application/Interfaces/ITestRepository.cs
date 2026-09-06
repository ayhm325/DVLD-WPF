using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Interfaces;

public interface ITestRepository
{
    Task<Test?> GetByIdAsync(int id);
    Task<Test?> GetForUpdateAsync(int id);
    Task<List<Test>> GetAllAsync();

    Task<List<Test>> GetByTestAppointmentIdAsync(int appointmentId);
    Task<List<Test>> GetByUserIdAsync(int userId);

    Task<int> GetTrialCountByApplicationIdAsync(int ldlAppId);

    Task<bool> IsTestExistsAsync(int id);
    Task<bool> IsTestAlreadyTakenAsync(int appointmentId);

    Task AddAsync(Test test);
    void Delete(Test test);

    Task<int> CountAsync(
        Expression<Func<Test, bool>> predicate);
}
