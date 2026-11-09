using Domain.Entities;

namespace Application.Interfaces;

public interface ITestRepository
{
    Task<Test?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Test>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<Test>> GetByTestAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<List<Test>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
   

    Task<bool> IsTestAlreadyTakenAsync(int appointmentId, CancellationToken cancellationToken = default);

    Task AddAsync(Test test, CancellationToken cancellationToken = default);
}