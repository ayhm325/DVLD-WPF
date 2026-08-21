using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;

namespace Application.Validators;

public static class TestAppointmentValidator
{
    // CREATE
    public static Result ValidateCreate(CreateTestAppointmentDto? dto)
    {
        if (dto is null)
            return Result.Failure("Test appointment data is required.");

        var errors = new List<string>();

        if (dto.TestTypeID <= 0)
            errors.Add("Test type is required.");
        if (dto.LocalDrivingLicenseApplicationID <= 0)
            errors.Add("Local driving license application is required.");
        if (dto.AppointmentDate == default)
            errors.Add("Appointment date is required.");
        else if (dto.AppointmentDate <= DateTime.Now)
            errors.Add("Appointment date must be in the future.");
        if (dto.RetakeTestApplicationID.HasValue && dto.RetakeTestApplicationID.Value <= 0)
            errors.Add("Invalid retake test application ID.");

        return CreateResult(errors);
    }

    // UPDATE
    public static Result ValidateUpdate(UpdateTestAppointmentDto? dto)
    {
        if (dto is null)
            return Result.Failure("Test appointment data is required.");

        var errors = new List<string>();

        if (dto.TestAppointmentID <= 0)
            errors.Add("Invalid test appointment ID.");
        if (dto.AppointmentDate == default)
            errors.Add("Appointment date is required.");
        else if (dto.AppointmentDate <= DateTime.Now)
            errors.Add("Appointment date must be in the future.");

        return CreateResult(errors);
    }

    // SAVE TEST RESULT
    public static Result ValidateSaveTestResult(SaveTestResultDto? dto)
    {
        if (dto is null)
            return Result.Failure("Test result data is required.");

        var errors = new List<string>();

        if (dto.TestAppointmentID <= 0)
            errors.Add("Invalid test appointment ID.");
        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Trim().Length > 500)
            errors.Add("Test notes cannot exceed 500 characters.");

        return CreateResult(errors);
    }

    // ID
    public static Result ValidateId(int id)
    {
        return id > 0
            ? Result.Success()
            : Result.Failure("Invalid test appointment ID.");
    }

    // TEST TYPE ID
    public static Result ValidateTestTypeId(int testTypeId)
    {
        return Enum.IsDefined(typeof(TestTypeEnum), testTypeId)
            ? Result.Success()
            : Result.Failure("Invalid test type.");
    }

    // APPLICATION ID
    public static Result ValidateApplicationId(int applicationId)
    {
        return applicationId > 0
            ? Result.Success()
            : Result.Failure("Invalid local driving license application ID.");
    }

    // USER ID
    public static Result ValidateUserId(int userId)
    {
        return userId > 0
            ? Result.Success()
            : Result.Failure("Invalid user ID.");
    }

    // RESULT
    private static Result CreateResult(List<string> errors)
    {
        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(string.Join(Environment.NewLine, errors));
    }
}