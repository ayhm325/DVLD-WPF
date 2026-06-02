using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using Application.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Application.Validators
{
    public static class PersonValidator
    {
        public static ValidationResult Validate(Person person)
        {
            var errors = new List<string>();

            if (person == null)
            {
                errors.Add("Person cannot be null.");
                return ValidationResult.Failure(errors);
            }

            // ================= NATIONAL NO =================
            if (string.IsNullOrWhiteSpace(person.NationalNo))
                errors.Add("National number is required.");
            else if (!Regex.IsMatch(person.NationalNo, @"^\d{10}$"))
                errors.Add("National number must be exactly 10 digits.");

            // ================= FIRST NAME =================
            if (string.IsNullOrWhiteSpace(person.FirstName))
                errors.Add("First name is required.");

            // ================= SECOND NAME =================
            if (string.IsNullOrWhiteSpace(person.SecondName))
                errors.Add("Second name is required.");

            // ================= LAST NAME =================
            if (string.IsNullOrWhiteSpace(person.LastName))
                errors.Add("Last name is required.");

            // ================= EMAIL =================
            if (!string.IsNullOrWhiteSpace(person.Email))
            {
                if (!Regex.IsMatch(person.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    errors.Add("Invalid email format.");
            }

            // ================= PHONE =================
            if (string.IsNullOrWhiteSpace(person.Phone))
                errors.Add("Phone is required.");
            else if (!Regex.IsMatch(person.Phone, @"^(077|078|079)\d{7}$"))
                errors.Add("Phone must start with 077 / 078 / 079 and be 10 digits.");

            // ================= DATE OF BIRTH =================
            if (person.DateOfBirth == default)
                errors.Add("Date of birth is required.");
            else
            {
                // حساب التاريخ الأقصى المسموح به (اليوم ناقص 18 سنة)
                DateTime maxAllowedDate = DateTime.Now.AddYears(-18);

                if (person.DateOfBirth > maxAllowedDate)
                    errors.Add("The person must be at least 18 years old.");

                if (person.DateOfBirth < DateTime.Now.AddYears(-120))
                    errors.Add("Date of birth is not realistic.");
            }

            // ================= ADDRESS =================
            if (string.IsNullOrWhiteSpace(person.Address))
                errors.Add("Address is required.");

            // ================= GENDER =================
            if (!Enum.IsDefined(typeof(Gender), person.Gender))
                errors.Add("Invalid gender value.");

            if (errors.Count > 0)
                return ValidationResult.Failure(errors);

            return ValidationResult.Success();
        }
    }
}