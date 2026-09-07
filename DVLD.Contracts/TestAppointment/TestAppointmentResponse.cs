namespace DVLD.Contracts.TestAppointment;

public sealed class TestAppointmentResponse
{
    public int TestAppointmentId { get; init; }
    public int TestTypeId { get; init; }
    public TestResult TestResult { get; init; }
    public string CreatedByUserName { get; init; } = string.Empty;
    public string TestTypeName { get; init; } = string.Empty;
    public int LocalDrivingLicenseApplicationId { get; init; }
    public DateTime AppointmentDate { get; init; }
    public decimal PaidFees { get; init; }
    public int CreatedByUserId { get; init; }
    public bool IsLocked { get; init; }
    public int? RetakeTestApplicationId { get; init; }
    public string TestResultText { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string AppointmentDateFormatted { get; init; } = string.Empty;
}
