namespace Domain.Entities
{
    public class Driver
    {
        public int DriverID { get; set; }


        public int PersonID { get; set; }


        public int CreatedByUserID { get; set; }


        public DateTime CreatedDate { get; set; }



        // Navigation Properties

        public virtual Person Person { get; set; } = null!;


        public virtual User CreatedByUser { get; set; } = null!;


        public virtual ICollection<License> Licenses { get; set; }
            = new List<License>();


        public virtual ICollection<InternationalLicense> InternationalLicenses { get; set; }
            = new List<InternationalLicense>();
    }
}