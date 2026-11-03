namespace Domain.Entities;

using Domain.Enums;

public class TestAppointment
{
    public int TestAppointmentID { get; set; }

    public int TestTypeID { get; set; }

    public int LocalDrivingLicenseApplicationID { get; set; }

    public DateTime AppointmentDate { get; set; }

    public decimal PaidFees { get; set; }

    public int CreatedByUserID { get; set; }

    public bool IsLocked { get; set; }

    public int? RetakeTestApplicationID { get; set; }

    public AppointmentStatus? Status { get; set; } = AppointmentStatus.Scheduled;

    public DateTime? CancelledAt { get; set; }

    public int? CancelledByUserID { get; set; }

    public DateTime? NoShowDate { get; set; }

    public virtual Test? Test { get; set; }

    public virtual TestType TestType { get; set; } = null!;

    public virtual LocalDrivingLicenseApplication
        LocalDrivingLicenseApplication
    { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual User? CancelledByUser { get; set; }

    public virtual ApplicationD? RetakeTestApplication { get; set; }
}
