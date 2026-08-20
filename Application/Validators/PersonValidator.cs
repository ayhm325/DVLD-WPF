using Application.Common;
using Domain.Entities;
using Domain.Enums;
using System.Text.RegularExpressions;

namespace Application.Validators
{
    public static class PersonValidator
    {
        // =========================================================
        // REGEX
        // =========================================================

        private static readonly Regex NationalNumberRegex =
            new(
                @"^\d{10}$",
                RegexOptions.Compiled);

        private static readonly Regex EmailRegex =
            new(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.Compiled);

        private static readonly Regex PhoneRegex =
            new(
                @"^(077|078|079)\d{7}$",
                RegexOptions.Compiled);


        // =========================================================
        // VALIDATE
        // =========================================================

        public static ValidationResult Validate(Person? person)
        {
            var errors = new List<string>();

            if (person is null)
            {
                errors.Add("Person data is required.");
                return ValidationResult.Failure(errors);
            }


            // =========================================================
            // NATIONAL NUMBER
            // =========================================================

            var nationalNo =
                person.NationalNo?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nationalNo))
            {
                errors.Add(
                    "National number is required.");
            }
            else if (!NationalNumberRegex.IsMatch(nationalNo))
            {
                errors.Add(
                    "National number must be exactly 10 digits.");
            }


            // =========================================================
            // FIRST NAME
            // =========================================================

            var firstName =
                person.FirstName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(firstName))
            {
                errors.Add(
                    "First name is required.");
            }
            else if (firstName.Length > 50)
            {
                errors.Add(
                    "First name cannot exceed 50 characters.");
            }


            // =========================================================
            // SECOND NAME
            // =========================================================

            var secondName =
                person.SecondName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(secondName))
            {
                errors.Add(
                    "Second name is required.");
            }
            else if (secondName.Length > 50)
            {
                errors.Add(
                    "Second name cannot exceed 50 characters.");
            }


            // =========================================================
            // THIRD NAME
            // =========================================================

            var thirdName =
                person.ThirdName?.Trim();

            if (!string.IsNullOrWhiteSpace(thirdName) &&
                thirdName.Length > 50)
            {
                errors.Add(
                    "Third name cannot exceed 50 characters.");
            }


            // =========================================================
            // LAST NAME
            // =========================================================

            var lastName =
                person.LastName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(lastName))
            {
                errors.Add(
                    "Last name is required.");
            }
            else if (lastName.Length > 50)
            {
                errors.Add(
                    "Last name cannot exceed 50 characters.");
            }


            // =========================================================
            // EMAIL
            // =========================================================

            var email =
                person.Email?.Trim();

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (email.Length > 100)
                {
                    errors.Add(
                        "Email cannot exceed 100 characters.");
                }
                else if (!EmailRegex.IsMatch(email))
                {
                    errors.Add(
                        "Invalid email format.");
                }
            }


            // =========================================================
            // PHONE
            // =========================================================

            var phone =
                person.Phone?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(phone))
            {
                errors.Add(
                    "Phone number is required.");
            }
            else if (!PhoneRegex.IsMatch(phone))
            {
                errors.Add(
                    "Phone number must start with 077, 078, or 079 and contain exactly 10 digits.");
            }


            // =========================================================
            // DATE OF BIRTH
            // =========================================================

            if (person.DateOfBirth == default)
            {
                errors.Add(
                    "Date of birth is required.");
            }
            else
            {
                var today = DateTime.Today;

                var minimumDate =
                    today.AddYears(-120);

                var maximumDate =
                    today.AddYears(-18);

                if (person.DateOfBirth > maximumDate)
                {
                    errors.Add(
                        "The person must be at least 18 years old.");
                }

                if (person.DateOfBirth < minimumDate)
                {
                    errors.Add(
                        "Date of birth is not realistic.");
                }
            }


            // =========================================================
            // ADDRESS
            // =========================================================

            var address =
                person.Address?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(address))
            {
                errors.Add(
                    "Address is required.");
            }
            else if (address.Length > 200)
            {
                errors.Add(
                    "Address cannot exceed 200 characters.");
            }


            // =========================================================
            // GENDER
            // =========================================================

            if (!Enum.IsDefined(
                typeof(Gender),
                person.Gender))
            {
                errors.Add(
                    "Invalid gender value.");
            }


            // =========================================================
            // NATIONALITY COUNTRY
            // =========================================================

            if (person.NationalityCountryID <= 0)
            {
                errors.Add(
                    "Nationality country is required.");
            }


            // =========================================================
            // RESULT
            // =========================================================

            return errors.Count > 0
                ? ValidationResult.Failure(errors)
                : ValidationResult.Success();
        }
    }
}