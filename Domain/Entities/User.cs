

namespace DVLD.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }
        // Foreign Key
        public int PersonId { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool IsActive { get; set; }
        // Navigation Property
        public Person Person { get; set; } = null!;
    }
}
