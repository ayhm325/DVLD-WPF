using Application.DTOs.DetainedLicenseDTO;
using Domain.Entities;

namespace Application.Mappers;

public static class DetainedLicenseMapper
{
    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    public static DetainedLicenseDto ToDto(
        DetainedLicense entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var person =
            entity.License?
                .Driver?
                .Person;

        return new DetainedLicenseDto
        {
            DetainID =
                entity.DetainID,

            LicenseID =
                entity.LicenseID,

            PersonID =
                person?.PersonId ?? 0,

            ApplicantPersonID =
                person?.PersonId ?? 0,

            DetainDate =
                entity.DetainDate,

            FineFees =
                entity.FineFees,

            CreatedByUserID =
                entity.CreatedByUserID,

            CreatedByUserName =
                entity.CreatedByUser?.UserName
                ?? string.Empty,

            IsReleased =
                entity.IsReleased,

            ReleaseDate =
                entity.ReleaseDate,

            ReleasedByUserID =
                entity.ReleasedByUserID,

            ReleaseApplicationID =
                entity.ReleaseApplicationID,

            NationalNo =
                person?.NationalNo
                ?? string.Empty,

            FullName =
                person?.FullName
                ?? string.Empty
        };
    }


    // =========================================================
    // CREATE DTO -> ENTITY
    // =========================================================

    public static DetainedLicense
        ToEntity(
            CreateDetainedLicenseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new DetainedLicense
        {
            LicenseID =
                dto.LicenseID,

            DetainDate =
                dto.DetainDate,

            FineFees =
                dto.FineFees,

            CreatedByUserID =
                dto.CreatedByUserID,

            IsReleased =
                false,

            ReleaseDate =
                null,

            ReleasedByUserID =
                null,

            ReleaseApplicationID =
                null
        };
    }
}