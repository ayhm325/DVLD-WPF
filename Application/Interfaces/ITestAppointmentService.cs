using Application.DTOs;

namespace Application.Interfaces
{
    public interface ITestAppointmentService
    {
        // =========================
        // GET
        // =========================

        Task<TestAppointmentDto?> GetByIdAsync(int id);

        Task<List<TestAppointmentDto>> GetAllAsync();

        Task<List<TestAppointmentDto>> GetByApplicationIdAsync(int applicationId);

        Task<List<TestAppointmentDto>> GetByTestTypeIdAsync(int testTypeId);

        Task<List<TestAppointmentDto>> GetByCreatedUserIdAsync(int userId);

        // =========================
        // BUSINESS RULES
        // =========================

        Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime);

        Task<bool> HasUserConflictAsync(int userId, DateTime dateTime);

        Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime);

        // =========================
        // COMMANDS
        // =========================

        Task AddAsync(TestAppointmentDto dto);

        Task UpdateAsync(TestAppointmentDto dto);

        Task DeleteAsync(int id);
    }
}