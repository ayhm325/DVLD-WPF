using Application.Common.Results;
using Application.DTOs.TestTypeDTO;

namespace Application.Interfaces;

public interface ITestTypeService
{
    Task<Result<List<TestTypeDto>>> GetAllTestTypesAsync(CancellationToken cancellationToken = default);

    Task<Result<TestTypeDto>> GetTestTypeByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateTestTypeAsync(
        int id,
        TestTypeDto dto,
        CancellationToken cancellationToken = default);
}