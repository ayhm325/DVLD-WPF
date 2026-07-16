

using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public  class Country
    {
        [Key]
        public int CountryId { get; set; }
        public string CountryName { get; set; } = null!;
        // Navigation Property (One Country -> Many People)
        public virtual List<Person>? People { get; set; }
    }
}
