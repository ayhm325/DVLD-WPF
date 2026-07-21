using Application.Common.Results;
using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ITestTypeService
    {
        Task<Result<List<TestTypeDto>>> GetAllTestTypesAsync();

        Task<Result<TestTypeDto>> GetTestTypeByIdAsync(int id);

        Task<Result> UpdateTestTypeAsync(int id, TestTypeDto dto);
    }
}