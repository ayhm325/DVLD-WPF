using System;

namespace Application.DTOs
{
    public class TestAppointmentDto
    {
        public int TestAppointmentID { get; set; }

        public int TestTypeID { get; set; }
        public string TestTypeName { get; set; } = string.Empty;

        public int LocalDrivingLicenseApplicationID { get; set; }

        public DateTime AppointmentDate { get; set; }

        public decimal PaidFees { get; set; }

        public int CreatedByUserID { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;

        public bool IsLocked { get; set; }

        public int? RetakeTestApplicationID { get; set; }

        // =========================
        // READ-ONLY UI PROPERTY
        // =========================
        public string Status => IsLocked ? "Locked" : "Open";

        public string AppointmentDateFormatted =>
            AppointmentDate.ToString("yyyy-MM-dd HH:mm");
    }
}