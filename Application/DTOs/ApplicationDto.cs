

using Domain.Enums;

namespace Application.DTOs
{
    public class ApplicationDto
    {
        public int ApplicationID { get; set; }

        public int ApplicantPersonID { get; set; }

        public DateTime ApplicationDate { get; set; }

        public int ApplicationTypeID { get; set; }

        public AppStatus ApplicationStatus { get; set; }

        public DateTime LastStatusDate { get; set; }

        public decimal PaidFees { get; set; } 

        public int CreatedByUserID { get; set; }

        public string StatusText => ApplicationStatus switch
        {
            AppStatus.New       => "New",
            AppStatus.Cancelled => "Cancelled",
            AppStatus.Completed => "Completed",
            _                   => "Unknown" 
        };
    }
}
