namespace Application.DTOs.TestAppointmentDTO;

public class CreateTestAppointmentDto
{
    public int TestTypeID { get; set; }

    public int LocalDrivingLicenseApplicationID { get; set; }

    public DateTime AppointmentDate { get; set; }

    public int? RetakeTestApplicationID { get; set; }
}