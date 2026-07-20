using Domain.Enums;

namespace Domain.Entities
{
    public class Person
    {
        public int PersonId { get; set; }


        public string NationalNo { get; set; } = null!;


        public string FirstName { get; set; } = null!;


        public string SecondName { get; set; } = null!;


        public string? ThirdName { get; set; }


        public string LastName { get; set; } = null!;


        public DateTime DateOfBirth { get; set; }


        public Gender Gender { get; set; }


        public string Address { get; set; } = null!;


        public string Phone { get; set; } = null!;


        public string? Email { get; set; }


        public int NationalityCountryID { get; set; }


        public string? ImagePath { get; set; }



        // Navigation Properties

        public virtual Country? Country { get; set; }


        public virtual ICollection<ApplicationD> Applications { get; set; }
            = new List<ApplicationD>();



        // Computed Property

        public string FullName =>
            string.Join(" ",
                new[]
                {
                    FirstName,
                    SecondName,
                    ThirdName,
                    LastName
                }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}