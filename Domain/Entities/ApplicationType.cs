



using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class ApplicationType
    {
        [Key]
        public int ApplicationTypeId { get; set; }
        public string ApplicationTypeTitle { get; set; } = null!;
        public decimal ApplicationFees { get; set; }
    }
}
