





using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Person
    {
        [Key]
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
        // Foreign Key
        public int NationalityCountryID { get; set; }
        public string? ImagePath { get; set; }

        // Navigation Property
        [ForeignKey("NationalityCountryID")]
        public virtual Country? Country { get; set; }

        // Navigation Property (One Person -> Many Applications)
        public virtual ICollection<ApplicationD> Applications { get; set; } = new List<ApplicationD>();

        public string FullName =>
                    string.Join(" ",
                     new[] { FirstName, SecondName, ThirdName, LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
