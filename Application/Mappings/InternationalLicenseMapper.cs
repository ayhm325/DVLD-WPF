using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappers;

public static class InternationalLicenseMapper
{
    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    public static InternationalDto ToDto(
        InternationalLicense entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

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
                entity.Driver?.PersonID ?? 0,

            FullName =
                entity.Driver?.Person?.FullName
                ?? string.Empty,

            DateOfBirth =
                entity.Driver?.Person?.DateOfBirth
                ?? DateTime.MinValue,

            ImagePath =
                entity.Driver?.Person?.ImagePath
                ?? string.Empty,

            NationalNo =
                entity.Driver?.Person?.NationalNo
                ?? string.Empty,

            Gender =
                entity.Driver?.Person?.Gender.ToString()
                ?? string.Empty,

            Fees =
                entity.Application?.PaidFees
                ?? 0m,

            CreatedByUserName =
                entity.CreatedByUser?.UserName
                ?? string.Empty
        };
    }


    // =========================================================
    // DTO -> ENTITY
    // =========================================================

    public static InternationalLicense ToEntity(
        CreateInternationalLicenseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new InternationalLicense
        {
            ApplicationID =
                dto.ApplicationID,

            DriverID =
                dto.DriverID,

            IssuedUsingLocalLicenseID =
                dto.IssuedUsingLocalLicenseID,

            IssueDate =
                dto.IssueDate,

            ExpirationDate =
                dto.ExpirationDate,

            IsActive =
                dto.IsActive,

            CreatedByUserID =
                dto.CreatedByUserID
        };
    }


    // =========================================================
    // LICENSE DTO -> DRIVER LICENSE INFO DTO
    // =========================================================

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
                license.LicenseClassName
                ?? "Unknown",

            PersonID =
                driver?.PersonID ?? 0,

            FullName =
                driver?.FullName
                ?? string.Empty,

            NationalNo =
                driver?.NationalNo
                ?? string.Empty,

            Gender =
                driver?.Gender == Gender.Male
                    ? "Male"
                    : "Female",

            DateOfBirth =
                driver?.DateOfBirth
                ?? DateTime.MinValue,

            IssueDate =
                license.IssueDate,

            ExpirationDate =
                license.ExpirationDate,

            IsActive =
                license.IsActive,

            Notes =
                license.Notes,

            IssueReason =
                ((IssueReason)license.IssueReason)
                .ToString(),

            ImagePath =
                driver?.ImagePath
                ?? string.Empty
        };
    }
}