namespace DVLD.Contracts.TestAppointment;

public sealed class AppointmentAvailabilityResponse
{
    public int LocalAppId { get; init; }

    public int TestTypeId { get; init; }

    public List<AppointmentDayAvailabilityResponse> AvailableDays { get; init; } = [];
}

public sealed class AppointmentDayAvailabilityResponse
{
    public string Date { get; init; } = string.Empty;

    public List<AppointmentSlotResponse> Slots { get; init; } = [];
}

public sealed class AppointmentSlotResponse
{
    public string StartTime { get; init; } = string.Empty;

    public string EndTime { get; init; } = string.Empty;
}
