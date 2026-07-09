using Domain.Enums;


namespace Application.DTOs
{
    public class ApplicationBasicInfoDto
    {
        public int ApplicantPersonID { get; set; }

        public int ApplicationID { get; set; }                        

        public AppStatus ApplicationStatus { get; set; }
        public string StatusText => ApplicationStatus switch
        {
            AppStatus.New => "New",
            AppStatus.Cancelled => "Cancelled",
            AppStatus.Completed => "Completed",
            _ => "Unknown"
        };

        public decimal PaidFees { get; set; }

        public string? ApplicationTypeName { get; set; }

        public string? ApplicantFullName { get; set; }

        public DateTime ApplicationDate { get; set; }

        public DateTime LastStatusDate { get; set; }                

        public string? CreatedByUserName { get; set; }

       
    }
}
