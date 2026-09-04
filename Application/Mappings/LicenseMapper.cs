using Application.DTOs;
using Application.DTOs.DriverDTO;
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
            ApplicationInfo = license.Application is not null
                ? $"App #{license.ApplicationID}"
                : null,

            DriverID = license.DriverID,
            DriverName = person?.FullName,

            Driver = license.Driver is null
                ? null
                : new DriverDto
                {
                    DriverID = license.Driver.DriverID,
                    PersonID = license.Driver.PersonID,
                    FullName = person?.FullName ?? string.Empty,
                    NationalNo = person?.NationalNo ?? string.Empty,
                    DateOfBirth = person?.DateOfBirth ?? DateTime.MinValue,
                    Gender = person?.Gender ?? Gender.Male,
                    ImagePath = person?.ImagePath,
                    ActiveLicenses = license.Driver.Licenses?
                        .Count(l => l.IsActive) ?? 0,
                    CreatedByUserID = license.Driver.CreatedByUserID,
                    CreatedByUserName =
                        license.Driver.CreatedByUser?.UserName ?? string.Empty,
                    CreatedDate = license.Driver.CreatedDate
                },

            LicenseClassID = license.LicenseClass,
            LicenseClassName = license.LicenseClassInfo?.ClassName,
            IssueDate = license.IssueDate,
            ExpirationDate = license.ExpirationDate,
            Notes = license.Notes,
            PaidFees = license.PaidFees,
            IsActive = license.IsActive,
            IssueReason = license.IssueReason,
            IssueReasonText =
                ((IssueReason)license.IssueReason).ToString(),
            CreatedByUserID = license.CreatedByUserID,
            CreatedByUserName =
                license.CreatedByUser?.UserName ?? "Unknown"
        };
    }

    public static License ToEntity(CreateLicenseDto dto)
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
            IsActive = dto.IsActive,
            IssueReason = dto.IssueReason
        };
    }
}