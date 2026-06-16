using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Net.Mime.MediaTypeNames;

namespace Domain.Entities
{
    public class Test
    {
        [Key]
        public int TestID { get; set; }
        public int TestAppointmentID { get; set; }
        public bool TestResult { get; set; }
        public string? Notes { get; set; }
        public int CreatedByUserID { get; set; }

        [ForeignKey(nameof(CreatedByUserID))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(TestAppointmentID))]
        public virtual TestAppointment? TestAppointment { get; set; }


    }
}
