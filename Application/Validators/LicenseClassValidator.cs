using Application.Common.Results;
using Application.DTOs;

namespace Application.Validators;

public static class LicenseClassValidator
{
    // ID
    public static Result ValidateId(int id)
    {
        return id > 0
            ? Result.Success()
            : Result.Failure("Invalid license class ID.");
    }

    // VALIDATE
    public static Result Validate(LicenseClassDto? dto)
    {
        if (dto is null)
            return Result.Failure("License class data is required.");

        var errors = new List<string>();

        if (dto.LicenseClassID <= 0)
            errors.Add("A valid license class ID is required.");

        // Class name
        var className = dto.LicenseClassName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(className))
            errors.Add("License class name is required.");
        else if (className.Length > 100)
            errors.Add("License class name cannot exceed 100 characters.");

        // Description
        var description = dto.LicenseClassDescription?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
            errors.Add("License class description is required.");
        else if (description.Length > 500)
            errors.Add("License class description cannot exceed 500 characters.");

        // Min age
        if (dto.MinAllowedAge <= 0)
            errors.Add("Minimum allowed age must be greater than zero.");

        // Validity length
        if (dto.DefaultValidityLength <= 0)
            errors.Add("Default validity length must be greater than zero.");

        // Fees
        if (dto.LicenseClassFees < 0)
            errors.Add("License class fees cannot be negative.");
        if (dto.LicenseClassFees > 9999999999999999.99m)
            errors.Add("License class fees exceed the allowed value.");

        return errors.Count > 0
            ? Result.Failure(string.Join(Environment.NewLine, errors))
            : Result.Success();
    }
}