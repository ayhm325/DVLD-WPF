


using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class TestType
    {
        [Key]
        public int TestTypeId { get; set; }
        public string TestTypeTitle { get; set; } = null!;
        public string TestTypeDescription { get; set; } = null!;
        public decimal TestTypeFees { get; set; }

    }
}
