using Domain.Entities;

namespace Application.Interfaces;

public interface ITestTypeRepository
{
    Task<List<TestType>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TestType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TestType?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
}