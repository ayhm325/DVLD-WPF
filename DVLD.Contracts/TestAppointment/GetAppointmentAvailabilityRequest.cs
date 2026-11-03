namespace DVLD.Contracts.TestAppointment;

public sealed class GetAppointmentAvailabilityRequest
{
    public int LocalAppId { get; init; }

    public int TestTypeId { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }
}
