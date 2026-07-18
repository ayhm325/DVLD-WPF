namespace Application.DTOs
{
    public class DetainedLicenseDto
    {
        public int DetainID { get; set; }

        public int LicenseID { get; set; }

        public int PersonID { get; set; }

        public DateTime DetainDate { get; set; }

        public decimal FineFees { get; set; }        

        public int CreatedByUserID { get; set; }

        public string CreatedByUserName { get; set; } = string.Empty;

        public bool IsReleased { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public int? ReleasedByUserID { get; set; }

        public int? ReleaseApplicationID { get; set; }

        public int ApplicantPersonID { get; set; }

        public string NationalNo { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string IsReleasedText => IsReleased ? "Yes" : "No";
    }
}