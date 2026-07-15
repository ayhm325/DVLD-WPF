using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Driver
    {
        [Key]
        public int DriverID { get; set; }
        public int PersonID { get; set; }
        public int CreatedByUserID { get; set; }
        public DateTime CreatedDate { get; set; }

        [ForeignKey(nameof(PersonID))]
        public virtual Person? Person { get; set; }

        [ForeignKey(nameof(CreatedByUserID))]
        public virtual User? CreatedByUser { get; set; }

        public virtual ICollection<License> Licenses { get; set; } = new List<License>();

        public virtual ICollection<InternationalLicense> InternationalLicenses { get; set; } = new List<InternationalLicense>();

    }
}
