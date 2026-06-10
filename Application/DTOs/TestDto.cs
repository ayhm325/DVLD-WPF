namespace Application.DTOs
{
    public class TestDto
    {
        public int TestID { get; set; }

        public int TestAppointmentID { get; set; }

        public byte TestResult { get; set; }
        public string TestResultText =>
            TestResult == 1 ? "Passed" : "Failed";

        public string? Notes { get; set; }

        public int CreatedByUserID { get; set; }
        public string? CreatedByUserName { get; set; }

        // =========================
        // UI HELPERS
        // =========================
        public string? TestTypeName { get; set; }
        public DateTime? AppointmentDate { get; set; }

        public bool IsPassed => TestResult == 1;
    }
}