

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        // Foreign Key
        public int PersonId { get; set; }
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool IsActive { get; set; }


        // Navigation Property
        [ForeignKey("PersonId")]
        public virtual Person Person { get; set; } = null!;
    }
}
