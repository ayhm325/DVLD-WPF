using Application.Common.Results;
using Application.DTOs.InternationalLicenseDTO;

namespace Application.Validators;

public static class InternationalLicenseValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(
        CreateInternationalLicenseDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "International license data is required.");
        }

        var errors = new List<string>();

        if (dto.ApplicationID <= 0)
            errors.Add(
                "A valid application is required.");

        if (dto.DriverID <= 0)
            errors.Add(
                "A valid driver is required.");

        if (dto.IssuedUsingLocalLicenseID <= 0)
            errors.Add(
                "A valid local license is required.");

        if (dto.IssueDate == default)
            errors.Add(
                "Issue date is required.");

        if (dto.ExpirationDate == default)
            errors.Add(
                "Expiration date is required.");

        if (dto.IssueDate != default &&
            dto.ExpirationDate != default &&
            dto.ExpirationDate <= dto.IssueDate)
        {
            errors.Add(
                "Expiration date must be after issue date.");
        }

        if (dto.CreatedByUserID <= 0)
            errors.Add(
                "A valid creating user is required.");

        return CreateResult(errors);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(
        UpdateInternationalLicenseDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "International license data is required.");
        }

        var errors = new List<string>();

        if (dto.InternationalLicenseID <= 0)
            errors.Add(
                "A valid international license ID is required.");

        if (dto.ApplicationID <= 0)
            errors.Add(
                "A valid application is required.");

        if (dto.DriverID <= 0)
            errors.Add(
                "A valid driver is required.");

        if (dto.IssuedUsingLocalLicenseID <= 0)
            errors.Add(
                "A valid local license is required.");

        if (dto.IssueDate == default)
            errors.Add(
                "Issue date is required.");

        if (dto.ExpirationDate == default)
            errors.Add(
                "Expiration date is required.");

        if (dto.IssueDate != default &&
            dto.ExpirationDate != default &&
            dto.ExpirationDate <= dto.IssueDate)
        {
            errors.Add(
                "Expiration date must be after issue date.");
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
                "Invalid international license ID.");
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

    public static Result ValidateApplicationId(
        int applicationId)
    {
        return applicationId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid application ID.");
    }


    // =========================================================
    // LOCAL LICENSE ID
    // =========================================================

    public static Result ValidateLocalLicenseId(
        int localLicenseId)
    {
        return localLicenseId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid local license ID.");
    }


    // =========================================================
    // RESULT
    // =========================================================

    private static Result CreateResult(
        List<string> errors)
    {
        return errors.Count > 0
            ? Result.ValidationFailure(
                string.Join(
                    Environment.NewLine,
                    errors))
            : Result.Success();
    }
}