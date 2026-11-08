using System.Data;
using Application.Common.Results;
using Application.Common.Time;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DetainedLicenseDTO;
using Application.DTOs.DriverDTO;
using Application.DTOs.LicenseDTO;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.UnitTests.Common;

public sealed class JordanClockTimeStandardizationTests
{
    // =========================================================================
    // 1. CROSS-PLATFORM TIMEZONE RESOLUTION (WINDOWS / LINUX / CONTAINER)
    // =========================================================================

    [Fact]
    public void ResolveTimeZone_WhenWindowsIdExists_ReturnsWindowsZone()
    {
        var expected = TimeZoneInfo.CreateCustomTimeZone(
            JordanTimeZoneProvider.WindowsTimeZoneId,
            TimeSpan.FromHours(3),
            "Windows Amman",
            "Windows Amman");

        var resolved = JordanTimeZoneProvider.ResolveTimeZone(id =>
            id == JordanTimeZoneProvider.WindowsTimeZoneId
                ? expected
                : throw new TimeZoneNotFoundException(id));

        Assert.Same(expected, resolved);
    }

    [Fact]
    public void ResolveTimeZone_WhenWindowsIdMissing_FallsBackToIanaAsiaAmman()
    {
        var expected = TimeZoneInfo.CreateCustomTimeZone(
            JordanTimeZoneProvider.IanaTimeZoneId,
            TimeSpan.FromHours(3),
            "IANA Amman",
            "IANA Amman");

        var resolved = JordanTimeZoneProvider.ResolveTimeZone(id =>
            id == JordanTimeZoneProvider.IanaTimeZoneId
                ? expected
                : throw new TimeZoneNotFoundException(id));

        Assert.Same(expected, resolved);
    }

    [Fact]
    public void ResolveTimeZone_WhenOsTzDataMissingOrCorrupt_FallsBackToExplicitFixedUtcPlus3AndNeverLocal()
    {
        var resolvedMissing = JordanTimeZoneProvider.ResolveTimeZone(
            id => throw new TimeZoneNotFoundException(id));

        var resolvedInvalid = JordanTimeZoneProvider.ResolveTimeZone(
            id => throw new InvalidTimeZoneException(id));

        Assert.Equal(TimeSpan.FromHours(3), resolvedMissing.BaseUtcOffset);
        Assert.False(resolvedMissing.SupportsDaylightSavingTime);
        Assert.Equal(TimeSpan.FromHours(3), resolvedInvalid.BaseUtcOffset);
        Assert.False(resolvedInvalid.SupportsDaylightSavingTime);

        var winterUtc = new DateTime(2026, 1, 15, 21, 30, 0, DateTimeKind.Utc);
        var summerUtc = new DateTime(2026, 7, 15, 21, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 1, 16, 0, 30, 0), TimeZoneInfo.ConvertTimeFromUtc(winterUtc, resolvedMissing));
        Assert.Equal(new DateTime(2026, 7, 16, 0, 30, 0), TimeZoneInfo.ConvertTimeFromUtc(summerUtc, resolvedMissing));
    }

    [Fact]
    public void ProductionJordanClock_ExposesConsistentKindsAndValues()
    {
        var clock = new JordanClock();

        var utcBefore = DateTime.UtcNow;
        var jordanNow = clock.Now;
        var jordanToday = clock.Today;
        var clockUtc = clock.UtcNow;
        var utcAfter = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Unspecified, jordanNow.Kind);
        Assert.Equal(DateTimeKind.Utc, clockUtc.Kind);
        Assert.InRange(clockUtc, utcBefore, utcAfter);

        var expectedMinJordan = JordanTimeZoneProvider.ConvertUtcToJordan(utcBefore);
        var expectedMaxJordan = JordanTimeZoneProvider.ConvertUtcToJordan(utcAfter);
        Assert.InRange(jordanNow, expectedMinJordan, expectedMaxJordan);
        Assert.InRange(
            jordanToday,
            DateOnly.FromDateTime(expectedMinJordan),
            DateOnly.FromDateTime(expectedMaxJordan));
    }

    // =========================================================================
    // 2. UTC / JORDAN MIDNIGHT DIVERGENCE (21:00–23:59 UTC = NEXT DAY IN JORDAN)
    // =========================================================================

    [Fact]
    public void FakeJordanClock_AroundJordanMidnight_SwitchesCalendarDateAt2100Utc()
    {
        // 2026-11-03 20:59:59Z -> 2026-11-03 23:59:59 Jordan
        var clock = FakeJordanClock.FromUtc(new DateTime(2026, 11, 3, 20, 59, 59, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 11, 3, 23, 59, 59, DateTimeKind.Unspecified), clock.Now);
        Assert.Equal(DateTimeKind.Unspecified, clock.Now.Kind);
        Assert.Equal(new DateOnly(2026, 11, 3), clock.Today);

        // Advance 1 second -> 2026-11-03 21:00:00Z -> 2026-11-04 00:00:00 Jordan
        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(new DateTime(2026, 11, 3, 21, 0, 0, DateTimeKind.Utc), clock.UtcNow);
        Assert.Equal(new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Unspecified), clock.Now);
        Assert.Equal(new DateOnly(2026, 11, 4), clock.Today);
    }

    // =========================================================================
    // 3. OFFICIAL DVLD LICENSE EXPIRATION RULE & PRECEDENCE OVER !IsActive
    // =========================================================================

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(14, 30, 0)]
    [InlineData(23, 59, 59)]
    public void LicenseExpirationPolicy_RemainsValidThroughoutExpirationCalendarDayAndExpiresAtNextMidnight(
        int expHour,
        int expMinute,
        int expSecond)
    {
        var expirationDate = new DateTime(2026, 11, 4, expHour, expMinute, expSecond, DateTimeKind.Unspecified);

        // Valid at 23:59:59 on 2026-11-04 Jordan
        Assert.False(LicenseExpirationPolicy.IsExpired(expirationDate, new DateOnly(2026, 11, 4)));
        Assert.True(LicenseExpirationPolicy.IsValidOn(expirationDate, new DateOnly(2026, 11, 4)));

        // Expired at 00:00:00 on 2026-11-05 Jordan (which is 2026-11-04 21:00:00Z UTC!)
        var clockAtJordanMidnight = FakeJordanClock.FromUtc(
            new DateTime(2026, 11, 4, 21, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateOnly(2026, 11, 5), clockAtJordanMidnight.Today);
        Assert.True(LicenseExpirationPolicy.IsExpired(expirationDate, clockAtJordanMidnight.Today));
    }

    [Fact]
    public async Task InternationalService_AroundJordanMidnight_FollowsOfficialJordanCalendarExpirationAndPrecedence()
    {
        var repo = new Mock<IInternationalRepository>();
        var licenseQuery = new Mock<ILicenseQueryService>();
        var appService = new Mock<IApplicationService>();
        var appTypeService = new Mock<IApplicationTypeService>();
        var currentUser = new Mock<ICurrentUserService>();
        var uow = new Mock<IUnitOfWork>();
        var tx = new Mock<IUnitOfWorkTransaction>();
        var logger = new Mock<ILogger<InternationalService>>();

        currentUser.SetupGet(x => x.IsLoggedIn).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(7);
        appTypeService.Setup(x => x.GetApplicationTypeByIdAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApplicationTypeDto>.Success(new ApplicationTypeDto { ApplicationTypeId = 6 }));
        uow.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
                IsolationLevel.Serializable,
                It.IsAny<CancellationToken>()))
            .Returns((Func<IUnitOfWorkTransaction, Task<Result<int>>> op, IsolationLevel _, CancellationToken __) =>
                op(tx.Object));
        repo.Setup(x => x.ExistsByLocalLicenseAsync(100)).ReturnsAsync(false);
        repo.Setup(x => x.HasActiveInternationalLicenseAsync(20, It.IsAny<DateOnly>())).ReturnsAsync(false);

        // Local license expiring on 2026-11-04 at 09:00:00 (morning timestamp)
        var localLicense = new LicenseDto
        {
            LicenseID = 100,
            DriverID = 20,
            LicenseClassID = 3,
            LicenseClassName = "Class 3",
            IssueDate = new DateTime(2016, 11, 4, 9, 0, 0),
            ExpirationDate = new DateTime(2026, 11, 4, 9, 0, 0),
            IsActive = true,
            Driver = new DriverDto { DriverID = 20, PersonID = 30, FullName = "Driver" }
        };
        licenseQuery.Setup(x => x.GetByIdAsync(100)).ReturnsAsync(Result<LicenseDto>.Success(localLicense));

        // Case A: At 2026-11-04 23:59:59 Jordan (20:59:59Z UTC), license is STILL VALID despite 09:00 timestamp!
        var clock = FakeJordanClock.FromUtc(new DateTime(2026, 11, 4, 20, 59, 59, DateTimeKind.Utc));
        var sut = new InternationalService(
            repo.Object, licenseQuery.Object, appService.Object, appTypeService.Object,
            currentUser.Object, uow.Object, logger.Object, clock);

        var infoValid = await sut.GetLocalLicenseInfoAsync(100);
        Assert.True(infoValid.IsSuccess);

        // Case B: 1 second later -> 2026-11-04 21:00:00Z UTC = 2026-11-05 00:00:00 Jordan -> EXPIRED!
        // Even if IsActive was already flipped to false by background sync, error MUST report "expired", not "not active"!
        clock.Advance(TimeSpan.FromSeconds(1));
        localLicense.IsActive = false;

        var infoExpired = await sut.GetLocalLicenseInfoAsync(100);
        Assert.True(infoExpired.IsFailure);
        Assert.Equal(ErrorType.Conflict, infoExpired.ErrorType);
        Assert.Equal("The local license is expired.", infoExpired.Error);

        var issueExpired = await sut.IssueInternationalLicenseAsync(100);
        Assert.True(issueExpired.IsFailure);
        Assert.Equal(ErrorType.Conflict, issueExpired.ErrorType);
        Assert.Equal("The local license is expired.", issueExpired.Error);
    }

    [Fact]
    public async Task ReplacementAndDetainServices_WhenLicenseIsExpiredAndInactive_ReportExpiredBeforeInactive()
    {
        var licenseRepo = new Mock<ILicenseRepository>();
        var detainedRepo = new Mock<IDetainedLicenseRepository>();
        var appService = new Mock<IApplicationService>();
        var appTypeService = new Mock<IApplicationTypeService>();
        var currentUser = new Mock<ICurrentUserService>();
        var uow = new Mock<IUnitOfWork>();
        var tx = new Mock<IUnitOfWorkTransaction>();

        currentUser.SetupGet(x => x.IsLoggedIn).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(1);
        appTypeService.Setup(x => x.GetApplicationTypeByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ApplicationTypeDto>.Success(new ApplicationTypeDto { ApplicationTypeId = 3 }));
        uow.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<IUnitOfWorkTransaction, Task<Result<int>>> op, IsolationLevel _, CancellationToken __) =>
                op(tx.Object));
        uow.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<DetainedLicenseDto>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<IUnitOfWorkTransaction, Task<Result<DetainedLicenseDto>>> op, IsolationLevel _, CancellationToken __) =>
                op(tx.Object));

        var expiredInactiveLicense = new License
        {
            LicenseID = 10,
            DriverID = 20,
            LicenseClass = 3,
            IsActive = false,
            IssueDate = new DateTime(2016, 11, 4),
            ExpirationDate = new DateTime(2026, 11, 4),
            Driver = new Driver { DriverID = 20, PersonID = 30 },
            LicenseClassInfo = new LicenseClass { LicenseClassID = 3, DefaultValidityLength = 10 }
        };
        licenseRepo.Setup(x => x.GetLicenseByIdAsync(10)).ReturnsAsync(expiredInactiveLicense);
        licenseRepo.Setup(x => x.GetForUpdateAsync(10)).ReturnsAsync(expiredInactiveLicense);

        // 2026-11-04 21:00:00Z = 2026-11-05 00:00:00 Jordan
        var clock = FakeJordanClock.FromUtc(new DateTime(2026, 11, 4, 21, 0, 0, DateTimeKind.Utc));

        var replacementService = new LicenseReplacementService(
            licenseRepo.Object, appService.Object, appTypeService.Object,
            currentUser.Object, uow.Object, Mock.Of<ILogger<LicenseReplacementService>>(), clock);

        var replaceResult = await replacementService.ReplaceLicenseAsync(10, "Damaged License");
        Assert.True(replaceResult.IsFailure);
        Assert.Equal("Cannot replace an expired license.", replaceResult.Error);

        var detainedService = new DetainedLicenseService(
            detainedRepo.Object, licenseRepo.Object, appService.Object, appTypeService.Object,
            uow.Object, currentUser.Object, Mock.Of<ILogger<DetainedLicenseService>>(), clock);

        var detainResult = await detainedService.AddAsync(new CreateDetainedLicenseDto { LicenseID = 10, FineFees = 25m });
        Assert.True(detainResult.IsFailure);
        Assert.Equal("An expired license cannot be detained.", detainResult.Error);
    }

    // =========================================================================
    // 4. APPLICATION TIMESTAMPS IN JORDAN LOCAL TIME (NO UTC DRIFT)
    // =========================================================================

    [Fact]
    public async Task ApplicationService_AroundJordanMidnight_StampsApplicationDateAndLastStatusDateInJordanTime()
    {
        var appRepo = new Mock<IApplicationRepository>();
        var appTypeRepo = new Mock<IApplicationTypeRepository>();
        var uow = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();

        currentUser.SetupGet(x => x.IsLoggedIn).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(10);
        appTypeRepo.Setup(x => x.GetApplicationTypeByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m, ApplicationTypeTitle = "New Local" });
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // 2026-11-03 21:30:00Z UTC = 2026-11-04 00:30:00 Jordan
        var clock = FakeJordanClock.FromUtc(new DateTime(2026, 11, 3, 21, 30, 0, DateTimeKind.Utc));
        var sut = new ApplicationService(appRepo.Object, appTypeRepo.Object, uow.Object, currentUser.Object, clock);

        ApplicationD? stored = null;
        appRepo.Setup(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>(), It.IsAny<CancellationToken>()))
            .Callback<ApplicationD, CancellationToken>((a, _) =>
            {
                a.ApplicationID = 500;
                stored = a;
            })
            .Returns(Task.CompletedTask);

        var createResult = await sut.AddNewApplicationAsync(new CreateApplicationDto
        {
            ApplicantPersonID = 99,
            ApplicationTypeID = 1
        });

        Assert.True(createResult.IsSuccess);
        Assert.NotNull(stored);
        Assert.Equal(new DateTime(2026, 11, 4, 0, 30, 0, DateTimeKind.Unspecified), stored!.ApplicationDate);
        Assert.Equal(new DateTime(2026, 11, 4, 0, 30, 0, DateTimeKind.Unspecified), stored.LastStatusDate);

        // Advance 5 minutes -> complete application -> LastStatusDate advances monotonically in Jordan time
        clock.Advance(TimeSpan.FromMinutes(5));
        appRepo.Setup(x => x.GetApplicationForUpdateAsync(500, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var completeResult = await sut.CompleteApplicationStateAsync(500);
        Assert.True(completeResult.IsSuccess);
        Assert.Equal(new DateTime(2026, 11, 4, 0, 35, 0, DateTimeKind.Unspecified), stored.LastStatusDate);
        Assert.True(stored.LastStatusDate >= stored.ApplicationDate);
    }

    // =========================================================================
    // 5. PERSON 18TH BIRTHDAY BOUNDARY AT JORDAN MIDNIGHT
    // =========================================================================

    [Fact]
    public void PersonValidator_EighteenthBirthdayBoundary_EvaluatesAgainstJordanCalendarDate()
    {
        var dto = new PersonCreateDto
        {
            NationalNo = "2008110499",
            FirstName = "Omar",
            SecondName = "Ali",
            LastName = "Hassan",
            // Born 2008-11-04 (even with an afternoon time component, calendar date governs)
            DateOfBirth = new DateTime(2008, 11, 4, 16, 45, 0),
            Gender = (int)Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            NationalityCountryID = 90
        };

        // At 2026-11-03 20:59:59Z UTC -> Jordan is 2026-11-03 23:59:59 -> still 17 years old!
        var clockBeforeMidnight = FakeJordanClock.FromUtc(new DateTime(2026, 11, 3, 20, 59, 59, DateTimeKind.Utc));
        var beforeResult = PersonValidator.Validate(dto, clockBeforeMidnight.Today);
        Assert.True(beforeResult.IsFailure);
        Assert.Contains("at least 18 years old", beforeResult.Error);

        // At 2026-11-03 21:00:00Z UTC -> Jordan is 2026-11-04 00:00:00 -> 18th birthday in Jordan!
        var clockAtJordanMidnight = FakeJordanClock.FromUtc(new DateTime(2026, 11, 3, 21, 0, 0, DateTimeKind.Utc));
        var afterResult = PersonValidator.Validate(dto, clockAtJordanMidnight.Today);
        Assert.True(afterResult.IsSuccess);
    }
}
