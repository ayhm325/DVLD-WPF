using Application.Common.Results;
using Application.Common.Time;
using Application.DTOs.DriverDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

namespace Application.UnitTests.Services;

public sealed class ReadOperationsOptimizationTests
{
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<ILocalDrivingLicenseApplicationService> _localApplicationService = new();
    private readonly Mock<IDetainedLicenseRepository> _detainedRepository = new();
    private readonly Mock<IJordanClock> _clock = new();

    public ReadOperationsOptimizationTests()
    {
        _clock.SetupGet(x => x.Now).Returns(new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Unspecified));
        _clock.SetupGet(x => x.Today).Returns(new DateOnly(2026, 11, 4));
        _detainedRepository.Setup(x => x.GetActiveDetainedLicenseIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        _detainedRepository.Setup(x => x.IsLicenseDetainedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private LicenseQueryService CreateQueryService() =>
        new(
            _licenseRepository.Object,
            _localApplicationService.Object,
            _detainedRepository.Object,
            _clock.Object);

    // =========================================================================
    // Target A: Pure Read Operations & Dynamic In-Memory Evaluation
    // =========================================================================

    [Fact]
    public async Task GetByIdAsync_WhenLicenseIsExpiredButActiveInDatabase_ReturnsIsActiveFalseAndIsExpiredTrue_WithoutCallingDeactivate()
    {
        // Arrange: License is still marked IsActive = true in database, but ExpirationDate was yesterday
        var expiredLicense = new License
        {
            LicenseID = 101,
            DriverID = 5,
            LicenseClass = 3,
            IsActive = true, // Still active in DB before background sync runs
            IssueDate = new DateTime(2016, 11, 3),
            ExpirationDate = new DateTime(2026, 11, 3) // Expired on 2026-11-03 Jordan cutoff
        };

        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredLicense);

        var service = CreateQueryService();

        // Act
        var result = await service.GetByIdAsync(101);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsExpired, "License must be reported as expired.");
        Assert.False(result.Value.IsActive, "Expired license must NEVER be presented as active.");

        // Crucial verification: GET request must NEVER execute write/deactivate operations
        _licenseRepository.Verify(x => x.DeactivateExpiredLicensesAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenExpiredLicensesPresent_ReturnsIsActiveFalseWithoutDatabaseDeactivation()
    {
        // Arrange
        var licenses = new List<License>
        {
            new()
            {
                LicenseID = 1,
                DriverID = 10,
                LicenseClass = 1,
                IsActive = true,
                IssueDate = new DateTime(2026, 1, 1),
                ExpirationDate = new DateTime(2036, 1, 1) // Active & Valid
            },
            new()
            {
                LicenseID = 2,
                DriverID = 11,
                LicenseClass = 1,
                IsActive = true,
                IssueDate = new DateTime(2016, 1, 1),
                ExpirationDate = new DateTime(2026, 1, 1) // Expired but DB flag is still 1
            }
        };

        _licenseRepository.Setup(x => x.GetAllLicensesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenses);

        var service = CreateQueryService();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        var validDto = result.Value.First(x => x.LicenseID == 1);
        Assert.True(validDto.IsActive);
        Assert.False(validDto.IsExpired);

        var expiredDto = result.Value.First(x => x.LicenseID == 2);
        Assert.False(expiredDto.IsActive, "Expired license must dynamically report IsActive = false.");
        Assert.True(expiredDto.IsExpired, "Expired license must dynamically report IsExpired = true.");

        // Pure read: no DeactivateExpiredLicensesAsync call
        _licenseRepository.Verify(x => x.DeactivateExpiredLicensesAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void LicenseMapper_ToDto_WhenLicenseExpired_DynamicallySetsIsActiveFalse()
    {
        var license = new License
        {
            LicenseID = 50,
            DriverID = 1,
            IsActive = true,
            ExpirationDate = new DateTime(2026, 1, 1)
        };

        // Even though license.IsActive == true in entity, when isExpired is true, dto.IsActive must be false
        var dto = LicenseMapper.ToDto(license, isExpired: true, isEligibleForRenewal: true);

        Assert.False(dto.IsActive);
        Assert.True(dto.IsExpired);
        Assert.True(dto.IsEligibleForRenewal);
    }

    // =========================================================================
    // Target B: Driver Active License Count & DriverMapper Fallback
    // =========================================================================

    [Fact]
    public void DriverMapper_WhenActiveLicensesCountIsPrecomputed_UsesPrecomputedCount()
    {
        var driver = new Driver
        {
            DriverID = 1,
            PersonID = 10,
            ActiveLicensesCount = 3, // Precomputed by SQL scalar subquery
            Licenses = new List<License>() // Navigation not loaded
        };

        var dto = DriverMapper.ToDto(driver);

        Assert.Equal(3, dto.ActiveLicenses);
    }

    [Fact]
    public void DriverMapper_WhenActiveLicensesCountIsZero_ReportsZero()
    {
        var driver = new Driver
        {
            DriverID = 2,
            PersonID = 20,
            ActiveLicensesCount = 0,
            Licenses = new List<License>()
        };

        var dto = DriverMapper.ToDto(driver);

        Assert.Equal(0, dto.ActiveLicenses);
    }

    [Fact]
    public void DriverMapper_WhenActiveLicensesCountIsNull_FallsBackToCountingLoadedLicenses()
    {
        // When ActiveLicensesCount is null (e.g. in legacy unit tests where entity.Licenses was mocked)
        var driver = new Driver
        {
            DriverID = 3,
            PersonID = 30,
            ActiveLicensesCount = null,
            Licenses = new List<License>
            {
                new() { LicenseID = 1, IsActive = true },
                new() { LicenseID = 2, IsActive = true },
                new() { LicenseID = 3, IsActive = false } // Inactive license excluded
            }
        };

        var dto = DriverMapper.ToDto(driver);

        Assert.Equal(2, dto.ActiveLicenses);
    }

    [Fact]
    public void DriverMapper_WhenUsedThroughLicenseMapper_ReportsZeroWhenLicensesNotLoaded()
    {
        var driver = new Driver
        {
            DriverID = 4,
            PersonID = 40,
            ActiveLicensesCount = null,
            Licenses = new List<License>() // Empty collection when loaded via LicenseRepository
        };

        var license = new License
        {
            LicenseID = 100,
            DriverID = 4,
            Driver = driver,
            IsActive = true,
            ExpirationDate = new DateTime(2028, 1, 1)
        };

        var licenseDto = LicenseMapper.ToDto(license);

        Assert.NotNull(licenseDto.Driver);
        Assert.Equal(0, licenseDto.Driver.ActiveLicenses);
    }
}
