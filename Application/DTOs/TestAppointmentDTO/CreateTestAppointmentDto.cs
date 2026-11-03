using Application.Validators;

namespace Application.DTOs.TestAppointmentDTO;

public class CreateTestAppointmentDto
{
    private DateTime _appointmentDate;

    public int TestTypeID { get; set; }

    public int LocalDrivingLicenseApplicationID { get; set; }

    public DateTime AppointmentDate
    {
        get => _appointmentDate;
        set => _appointmentDate = TestAppointmentValidator.ToFacilityTime(value);
    }

    public int? RetakeTestApplicationID { get; set; }
}
