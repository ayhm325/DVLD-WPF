using Domain.Entities;
using Domain.Enums;
using System.Linq.Expressions;

namespace Application.Interfaces
{
    public interface ITestAppointmentRepository
    {
        // GET
        Task<TestAppointment?> GetByIdAsync(int id);

        Task<List<TestAppointment>> GetAllAsync();

        Task<List<TestAppointment>> GetByApplicationIdAsync(int applicationId);

        Task<List<TestAppointment>> GetByTestTypeIdAsync(TestTypeEnum testType);

        Task<List<TestAppointment>> GetByCreatedUserIdAsync(int userId);

        Task<TestAppointment?> GetScheduleInfoAsync(int testAppointmentId);


        // CHECK
        Task<bool> ExistsAsync(Expression<Func<TestAppointment, bool>> predicate);

        Task<bool> HasConflictAsync(
            int testTypeId,
            DateTime dateTime);

        Task<bool> HasUserConflictAsync(
            int userId,
            DateTime dateTime);

        Task<bool> HasApplicationConflictAsync(
            int applicationId,
            DateTime dateTime);

        Task<bool> HasPassedAllTestsAsync(
            int appId);

        Task<bool> IsAppointmentAlreadyScheduledAsync(
            int localAppId,
            int testTypeId);


        // CREATE
        Task<bool> AddAsync(TestAppointment appointment);


        // UPDATE
        Task<bool> UpdateAsync(TestAppointment appointment);


        // DELETE
        Task DeleteAsync(int id);
    }
}