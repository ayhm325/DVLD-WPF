using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class InternationalLicense
    {
        [Key]
        public int InternationalLicenseID { get; set; }

        [Required]
        public int ApplicationID { get; set; }

        [Required]
        public int DriverID { get; set; }

        [Required]
        public int IssuedUsingLocalLicenseID { get; set; }

        [Required]
        public DateTime IssueDate { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [Required]
        public int CreatedByUserID { get; set; }

        // Navigation Properties

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