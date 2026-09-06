using Application.Common.Results;
using Application.DTOs;

namespace Application.Validators;

public static class LicenseClassValidator
{
    public static Result ValidateId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid license class ID.");

    public static Result Validate(LicenseClassDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure("License class data is required.");

        var errors = new List<string>();

        if (dto.LicenseClassID <= 0)
            errors.Add("A valid license class ID is required.");

        var className = dto.LicenseClassName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(className))
            errors.Add("License class name is required.");
        else if (className.Length > 100)
            errors.Add("License class name cannot exceed 100 characters.");

        var description = dto.LicenseClassDescription?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(description))
            errors.Add("License class description is required.");
        else if (description.Length > 500)
            errors.Add("License class description cannot exceed 500 characters.");

        if (dto.MinAllowedAge <= 0)
            errors.Add("Minimum allowed age must be greater than zero.");

        if (dto.DefaultValidityLength <= 0)
            errors.Add("Default validity length must be greater than zero.");

        if (dto.LicenseClassFees < 0)
            errors.Add("License class fees cannot be negative.");

        if (dto.LicenseClassFees > 9999999999999999.99m)
            errors.Add("License class fees exceed the allowed value.");

        return errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(
                string.Join(Environment.NewLine, errors));
    }
}