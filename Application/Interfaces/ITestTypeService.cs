using Application.Common.Results;
using Application.DTOs.TestTypeDTO;

namespace Application.Interfaces;

public interface ITestTypeService
{
    Task<Result<List<TestTypeDto>>>
        GetAllTestTypesAsync();

    Task<Result<TestTypeDto>>
        GetTestTypeByIdAsync(
            int id);

    Task<Result>
        UpdateTestTypeAsync(
            int id,
            TestTypeDto dto);
}