using Application.DTOs.LicenseDTO;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappers;

public static class LicenseMapper
{
    public static LicenseDto ToDto(License license)
    {
        ArgumentNullException.ThrowIfNull(license);

        var person = license.Driver?.Person;

        return new LicenseDto
        {
            LicenseID = license.LicenseID,
            ApplicationID = license.ApplicationID,
            DriverID = license.DriverID,
            DriverName = person?.FullName,

            Driver = license.Driver is null
                ? null
                : DriverMapper.ToDto(license.Driver),

            LicenseClassID = license.LicenseClass,
            LicenseClassName =
                license.LicenseClassInfo?.ClassName,

            IssueDate = license.IssueDate,
            ExpirationDate = license.ExpirationDate,
            Notes = license.Notes,
            PaidFees = license.PaidFees,
            IsActive = license.IsActive,

            IssueReason = (byte)license.IssueReason,

            IssueReasonText =
                Enum.IsDefined(license.IssueReason)
                    ? license.IssueReason.ToString()
                    : "Unknown",

            CreatedByUserID = license.CreatedByUserID,
            CreatedByUserName =
                license.CreatedByUser?.UserName
        };
    }

    public static License ToEntity(
        CreateLicenseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new License
        {
            ApplicationID = dto.ApplicationID,
            DriverID = dto.DriverID,
            LicenseClass = dto.LicenseClassID,
            IssueDate = dto.IssueDate,
            ExpirationDate = dto.ExpirationDate,

            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim(),

            PaidFees = dto.PaidFees,
            IsActive = true,
            IssueReason = dto.IssueReason
        };
    }
}