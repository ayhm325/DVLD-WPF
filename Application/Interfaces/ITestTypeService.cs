
using Application.DTOs;

namespace Application.Interfaces
{
    public interface ITestTypeService
    {
        Task<List<TestTypeDto>> GetAllTestTypesAsync();
    
        Task<TestTypeDto?> GetTestTypeByIdAsync(int id);   
    
        Task<bool> UpdateTestTypeAsync(int id, TestTypeDto dto);

        //Task<bool> IsTestTypeExistsAsync(int id);
    }
}
