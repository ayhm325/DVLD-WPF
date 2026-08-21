using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;

namespace Application.Validators;

public static class DetainedLicenseValidator
{
    // CREATE
    public static Result ValidateCreate(CreateDetainedLicenseDto? dto)
    {
        if (dto is null)
            return Result.Failure("Detained license data is required.");

        var errors = new List<string>();

        if (dto.LicenseID <= 0)
            errors.Add("A valid license is required.");
        if (dto.DetainDate == default)
            errors.Add("Detain date is required.");
        if (dto.FineFees < 0)
            errors.Add("Fine fees cannot be negative.");
        if (dto.FineFees > 9999999999999999.99m)
            errors.Add("Fine fees exceed the allowed value.");
        if (dto.CreatedByUserID <= 0)
            errors.Add("A valid creating user is required.");

        return CreateResult(errors);
    }

    // UPDATE
    public static Result ValidateUpdate(UpdateDetainedLicenseDto? dto)
    {
        if (dto is null)
            return Result.Failure("Detained license data is required.");

        var errors = new List<string>();

        if (dto.DetainID <= 0)
            errors.Add("A valid detained license ID is required.");
        if (dto.FineFees < 0)
            errors.Add("Fine fees cannot be negative.");
        if (dto.FineFees > 9999999999999999.99m)
            errors.Add("Fine fees exceed the allowed value.");

        // Release date
        if (dto.IsReleased && dto.ReleaseDate is null)
            errors.Add("Release date is required when the license is released.");
        if (!dto.IsReleased && dto.ReleaseDate is not null)
            errors.Add("Release date cannot be provided for a non-released license.");

        // Released by user
        if (dto.IsReleased && (!dto.ReleasedByUserID.HasValue || dto.ReleasedByUserID.Value <= 0))
            errors.Add("A valid releasing user is required when the license is released.");

        // Release application
        if (dto.IsReleased && (!dto.ReleaseApplicationID.HasValue || dto.ReleaseApplicationID.Value <= 0))
            errors.Add("A valid release application is required when the license is released.");

        return CreateResult(errors);
    }

    // RELEASE
    public static Result ValidateRelease(ReleaseDetainedLicenseDto? dto)
    {
        if (dto is null)
            return Result.Failure("Release data is required.");

        var errors = new List<string>();

        if (dto.DetainID <= 0)
            errors.Add("A valid detained license ID is required.");
        if (dto.ReleasedByUserID <= 0)
            errors.Add("A valid releasing user is required.");
        if (dto.ReleaseApplicationID <= 0)
            errors.Add("A valid release application is required.");

        return CreateResult(errors);
    }

    // ID
    public static Result ValidateId(int id)
    {
        return id > 0
            ? Result.Success()
            : Result.Failure("Invalid detained license ID.");
    }

    // LICENSE ID
    public static Result ValidateLicenseId(int licenseId)
    {
        return licenseId > 0
            ? Result.Success()
            : Result.Failure("Invalid license ID.");
    }

    // RESULT
    private static Result CreateResult(List<string> errors)
    {
        return errors.Count > 0
            ? Result.Failure(string.Join(Environment.NewLine, errors))
            : Result.Success();
    }
}