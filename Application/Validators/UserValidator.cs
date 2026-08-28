using Application.Common.Results;
using Application.DTOs.UserDTO;
using System.Text.RegularExpressions;

namespace Application.Validators;

public static class UserValidator
{
    private static readonly string[] ReservedUsernames =
    [
        "admin",
        "root",
        "system",
        "null"
    ];


    // =========================================================
    // CREATE
    // =========================================================

    public static Result ValidateCreateUser(
        CreateUserDto? dto)
    {
        if (dto is null)
        {
            return Result.Failure(
                "User data is required.");
        }

        var errors =
            new List<string>();

        ValidateUsername(
            dto.UserName,
            errors);

        ValidatePassword(
            dto.Password,
            errors);

        if (dto.PersonId <= 0)
        {
            errors.Add(
                "A valid person must be selected.");
        }

        return CreateValidationResult(
            errors);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public static Result ValidateUpdateUser(
        UpdateUserDto? dto)
    {
        if (dto is null)
        {
            return Result.Failure(
                "User data is required.");
        }

        var errors =
            new List<string>();

        ValidateUsername(
            dto.UserName,
            errors);

        if (dto.PersonId <= 0)
        {
            errors.Add(
                "A valid person must be selected.");
        }

        return CreateValidationResult(
            errors);
    }


    // =========================================================
    // LOGIN
    // =========================================================

    public static Result ValidateLogin(
        LoginRequestDto? dto)
    {
        if (dto is null)
        {
            return Result.Failure(
                "Login data is required.");
        }

        var errors =
            new List<string>();

        if (string.IsNullOrWhiteSpace(
            dto.UserName))
        {
            errors.Add(
                "Username is required.");
        }

        if (string.IsNullOrWhiteSpace(
            dto.Password))
        {
            errors.Add(
                "Password is required.");
        }

        return CreateValidationResult(
            errors);
    }


    // =========================================================
    // CHANGE PASSWORD
    // =========================================================

    public static Result ValidateChangePassword(
        ChangePasswordDto? dto)
    {
        if (dto is null)
        {
            return Result.Failure(
                "Change password data is required.");
        }

        var errors =
            new List<string>();

        if (string.IsNullOrWhiteSpace(
            dto.CurrentPassword))
        {
            errors.Add(
                "Current password is required.");
        }

        ValidatePassword(
            dto.NewPassword,
            errors);

        if (!string.IsNullOrWhiteSpace(
                dto.CurrentPassword) &&
            dto.CurrentPassword ==
                dto.NewPassword)
        {
            errors.Add(
                "The new password must be different from the current password.");
        }

        return CreateValidationResult(
            errors);
    }


    // =========================================================
    // USERNAME FORMAT
    // =========================================================

    public static Result ValidateUsernameFormat(
        string? username)
    {
        var errors =
            new List<string>();

        ValidateUsername(
            username,
            errors);

        return CreateValidationResult(
            errors);
    }


    // =========================================================
    // USERNAME
    // =========================================================

    private static void ValidateUsername(
        string? username,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add(
                "Username is required.");

            return;
        }

        username =
            username.Trim();


        if (username.Contains(' '))
        {
            errors.Add(
                "Username cannot contain spaces.");
        }


        if (username.Length < 3 ||
            username.Length > 20)
        {
            errors.Add(
                "Username must be 3-20 characters.");
        }


        if (!Regex.IsMatch(
                username,
                @"^[a-zA-Z][a-zA-Z0-9_]*$"))
        {
            errors.Add(
                "Username must start with a letter and contain only letters, numbers, or underscores.");
        }


        if (ReservedUsernames.Contains(
                username,
                StringComparer.OrdinalIgnoreCase))
        {
            errors.Add(
                "This username is reserved.");
        }
    }


    // =========================================================
    // PASSWORD
    // =========================================================

    private static void ValidatePassword(
        string? password,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(
                "Password is required.");

            return;
        }


        if (password.Length < 8)
        {
            errors.Add(
                "Password must be at least 8 characters.");
        }


        if (password.Length > 100)
        {
            errors.Add(
                "Password cannot exceed 100 characters.");
        }
    }


    // =========================================================
    // RESULT
    // =========================================================

    private static Result CreateValidationResult(
        List<string> errors)
    {
        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(
                string.Join(
                    Environment.NewLine,
                    errors));
    }
}
