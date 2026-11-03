namespace Application.DTOs.TestAppointmentDTO;

public class AppointmentAvailabilityDto
{
    public int LocalAppId { get; set; }

    public int TestTypeId { get; set; }

    public List<AppointmentDayAvailabilityDto> AvailableDays { get; set; } = [];
}

public class AppointmentDayAvailabilityDto
{
    public string Date { get; set; } = string.Empty;

    public List<AppointmentSlotDto> Slots { get; set; } = [];
}

public class AppointmentSlotDto
{
    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;
}
