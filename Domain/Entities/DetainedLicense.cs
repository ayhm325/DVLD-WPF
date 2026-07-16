using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class DetainedLicense
    {
        [Key]
        public int DetainID { get; set; }

        public int LicenseID { get; set; }

        public DateTime DetainDate { get; set; }

        public decimal FineFees { get; set; }

        public int CreatedByUserID { get; set; }

        public bool IsReleased { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public int? ReleasedByUserID { get; set; }

        public int? ReleaseApplicationID { get; set; }


        [ForeignKey(nameof(LicenseID))]
        public virtual License License { get; set; } = null!;


        [ForeignKey(nameof(CreatedByUserID))]
        public virtual User CreatedByUser { get; set; } = null!;


        [ForeignKey(nameof(ReleasedByUserID))]
        public virtual User? ReleasedByUser { get; set; }


        [ForeignKey(nameof(ReleaseApplicationID))]
        public virtual ApplicationD? ReleaseApplication { get; set; }
    }
}