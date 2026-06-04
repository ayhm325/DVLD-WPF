using Application.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Application.Validators
{
    public static class UserValidator
    {
        public static ValidationResult ValidateUser(string username, string password, string confirmPassword, bool isEditMode, bool userExists, object? selectedPerson)
        {
            var errors = new List<string>();

            // 1. اسم المستخدم
            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add("Username is required.");
            }
            else
            {
                if (username.Contains(" ")) errors.Add("Username cannot contain spaces.");
                if (username.Length < 3 || username.Length > 20) errors.Add("Username must be 3-20 characters.");

                string pattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";
                if (!Regex.IsMatch(username, pattern))
                    errors.Add("Username must start with a letter and contain only letters, numbers, or underscores.");

                var reservedWords = new List<string> { "admin", "root", "system", "null" };
                if (reservedWords.Contains(username.ToLower())) errors.Add("This username is reserved.");
            }

            // 2. كلمة المرور (مطلوبة فقط عند الإضافة)
            if (!isEditMode && string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Please enter a password for the new user account.");
            }
            else if (password != confirmPassword)
            {
                errors.Add("The entered passwords do not match.");
            }

            // 3. التحقق من وجود المستخدم (في حالة التعديل)
            if (isEditMode && !userExists)
            {
                errors.Add("The user you are trying to edit no longer exists.");
            }

            if (selectedPerson == null)
            {
                errors.Add("You must search for and select a person first.");
            }

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }


        public static ValidationResult ValidateUsernameFormat(string? username)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add("Username is required.");
            }
            else
            {
                if (username.Contains(" ")) errors.Add("No spaces allowed.");
                if (username.Length < 3 || username.Length > 20) errors.Add("3-20 characters.");
            }

            return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
        }


    }
}