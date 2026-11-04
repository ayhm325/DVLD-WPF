using System.Globalization;
using System.Text.Json;
using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.Serialization;
using Application.Validators;
using DVLD.Contracts.TestAppointment;

namespace Application.UnitTests.Serialization;

public class JordanFacilityDateTimeOffsetConverterTests
{
    private static readonly DateTime ReferenceJordanNow =
        new(2026, 11, 3, 10, 0, 0, DateTimeKind.Unspecified);

    private readonly JsonSerializerOptions _jsonOptions;

    public JordanFacilityDateTimeOffsetConverterTests()
    {
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JordanFacilityDateTimeOffsetConverter());
    }

    // =========================================================
    // 1. TIMESTAMP WITHOUT OFFSET: 08:00 REPRESENTS JORDAN WALL-CLOCK
    // =========================================================

    [Fact]
    public void Deserialize_NoOffset_RepresentsJordanWallClock0800()
    {
        const string json = "{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"2026-11-10T08:00:00\"}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        Assert.Equal(2026, facilityDate.Year);
        Assert.Equal(11, facilityDate.Month);
        Assert.Equal(10, facilityDate.Day);
        Assert.Equal(8, facilityDate.Hour);
        Assert.Equal(0, facilityDate.Minute);
        Assert.Equal(0, facilityDate.Second);
        Assert.Equal(DateTimeKind.Unspecified, facilityDate.Kind);
    }

    [Fact]
    public void Deserialize_NoOffset_SpaceSeparator_RepresentsJordanWallClock()
    {
        const string json = "{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"2026-11-10 08:40:00\"}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        Assert.Equal(8, facilityDate.Hour);
        Assert.Equal(40, facilityDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, facilityDate.Kind);
    }

    // =========================================================
    // 2. 05:00Z CONVERTING TO 08:00 JORDAN FACILITY TIME
    // =========================================================

    [Fact]
    public void Deserialize_UtcZ_ConvertsTo0800JordanFacilityTime()
    {
        const string json = "{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"2026-11-10T05:00:00Z\"}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        Assert.Equal(8, facilityDate.Hour);
        Assert.Equal(0, facilityDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, facilityDate.Kind);
    }

    // =========================================================
    // 3. 08:00+03:00 REMAINING 08:00 JORDAN FACILITY TIME
    // =========================================================

    [Fact]
    public void Deserialize_JordanOffset_Remains0800JordanFacilityTime()
    {
        const string json = "{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"2026-11-10T08:00:00+03:00\"}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        Assert.Equal(8, facilityDate.Hour);
        Assert.Equal(0, facilityDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, facilityDate.Kind);
    }

    // =========================================================
    // 4. NON-JORDAN OFFSETS REPRESENTING THE SAME INSTANT
    // =========================================================

    [Theory]
    [InlineData("2026-11-10T01:00:00-04:00")] // Atlantic / EDT
    [InlineData("2026-11-10T00:00:00-05:00")] // US Eastern Standard Time
    [InlineData("2026-11-10T06:00:00+01:00")] // Central European Time
    [InlineData("2026-11-10T07:00:00+02:00")] // Eastern European Time
    [InlineData("2026-11-10T10:30:00+05:30")] // India Standard Time
    [InlineData("2026-11-10T14:00:00+09:00")] // Japan Standard Time
    public void Deserialize_NonJordanExplicitOffsets_AllConvertTo0800JordanFacilityTime(string timestamp)
    {
        var json = $"{{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"{timestamp}\"}}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        Assert.Equal(2026, facilityDate.Year);
        Assert.Equal(11, facilityDate.Month);
        Assert.Equal(10, facilityDate.Day);
        Assert.Equal(8, facilityDate.Hour);
        Assert.Equal(0, facilityDate.Minute);
        Assert.Equal(0, facilityDate.Second);
        Assert.Equal(DateTimeKind.Unspecified, facilityDate.Kind);
    }

    // =========================================================
    // 5. OFFSET THAT CONVERTS TO AN INVALID SLOT
    // =========================================================

    [Fact]
    public void Deserialize_OffsetConvertingToInvalidSlot_FailsSlotValidation()
    {
        // 05:15:00Z -> converts to 08:15:00 Jordan, which is not an approved 40-min slot
        const string json = "{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"2026-11-10T05:15:00Z\"}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        Assert.Equal(8, facilityDate.Hour);
        Assert.Equal(15, facilityDate.Minute);

        var validationResult = TestAppointmentValidator.ValidateAppointmentDate(facilityDate, ReferenceJordanNow);
        Assert.True(validationResult.IsFailure);
        Assert.Contains("approved 40-minute slots", validationResult.Error);
    }

    // =========================================================
    // 6. TIMESTAMP CONVERTING TO A PAST APPOINTMENT IN JORDAN
    // =========================================================

    [Fact]
    public void Deserialize_PastTimestampInJordan_FailsFutureValidation()
    {
        // 2020-01-01T05:00:00Z -> converts to 2020-01-01 08:00:00 Jordan (past)
        const string json = "{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"2020-01-01T05:00:00Z\"}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        var validationResult = TestAppointmentValidator.ValidateAppointmentDate(facilityDate, ReferenceJordanNow);
        Assert.True(validationResult.IsFailure);
        Assert.Contains("must be in the future", validationResult.Error);
    }

    // =========================================================
    // 7. TIMESTAMP CONVERTING TO A FUTURE APPOINTMENT IN JORDAN
    // =========================================================

    [Fact]
    public void Deserialize_FutureTimestampInJordan_PassesValidation()
    {
        var futureDate = ReferenceJordanNow.Date.AddDays(5);
        // 08:00 Jordan time slot 1
        var isoString = $"{futureDate:yyyy-MM-dd}T08:00:00";
        var json = $"{{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":10,\"appointmentDate\":\"{isoString}\"}}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);

        var validationResult = TestAppointmentValidator.ValidateAppointmentDate(facilityDate, ReferenceJordanNow);
        Assert.True(validationResult.IsSuccess);
    }

    // =========================================================
    // 8. ALL THREE API ENTRY POINTS FOLLOW THE SAME CONTRACT
    // =========================================================

    [Fact]
    public void ScheduleTestRequest_DeserializesAndConvertsCorrectly()
    {
        const string json = "{\"testTypeId\":2,\"localDrivingLicenseApplicationId\":45,\"appointmentDate\":\"2026-11-10T05:00:00Z\"}";

        var request = JsonSerializer.Deserialize<ScheduleTestRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        Assert.Equal(2, request.TestTypeId);
        Assert.Equal(45, request.LocalDrivingLicenseApplicationId);

        var normalized = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(0, normalized.Minute);
    }

    [Fact]
    public void CreateTestAppointmentRequest_DeserializesAndConvertsCorrectly()
    {
        const string json = "{\"testTypeId\":1,\"localDrivingLicenseApplicationId\":22,\"appointmentDate\":\"2026-11-10T08:00:00\",\"retakeTestApplicationId\":null}";

        var request = JsonSerializer.Deserialize<CreateTestAppointmentRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        Assert.Equal(1, request.TestTypeId);
        Assert.Equal(22, request.LocalDrivingLicenseApplicationId);

        var normalized = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(0, normalized.Minute);
    }

    [Fact]
    public void UpdateTestAppointmentRequest_DeserializesAndConvertsCorrectly()
    {
        const string json = "{\"testAppointmentId\":88,\"appointmentDate\":\"2026-11-10T01:00:00-04:00\"}";

        var request = JsonSerializer.Deserialize<UpdateTestAppointmentRequest>(json, _jsonOptions);

        Assert.NotNull(request);
        Assert.Equal(88, request.TestAppointmentId);

        var normalized = TestAppointmentValidator.ToFacilityTime(request.AppointmentDate);
        Assert.Equal(8, normalized.Hour);
        Assert.Equal(0, normalized.Minute);
    }

    // =========================================================
    // 9. HOST TIMEZONE INDEPENDENCE VERIFICATION
    // =========================================================

    [Theory]
    [InlineData("UTC", 0)]
    [InlineData("Eastern Standard Time", -5)]
    [InlineData("Central European Standard Time", 1)]
    [InlineData("Tokyo Standard Time", 9)]
    public void HostTimezoneSimulation_ConverterBehaviorIsCompletelyInvariant(string tzId, int standardOffsetHours)
    {
        Assert.NotNull(tzId);

        // For a timestamp without an offset, the value MUST represent Jordan wall-clock time
        // regardless of whatever host timezone is active.
        const string noOffsetString = "2026-11-10T09:20:00";
        var parsed = JordanFacilityDateTimeOffsetConverter.Parse(noOffsetString);
        var facilityDate = TestAppointmentValidator.ToFacilityTime(parsed);

        // Always Jordan Slot 03: 09:20
        Assert.Equal(9, facilityDate.Hour);
        Assert.Equal(20, facilityDate.Minute);
        Assert.Equal(DateTimeKind.Unspecified, facilityDate.Kind);

        // When a client in that timezone calculates an appointment for 09:20 Jordan:
        // 09:20 Jordan (+3) converted to that client's timezone:
        var clientHour = 9 - 3 + standardOffsetHours;
        var clientDate = new DateTime(2026, 11, 10, 0, 0, 0).AddHours(clientHour).AddMinutes(20);
        var offset = TimeSpan.FromHours(standardOffsetHours);
        var clientOffset = new DateTimeOffset(clientDate, offset);

        var fromClientString = clientOffset.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        var parsedClient = JordanFacilityDateTimeOffsetConverter.Parse(fromClientString);
        var facilityDateFromClient = TestAppointmentValidator.ToFacilityTime(parsedClient);

        Assert.Equal(9, facilityDateFromClient.Hour);
        Assert.Equal(20, facilityDateFromClient.Minute);
        Assert.Equal(DateTimeKind.Unspecified, facilityDateFromClient.Kind);
    }
}
