using Application.Common.Results;
using Application.DTOs.TestDTO;

namespace Application.Validators;

public static class TestValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(TestDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Test data is required.");

        var errors = new List<string>();

        if (dto.TestAppointmentID <= 0)
            errors.Add("Invalid test appointment ID.");

        ValidateNotes(
            dto.Notes,
            errors);

        return CreateResult(errors);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(TestDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Test data is required.");

        var errors = new List<string>();

        if (dto.TestID <= 0)
            errors.Add("Invalid test ID.");

        if (dto.TestAppointmentID <= 0)
            errors.Add("Invalid test appointment ID.");

        ValidateNotes(
            dto.Notes,
            errors);

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
                "Invalid test ID.");
    }


    // =========================================================
    // APPOINTMENT ID
    // =========================================================

    public static Result ValidateAppointmentId(
        int appointmentId)
    {
        return appointmentId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test appointment ID.");
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
    // NOTES
    // =========================================================

    private static void ValidateNotes(
        string? notes,
        List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(notes) &&
            notes.Trim().Length > 500)
        {
            errors.Add(
                "Test notes cannot exceed 500 characters.");
        }
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