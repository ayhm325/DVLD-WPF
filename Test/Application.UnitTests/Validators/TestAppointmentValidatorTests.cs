using System.Text.Json;
using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.Validators;
using Domain.Enums;

namespace Application.UnitTests.Validators;

public class TestAppointmentValidatorTests
{
    private static readonly DateTime ReferenceJordanTime =
        new(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified);

    private static DateTime GetFutureDateAtTime(int hour, int minute, int second = 0, int millisecond = 0)
    {
        var futureDate = ReferenceJordanTime.Date.AddDays(2);
        while (futureDate.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
        {
            futureDate = futureDate.AddDays(1);
        }
        return new DateTime(
            futureDate.Year,
            futureDate.Month,
            futureDate.Day,
            hour,
            minute,
            second,
            millisecond,
            DateTimeKind.Unspecified);
    }

    // =========================================================
    // ALL 12 VALID SLOTS
    // =========================================================

    [Theory]
    [InlineData(8, 0)]
    [InlineData(8, 40)]
    [InlineData(9, 20)]
    [InlineData(10, 0)]
    [InlineData(10, 40)]
    [InlineData(11, 20)]
    [InlineData(12, 0)]
    [InlineData(12, 40)]
    [InlineData(13, 20)]
    [InlineData(14, 0)]
    [InlineData(14, 40)]
    [InlineData(15, 20)]
    public void ValidateAppointmentDate_All12ValidSlots_ReturnsSuccess(int hour, int minute)
    {
        var appointmentDate = GetFutureDateAtTime(hour, minute);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
    }

    // =========================================================
    // BOUNDARY & INVALID TIME REJECTIONS
    // =========================================================

    [Fact]
    public void ValidateAppointmentDate_BeforeWorkingHours_0759_ReturnsValidationFailure()
    {
        var appointmentDate = GetFutureDateAtTime(7, 59);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_ExactStartBoundary_0800_ReturnsSuccess()
    {
        var appointmentDate = GetFutureDateAtTime(8, 0);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAppointmentDate_SecondSlot_0840_ReturnsSuccess()
    {
        var appointmentDate = GetFutureDateAtTime(8, 40);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAppointmentDate_LastValidSlot_1520_ReturnsSuccess()
    {
        var appointmentDate = GetFutureDateAtTime(15, 20);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAppointmentDate_FacilityClosingTime_1600_ReturnsValidationFailure()
    {
        var appointmentDate = GetFutureDateAtTime(16, 0);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_AfterWorkingHours_1601_ReturnsValidationFailure()
    {
        var appointmentDate = GetFutureDateAtTime(16, 1);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_ArbitraryTime_0915_ReturnsValidationFailure()
    {
        var appointmentDate = GetFutureDateAtTime(9, 15);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.",
            result.Error);
    }

    // =========================================================
    // SECONDS & FRACTIONAL SECONDS REJECTIONS
    // =========================================================

    [Fact]
    public void ValidateAppointmentDate_NonZeroSeconds_ReturnsValidationFailure()
    {
        var appointmentDate = GetFutureDateAtTime(8, 0, second: 15);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must not contain seconds or fractional seconds.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_FractionalSeconds_ReturnsValidationFailure()
    {
        var appointmentDate = GetFutureDateAtTime(8, 0, second: 0, millisecond: 250);

        var result = TestAppointmentValidator.ValidateAppointmentDate(appointmentDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must not contain seconds or fractional seconds.",
            result.Error);
    }

    // =========================================================
    // PAST & FUTURE DATES (JORDAN LOCAL-TIME SEMANTICS)
    // =========================================================

    [Fact]
    public void ValidateAppointmentDate_PastDateInJordanTime_ReturnsValidationFailure()
    {
        var currentJordanTime = ReferenceJordanTime;
        var pastDate = currentJordanTime.Date.AddDays(-1).Add(new TimeSpan(8, 0, 0));

        var result = TestAppointmentValidator.ValidateAppointmentDate(pastDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment date must be in the future.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_TomorrowInJordanTime_ReturnsValidationFailure()
    {
        var currentJordanTime = ReferenceJordanTime;
        var tomorrow = currentJordanTime.Date.AddDays(1).Add(new TimeSpan(8, 0, 0));

        var result = TestAppointmentValidator.ValidateAppointmentDate(tomorrow, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointments must be scheduled at least two calendar days after the current facility date.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_TwoDaysAheadWorkingDay_ReturnsSuccess()
    {
        var currentJordanTime = ReferenceJordanTime;
        var eligibleDate = currentJordanTime.Date.AddDays(2);
        while (eligibleDate.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
        {
            eligibleDate = eligibleDate.AddDays(1);
        }
        eligibleDate = eligibleDate.Add(new TimeSpan(8, 0, 0));

        var result = TestAppointmentValidator.ValidateAppointmentDate(eligibleDate, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAppointmentDate_DefaultDateTime_ReturnsValidationFailure()
    {
        var result = TestAppointmentValidator.ValidateAppointmentDate(default, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment date is required.",
            result.Error);
    }

    // =========================================================
    // TIMEZONE AUDIT: UNSPECIFIED, UTC, LOCAL & EXPLICIT OFFSETS
    // =========================================================

    [Fact]
    public void ToFacilityTime_UnspecifiedKind_TreatedDirectlyAsJordanWallClock()
    {
        var unspecified = new DateTime(2026, 11, 10, 8, 40, 0, DateTimeKind.Unspecified);

        var normalized = TestAppointmentValidator.ToFacilityTime(unspecified);

        Assert.Equal(2026, normalized.Year);
        Assert.Equal(11, normalized.Month);
        Assert.Equal(10, normalized.Day);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(40, normalized.Minute);
        Assert.Equal(0, normalized.Second);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);
    }

    [Fact]
    public void ToFacilityTime_UtcKind_ConvertsUtcToJordanFacilityTime()
    {
        // 05:40 UTC + 3 hours = 08:40 in Jordan
        var utcTime = new DateTime(2026, 11, 10, 5, 40, 0, DateTimeKind.Utc);

        var normalized = TestAppointmentValidator.ToFacilityTime(utcTime);

        Assert.Equal(2026, normalized.Year);
        Assert.Equal(11, normalized.Month);
        Assert.Equal(10, normalized.Day);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(40, normalized.Minute);
        Assert.Equal(0, normalized.Second);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);
    }

    [Fact]
    public void ToFacilityTime_LocalKind_ConvertsFromMachineLocalToJordanWithoutAssumingLocalIsJordan()
    {
        var localTime = DateTime.SpecifyKind(ReferenceJordanTime.Date.AddDays(2).Add(new TimeSpan(8, 0, 0)), DateTimeKind.Local);

        var normalized = TestAppointmentValidator.ToFacilityTime(localTime);

        var expected = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, TestAppointmentValidator.JordanTimeZone);
        Assert.Equal(expected.Year, normalized.Year);
        Assert.Equal(expected.Month, normalized.Month);
        Assert.Equal(expected.Day, normalized.Day);
        Assert.Equal(expected.Hour, normalized.Hour);
        Assert.Equal(expected.Minute, normalized.Minute);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);
    }

    [Theory]
    [InlineData(0, 5, 0)]    // 05:00 UTC (+00:00) -> 08:00 Jordan
    [InlineData(3, 8, 0)]    // 08:00 Jordan (+03:00) -> 08:00 Jordan
    [InlineData(-4, 1, 0)]   // 01:00 EDT (-04:00) -> 08:00 Jordan
    [InlineData(9, 14, 0)]   // 14:00 JST (+09:00) -> 08:00 Jordan
    public void ToFacilityTime_DateTimeOffset_ExplicitOffsets_ConvertToJordanSlot0800(int offsetHours, int hour, int minute)
    {
        var futureDate = ReferenceJordanTime.Date.AddDays(2);
        var offset = new DateTimeOffset(futureDate.Year, futureDate.Month, futureDate.Day, hour, minute, 0, TimeSpan.FromHours(offsetHours));

        var normalized = TestAppointmentValidator.ToFacilityTime(offset);

        Assert.Equal(8, normalized.Hour);
        Assert.Equal(0, normalized.Minute);
        Assert.Equal(0, normalized.Second);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);
    }

    [Theory]
    [InlineData("2026-11-10T08:00:00", 8, 0)]         // Unspecified wall-clock
    [InlineData("2026-11-10T05:00:00Z", 8, 0)]        // UTC Z -> Jordan
    [InlineData("2026-11-10T08:00:00+03:00", 8, 0)]   // Jordan offset
    [InlineData("2026-11-10T05:40:00Z", 8, 40)]       // UTC Z -> Jordan 08:40
    [InlineData("2026-11-10T08:40:00+03:00", 8, 40)]  // Jordan offset -> 08:40
    [InlineData("2026-11-10T12:20:00Z", 15, 20)]      // UTC Z -> Jordan 15:20
    [InlineData("2026-11-10T15:20:00+03:00", 15, 20)] // Jordan offset -> 15:20
    public void ParseToFacilityTime_ValidInputs_ConvertDirectlyToCanonicalJordanSlot(string input, int expectedHour, int expectedMinute)
    {
        var normalized = TestAppointmentValidator.ParseToFacilityTime(input);

        Assert.Equal(expectedHour, normalized.Hour);
        Assert.Equal(expectedMinute, normalized.Minute);
        Assert.Equal(0, normalized.Second);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);

        var result = TestAppointmentValidator.ValidateAppointmentDate(normalized, ReferenceJordanTime);
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("2026-11-10T08:00:00Z")]       // 08:00 UTC -> 11:00 Jordan (invalid slot between 10:40 and 11:20)
    [InlineData("2026-11-10T08:40:00Z")]       // 08:40 UTC -> 11:40 Jordan (invalid slot between 11:20 and 12:00)
    [InlineData("2026-11-10T16:00:00+03:00")]  // 16:00 Jordan (closing time, invalid)
    [InlineData("2026-11-10T07:59:00+03:00")]  // 07:59 Jordan (before hours, invalid)
    public void ParseToFacilityTime_TimestampsConvertingToInvalidSlot_RejectedByValidator(string dateString)
    {
        var facilityDate = TestAppointmentValidator.ParseToFacilityTime(dateString);

        var result = TestAppointmentValidator.ValidateAppointmentDate(facilityDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.",
            result.Error);
    }

    [Fact]
    public void ToFacilityTime_Idempotent_NoDoubleConversion()
    {
        var utcDate = new DateTime(2026, 11, 10, 5, 40, 0, DateTimeKind.Utc);

        var pass1 = TestAppointmentValidator.ToFacilityTime(utcDate);
        var pass2 = TestAppointmentValidator.ToFacilityTime(pass1);

        Assert.Equal(pass1, pass2);
        Assert.Equal(8, pass2.Hour);
        Assert.Equal(40, pass2.Minute);
        Assert.Equal(DateTimeKind.Unspecified, pass2.Kind);
    }

    [Fact]
    public void ValidateAppointmentDate_AroundMidnightBoundary_EvaluatesCorrectlyInJordanTime()
    {
        var currentJordan = ReferenceJordanTime;
        var pastToday = new DateTime(currentJordan.Year, currentJordan.Month, currentJordan.Day, 7, 59, 0, DateTimeKind.Unspecified);

        var result = TestAppointmentValidator.ValidateAppointmentDate(pastToday, ReferenceJordanTime);

        Assert.True(result.IsFailure);
    }

    // =========================================================
    // API JSON DESERIALIZATION CONTRACT
    // =========================================================

    private sealed record TestAppointmentPayload(
        int TestTypeId,
        int LocalDrivingLicenseApplicationId,
        DateTime AppointmentDate);

    [Fact]
    public void ScheduleTestRequest_JsonDeserialization_WithoutOffset_PreservesWallClock()
    {
        var json = """{"testTypeId":1,"localDrivingLicenseApplicationId":10,"appointmentDate":"2026-11-10T08:40:00"}""";
        var request = JsonSerializer.Deserialize<TestAppointmentPayload>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(request);
        var normalized = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(40, normalized.Minute);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);
    }

    [Fact]
    public void ScheduleTestRequest_JsonDeserialization_WithUtcZ_ConvertsToJordanTime()
    {
        var json = """{"testTypeId":1,"localDrivingLicenseApplicationId":10,"appointmentDate":"2026-11-10T05:40:00Z"}""";
        var request = JsonSerializer.Deserialize<TestAppointmentPayload>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(request);
        var normalized = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(40, normalized.Minute);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);
    }

    [Fact]
    public void ScheduleTestRequest_JsonDeserialization_WithJordanOffset_ConvertsToJordanTime()
    {
        var json = """{"testTypeId":1,"localDrivingLicenseApplicationId":10,"appointmentDate":"2026-11-10T08:40:00+03:00"}""";
        var request = JsonSerializer.Deserialize<TestAppointmentPayload>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(request);
        var normalized = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(40, normalized.Minute);
        Assert.Equal(DateTimeKind.Unspecified, normalized.Kind);
    }

    // =========================================================
    // HISTORICAL COMPATIBILITY: MIDNIGHT REJECTION FOR NEW BOOKING
    // =========================================================

    [Fact]
    public void ValidateAppointmentDate_HistoricalMidnightTimestamp_RejectedForNewBooking()
    {
        var midnightDate = ReferenceJordanTime.Date.AddDays(2); // 00:00:00

        var result = TestAppointmentValidator.ValidateAppointmentDate(midnightDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.",
            result.Error);
    }

    // =========================================================
    // VALIDATE SCHEDULE (INTEGRATED)
    // =========================================================

    [Fact]
    public void ValidateSchedule_ValidSlot_ReturnsSuccess()
    {
        var futureDate = GetFutureDateAtTime(8, 40);

        var result = TestAppointmentValidator.ValidateSchedule(100, (int)TestTypeEnum.Theory, futureDate, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateSchedule_InvalidSlot_ReturnsValidationFailure()
    {
        var futureDate = GetFutureDateAtTime(9, 15);

        var result = TestAppointmentValidator.ValidateSchedule(100, (int)TestTypeEnum.Theory, futureDate, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment time must be one of the approved 40-minute slots between 08:00 and 15:20.",
            result.Error);
    }

    // =========================================================
    // VALIDATE CREATE DTO
    // =========================================================

    [Fact]
    public void ValidateCreate_ValidSlot_ReturnsSuccess()
    {
        var dto = new CreateTestAppointmentDto
        {
            TestTypeID = (int)TestTypeEnum.Written,
            LocalDrivingLicenseApplicationID = 50,
            AppointmentDate = GetFutureDateAtTime(10, 40)
        };

        var result = TestAppointmentValidator.ValidateCreate(dto, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, dto.AppointmentDate.Hour);
        Assert.Equal(40, dto.AppointmentDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, dto.AppointmentDate.Kind);
    }

    [Fact]
    public void ValidateCreate_UtcDate_NormalizesToJordanTime()
    {
        // 07:40 UTC + 3 = 10:40 Jordan (Slot 05)
        var dto = new CreateTestAppointmentDto
        {
            TestTypeID = (int)TestTypeEnum.Written,
            LocalDrivingLicenseApplicationID = 50,
            AppointmentDate = new DateTime(2026, 11, 10, 7, 40, 0, DateTimeKind.Utc)
        };

        var result = TestAppointmentValidator.ValidateCreate(dto, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, dto.AppointmentDate.Hour);
        Assert.Equal(40, dto.AppointmentDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, dto.AppointmentDate.Kind);
    }

    [Fact]
    public void ValidateCreate_InvalidSlot_ReturnsValidationFailure()
    {
        var dto = new CreateTestAppointmentDto
        {
            TestTypeID = (int)TestTypeEnum.Written,
            LocalDrivingLicenseApplicationID = 50,
            AppointmentDate = GetFutureDateAtTime(16, 0)
        };

        var result = TestAppointmentValidator.ValidateCreate(dto, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // =========================================================
    // VALIDATE UPDATE DTO
    // =========================================================

    [Fact]
    public void ValidateUpdate_ValidSlot_ReturnsSuccess()
    {
        var dto = new UpdateTestAppointmentDto
        {
            TestAppointmentID = 10,
            AppointmentDate = GetFutureDateAtTime(14, 0)
        };

        var result = TestAppointmentValidator.ValidateUpdate(dto, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, dto.AppointmentDate.Hour);
        Assert.Equal(0, dto.AppointmentDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, dto.AppointmentDate.Kind);
    }

    [Fact]
    public void ValidateUpdate_UtcDate_NormalizesToJordanTime()
    {
        // 11:00 UTC + 3 = 14:00 Jordan (Slot 10)
        var dto = new UpdateTestAppointmentDto
        {
            TestAppointmentID = 10,
            AppointmentDate = new DateTime(2026, 11, 10, 11, 0, 0, DateTimeKind.Utc)
        };

        var result = TestAppointmentValidator.ValidateUpdate(dto, ReferenceJordanTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, dto.AppointmentDate.Hour);
        Assert.Equal(0, dto.AppointmentDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, dto.AppointmentDate.Kind);
    }

    [Fact]
    public void ValidateUpdate_InvalidSlot_ReturnsValidationFailure()
    {
        var dto = new UpdateTestAppointmentDto
        {
            TestAppointmentID = 10,
            AppointmentDate = GetFutureDateAtTime(7, 59)
        };

        var result = TestAppointmentValidator.ValidateUpdate(dto, ReferenceJordanTime);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // =========================================================
    // MINIMUM TWO-CALENDAR-DAY ADVANCE BOOKING BOUNDARY TESTS
    // =========================================================

    [Fact]
    public void ValidateAppointmentDate_Nov3CurrentJordanDate_Nov3Booking_RejectedSameDay()
    {
        var currentJordanTime = new DateTime(2026, 11, 3, 14, 0, 0, DateTimeKind.Unspecified);
        var requestedDate = new DateTime(2026, 11, 3, 14, 40, 0, DateTimeKind.Unspecified);

        var result = TestAppointmentValidator.ValidateAppointmentDate(requestedDate, currentJordanTime);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "Appointments must be scheduled at least two calendar days after the current facility date.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_Nov3CurrentJordanDate_Nov4Booking_RejectedTomorrow()
    {
        var currentJordanTime = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified);
        var requestedDate = new DateTime(2026, 11, 4, 8, 0, 0, DateTimeKind.Unspecified);

        var result = TestAppointmentValidator.ValidateAppointmentDate(requestedDate, currentJordanTime);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "Appointments must be scheduled at least two calendar days after the current facility date.",
            result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_Nov3CurrentJordanDate_Nov5Booking_AllowedEarliestEligible()
    {
        var currentJordanTime = new DateTime(2026, 11, 3, 16, 30, 0, DateTimeKind.Unspecified);
        var requestedDate = new DateTime(2026, 11, 5, 8, 0, 0, DateTimeKind.Unspecified);

        var result = TestAppointmentValidator.ValidateAppointmentDate(requestedDate, currentJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAppointmentDate_Nov3CurrentJordanDate_Nov6Booking_RejectedFridayWeekend()
    {
        var currentJordanTime = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified);
        var requestedDate = new DateTime(2026, 11, 6, 8, 0, 0, DateTimeKind.Unspecified);

        var result = TestAppointmentValidator.ValidateAppointmentDate(requestedDate, currentJordanTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Appointments cannot be scheduled on Friday or Saturday.", result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_Nov3CurrentJordanDate_Nov7Booking_RejectedSaturdayWeekend()
    {
        var currentJordanTime = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified);
        var requestedDate = new DateTime(2026, 11, 7, 8, 0, 0, DateTimeKind.Unspecified);

        var result = TestAppointmentValidator.ValidateAppointmentDate(requestedDate, currentJordanTime);

        Assert.True(result.IsFailure);
        Assert.Contains("Appointments cannot be scheduled on Friday or Saturday.", result.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_Nov3CurrentJordanDate_Nov8Booking_AllowedNextWorkingWeek()
    {
        var currentJordanTime = new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified);
        var requestedDate = new DateTime(2026, 11, 8, 8, 0, 0, DateTimeKind.Unspecified);

        var result = TestAppointmentValidator.ValidateAppointmentDate(requestedDate, currentJordanTime);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAppointmentDate_AroundMidnight_Nov3JustBeforeMidnight_Nov5IsEligible()
    {
        var currentJordanTime = new DateTime(2026, 11, 3, 23, 59, 59, DateTimeKind.Unspecified);
        var requestedNov5 = new DateTime(2026, 11, 5, 8, 0, 0, DateTimeKind.Unspecified);
        var requestedNov4 = new DateTime(2026, 11, 4, 8, 0, 0, DateTimeKind.Unspecified);

        var resultNov5 = TestAppointmentValidator.ValidateAppointmentDate(requestedNov5, currentJordanTime);
        var resultNov4 = TestAppointmentValidator.ValidateAppointmentDate(requestedNov4, currentJordanTime);

        Assert.True(resultNov5.IsSuccess);
        Assert.True(resultNov4.IsFailure);
        Assert.Contains(
            "Appointments must be scheduled at least two calendar days after the current facility date.",
            resultNov4.Error);
    }

    [Fact]
    public void ValidateAppointmentDate_AroundMidnight_Nov4JustAfterMidnight_Nov5IsNowRejected()
    {
        var currentJordanTime = new DateTime(2026, 11, 4, 0, 0, 1, DateTimeKind.Unspecified);
        var requestedNov5 = new DateTime(2026, 11, 5, 8, 0, 0, DateTimeKind.Unspecified);
        var requestedNov8 = new DateTime(2026, 11, 8, 8, 0, 0, DateTimeKind.Unspecified);

        var resultNov5 = TestAppointmentValidator.ValidateAppointmentDate(requestedNov5, currentJordanTime);
        var resultNov8 = TestAppointmentValidator.ValidateAppointmentDate(requestedNov8, currentJordanTime);

        Assert.True(resultNov5.IsFailure);
        Assert.Contains(
            "Appointments must be scheduled at least two calendar days after the current facility date.",
            resultNov5.Error);
        Assert.True(resultNov8.IsSuccess);
    }
}
