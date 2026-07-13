



namespace Application.DTOs
{
    public class LicenseClassDto
    {
        public int LicenseClassID { get; set; }

        public string LicenseClassName { get; set; } = null!;

        public string LicenseClassDescription { get; set; } = null!;

        public byte MinAllowedAge { get; set; }

        public byte DefaultValidityLength { get; set; }

        public decimal LicenseClassFees { get; set; }

    }
}