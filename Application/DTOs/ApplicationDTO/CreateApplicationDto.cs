using Domain.Enums;

namespace Application.DTOs.ApplicationDTO;

public class CreateApplicationDto
{
    public int ApplicantPersonID { get; set; }

    public DateTime ApplicationDate { get; set; }

    public int ApplicationTypeID { get; set; }

    public AppStatus ApplicationStatus { get; set; }

    public DateTime LastStatusDate { get; set; }

    public decimal PaidFees { get; set; }

    public int CreatedByUserID { get; set; }
}