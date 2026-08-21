namespace Application.DTOs.TestDTO;

public class TestDto
{
    public int TestID { get; set; }
    public int TestAppointmentID { get; set; }
    public bool TestResult { get; set; }
    public string? Notes { get; set; }
    public int CreatedByUserID { get; set; }

    // Display
    public string? CreatedByUserName { get; set; }
    public string? TestTypeName { get; set; }
    public DateTime? AppointmentDate { get; set; }

    // UI Helpers
    public string TestResultText => TestResult ? "Passed" : "Failed";
    public bool IsPassed => TestResult;
}