using Application.DTOs;

namespace Application.Interfaces
{
    public interface ITestService
    {
        Task<TestDto?> GetByIdAsync(int id);
        Task<List<TestDto>> GetAllAsync();
        Task<List<TestDto>> GetByTestAppointmentIdAsync(int appointmentId);
        Task<List<TestDto>> GetByUserIdAsync(int userId);

        Task<bool> IsTestExistsAsync(int id);
        Task<bool> IsTestAlreadyTakenAsync(int appointmentId);

        Task<int> AddAsync(TestDto dto);
        Task<bool> UpdateAsync(TestDto dto);
        Task<bool> DeleteAsync(int id);
    }
}