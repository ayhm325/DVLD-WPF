using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public class LicenseQueryServiceRealLifecycleTests
{
    private readonly Mock<ILicenseRepository> _licenseRepo = new();
    private readonly Mock<ILocalDrivingLicenseApplicationService> _localAppService = new();
    private readonly Mock<IDetainedLicenseRepository> _detainedRepo = new();
    private readonly Mock<IJordanClock> _clock = new();

    public LicenseQueryServiceRealLifecycleTests()
    {
        // Fixed Jordan date: 2026-11-04
        _clock.SetupGet(x => x.Now).Returns(new DateTime(2026, 11, 4, 1, 0, 0, DateTimeKind.Utc));
    }

    private LicenseQueryService CreateSut() =>
        new(_licenseRepo.Object, _localAppService.Object, _detainedRepo.Object, _clock.Object);

    [Fact]
    public async Task GetLicenseDetailsById_WhenLicense21_NaturallyExpiredNeverRenewed_IsEligible()
    {
        // License #21: Driver 13, Class 3, Expired 2023-01-01, IsActive = false
        var lic21 = new License
        {
            LicenseID = 21,
            DriverID = 13,
            LicenseClass = 3,
            IssueDate = new DateTime(2018, 1, 1),
            ExpirationDate = new DateTime(2023, 1, 1),
            IsActive = false,
            IssueReason = IssueReason.FirstTime,
            Driver = new Driver { DriverID = 13, PersonID = 13, Person = new Person { FirstName = "Driver", LastName = "13", NationalNo = "N13" } },
            LicenseClassInfo = new LicenseClass { LicenseClassID = 3, ClassName = "Class 3", ClassFees = 50 }
        };

        _licenseRepo.Setup(x => x.GetLicenseByIdAsync(21)).ReturnsAsync(lic21);
        _detainedRepo.Setup(x => x.IsLicenseDetainedAsync(21)).ReturnsAsync(false);
        _licenseRepo.Setup(x => x.GetNewerLicenseForDriverAndClassAsync(13, 3, 21)).ReturnsAsync((License?)null);

        var sut = CreateSut();
        var result = await sut.GetLicenseDetailsByIdAsync(21);

        Assert.True(result.IsSuccess);
        var details = result.Value!;
        Assert.False(details.IsActive);
        Assert.True(details.IsExpired);
        Assert.True(details.IsEligibleForRenewal);
        Assert.Null(details.IneligibilityReason);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenLicense3_AlreadyRenewedInto4_IsIneligible()
    {
        // License #3: Driver 3, Class 3, Expired 2025-01-01, already renewed into License #4
        var lic3 = new License
        {
            LicenseID = 3,
            DriverID = 3,
            LicenseClass = 3,
            IssueDate = new DateTime(2015, 1, 1),
            ExpirationDate = new DateTime(2025, 1, 1),
            IsActive = false,
            IssueReason = IssueReason.FirstTime,
            Driver = new Driver { DriverID = 3, PersonID = 3, Person = new Person { FirstName = "Driver", LastName = "3", NationalNo = "N3" } },
            LicenseClassInfo = new LicenseClass { LicenseClassID = 3, ClassName = "Class 3", ClassFees = 50 }
        };

        var lic4 = new License
        {
            LicenseID = 4,
            DriverID = 3,
            LicenseClass = 3,
            IssueDate = new DateTime(2026, 11, 3),
            ExpirationDate = new DateTime(2036, 11, 3),
            IsActive = true,
            IssueReason = IssueReason.Renew
        };

        _licenseRepo.Setup(x => x.GetLicenseByIdAsync(3)).ReturnsAsync(lic3);
        _detainedRepo.Setup(x => x.IsLicenseDetainedAsync(3)).ReturnsAsync(false);
        _licenseRepo.Setup(x => x.GetNewerLicenseForDriverAndClassAsync(3, 3, 3)).ReturnsAsync(lic4);

        var sut = CreateSut();
        var result = await sut.GetLicenseDetailsByIdAsync(3);

        Assert.True(result.IsSuccess);
        var details = result.Value!;
        Assert.False(details.IsActive);
        Assert.True(details.IsExpired);
        Assert.False(details.IsEligibleForRenewal);
        Assert.Equal("This license was already renewed (License #4).", details.IneligibilityReason);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenLicenseIsCurrentlyDetained_IsIneligible()
    {
        var lic = new License
        {
            LicenseID = 99,
            DriverID = 50,
            LicenseClass = 3,
            IssueDate = new DateTime(2015, 1, 1),
            ExpirationDate = new DateTime(2025, 1, 1),
            IsActive = false,
            IssueReason = IssueReason.FirstTime,
            Driver = new Driver { DriverID = 50, PersonID = 50, Person = new Person { FirstName = "Driver", LastName = "50", NationalNo = "N50" } },
            LicenseClassInfo = new LicenseClass { LicenseClassID = 3, ClassName = "Class 3", ClassFees = 50 }
        };

        _licenseRepo.Setup(x => x.GetLicenseByIdAsync(99)).ReturnsAsync(lic);
        _detainedRepo.Setup(x => x.IsLicenseDetainedAsync(99)).ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.GetLicenseDetailsByIdAsync(99);

        Assert.True(result.IsSuccess);
        var details = result.Value!;
        Assert.False(details.IsEligibleForRenewal);
        Assert.Equal("Cannot renew a currently detained license. The license must be released first.", details.IneligibilityReason);
    }

    [Fact]
    public async Task GetLicenseDetailsById_WhenLicenseNotYetExpired_IsIneligible()
    {
        var lic = new License
        {
            LicenseID = 101,
            DriverID = 60,
            LicenseClass = 3,
            IssueDate = new DateTime(2026, 1, 1),
            ExpirationDate = new DateTime(2036, 1, 1), // in future
            IsActive = true,
            IssueReason = IssueReason.FirstTime,
            Driver = new Driver { DriverID = 60, PersonID = 60, Person = new Person { FirstName = "Driver", LastName = "60", NationalNo = "N60" } },
            LicenseClassInfo = new LicenseClass { LicenseClassID = 3, ClassName = "Class 3", ClassFees = 50 }
        };

        _licenseRepo.Setup(x => x.GetLicenseByIdAsync(101)).ReturnsAsync(lic);

        var sut = CreateSut();
        var result = await sut.GetLicenseDetailsByIdAsync(101);

        Assert.True(result.IsSuccess);
        var details = result.Value!;
        Assert.True(details.IsActive);
        Assert.False(details.IsExpired);
        Assert.False(details.IsEligibleForRenewal);
        Assert.Equal("License is not expired yet. Renewal is only permitted starting the calendar day after expiration.", details.IneligibilityReason);
    }
}
