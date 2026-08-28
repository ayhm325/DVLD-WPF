using Application.DTOs.TestAppointmentDTO;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappers;

public static class TestAppointmentMapper
{
    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    public static TestAppointmentDto ToDto(
        TestAppointment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var result = TestResultType.NotTaken;

        if (entity.Test is not null)
        {
            result = entity.Test.TestResult
                ? TestResultType.Pass
                : TestResultType.Fail;
        }

        return new TestAppointmentDto
        {
            TestAppointmentID =
                entity.TestAppointmentID,

            TestTypeID =
                entity.TestTypeID,

            TestTypeName =
                entity.TestType?.TestTypeTitle
                ?? string.Empty,

            LocalDrivingLicenseApplicationID =
                entity.LocalDrivingLicenseApplicationID,

            AppointmentDate =
                entity.AppointmentDate,

            PaidFees =
                entity.PaidFees,

            CreatedByUserID =
                entity.CreatedByUserID,

            CreatedByUserName =
                entity.User?.UserName
                ?? "N/A",

            IsLocked =
                entity.IsLocked,

            RetakeTestApplicationID =
                entity.RetakeTestApplicationID,

            TestResult =
                result
        };
    }


    // =========================================================
    // ENTITY -> SCHEDULE DTO
    // =========================================================

    public static ScheduleTestDto ToScheduleDto(
        TestAppointment entity,
        int trial)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ScheduleTestDto
        {
            AppointmentID =
                entity.TestAppointmentID,

            RetakeTestApplicationID =
                entity.RetakeTestApplicationID,

            LocalDrivingLicenseApplicationID =
                entity.LocalDrivingLicenseApplicationID,

            LicenseClassName =
                entity.LocalDrivingLicenseApplication?
                    .LicenseClass?
                    .ClassName,

            FullName =
                entity.LocalDrivingLicenseApplication?
                    .Application?
                    .Person?
                    .FullName,

            Trial =
                trial,

            Date =
                entity.AppointmentDate,

            Fees =
                entity.TestType?.TestTypeFees
                ?? entity.PaidFees,

            TestTypeID =
                entity.TestTypeID,

            RetakerFees =
                entity.RetakeTestApplication != null
                    ? entity.TestType?.TestTypeFees ?? 0
                    : 0,

            TestID =
                entity.Test?.TestID ?? 0,

            Result =
                entity.Test?.TestResult ?? false,

            Notes =
                entity.Test?.Notes
        };
    }


    // =========================================================
    // DTO -> ENTITY
    // =========================================================

    public static TestAppointment ToEntity(
        CreateTestAppointmentDto dto,
        decimal paidFees,
        int createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new TestAppointment
        {
            TestTypeID =
                dto.TestTypeID,

            LocalDrivingLicenseApplicationID =
                dto.LocalDrivingLicenseApplicationID,

            AppointmentDate =
                dto.AppointmentDate,

            PaidFees =
                paidFees,

            CreatedByUserID =
                createdByUserId,

            IsLocked =
                false,

            RetakeTestApplicationID =
                dto.RetakeTestApplicationID
        };
    }
}