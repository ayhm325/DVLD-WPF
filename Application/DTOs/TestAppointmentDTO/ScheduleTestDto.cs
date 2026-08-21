namespace Application.DTOs.TestAppointmentDTO;

public class ScheduleTestDto
{
    public int AppointmentID { get; set; }

    public int? RetakeTestApplicationID { get; set; }

    public int LocalDrivingLicenseApplicationID { get; set; }

    public string? LicenseClassName { get; set; }

    public string? FullName { get; set; }

    public int Trial { get; set; }

    public DateTime Date { get; set; }

    public decimal Fees { get; set; }

    public int TestTypeID { get; set; }

    public decimal RetakerFees { get; set; }

    public int TestID { get; set; }

    public bool Result { get; set; }

    public string? Notes { get; set; }
}