using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;

namespace Application.Validators;

public static class TestAppointmentValidator
{
    public static Result ValidateCreate(
        CreateTestAppointmentDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Test appointment data is required.");

        var errors = new List<string>();

        if (dto.TestTypeID <= 0)
        {
            errors.Add("Test type is required.");
        }
        else if (!Enum.IsDefined(
                     typeof(TestTypeEnum),
                     dto.TestTypeID))
        {
            errors.Add("Invalid test type.");
        }

        if (dto.LocalDrivingLicenseApplicationID <= 0)
        {
            errors.Add(
                "Local driving license application is required.");
        }

        if (dto.AppointmentDate == default)
        {
            errors.Add("Appointment date is required.");
        }
        else if (dto.AppointmentDate <= DateTime.UtcNow)
        {
            errors.Add(
                "Appointment date must be in the future.");
        }

        if (dto.RetakeTestApplicationID.HasValue &&
            dto.RetakeTestApplicationID.Value <= 0)
        {
            errors.Add(
                "Invalid retake test application ID.");
        }

        return CreateResult(errors);
    }

    public static Result ValidateUpdate(
        UpdateTestAppointmentDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Test appointment data is required.");

        var errors = new List<string>();

        if (dto.TestAppointmentID <= 0)
        {
            errors.Add(
                "Invalid test appointment ID.");
        }

        if (dto.AppointmentDate == default)
        {
            errors.Add("Appointment date is required.");
        }
        else if (dto.AppointmentDate <= DateTime.UtcNow)
        {
            errors.Add(
                "Appointment date must be in the future.");
        }

        return CreateResult(errors);
    }

    public static Result ValidateId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test appointment ID.");

    public static Result ValidateTestTypeId(int testTypeId) =>
        Enum.IsDefined(
            typeof(TestTypeEnum),
            testTypeId)
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test type.");

    public static Result ValidateApplicationId(int applicationId) =>
        applicationId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid local driving license application ID.");

    public static Result ValidateUserId(int userId) =>
        userId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid user ID.");

    private static Result CreateResult(
        List<string> errors) =>
        errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(
                string.Join(
                    Environment.NewLine,
                    errors));
}