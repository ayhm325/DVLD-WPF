namespace Domain.Entities
{
    public class ApplicationD
    {
        public int ApplicationID { get; set; }

        public int ApplicantPersonID { get; set; }

        public DateTime ApplicationDate { get; set; }

        public int ApplicationTypeID { get; set; }

        public byte ApplicationStatus { get; set; }

        public DateTime LastStatusDate { get; set; }

        public decimal PaidFees { get; set; }

        public int CreatedByUserID { get; set; }


        // Navigation Properties

        public virtual Person Person { get; set; } = null!;


        public virtual ApplicationType? ApplicationType { get; set; }


        public virtual User CreatedByUser { get; set; } = null!;


        public virtual ICollection<License> Licenses { get; set; }
            = new List<License>();
    }
}