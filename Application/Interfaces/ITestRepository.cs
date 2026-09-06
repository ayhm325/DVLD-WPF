using Domain.Entities;

namespace Application.Interfaces;

public interface ITestRepository
{
    Task<Test?> GetByIdAsync(int id);
    Task<List<Test>> GetAllAsync();

    Task<List<Test>> GetByTestAppointmentIdAsync(int appointmentId);
    Task<List<Test>> GetByUserIdAsync(int userId);

    Task<int> GetTrialCountByApplicationIdAsync(
        int localDrivingLicenseApplicationId);

    Task<bool> IsTestAlreadyTakenAsync(int appointmentId);

    Task AddAsync(Test test);
}
