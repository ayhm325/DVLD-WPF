using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class TestAppointment
    {
        [Key]
        public int TestAppointmentID { get; set; }


        public int TestTypeID { get; set; }

        public int LocalDrivingLicenseApplicationID { get; set; }

        public DateTime AppointmentDate { get; set; }

        public decimal PaidFees { get; set; }

        public int CreatedByUserID { get; set; }

        public bool IsLocked { get; set; }

        public int? RetakeTestApplicationID { get; set; }


        // One-to-One (اختيارية)
        public virtual Test? Test { get; set; }


        // Required Relationships

        [ForeignKey(nameof(TestTypeID))]
        public virtual TestType TestType { get; set; } = null!;


        [ForeignKey(nameof(LocalDrivingLicenseApplicationID))]
        public virtual LocalDrivingLicenseApplication LocalDrivingLicenseApplication { get; set; } = null!;


        [ForeignKey(nameof(CreatedByUserID))]
        public virtual User User { get; set; } = null!;


        // Optional Relationship

        [ForeignKey(nameof(RetakeTestApplicationID))]
        public virtual ApplicationD? RetakeTestApplication { get; set; }

    }
}