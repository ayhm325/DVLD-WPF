

namespace DVLD.Domain.Entities
{
    public  class Country
    {

        public int CountryId { get; set; }
        public string CountryName { get; set; } = null!;
        // Navigation Property (One Country -> Many People)
        public List<Person>? People { get; set; }

       
    }
}
