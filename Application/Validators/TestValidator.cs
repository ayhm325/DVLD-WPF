using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;

namespace Application.Validators;

public static class TestValidator
{
    public static Result ValidateCreate(
        SaveTestResultDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Test result data is required.");

        var errors = new List<string>();

        if (dto.TestAppointmentID <= 0)
            errors.Add("Invalid test appointment ID.");

        if (!string.IsNullOrWhiteSpace(dto.Notes) &&
            dto.Notes.Trim().Length > 500)
        {
            errors.Add(
                "Test notes cannot exceed 500 characters.");
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(
                string.Join(Environment.NewLine, errors));
    }

    public static Result ValidateId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test ID.");

    public static Result ValidateAppointmentId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test appointment ID.");

    public static Result ValidateUserId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid user ID.");
}
