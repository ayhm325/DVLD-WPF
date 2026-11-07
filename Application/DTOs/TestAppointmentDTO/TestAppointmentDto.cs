using Domain.Enums;

namespace Application.DTOs.TestAppointmentDTO;

public class TestAppointmentDto
{
    public int TestAppointmentID { get; set; }

    public int TestTypeID { get; set; }

    public TestResultType TestResult { get; set; }

    public string CreatedByUserName { get; set; } = string.Empty;

    public string TestTypeName { get; set; } = string.Empty;

    public int LocalDrivingLicenseApplicationID { get; set; }

    public string ApplicantFullName { get; set; } = string.Empty;

    public string NationalNo { get; set; } = string.Empty;

    public string LicenseClassName { get; set; } = string.Empty;

    public DateTime AppointmentDate { get; set; }

    public decimal PaidFees { get; set; }

    public int CreatedByUserID { get; set; }

    public bool IsLocked { get; set; }

    public int? RetakeTestApplicationID { get; set; }

    public AppointmentStatus? LifecycleStatus { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int? CancelledByUserID { get; set; }

    public DateTime? NoShowDate { get; set; }

    // =========================================================
    // UI / Display Helpers
    // =========================================================

    public string AppointmentDateFormatted =>
        AppointmentDate.ToString("yyyy-MM-dd HH:mm");

    public string TestResultText =>
        TestResult.ToString();

    public string Status =>
        LifecycleStatus switch
        {
            AppointmentStatus.Cancelled => "Cancelled",
            AppointmentStatus.Completed => "Completed",
            AppointmentStatus.NoShow => "NoShow",
            _ => IsLocked ? "Locked" : "Open"
        };
}
