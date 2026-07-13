

using Domain.Enums;

namespace Application.DTOs
{
    public class LocalDrivingLicenseApplicationListDto
    {
        public int LocalDrivingLicenseApplicationID { get; set; }

        public int LicenseClassID { get; set; }

        public string? LicenseClassName { get; set; }       

        public string? NationalNo { get; set; }

        public string? FullName { get; set; }

        public DateTime ApplicationDate { get; set; }

        public int PassedTest { get; set; }

        public AppStatus ApplicationStatus { get; set; }

        public string StatusText => ApplicationStatus.ToString();

        public decimal Fees { get; set; }

        public bool HasLicense { get; set; }

        public int ApplicantPersonID { get; set; }


    }
}
