using Domain.Entities;

namespace Application.Interfaces;

public interface ITestTypeRepository
{
    Task<List<TestType>> GetAllAsync();

    Task<TestType?> GetByIdAsync(int id);
}