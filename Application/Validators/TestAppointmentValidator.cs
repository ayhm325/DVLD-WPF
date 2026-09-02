using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;

namespace Application.Validators;

public static class TestAppointmentValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(
        CreateTestAppointmentDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "Test appointment data is required.");
        }

        var errors = new List<string>();

        // -----------------------------------------------------
        // TEST TYPE
        // -----------------------------------------------------

        if (dto.TestTypeID <= 0)
        {
            errors.Add(
                "Test type is required.");
        }
        else if (!Enum.IsDefined(
                     typeof(TestTypeEnum),
                     dto.TestTypeID))
        {
            errors.Add(
                "Invalid test type.");
        }

        // -----------------------------------------------------
        // LOCAL APPLICATION
        // -----------------------------------------------------

        if (dto.LocalDrivingLicenseApplicationID <= 0)
        {
            errors.Add(
                "Local driving license application is required.");
        }

        // -----------------------------------------------------
        // APPOINTMENT DATE
        // -----------------------------------------------------

        if (dto.AppointmentDate == default)
        {
            errors.Add(
                "Appointment date is required.");
        }
        else if (dto.AppointmentDate <= DateTime.Now)
        {
            errors.Add(
                "Appointment date must be in the future.");
        }

        // -----------------------------------------------------
        // RETAKE APPLICATION
        // -----------------------------------------------------

        if (dto.RetakeTestApplicationID.HasValue &&
            dto.RetakeTestApplicationID.Value <= 0)
        {
            errors.Add(
                "Invalid retake test application ID.");
        }

        return CreateResult(errors);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(
        UpdateTestAppointmentDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "Test appointment data is required.");
        }

        var errors = new List<string>();

        // -----------------------------------------------------
        // APPOINTMENT ID
        // -----------------------------------------------------

        if (dto.TestAppointmentID <= 0)
        {
            errors.Add(
                "Invalid test appointment ID.");
        }

        // -----------------------------------------------------
        // APPOINTMENT DATE
        // -----------------------------------------------------

        if (dto.AppointmentDate == default)
        {
            errors.Add(
                "Appointment date is required.");
        }
        else if (dto.AppointmentDate <= DateTime.Now)
        {
            errors.Add(
                "Appointment date must be in the future.");
        }

        return CreateResult(errors);
    }


    // =========================================================
    // SAVE TEST RESULT
    // =========================================================

    public static Result ValidateSaveTestResult(
        SaveTestResultDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "Test result data is required.");
        }

        var errors = new List<string>();

        // -----------------------------------------------------
        // APPOINTMENT ID
        // -----------------------------------------------------

        if (dto.TestAppointmentID <= 0)
        {
            errors.Add(
                "Invalid test appointment ID.");
        }

        // -----------------------------------------------------
        // NOTES
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(dto.Notes) &&
            dto.Notes.Trim().Length > 500)
        {
            errors.Add(
                "Test notes cannot exceed 500 characters.");
        }

        return CreateResult(errors);
    }


    // =========================================================
    // ID
    // =========================================================

    public static Result ValidateId(
        int id)
    {
        return id > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test appointment ID.");
    }


    // =========================================================
    // TEST TYPE ID
    // =========================================================

    public static Result ValidateTestTypeId(
        int testTypeId)
    {
        return Enum.IsDefined(
            typeof(TestTypeEnum),
            testTypeId)
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test type.");
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
                "Invalid local driving license application ID.");
    }


    // =========================================================
    // USER ID
    // =========================================================

    public static Result ValidateUserId(
        int userId)
    {
        return userId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid user ID.");
    }


    // =========================================================
    // RESULT
    // =========================================================

    private static Result CreateResult(
        List<string> errors)
    {
        return errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(
                string.Join(
                    Environment.NewLine,
                    errors));
    }
}