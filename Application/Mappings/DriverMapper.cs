using Application.DTOs;
using Application.DTOs.DriverDTO;
using Domain.Entities;

namespace Application.Mappers;

public static class DriverMapper
{
    public static DriverDto ToDto(Driver entity)
    {
        return new DriverDto
        {
            DriverID = entity.DriverID,
            PersonID = entity.PersonID,
            FullName = entity.Person?.FullName ?? string.Empty,
            NationalNo = entity.Person?.NationalNo ?? string.Empty,
            DateOfBirth = entity.Person?.DateOfBirth ?? DateTime.MinValue,
            Gender = entity.Person?.Gender ?? default,
            ImagePath = entity.Person?.ImagePath,
            CreatedByUserID = entity.CreatedByUserID,
            CreatedByUserName = entity.CreatedByUser?.UserName ?? string.Empty,
            CreatedDate = entity.CreatedDate,
            ActiveLicenses = entity.Licenses?.Count(l => l.IsActive) ?? 0
        };
    }

    public static List<DriverDto> ToDtoList(IEnumerable<Driver> entities)
    {
        return entities.Select(ToDto).ToList();
    }

    public static Driver ToEntity(CreateDriverDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Driver
        {
            PersonID = dto.PersonID,
            CreatedDate = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(Driver entity, UpdateDriverDto dto)
    {
        entity.PersonID = dto.PersonID;
    }
}