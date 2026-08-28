using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Domain.Enums;

namespace Application.Validators;

public static class LicenseValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(CreateLicenseDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "License data is required.");
        }

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
        {
            errors.Add(
                "Expiration date must be after issue date.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Notes) &&
            dto.Notes.Trim().Length > 500)
        {
            errors.Add(
                "License notes cannot exceed 500 characters.");
        }

        if (dto.PaidFees < 0)
            errors.Add("Paid fees cannot be negative.");

        if (dto.PaidFees > 9999999999999999.99m)
            errors.Add("Paid fees exceed the allowed value.");

        if (!Enum.IsDefined(
                typeof(IssueReason),
                (int)dto.IssueReason))
        {
            errors.Add(
                "Invalid license issue reason.");
        }

        if (dto.CreatedByUserID <= 0)
            errors.Add(
                "A valid creating user is required.");

        return CreateResult(errors);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(UpdateLicenseDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "License data is required.");
        }

        var errors = new List<string>();

        if (dto.LicenseID <= 0)
            errors.Add("A valid license ID is required.");

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
        {
            errors.Add(
                "Expiration date must be after issue date.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Notes) &&
            dto.Notes.Trim().Length > 500)
        {
            errors.Add(
                "License notes cannot exceed 500 characters.");
        }

        if (dto.PaidFees < 0)
            errors.Add("Paid fees cannot be negative.");

        if (dto.PaidFees > 9999999999999999.99m)
            errors.Add("Paid fees exceed the allowed value.");

        if (!Enum.IsDefined(
                typeof(IssueReason),
                (int)dto.IssueReason))
        {
            errors.Add(
                "Invalid license issue reason.");
        }

        return CreateResult(errors);
    }

    // =========================================================
    // ID
    // =========================================================

    public static Result ValidateId(int id)
    {
        return id > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid license ID.");
    }

    // =========================================================
    // DRIVER ID
    // =========================================================

    public static Result ValidateDriverId(int driverId)
    {
        return driverId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid driver ID.");
    }

    // =========================================================
    // APPLICATION ID
    // =========================================================

    public static Result ValidateApplicationId(int applicationId)
    {
        return applicationId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid application ID.");
    }

    // =========================================================
    // LICENSE CLASS ID
    // =========================================================

    public static Result ValidateLicenseClassId(int licenseClassId)
    {
        return licenseClassId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid license class ID.");
    }

    // =========================================================
    // RESULT
    // =========================================================

    private static Result CreateResult(List<string> errors)
    {
        return errors.Count > 0
            ? Result.ValidationFailure(
                string.Join(Environment.NewLine, errors))
            : Result.Success();
    }
}