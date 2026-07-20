namespace Domain.Entities
{
    public class TestAppointment
    {
        public int TestAppointmentID { get; set; }


        public int TestTypeID { get; set; }


        public int LocalDrivingLicenseApplicationID { get; set; }


        public DateTime AppointmentDate { get; set; }


        public decimal PaidFees { get; set; }


        public int CreatedByUserID { get; set; }


        public bool IsLocked { get; set; }


        public int? RetakeTestApplicationID { get; set; }



        // Optional One-To-One

        public virtual Test? Test { get; set; }



        // Navigation Properties

        public virtual TestType TestType { get; set; } = null!;


        public virtual LocalDrivingLicenseApplication LocalDrivingLicenseApplication { get; set; } = null!;


        public virtual User User { get; set; } = null!;


        public virtual ApplicationD? RetakeTestApplication { get; set; }
    }
}