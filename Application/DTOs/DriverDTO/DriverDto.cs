using Domain.Enums;

namespace Application.DTOs.DriverDTO;

public class DriverDto
{
    public int DriverID { get; set; }

    public int PersonID { get; set; }

    // معلومات الشخص
    public string FullName { get; set; } = string.Empty;

    public string NationalNo { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string? ImagePath { get; set; }

    public int ActiveLicenses { get; set; }

    // معلومات الإنشاء
    public int CreatedByUserID { get; set; }

    public string CreatedByUserName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public string CreatedDateFormatted =>
        CreatedDate.ToString("yyyy-MM-dd");
}