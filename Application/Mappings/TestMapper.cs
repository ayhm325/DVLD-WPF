using Application.DTOs.TestDTO;
using Domain.Entities;

namespace Application.Mappers;

public static class TestMapper
{
    public static TestDto ToDto(Test entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TestDto
        {
            TestID = entity.TestID,
            TestAppointmentID = entity.TestAppointmentID,
            TestResult = entity.TestResult,
            Notes = entity.Notes,
            CreatedByUserID = entity.CreatedByUserID,
            CreatedByUserName = entity.User?.UserName,
            TestTypeName = entity.TestAppointment?
                .TestType?.TestTypeTitle,
            AppointmentDate = entity.TestAppointment?
                .AppointmentDate
        };
    }

    public static Test ToEntity(
        TestDto dto,
        int createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Test
        {
            TestAppointmentID = dto.TestAppointmentID,
            TestResult = dto.TestResult,
            Notes = NormalizeNotes(dto.Notes),
            CreatedByUserID = createdByUserId
        };
    }

    public static void UpdateEntity(
        Test entity,
        TestDto dto)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(dto);

        entity.TestResult = dto.TestResult;
        entity.Notes = NormalizeNotes(dto.Notes);
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
}
