using Domain.Entities;
using System.Linq.Expressions;

namespace Application.Interfaces
{
    public interface ITestRepository
    {
        // =========================
        // GET
        // =========================

        Task<Test?> GetByIdAsync(int id);

        Task<List<Test>> GetAllAsync();

        Task<List<Test>> GetByTestAppointmentIdAsync(
            int appointmentId);

        Task<List<Test>> GetByUserIdAsync(
            int userId);

        Task<int> GetTrialCountByApplicationIdAsync(
            int ldlAppId);


        // =========================
        // CHECKS
        // =========================

        Task<bool> IsTestExistsAsync(
            int id);

        Task<bool> IsTestAlreadyTakenAsync(
            int appointmentId);


        // =========================
        // CREATE
        // =========================

        Task<int> AddAsync(
            Test test);


        // =========================
        // UPDATE
        // =========================

        Task<bool> UpdateAsync(
            Test test);


        // =========================
        // DELETE
        // =========================

        Task<bool> DeleteAsync(
            int id);


        // =========================
        // EXTRA
        // =========================

        Task<int> CountAsync(
            Expression<Func<Test, bool>> predicate);
    }
}