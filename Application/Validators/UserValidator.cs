using Application.Common;
using Application.DTOs.UserDTO;
using System.Text.RegularExpressions;

namespace Application.Validators
{
    public static class UserValidator
    {
        private static readonly string[] ReservedUsernames =
        [
            "admin",
            "root",
            "system",
            "null"
        ];

        public static ValidationResult ValidateCreateUser(
            CreateUserDto dto)
        {
            var errors = new List<string>();

            ValidateUsername(dto.UserName, errors);
            ValidatePassword(dto.Password, errors);

            if (dto.PersonId <= 0)
            {
                errors.Add("A valid person must be selected.");
            }

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        public static ValidationResult ValidateUpdateUser(
            UpdateUserDto dto)
        {
            var errors = new List<string>();

            ValidateUsername(dto.UserName, errors);

            if (dto.PersonId <= 0)
            {
                errors.Add("A valid person must be selected.");
            }

            // Password is optional during update.
            // Validate it only when the user wants to change it.
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                ValidatePassword(dto.Password, errors);
            }

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        public static ValidationResult ValidateLogin(
            LoginRequestDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                errors.Add("Username is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                errors.Add("Password is required.");
            }

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        public static ValidationResult ValidateChangePassword(
            ChangePasswordDto dto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
            {
                errors.Add("Current password is required.");
            }

            ValidatePassword(dto.NewPassword, errors);

            if (!string.IsNullOrWhiteSpace(dto.CurrentPassword) &&
                dto.CurrentPassword == dto.NewPassword)
            {
                errors.Add(
                    "The new password must be different from the current password.");
            }

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        public static ValidationResult ValidateUsernameFormat(
            string? username)
        {
            var errors = new List<string>();

            ValidateUsername(username, errors);

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }

        private static void ValidateUsername(
            string? username,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add("Username is required.");
                return;
            }

            if (username.Contains(' '))
            {
                errors.Add("Username cannot contain spaces.");
            }

            if (username.Length < 3 || username.Length > 20)
            {
                errors.Add("Username must be 3-20 characters.");
            }

            const string pattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";

            if (!Regex.IsMatch(username, pattern))
            {
                errors.Add(
                    "Username must start with a letter and contain only letters, numbers, or underscores.");
            }

            if (ReservedUsernames.Contains(
                username,
                StringComparer.OrdinalIgnoreCase))
            {
                errors.Add("This username is reserved.");
            }
        }

        private static void ValidatePassword(
            string? password,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required.");
                return;
            }

            if (password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters.");
            }

            if (password.Length > 100)
            {
                errors.Add("Password cannot exceed 100 characters.");
            }
        }
    }
}