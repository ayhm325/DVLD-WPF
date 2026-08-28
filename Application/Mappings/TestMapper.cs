using Application.DTOs.TestDTO;
using Domain.Entities;

namespace Application.Mappers;

public static class TestMapper
{
    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    public static TestDto ToDto(Test entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TestDto
        {
            TestID =
                entity.TestID,

            TestAppointmentID =
                entity.TestAppointmentID,

            TestResult =
                entity.TestResult,

            Notes =
                entity.Notes,

            CreatedByUserID =
                entity.CreatedByUserID,

            CreatedByUserName =
                entity.User?.UserName,

            TestTypeName =
                entity.TestAppointment?
                    .TestType?
                    .TestTypeTitle,

            AppointmentDate =
                entity.TestAppointment?
                    .AppointmentDate
        };
    }

    // =========================================================
    // DTO -> ENTITY
    // =========================================================

    public static Test ToEntity(
        TestDto dto,
        int createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Test
        {
            TestAppointmentID =
                dto.TestAppointmentID,

            TestResult =
                dto.TestResult,

            Notes =
                string.IsNullOrWhiteSpace(dto.Notes)
                    ? null
                    : dto.Notes.Trim(),

            CreatedByUserID =
                createdByUserId
        };
    }

    // =========================================================
    // UPDATE ENTITY FROM DTO
    // =========================================================

    public static void UpdateEntity(
        Test entity,
        TestDto dto)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(dto);

        entity.TestResult =
            dto.TestResult;

        entity.Notes =
            string.IsNullOrWhiteSpace(dto.Notes)
                ? null
                : dto.Notes.Trim();
    }
}