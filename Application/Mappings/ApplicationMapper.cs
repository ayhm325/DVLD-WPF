using Application.DTOs.ApplicationDTO;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappers;

public static class ApplicationMapper
{
    public static ApplicationDto ToDto(ApplicationD entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ApplicationDto
        {
            ApplicationID = entity.ApplicationID,
            ApplicantPersonID = entity.ApplicantPersonID,
            ApplicationDate = entity.ApplicationDate,
            ApplicationTypeID = entity.ApplicationTypeID,
            ApplicationStatus = entity.ApplicationStatus,
            LastStatusDate = entity.LastStatusDate,
            PaidFees = entity.PaidFees,
            CreatedByUserID = entity.CreatedByUserID,
            CreatedByUserName = entity.CreatedByUser?.UserName ?? string.Empty
        };
    }

    public static ApplicationBasicInfoDto ToBasicInfoDto(ApplicationD entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ApplicationBasicInfoDto
        {
            ApplicationID = entity.ApplicationID,
            ApplicantPersonID = entity.ApplicantPersonID,
            ApplicationStatus = entity.ApplicationStatus,
            PaidFees = entity.PaidFees,
            ApplicationTypeName = entity.ApplicationType?.ApplicationTypeTitle,
            ApplicantFullName = entity.Person?.FullName,
            ApplicationDate = entity.ApplicationDate,
            LastStatusDate = entity.LastStatusDate,
            CreatedByUserName = entity.CreatedByUser?.UserName
        };
    }

    public static ApplicationD ToEntity(
        CreateApplicationDto dto,
        decimal paidFees,
        int createdByUserId,
        DateTime applicationDate)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ApplicationD
        {
            ApplicantPersonID = dto.ApplicantPersonID,
            ApplicationTypeID = dto.ApplicationTypeID,
            ApplicationDate = applicationDate,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = applicationDate,
            PaidFees = paidFees,
            CreatedByUserID = createdByUserId
        };
    }
}
