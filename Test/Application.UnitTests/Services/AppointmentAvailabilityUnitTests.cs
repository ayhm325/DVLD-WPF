using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;
using Xunit;

namespace Application.UnitTests.Services;

public sealed class AppointmentAvailabilityUnitTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITestAppointmentRepository> _repository = new();
    private readonly Mock<ITestTypeRepository> _testTypeRepository = new();
    private readonly Mock<ILocalDrivingLicenseApplicationService> _localApplicationService = new();
    private readonly Mock<IApplicationTypeService> _applicationTypeService = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ITestWorkflowService> _workflowService = new();
    private readonly Mock<ILogger<TestAppointmentService>> _logger = new();
    private readonly Common.FakeJordanClock _clock = new();

    private TestAppointmentService CreateService() =>
        new(
            _unitOfWork.Object,
            _repository.Object,
            _testTypeRepository.Object,
            _localApplicationService.Object,
            _applicationTypeService.Object,
            _currentUserService.Object,
            _workflowService.Object,
            _applicationService.Object,
            _logger.Object,
            _clock);

    private static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
    {
        var currentJordan = Common.FakeJordanClock.DefaultJordanNow.Date.AddDays(2);
        while (currentJordan.DayOfWeek != dayOfWeek)
        {
            currentJordan = currentJordan.AddDays(1);
        }
        return currentJordan;
    }

    [Fact]
    public async Task GetAvailability_01_ValidApplicationAndEligibleTestType_ReturnsAvailableWorkingDaysAndSlots()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var thursday = sunday.AddDays(4);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), new HashSet<DateTime>(), (DateTime?)null));

        // Act
        var result = await service.GetAvailabilityAsync(1, 1, sunday, thursday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.LocalAppId);
        Assert.Equal(1, result.Value.TestTypeId);
        Assert.Equal(5, result.Value.AvailableDays.Count); // Sun, Mon, Tue, Wed, Thu
        Assert.All(result.Value.AvailableDays, d =>
        {
            var parsed = DateTime.Parse(d.Date);
            Assert.NotEqual(DayOfWeek.Friday, parsed.DayOfWeek);
            Assert.NotEqual(DayOfWeek.Saturday, parsed.DayOfWeek);
            Assert.Equal(12, d.Slots.Count);
        });
    }

    [Fact]
    public async Task GetAvailability_02_InvalidLocalApplication_ReturnsValidationOrNotFound()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        // Sub-scenario A: localAppId <= 0
        var valResult = await service.GetAvailabilityAsync(0, 1, sunday, sunday.AddDays(1));
        Assert.True(valResult.IsFailure);
        Assert.Equal(ErrorType.Validation, valResult.ErrorType);

        // Sub-scenario B: local application does not exist
        _workflowService
            .Setup(w => w.CanScheduleTestAsync(999, TestTypeEnum.Theory))
            .ReturnsAsync(Result.NotFound("Local driving license application not found."));

        var notFoundResult = await service.GetAvailabilityAsync(999, 1, sunday, sunday.AddDays(1));
        Assert.True(notFoundResult.IsFailure);
        Assert.Equal(ErrorType.NotFound, notFoundResult.ErrorType);
        Assert.Contains("not found", notFoundResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAvailability_03_InvalidTestType_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        // Act
        var resultZero = await service.GetAvailabilityAsync(1, 0, sunday, sunday.AddDays(1));
        var resultInvalid = await service.GetAvailabilityAsync(1, 99, sunday, sunday.AddDays(1));

        // Assert
        Assert.True(resultZero.IsFailure);
        Assert.Equal(ErrorType.Validation, resultZero.ErrorType);
        Assert.True(resultInvalid.IsFailure);
        Assert.Equal(ErrorType.Validation, resultInvalid.ErrorType);
        Assert.Contains("Invalid test type", resultInvalid.Error);
    }

    [Fact]
    public async Task GetAvailability_04_TestTypeNotCurrentlyEligible_ReturnsConflict()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Written))
            .ReturnsAsync(Result.Conflict("The Written test cannot be scheduled yet. The next required test is Theory."));

        // Act
        var result = await service.GetAvailabilityAsync(1, (int)TestTypeEnum.Written, sunday, sunday.AddDays(1));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Contains("next required test is Theory", result.Error);
    }

    [Fact]
    public async Task GetAvailability_05_WeekendExclusion_ExcludesFridayAndSaturday()
    {
        // Arrange
        var service = CreateService();
        var friday = GetNextWeekday(DayOfWeek.Friday);
        var saturday = friday.AddDays(1);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), new HashSet<DateTime>(), (DateTime?)null));

        // Act
        var result = await service.GetAvailabilityAsync(1, 1, friday, saturday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.AvailableDays);
    }

    [Fact]
    public async Task GetAvailability_06_All12ValidTimeSlots_ReturnedWithExactTimes()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), new HashSet<DateTime>(), (DateTime?)null));

        var expectedStartTimes = new[]
        {
            "08:00", "08:40", "09:20", "10:00", "10:40", "11:20",
            "12:00", "12:40", "13:20", "14:00", "14:40", "15:20"
        };
        var expectedEndTimes = new[]
        {
            "08:40", "09:20", "10:00", "10:40", "11:20", "12:00",
            "12:40", "13:20", "14:00", "14:40", "15:20", "16:00"
        };

        // Act
        var result = await service.GetAvailabilityAsync(1, 1, sunday, sunday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        var day = result.Value.AvailableDays[0];
        Assert.Equal(12, day.Slots.Count);
        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(expectedStartTimes[i], day.Slots[i].StartTime);
            Assert.Equal(expectedEndTimes[i], day.Slots[i].EndTime);
        }
    }

    [Fact]
    public async Task GetAvailability_07_SameTestTypeSlotOccupancy_ExcludesOccupiedSlot()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var occupiedSlot = sunday.Add(new TimeSpan(8, 0, 0));

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime> { occupiedSlot }, new HashSet<DateTime>(), (DateTime?)null));

        // Act
        var result = await service.GetAvailabilityAsync(1, 1, sunday, sunday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        var slots = result.Value.AvailableDays[0].Slots;
        Assert.Equal(11, slots.Count);
        Assert.DoesNotContain(slots, s => s.StartTime == "08:00");
        Assert.Contains(slots, s => s.StartTime == "08:40");
    }

    [Fact]
    public async Task GetAvailability_08_DifferentTestTypesSharingSameTime_AllowsSlot()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        // Practical (Type 3) is booked at 08:00, but occupiedSlots queried for Theory (Type 1) is empty!
        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), new HashSet<DateTime>(), (DateTime?)null));

        // Act
        var result = await service.GetAvailabilityAsync(1, 1, sunday, sunday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        var slots = result.Value.AvailableDays[0].Slots;
        Assert.Equal(12, slots.Count);
        Assert.Contains(slots, s => s.StartTime == "08:00");
    }

    [Fact]
    public async Task GetAvailability_09_SameApplicantAppointmentOnAnotherApplication_ExcludesEntireDay()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);
        var monday = sunday.AddDays(1);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        // Applicant already has an appointment on Sunday (e.g. from another app)
        var applicantDates = new HashSet<DateTime> { sunday.Date };

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), applicantDates, (DateTime?)null));

        // Act
        var result = await service.GetAvailabilityAsync(1, 1, sunday, monday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        Assert.Equal(monday.ToString("yyyy-MM-dd"), result.Value.AvailableDays[0].Date);
        Assert.DoesNotContain(result.Value.AvailableDays, d => d.Date == sunday.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task GetAvailability_10_PreviousTestFailure_EarliestEligibleDateIsFirstWorkingDayAfter()
    {
        // Arrange
        var service = CreateService();
        var thursday = GetNextWeekday(DayOfWeek.Thursday);
        var sunday = thursday.AddDays(3);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        // Previous failed test occurred on Thursday
        var lastTestDate = thursday.Add(new TimeSpan(10, 0, 0));

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), new HashSet<DateTime>(), (DateTime?)lastTestDate));

        // Act: range from Thursday to following Sunday
        var result = await service.GetAvailabilityAsync(1, 1, thursday, sunday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        // Thursday is excluded (test day), Fri & Sat are excluded (weekend), Sunday is the first eligible day!
        Assert.Equal(sunday.ToString("yyyy-MM-dd"), result.Value.AvailableDays[0].Date);
    }

    [Fact]
    public async Task GetAvailability_11_PreviousTestPass_EarliestEligibleDateIsFirstWorkingDayAfter()
    {
        // Arrange
        var service = CreateService();
        var thursday = GetNextWeekday(DayOfWeek.Thursday);
        var sunday = thursday.AddDays(3);

        // Passed Theory on Thursday, now scheduling Written
        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Written))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 2))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        // Theory passed on Thursday
        var lastTestDate = thursday.Add(new TimeSpan(9, 20, 0));

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(2, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), new HashSet<DateTime>(), (DateTime?)lastTestDate));

        // Act: range from Thursday to Sunday
        var result = await service.GetAvailabilityAsync(1, (int)TestTypeEnum.Written, thursday, sunday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AvailableDays);
        Assert.Equal(sunday.ToString("yyyy-MM-dd"), result.Value.AvailableDays[0].Date);
    }

    [Fact]
    public async Task GetAvailability_12_EmptyAvailabilityResult_WhenAllSlotsOccupied()
    {
        // Arrange
        var service = CreateService();
        var sunday = GetNextWeekday(DayOfWeek.Sunday);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        // All 12 slots on Sunday are occupied
        var allSlots = TestAppointmentValidator.ValidSlotTimes
            .Select(t => sunday.Add(t))
            .ToHashSet();

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((allSlots, new HashSet<DateTime>(), (DateTime?)null));

        // Act
        var result = await service.GetAvailabilityAsync(1, 1, sunday, sunday);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.AvailableDays);
    }

    [Fact]
    public async Task GetAvailability_WhenRangeIsTodayAndTomorrow_ReturnsEmptyAvailableDaysWithoutSlots()
    {
        // Arrange
        var service = CreateService();
        var jordanNow = _clock.Now;
        var today = jordanNow.Date;
        var tomorrow = today.AddDays(1);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        // Act - request window covering only today and tomorrow (< 2 days advance)
        var result = await service.GetAvailabilityAsync(1, 1, today, tomorrow);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.AvailableDays);
    }

    [Fact]
    public async Task GetAvailability_WhenRangeIncludesToday_NeverReturnsTodayOrTomorrow()
    {
        // Arrange
        var service = CreateService();
        var jordanNow = _clock.Now;
        var today = jordanNow.Date;
        var tomorrow = today.AddDays(1);
        var twoDaysAhead = today.AddDays(2);
        var eightDaysAhead = today.AddDays(8);

        _workflowService
            .Setup(w => w.CanScheduleTestAsync(1, TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(r => r.IsAppointmentAlreadyScheduledAsync(1, 1))
            .ReturnsAsync(false);

        _repository
            .Setup(r => r.GetApplicantPersonIdByLocalAppIdAsync(1))
            .ReturnsAsync(10);

        _repository
            .Setup(r => r.GetAvailabilityDataAsync(1, 10, 1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((new HashSet<DateTime>(), new HashSet<DateTime>(), (DateTime?)null));

        // Act - request starting from today
        var result = await service.GetAvailabilityAsync(1, 1, today, eightDaysAhead);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.AvailableDays);

        var todayStr = today.ToString("yyyy-MM-dd");
        var tomorrowStr = tomorrow.ToString("yyyy-MM-dd");

        Assert.DoesNotContain(result.Value.AvailableDays, d => d.Date == todayStr);
        Assert.DoesNotContain(result.Value.AvailableDays, d => d.Date == tomorrowStr);

        // All returned dates must be at least today + 2 days and not Friday or Saturday
        foreach (var day in result.Value.AvailableDays)
        {
            var parsed = DateTime.Parse(day.Date);
            Assert.True((parsed.Date - today).Days >= 2);
            Assert.NotEqual(DayOfWeek.Friday, parsed.DayOfWeek);
            Assert.NotEqual(DayOfWeek.Saturday, parsed.DayOfWeek);
        }
    }
}
