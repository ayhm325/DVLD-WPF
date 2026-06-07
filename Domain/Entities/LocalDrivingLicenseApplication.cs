
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class LocalDrivingLicenseApplication
    {
        [Key]
        public int LocalDrivingLicenseApplicationID { get; set; }
        public int ApplicationID { get; set; }
        public int LicenseClassID { get; set; }

        [ForeignKey(nameof(ApplicationID))]
        public virtual ApplicationD Application { get; set; } = null!;

        [ForeignKey(nameof(LicenseClassID))]
        public virtual LicenseClass LicenseClass { get; set; } = null!;

    }
}
