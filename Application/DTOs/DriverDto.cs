using Domain.Enums;
using System;

namespace Application.DTOs
{
    public class DriverDto
    {
        public int DriverID { get; set; }

        public int PersonID { get; set; }


        // معلومات الشخص للعرض
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


        // خصائص مساعدة للواجهة
        public string CreatedDateFormatted =>
            CreatedDate.ToString("yyyy-MM-dd");
    }
}