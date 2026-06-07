

namespace Application.DTOs
{
    public class LocalDrivingLicenseApplicationDto
    {
        public int ApplicationID { get; set; }
        public string?  DrivingClass { get; set; }
        public string? NationalNo { get; set; }
        public string? FullName { get; set; }
        public DateTime ApplicationDate { get; set; }
        public int PassedTest { get; set; }
        public byte ApplicationStatus { get; set; }

        public string StatusText => ApplicationStatus switch
        {
            1 => "New",
            2 => "Cancelled",
            3 => "Completed",
            _ => "Unknown"
        };

    }
}
