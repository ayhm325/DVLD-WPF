using Application.Common.Results;
using Application.Common.Time;
using Application.DTOs.TestAppointmentDTO;
using Domain.Enums;

namespace Application.Validators;

public static class TestAppointmentValidator
{
    // =========================================================
    // TIME SLOTS & FACILITY WORKING HOURS
    // =========================================================

    public static readonly TimeSpan[] ValidSlotTimes =
    [
        new(8, 0, 0),
        new(8, 40, 0),
        new(9, 20, 0),
        new(10, 0, 0),
        new(10, 40, 0),
        new(11, 20, 0),
        new(12, 0, 0),
        new(12, 40, 0),
        new(13, 20, 0),
        new(14, 0, 0),
        new(14, 40, 0),
        new(15, 20, 0)
    ];

    private static readonly HashSet<TimeSpan> ValidSlotTimesSet = new(ValidSlotTimes);

    public static TimeZoneInfo JordanTimeZone => JordanTimeZoneProvider.TimeZone;

    public static bool IsValidSlotTime(TimeSpan timeOfDay) =>
        ValidSlotTimesSet.Contains(timeOfDay);

    // =========================================================
    // TIMEZONE NORMALIZATION
    // =========================================================

    public static DateTime ToFacilityTime(DateTime dateTime)
    {
        if (dateTime == default)
        {
            return default;
        }

        return dateTime.Kind switch
        {
            DateTimeKind.Utc => JordanTimeZoneProvider.ConvertUtcToJordan(dateTime),
            DateTimeKind.Local => JordanTimeZoneProvider.ConvertUtcToJordan(dateTime.ToUniversalTime()),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified)
        };
    }

    public static DateTime ToFacilityTime(DateTimeOffset dateTimeOffset)
    {
        var inJordan = TimeZoneInfo.ConvertTime(dateTimeOffset, JordanTimeZone).DateTime;
        return DateTime.SpecifyKind(inJordan, DateTimeKind.Unspecified);
    }

    public static DateTime ParseToFacilityTime(string dateString)
    {
        var offset = Serialization.JordanFacilityDateTimeOffsetConverter.Parse(dateString);
        return ToFacilityTime(offset);
    }

    // =========================================================
    // APPOINTMENT DATE & TIME VALIDATION
    // =========================================================

    public static Result ValidateAppointmentDate(DateTime appointmentDate, DateTime currentJordanTime)
    {
        if (appointmentDate == default)
        {
            return Result.ValidationFailure("Appointment date is required.");
        }

        var errors = new List<string>();

        var facilityDate = ToFacilityTime(appointmentDate);
        var jordanNow = ToFacilityTime(currentJordanTime);
        var currentJordanDate = jordanNow.Date;

        if (facilityDate <= jordanNow)
        {
            errors.Add("Appointment date must be in the future.");
        }

        if ((facilityDate.Date - currentJordanDate).Days < 2)
        {
            errors.Add("Appointments must be scheduled at least two calendar days after the current facility date.");
        }

        if (facilityDate.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
        {
            errors.Add("Appointments cannot be scheduled on Friday or Saturday.");
        }

        var hasSecondsOrFraction = facilityDate.Second != 0 ||
                                   facilityDate.Millisecond != 0 ||
                                   (facilityDate.Ticks % TimeSpan.TicksPerSecond) != 0;

        if (hasSecondsOrFraction)
        {
            errors.Add("Appointment time must not contain seconds or fractional seconds.");
        }

        var timeOfDay = new TimeSpan(facilityDate.Hour, facilityDate.Minute, 0);
        if (!ValidSlotTimesSet.Contains(timeOfDay))
        {
            errors.Add("Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.");
        }

        return CreateResult(errors);
    }

    // =========================================================
    // DTO & ACTION VALIDATORS
    // =========================================================

    public static Result ValidateCreate(CreateTestAppointmentDto? dto, DateTime currentJordanTime)
    {
        if (dto is null)
        {
            return Result.ValidationFailure("Test appointment data is required.");
        }

        var errors = new List<string>();

        if (dto.TestTypeID <= 0)
        {
            errors.Add("Test type is required.");
        }
        else if (!Enum.IsDefined(typeof(TestTypeEnum), dto.TestTypeID))
        {
            errors.Add("Invalid test type.");
        }

        if (dto.LocalDrivingLicenseApplicationID <= 0)
        {
            errors.Add("Local driving license application is required.");
        }

        var dateValidation = ValidateAppointmentDate(dto.AppointmentDate, currentJordanTime);
        if (dateValidation.IsFailure)
        {
            errors.Add(dateValidation.Error);
        }

        if (dto.RetakeTestApplicationID.HasValue && dto.RetakeTestApplicationID.Value <= 0)
        {
            errors.Add("Invalid retake test application ID.");
        }

        return CreateResult(errors);
    }

    public static Result ValidateSchedule(
        int localAppId,
        int testTypeId,
        DateTime appointmentDate,
        DateTime currentJordanTime)
    {
        var errors = new List<string>();

        if (localAppId <= 0)
        {
            errors.Add("Invalid local driving license application ID.");
        }

        if (testTypeId <= 0)
        {
            errors.Add("Test type is required.");
        }
        else if (!Enum.IsDefined(typeof(TestTypeEnum), testTypeId))
        {
            errors.Add("Invalid test type.");
        }

        var dateValidation = ValidateAppointmentDate(appointmentDate, currentJordanTime);
        if (dateValidation.IsFailure)
        {
            errors.Add(dateValidation.Error);
        }

        return CreateResult(errors);
    }

    public static Result ValidateAvailabilityRequest(
        int localAppId,
        int testTypeId,
        DateTime startDate,
        DateTime endDate)
    {
        var errors = new List<string>();

        if (localAppId <= 0)
        {
            errors.Add("Invalid local driving license application ID.");
        }

        if (testTypeId <= 0)
        {
            errors.Add("Test type is required.");
        }
        else if (!Enum.IsDefined(typeof(TestTypeEnum), testTypeId))
        {
            errors.Add("Invalid test type.");
        }

        if (startDate == default)
        {
            errors.Add("Start date is required.");
        }

        if (endDate == default)
        {
            errors.Add("End date is required.");
        }

        if (startDate != default && endDate != default)
        {
            var facilityStartDate = ToFacilityTime(startDate).Date;
            var facilityEndDate = ToFacilityTime(endDate).Date;

            if (facilityStartDate > facilityEndDate)
            {
                errors.Add("Start date cannot be later than end date.");
            }
        }

        return CreateResult(errors);
    }

    public static Result ValidateUpdate(UpdateTestAppointmentDto? dto, DateTime currentJordanTime)
    {
        if (dto is null)
        {
            return Result.ValidationFailure("Test appointment data is required.");
        }

        var errors = new List<string>();

        if (dto.TestAppointmentID <= 0)
        {
            errors.Add("Invalid test appointment ID.");
        }

        var dateValidation = ValidateAppointmentDate(dto.AppointmentDate, currentJordanTime);
        if (dateValidation.IsFailure)
        {
            errors.Add(dateValidation.Error);
        }

        return CreateResult(errors);
    }

    public static Result ValidateId(int id) =>
        id > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid test appointment ID.");

    // =========================================================
    // CANCELLATION & MODIFICATION CUTOFF
    // =========================================================

    public static Result ValidateModificationOrCancellationCutoff(
        DateTime appointmentDate,
        DateTime currentJordanTime)
    {
        var facilityDate = ToFacilityTime(appointmentDate).Date;
        var currentJordanDate = ToFacilityTime(currentJordanTime).Date;

        if ((facilityDate - currentJordanDate).Days < 2)
        {
            return Result.ValidationFailure(
                "Appointments cannot be modified or cancelled less than two calendar days before the scheduled date.");
        }

        return Result.Success();
    }

    public static Result ValidateTestTypeId(int testTypeId) =>
        Enum.IsDefined(typeof(TestTypeEnum), testTypeId)
            ? Result.Success()
            : Result.ValidationFailure("Invalid test type.");

    public static Result ValidateApplicationId(int applicationId) =>
        applicationId > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid local driving license application ID.");

    public static Result ValidateUserId(int userId) =>
        userId > 0
            ? Result.Success()
            : Result.ValidationFailure("Invalid user ID.");

    private static Result CreateResult(List<string> errors) =>
        errors.Count == 0
            ? Result.Success()
            : Result.ValidationFailure(string.Join(Environment.NewLine, errors));
}
