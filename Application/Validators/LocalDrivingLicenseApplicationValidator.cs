using Application.Common.Results;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;

namespace Application.Validators;

public static class LocalDrivingLicenseApplicationValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(
        CreateLocalDrivingLicenseApplicationDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "Local driving license application data is required.");
        }

        var errors = new List<string>();      

        if (dto.LicenseClassID <= 0)
            errors.Add("A valid license class is required.");

        return CreateResult(errors);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(
        int id,
        UpdateLocalDrivingLicenseApplicationDto? dto)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid local driving license application ID.");
        }

        if (dto is null)
        {
            return Result.ValidationFailure(
                "Local driving license application data is required.");
        }

        var errors = new List<string>();

        if (dto.LicenseClassID <= 0)
            errors.Add("A valid license class is required.");

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
                "Invalid local driving license application ID.");
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
    // PERSON ID
    // =========================================================

    public static Result ValidatePersonId(int personId)
    {
        return personId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid applicant person ID.");
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
    // AGE
    // =========================================================

    public static Result ValidateAge(
        DateTime dateOfBirth,
        int minimumAllowedAge,
        DateTime referenceDate)
    {
        var today = referenceDate.Date;
        var birthDate = dateOfBirth.Date;

        if (birthDate > today)
        {
            return Result.ValidationFailure(
                "Date of birth cannot be in the future.");
        }

        var age = today.Year - birthDate.Year;
        if (birthDate.AddYears(age) > today)
        {
            age--;
        }

        if (age < minimumAllowedAge)
        {
            return Result.ValidationFailure(
                $"Applicant does not meet the minimum age requirement for this license class. Minimum allowed age is {minimumAllowedAge}, but applicant is {age} years old.");
        }

        return Result.Success();
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
