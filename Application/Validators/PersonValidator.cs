using DVLD.Domain.Entities;
using DVLD.Domain.Enums;

namespace Application.Validators
{
    public static class PersonValidator
    {
        public static bool Validate(Person person, out string error)
        {
            error = string.Empty;

            if (person is null)
            {
                error = "Person object cannot be null.";
                return false;
            }

            // National No
            if (string.IsNullOrWhiteSpace(person.NationalNo))
                return Fail("National number is required", out error);

            if (person.NationalNo.Length < 5)
                return Fail("National number is too short", out error);

            // Names
            if (string.IsNullOrWhiteSpace(person.FirstName))
                return Fail("First name is required", out error);

            if (string.IsNullOrWhiteSpace(person.SecondName))
                return Fail("Second name is required", out error);

            if (string.IsNullOrWhiteSpace(person.LastName))
                return Fail("Last name is required", out error);

            // Date of Birth
            if (person.DateOfBirth == default)
                return Fail("Date of birth is required", out error);

            if (person.DateOfBirth > DateTime.Now)
                return Fail("Date of birth cannot be in the future", out error);

            if (person.DateOfBirth < DateTime.Now.AddYears(-120))
                return Fail("Date of birth is not realistic", out error);

            // Gender
            if (!Enum.IsDefined(typeof(Gender), person.Gender))
                return Fail("Invalid gender value", out error);

            // Address
            if (string.IsNullOrWhiteSpace(person.Address))
                return Fail("Address is required", out error);

            // Phone
            if (string.IsNullOrWhiteSpace(person.Phone))
                return Fail("Phone is required", out error);

            if (person.Phone.Length < 7)
                return Fail("Phone number is too short", out error);

            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}