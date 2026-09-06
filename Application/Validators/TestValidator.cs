using Application.Common.Results;
using Application.DTOs.TestDTO;

namespace Application.Validators;

public static class TestValidator
{
    public static Result ValidateCreate(TestDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure("Test data is required.");

        var errors = new List<string>();

        if (dto.TestAppointmentID <= 0)
            errors.Add("Invalid test appointment ID.");

        ValidateNotes(dto.Notes, errors);

        return CreateResult(errors);
    }

    public static Result ValidateUpdate(TestDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure("Test data is required.");

        var errors = new List<string>();

        if (dto.TestID <= 0)
            errors.Add("Invalid test ID.");

        if (dto.TestAppointmentID <= 0)
            errors.Add("Invalid test appointment ID.");

        ValidateNotes(dto.Notes, errors);

        return CreateResult(errors);
    }

    public static Result ValidateId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid test ID.");

    public static Result ValidateAppointmentId(int appointmentId) =>
        appointmentId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test appointment ID.");

    public static Result ValidateUserId(int userId) =>
        userId > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid user ID.");

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

    private static Result CreateResult(List<string> errors) =>
        errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(
                string.Join(Environment.NewLine, errors));
}
