using Domain.Entities;

namespace Application.Interfaces
{
    public interface ITestTypeRepository
    {
        // =========================
        // GET OPERATIONS
        // =========================

        Task<List<TestType>> GetAllTestTypeAsync();

        Task<TestType?> GetTestTypeByIdAsync(int id);


        // =========================
        // UPDATE OPERATION
        // =========================

        Task<bool> UpdateTestTypeAsync(TestType testtype);
    }
}