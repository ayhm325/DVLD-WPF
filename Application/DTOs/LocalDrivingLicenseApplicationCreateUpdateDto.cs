


namespace Application.DTOs
{
    public class LocalDrivingLicenseApplicationCreateUpdateDto
    {
        public int PersonId { get; set; }
        public int LicenseClassId { get; set; }
        public int CreatedByUserId { get; set; }
    }
}