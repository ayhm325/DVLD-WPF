using Application.Common.Results;
using Application.DTOs.PersonDTO;
using Domain.Enums;
using System.Text.RegularExpressions;

namespace Application.Validators;

public static class PersonValidator
{
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

    public static Result Validate(PersonCreateDto? person)
    {
        if (person is null)
        {
            return Result.ValidationFailure(
                "Person data is required.");
        }

        return BuildResult(
            ValidateCommon(
                person.NationalNo,
                person.FirstName,
                person.SecondName,
                person.ThirdName,
                person.LastName,
                person.DateOfBirth,
                person.Gender,
                person.Address,
                person.Phone,
                person.Email,
                person.NationalityCountryID));
    }

    public static Result Validate(PersonUpdateDto? person)
    {
        if (person is null)
        {
            return Result.ValidationFailure(
                "Person data is required.");
        }

        return BuildResult(
            ValidateCommon(
                person.NationalNo,
                person.FirstName,
                person.SecondName,
                person.ThirdName,
                person.LastName,
                person.DateOfBirth,
                person.Gender,
                person.Address,
                person.Phone,
                person.Email,
                person.NationalityCountryID));
    }

    private static List<string> ValidateCommon(
        string? nationalNo,
        string? firstName,
        string? secondName,
        string? thirdName,
        string? lastName,
        DateTime dateOfBirth,
        Gender gender,
        string? address,
        string? phone,
        string? email,
        int nationalityCountryId)
    {
        var errors = new List<string>();

        nationalNo = nationalNo?.Trim();

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

        firstName = firstName?.Trim();

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

        secondName = secondName?.Trim();

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

        thirdName = thirdName?.Trim();

        if (!string.IsNullOrWhiteSpace(thirdName) &&
            thirdName.Length > 50)
        {
            errors.Add(
                "Third name cannot exceed 50 characters.");
        }

        lastName = lastName?.Trim();

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

        email = email?.Trim();

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

        phone = phone?.Trim();

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

        if (dateOfBirth == default)
        {
            errors.Add(
                "Date of birth is required.");
        }
        else
        {
            var today = DateTime.Today;

            if (dateOfBirth > today)
            {
                errors.Add(
                    "Date of birth cannot be in the future.");
            }
            else if (dateOfBirth > today.AddYears(-18))
            {
                errors.Add(
                    "The person must be at least 18 years old.");
            }

            if (dateOfBirth < today.AddYears(-120))
            {
                errors.Add(
                    "Date of birth is not realistic.");
            }
        }

        address = address?.Trim();

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

        if (!Enum.IsDefined(
                typeof(Gender),
                gender))
        {
            errors.Add(
                "Invalid gender value.");
        }

        if (nationalityCountryId <= 0)
        {
            errors.Add(
                "Nationality country is required.");
        }

        return errors;
    }

    private static Result BuildResult(
        IReadOnlyCollection<string> errors)
    {
        return errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(
                string.Join(
                    Environment.NewLine,
                    errors));
    }
}