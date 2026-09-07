namespace DVLD.Contracts.TestAppointment;

public sealed class CreateTestAppointmentRequest
{
    public int TestTypeId { get; init; }
    public int LocalDrivingLicenseApplicationId { get; init; }
    public DateTime AppointmentDate { get; init; }
    public int? RetakeTestApplicationId { get; init; }
}
