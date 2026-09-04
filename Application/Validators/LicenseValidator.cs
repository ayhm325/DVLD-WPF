using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Domain.Enums;

namespace Application.Validators;

public static class LicenseValidator
{
    public static Result ValidateCreate(CreateLicenseDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure("License data is required.");

        var errors = new List<string>();

        if (dto.ApplicationID <= 0)
            errors.Add("A valid application is required.");

        if (dto.DriverID <= 0)
            errors.Add("A valid driver is required.");

        if (dto.LicenseClassID <= 0)
            errors.Add("A valid license class is required.");

        if (dto.IssueDate == default)
            errors.Add("Issue date is required.");

        if (dto.ExpirationDate == default)
            errors.Add("Expiration date is required.");

        if (dto.IssueDate != default &&
            dto.ExpirationDate != default &&
            dto.ExpirationDate <= dto.IssueDate)
            errors.Add("Expiration date must be after issue date.");

        if (!string.IsNullOrWhiteSpace(dto.Notes) &&
            dto.Notes.Trim().Length > 500)
            errors.Add("License notes cannot exceed 500 characters.");

        if (dto.PaidFees < 0)
            errors.Add("Paid fees cannot be negative.");

        if (dto.PaidFees > 9999999999999999.99m)
            errors.Add("Paid fees exceed the allowed value.");

        if (!Enum.IsDefined(typeof(IssueReason), (int)dto.IssueReason))
            errors.Add("Invalid license issue reason.");

        return errors.Count > 0
            ? Result.ValidationFailure(string.Join(Environment.NewLine, errors))
            : Result.Success();
    }

    public static Result ValidateId(int id) =>
        id > 0 ? Result.Success() : Result.ValidationFailure("Invalid license ID.");

    public static Result ValidateDriverId(int driverId) =>
        driverId > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid driver ID.");

    public static Result ValidateApplicationId(int applicationId) =>
        applicationId > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid application ID.");

    public static Result ValidateLicenseClassId(int licenseClassId) =>
        licenseClassId > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid license class ID.");
}