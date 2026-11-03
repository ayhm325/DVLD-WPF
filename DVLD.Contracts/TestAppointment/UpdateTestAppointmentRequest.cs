namespace DVLD.Contracts.TestAppointment;

public sealed class UpdateTestAppointmentRequest
{
    public int TestAppointmentId { get; init; }
    public DateTimeOffset AppointmentDate { get; init; }
}
