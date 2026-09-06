using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappers;

public static class InternationalLicenseMapper
{
    public static InternationalDto ToDto(
    InternationalLicense entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var person = entity.Driver?.Person;

        return new InternationalDto
        {
            InternationalLicenseID =
                entity.InternationalLicenseID,

            ApplicationID =
                entity.ApplicationID,

            DriverID =
                entity.DriverID,

            IssuedUsingLocalLicenseID =
                entity.IssuedUsingLocalLicenseID,

            IssueDate =
                entity.IssueDate,

            ExpirationDate =
                entity.ExpirationDate,

            IsActive =
                entity.IsActive,

            CreatedByUserID =
                entity.CreatedByUserID,

            PersonID =
                person?.PersonId ?? 0,

            FullName =
                person?.FullName ?? string.Empty,

            DateOfBirth =
                person?.DateOfBirth ?? DateTime.MinValue,

            ImagePath =
                person?.ImagePath ?? string.Empty,

            NationalNo =
                person?.NationalNo ?? string.Empty,

            Gender =
                person?.Gender.ToString() ?? string.Empty,

            Fees =
                entity.Application?.PaidFees ?? 0m,

            CreatedByUserName =
                entity.CreatedByUser?.UserName ?? string.Empty
        };
    }

    public static DriverLicenseInfoDto ToDriverLicenseInfoDto(
        LicenseDto license)
    {
        ArgumentNullException.ThrowIfNull(license);

        var driver = license.Driver;

        return new DriverLicenseInfoDto
        {
            LicenseId =
                license.LicenseID,

            DriverId =
                license.DriverID,

            LicenseClass =
                license.LicenseClassName ?? "Unknown",

            PersonID =
                driver?.PersonID ?? 0,

            FullName =
                driver?.FullName ?? string.Empty,

            NationalNo =
                driver?.NationalNo ?? string.Empty,

            Gender =
                driver?.Gender == Gender.Male
                    ? "Male"
                    : "Female",

            DateOfBirth =
                driver?.DateOfBirth ?? DateTime.MinValue,

            IssueDate =
                license.IssueDate,

            ExpirationDate =
                license.ExpirationDate,

            IsActive =
                license.IsActive,

            Notes =
                license.Notes,

            IssueReason =
                ((IssueReason)license.IssueReason).ToString(),

            ImagePath =
                driver?.ImagePath ?? string.Empty
        };
    }
}
