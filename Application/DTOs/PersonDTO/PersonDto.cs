

namespace Application.DTOs.PersonDTO;

public class PersonDto
{
    public int PersonId { get; set; }

    public string NationalNo { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string SecondName { get; set; } = null!;

    public string? ThirdName { get; set; }

    public string LastName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public DateTime DateOfBirth { get; set; }

    public int Gender { get; set; }

    public string Address { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public int NationalityCountryID { get; set; }

    public string? CountryName { get; set; }

    public string? ImagePath { get; set; }
}