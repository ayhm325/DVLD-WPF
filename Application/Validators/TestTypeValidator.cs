using Application.Common.Results;
using Application.DTOs.TestTypeDTO;

namespace Application.Validators;

public static class TestTypeValidator
{
    // UPDATE
    public static Result ValidateUpdate(int id, TestTypeDto? dto)
    {
        var errors = new List<string>();

        if (id <= 0)
            errors.Add("Invalid test type ID.");

        if (dto is null)
            return Result.Failure("Test type data is required.");

        // Title
        if (string.IsNullOrWhiteSpace(dto.TestTypeTitle))
            errors.Add("Test type title is required.");
        else if (dto.TestTypeTitle.Trim().Length > 100)
            errors.Add("Test type title cannot exceed 100 characters.");

        // Description
        if (string.IsNullOrWhiteSpace(dto.TestTypeDescription))
            errors.Add("Test type description is required.");
        else if (dto.TestTypeDescription.Trim().Length > 250)
            errors.Add("Test type description cannot exceed 250 characters.");

        // Fees
        if (dto.TestTypeFees < 0)
            errors.Add("Test type fees cannot be negative.");

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(string.Join(Environment.NewLine, errors));
    }

    // ID
    public static Result ValidateId(int id)
    {
        return id > 0
            ? Result.Success()
            : Result.Failure("Invalid test type ID.");
    }
}