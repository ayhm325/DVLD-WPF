using Application.Validators;

namespace Application.DTOs.TestAppointmentDTO;

public class UpdateTestAppointmentDto
{
    private DateTime _appointmentDate;

    public int TestAppointmentID { get; set; }

    public DateTime AppointmentDate
    {
        get => _appointmentDate;
        set => _appointmentDate = TestAppointmentValidator.ToFacilityTime(value);
    }
}
