using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;

namespace Application.Validators;

public static class DriverValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(CreateDriverDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Driver data is required.");

        var errors = new List<string>();

        if (dto.PersonID <= 0)
            errors.Add("A valid person is required.");

        if (dto.CreatedByUserID <= 0)
            errors.Add("A valid creating user is required.");

        return CreateResult(errors);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(UpdateDriverDto? dto)
    {
        if (dto is null)
            return Result.ValidationFailure(
                "Driver data is required.");

        var errors = new List<string>();

        if (dto.DriverID <= 0)
            errors.Add("A valid driver ID is required.");

        if (dto.PersonID <= 0)
            errors.Add("A valid person is required.");

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
                "Invalid driver ID.");
    }


    // =========================================================
    // PERSON ID
    // =========================================================

    public static Result ValidatePersonId(int personId)
    {
        return personId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid person ID.");
    }


    // =========================================================
    // CREATED USER ID
    // =========================================================

    public static Result ValidateCreatedUserId(int userId)
    {
        return userId > 0
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid creating user ID.");
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