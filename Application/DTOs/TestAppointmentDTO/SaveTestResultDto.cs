namespace Application.DTOs.TestAppointmentDTO;

public class SaveTestResultDto
{
    public int TestAppointmentID { get; set; }

    public bool TestResult { get; set; }

    public string? Notes { get; set; }

    public int CreatedByUserID { get; set; }
}