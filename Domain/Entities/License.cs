

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class License
    {
        [Key]
        public int LicenseID { get; set; }
        public int ApplicationID { get; set; }
        public int DriverID { get; set; }
        public int LicenseClassId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Notes { get; set; }
        public decimal PaidFees { get; set; }
        public byte IsActive { get; set; }
        public byte IssueReason { get; set; }
        public int CreatedByUserID { get; set; }

        [ForeignKey(nameof(ApplicationID))]
        public virtual ApplicationD? Application { get; set; }

        [ForeignKey(nameof(DriverID))]
        public virtual Person? Driver { get; set; }

        [ForeignKey(nameof(LicenseID))]
        public virtual LicenseClass? LicenseClass { get; set; }

        [ForeignKey(nameof(CreatedByUserID))]
        public virtual User? CreatedByUser { get; set; }

    }
}
