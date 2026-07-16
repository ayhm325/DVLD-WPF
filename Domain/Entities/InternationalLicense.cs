using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class InternationalLicense
    {
        [Key]
        public int InternationalLicenseID { get; set; }

        public int ApplicationID { get; set; }

        public int DriverID { get; set; }

        public int IssuedUsingLocalLicenseID { get; set; }

        public DateTime IssueDate { get; set; }

        public DateTime ExpirationDate { get; set; }

        public bool IsActive { get; set; }

        public int CreatedByUserID { get; set; }


        [ForeignKey(nameof(ApplicationID))]
        public virtual ApplicationD Application { get; set; } = null!;


        [ForeignKey(nameof(DriverID))]
        public virtual Driver Driver { get; set; } = null!;


        [ForeignKey(nameof(IssuedUsingLocalLicenseID))]
        public virtual License IssuedUsingLocalLicense { get; set; } = null!;


        [ForeignKey(nameof(CreatedByUserID))]
        public virtual User CreatedByUser { get; set; } = null!;
    }
}