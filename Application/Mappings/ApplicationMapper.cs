using Application.DTOs.ApplicationDTO;
using Domain.Entities;

namespace Application.Mappers;

public static class ApplicationMapper
{
    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    public static ApplicationDto ToDto(
        ApplicationD entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ApplicationDto
        {
            ApplicationID =
                entity.ApplicationID,

            ApplicantPersonID =
                entity.ApplicantPersonID,

            ApplicationDate =
                entity.ApplicationDate,

            ApplicationTypeID =
                entity.ApplicationTypeID,

            ApplicationStatus =
                entity.ApplicationStatus,

            LastStatusDate =
                entity.LastStatusDate,

            PaidFees =
                entity.PaidFees,

            CreatedByUserID =
                entity.CreatedByUserID,

            CreatedByUserName =
                entity.CreatedByUser?.UserName
                ?? string.Empty
        };
    }


    // =========================================================
    // ENTITY -> BASIC INFO DTO
    // =========================================================

    public static ApplicationBasicInfoDto
        ToBasicInfoDto(
            ApplicationD entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ApplicationBasicInfoDto
        {
            ApplicationID =
                entity.ApplicationID,

            ApplicantPersonID =
                entity.ApplicantPersonID,

            ApplicationStatus =
                entity.ApplicationStatus,

            PaidFees =
                entity.PaidFees,

            ApplicationTypeName =
                entity.ApplicationType?
                    .ApplicationTypeTitle,

            ApplicantFullName =
                entity.Person?.FullName,

            ApplicationDate =
                entity.ApplicationDate,

            LastStatusDate =
                entity.LastStatusDate,

            CreatedByUserName =
                entity.CreatedByUser?.UserName
        };
    }


    // =========================================================
    // DTO -> ENTITY
    // =========================================================

    public static ApplicationD ToEntity(
        CreateApplicationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ApplicationD
        {
            ApplicantPersonID =
                dto.ApplicantPersonID,

            ApplicationDate =
                dto.ApplicationDate,

            ApplicationTypeID =
                dto.ApplicationTypeID,

            ApplicationStatus =
                dto.ApplicationStatus,

            LastStatusDate =
                dto.LastStatusDate,

            PaidFees =
                dto.PaidFees,

            CreatedByUserID =
                dto.CreatedByUserID
        };
    }
}