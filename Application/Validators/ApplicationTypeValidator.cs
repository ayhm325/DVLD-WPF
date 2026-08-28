using Application.Common.Results;
using Application.DTOs;

namespace Application.Validators;

public static class ApplicationTypeValidator
{
    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreate(
        ApplicationTypeDto? dto)
    {
        if (dto is null)
        {
            return Result.ValidationFailure(
                "Application type data is required.");
        }


        var errors = new List<string>();


        // -----------------------------------------------------
        // Title
        // -----------------------------------------------------

        var title =
            dto.ApplicationTypeTitle?
                .Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(
                "Application type title is required.");
        }
        else if (title.Length > 100)
        {
            errors.Add(
                "Application type title cannot exceed 100 characters.");
        }


        // -----------------------------------------------------
        // Fees
        // -----------------------------------------------------

        if (dto.ApplicationTypeFees < 0)
        {
            errors.Add(
                "Application type fees cannot be negative.");
        }

        if (dto.ApplicationTypeFees >
            9999999999999999.99m)
        {
            errors.Add(
                "Application type fees exceed the allowed value.");
        }


        return CreateResult(errors);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdate(
        int id,
        ApplicationTypeDto? dto)
    {
        if (id <= 0)
        {
            return Result.ValidationFailure(
                "Invalid application type ID.");
        }


        if (dto is null)
        {
            return Result.ValidationFailure(
                "Application type data is required.");
        }


        var errors = new List<string>();


        // -----------------------------------------------------
        // Title
        // -----------------------------------------------------

        var title =
            dto.ApplicationTypeTitle?
                .Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(
                "Application type title is required.");
        }
        else if (title.Length > 100)
        {
            errors.Add(
                "Application type title cannot exceed 100 characters.");
        }


        // -----------------------------------------------------
        // Fees
        // -----------------------------------------------------

        if (dto.ApplicationTypeFees < 0)
        {
            errors.Add(
                "Application type fees cannot be negative.");
        }

        if (dto.ApplicationTypeFees >
            9999999999999999.99m)
        {
            errors.Add(
                "Application type fees exceed the allowed value.");
        }


        return CreateResult(errors);
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