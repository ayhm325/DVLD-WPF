

using Domain.Enums;

namespace Application.DTOs
{
    public class PersonDto
    {
        public int PersonId { get; set; }
        public string NationalNo { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string Address { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string? Email { get; set; }
        public string CountryName { get; set; } = null!;       
        public int NationalityCountryID { get; set; }
        public string? ImagePath { get; set; }
    }
}