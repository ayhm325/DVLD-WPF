

using Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class ApplicationD
    {
        [Key]
        public int ApplicationID { get; set; }

        public int ApplicantPersonID { get; set; }

        public DateTime ApplicationDate { get; set; }

        public int ApplicationTypeID { get; set; }

        public byte ApplicationStatus { get; set; } 

        public DateTime LastStatusDate { get; set; }

        public decimal PaidFees { get; set; } 

        public int CreatedByUserID { get; set; }

        [ForeignKey("ApplicantPersonID")]
        public virtual Person? Person { get; set; }

        [ForeignKey("ApplicationTypeID")]
        public virtual ApplicationType? ApplicationType { get; set; }

        [ForeignKey("CreatedByUserID")]
        public virtual User? CreatedByUser { get; set; }

        public ICollection<License> Licenses { get; set; } = new List<License>();
    }
}
