using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappers;

public static class LocalDrivingLicenseApplicationMapper
{
    public static LocalDrivingLicenseApplicationListDto ToDto(
        LocalDrivingLicenseApplication entity,
        int passedTestCount,
        bool hasLicense)
    {
        return new LocalDrivingLicenseApplicationListDto
        {
            LocalDrivingLicenseApplicationID =
                entity.LocalDrivingLicenseApplicationID,

            LicenseClassID =
                entity.LicenseClassID,

            LicenseClassName =
                entity.LicenseClass?.ClassName ?? "N/A",

            NationalNo =
                entity.Application?.Person?.NationalNo ?? "N/A",

            Fees =
                entity.LicenseClass?.ClassFees ?? 0,

            FullName =
                $"{entity.Application?.Person?.FirstName} " +
                $"{entity.Application?.Person?.SecondName} " +
                $"{entity.Application?.Person?.ThirdName} " +
                $"{entity.Application?.Person?.LastName}".Trim(),

            ApplicationDate =
                entity.Application?.ApplicationDate
                ?? DateTime.MinValue,

            PassedTest =
                passedTestCount,

            ApplicationStatus =
                entity.Application is not null &&
                Enum.IsDefined(
                    typeof(AppStatus),
                    entity.Application.ApplicationStatus)
                    ? (AppStatus)entity.Application.ApplicationStatus
                    : AppStatus.Cancelled,

            HasLicense =
                hasLicense,

            ApplicantPersonID =
                entity.Application?.Person?.PersonId ?? 0
        };
    }
}