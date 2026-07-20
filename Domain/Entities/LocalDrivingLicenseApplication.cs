namespace Domain.Entities
{
    public class LocalDrivingLicenseApplication
    {
        public int LocalDrivingLicenseApplicationID { get; set; }


        public int ApplicationID { get; set; }


        public int LicenseClassID { get; set; }



        // Navigation Properties

        public virtual ApplicationD Application { get; set; } = null!;


        public virtual LicenseClass LicenseClass { get; set; } = null!;
    }
}