namespace Domain.Entities
{
    public class Test
    {
        public int TestID { get; set; }


        public int TestAppointmentID { get; set; }


        public bool TestResult { get; set; }


        public string? Notes { get; set; }


        public int CreatedByUserID { get; set; }



        // Navigation Properties

        public virtual User User { get; set; } = null!;


        public virtual TestAppointment TestAppointment { get; set; } = null!;
    }
}