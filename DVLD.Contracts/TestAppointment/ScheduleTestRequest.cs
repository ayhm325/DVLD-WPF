namespace DVLD.Contracts.TestAppointment;

public sealed class ScheduleTestRequest
{
    public int TestTypeId { get; init; }

    public int LocalDrivingLicenseApplicationId { get; init; }

    public DateTime AppointmentDate { get; init; }
}