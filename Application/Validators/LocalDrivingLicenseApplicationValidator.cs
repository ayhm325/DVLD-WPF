using Application.Common.Results;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;

namespace Application.Validators;

public static class LocalDrivingLicenseApplicationValidator
{
    // CREATE
    public static Result ValidateCreate(CreateLocalDrivingLicenseApplicationDto? dto)
    {
        if (dto is null)
            return Result.Failure("Local driving license application data is required.");

        var errors = new List<string>();

        if (dto.ApplicationID <= 0)
            errors.Add("A valid application is required.");
        if (dto.LicenseClassID <= 0)
            errors.Add("A valid license class is required.");

        return CreateResult(errors);
    }

    // UPDATE
    public static Result ValidateUpdate(int id, UpdateLocalDrivingLicenseApplicationDto? dto)
    {
        if (id <= 0)
            return Result.Failure("Invalid local driving license application ID.");

        if (dto is null)
            return Result.Failure("Local driving license application data is required.");

        var errors = new List<string>();

        if (dto.LicenseClassID <= 0)
            errors.Add("A valid license class is required.");

        return CreateResult(errors);
    }

    // ID
    public static Result ValidateId(int id)
    {
        return id > 0
            ? Result.Success()
            : Result.Failure("Invalid local driving license application ID.");
    }

    // APPLICATION ID
    public static Result ValidateApplicationId(int applicationId)
    {
        return applicationId > 0
            ? Result.Success()
            : Result.Failure("Invalid application ID.");
    }

    // PERSON ID
    public static Result ValidatePersonId(int personId)
    {
        return personId > 0
            ? Result.Success()
            : Result.Failure("Invalid applicant person ID.");
    }

    // LICENSE CLASS ID
    public static Result ValidateLicenseClassId(int licenseClassId)
    {
        return licenseClassId > 0
            ? Result.Success()
            : Result.Failure("Invalid license class ID.");
    }

    // RESULT
    private static Result CreateResult(List<string> errors)
    {
        return errors.Count > 0
            ? Result.Failure(string.Join(Environment.NewLine, errors))
            : Result.Success();
    }
}