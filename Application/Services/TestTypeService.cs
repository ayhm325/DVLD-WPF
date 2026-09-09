using Application.Common.Results;
using Application.DTOs.TestTypeDTO;
using Application.Interfaces;
using Application.Validators;

namespace Application.Services;

public sealed class TestTypeService(
    ITestTypeRepository repository,
    IUnitOfWork unitOfWork) : ITestTypeService
{
    private readonly ITestTypeRepository _repository =
        repository
        ?? throw new ArgumentNullException(nameof(repository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork
        ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<Result<List<TestTypeDto>>> GetAllTestTypesAsync()
    {
        var testTypes =
            await _repository.GetAllAsync();

        return Result<List<TestTypeDto>>.Success(
            testTypes
                .Select(MapToDto)
                .ToList());
    }

    public async Task<Result<TestTypeDto>> GetTestTypeByIdAsync(
        int id)
    {
        var validation =
            TestTypeValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<TestTypeDto>.FromValidationFailure(
                validation.Error);
        }

        var testType =
            await _repository.GetByIdAsync(id);

        return testType is null
            ? Result<TestTypeDto>.FromNotFound(
                "Test type not found.")
            : Result<TestTypeDto>.Success(
                MapToDto(testType));
    }

    public async Task<Result> UpdateTestTypeAsync(
        int id,
        TestTypeDto dto)
    {
        var validation =
            TestTypeValidator.ValidateUpdate(id, dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var testType =
            await _repository.GetForUpdateAsync(id);

        if (testType is null)
        {
            return Result.NotFound(
                "Test type not found.");
        }

        testType.TestTypeTitle =
            dto.TestTypeTitle.Trim();

        testType.TestTypeDescription =
            dto.TestTypeDescription.Trim();

        testType.TestTypeFees =
            dto.TestTypeFees;

        var saved =
            await _unitOfWork.SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to update test type.");
    }

    private static TestTypeDto MapToDto(
        Domain.Entities.TestType entity) =>
        new()
        {
            TestTypeId = entity.TestTypeId,
            TestTypeTitle = entity.TestTypeTitle,
            TestTypeDescription =
                entity.TestTypeDescription,
            TestTypeFees = entity.TestTypeFees
        };
}