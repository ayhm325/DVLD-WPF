using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;

namespace Application.Validators;

public static class DetainedLicenseValidator
{
    public static Result ValidateCreate(
        CreateDetainedLicenseDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Detained license data is required.");

        var errors = new List<string>();

        if (dto.LicenseID <= 0)
            errors.Add("A valid license is required.");

        if (dto.FineFees < 0)
            errors.Add("Fine fees cannot be negative.");

        if (dto.FineFees > 9_999_999_999_999_999.99m)
            errors.Add("Fine fees exceed the allowed value.");

        return CreateResult(errors);
    }

    public static Result ValidateRelease(
        ReleaseDetainedLicenseDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Release data is required.");

        return dto.DetainID > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "A valid detention ID is required.");
    }

    public static Result ValidateId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid detained license ID.");

    public static Result ValidateLicenseId(int licenseId) =>
        licenseId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid license ID.");

    private static Result CreateResult(
        List<string> errors) =>
        errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(
                string.Join(Environment.NewLine, errors));
}