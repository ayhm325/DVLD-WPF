using Application.Common.Results;
using Application.DTOs.TestTypeDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class TestTypeService : ITestTypeService
{
    private readonly ITestTypeRepository _repository;

    public TestTypeService(ITestTypeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // GET ALL
    public async Task<Result<List<TestTypeDto>>> GetAllTestTypesAsync()
    {
        var testTypes = await _repository.GetAllTestTypeAsync();
        return Result<List<TestTypeDto>>.Success(testTypes.Select(MapToDto).ToList());
    }

    // GET BY ID
    public async Task<Result<TestTypeDto>> GetTestTypeByIdAsync(int id)
    {
        var validation = TestTypeValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<TestTypeDto>.FromFailure(validation.Error);

        var testType = await _repository.GetTestTypeByIdAsync(id);
        if (testType is null)
            return Result<TestTypeDto>.FromFailure("Test type not found.");

        return Result<TestTypeDto>.Success(MapToDto(testType));
    }

    // UPDATE
    public async Task<Result> UpdateTestTypeAsync(int id, TestTypeDto dto)
    {
        var validation = TestTypeValidator.ValidateUpdate(id, dto);
        if (validation.IsFailure)
            return validation;

        var testType = await _repository.GetTestTypeByIdAsync(id);
        if (testType is null)
            return Result.Failure("Test type not found.");

        testType.TestTypeTitle = dto.TestTypeTitle.Trim();
        testType.TestTypeDescription = dto.TestTypeDescription.Trim();
        testType.TestTypeFees = dto.TestTypeFees;

        var isSuccess = await _repository.UpdateTestTypeAsync(testType);
        return isSuccess ? Result.Success() : Result.Failure("Failed to update test type.");
    }

    // MAPPING
    private static TestTypeDto MapToDto(TestType entity)
    {
        return new TestTypeDto
        {
            TestTypeId = entity.TestTypeId,
            TestTypeTitle = entity.TestTypeTitle,
            TestTypeDescription = entity.TestTypeDescription,
            TestTypeFees = entity.TestTypeFees
        };
    }
}