using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DetainedLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace Application.UnitTests.Services;

public sealed class LicenseLifecycleTimeBoundaryTests
{
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<IDetainedLicenseRepository> _detainedRepository = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<IApplicationTypeService> _applicationTypeService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<ILogger<LicenseRenewalService>> _renewalLogger = new();
    private readonly Mock<ILogger<LicenseReplacementService>> _replacementLogger = new();
    private readonly Mock<ILogger<DetainedLicenseService>> _detainedLogger = new();
    private readonly Mock<IJordanClock> _clock = new();

    private readonly DateTime _expirationDate = new(2026, 11, 3, 0, 0, 0);

    public LicenseLifecycleTimeBoundaryTests()
    {
        _clock.SetupGet(x => x.Today).Returns(() => DateOnly.FromDateTime(_clock.Object.Now));
        _currentUserService.SetupGet(x => x.IsLoggedIn).Returns(true);
        _currentUserService.SetupGet(x => x.UserId).Returns(1);

        _unitOfWork.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Func<IUnitOfWorkTransaction, Task<Result<int>>> op, IsolationLevel _, CancellationToken __) =>
                await op(_transaction.Object));

        _unitOfWork.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<DetainedLicenseDto>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Func<IUnitOfWorkTransaction, Task<Result<DetainedLicenseDto>>> op, IsolationLevel _, CancellationToken __) =>
                await op(_transaction.Object));

        _unitOfWork.Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Func<IUnitOfWorkTransaction, Task<Result>> op, IsolationLevel _, CancellationToken __) =>
                await op(_transaction.Object));

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _applicationTypeService.Setup(x => x.GetApplicationTypeByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => Result<ApplicationTypeDto>.Success(new ApplicationTypeDto
            {
                ApplicationTypeId = id,
                ApplicationTypeTitle = $"App Type {id}",
                ApplicationTypeFees = 10m
            }));

        _applicationService.Setup(x => x.AddNewApplicationAsync(It.IsAny<CreateApplicationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(100));

        _applicationService.Setup(x => x.CompleteApplicationStateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _licenseRepository.Setup(x => x.DeactivateLicenseAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _licenseRepository.Setup(x => x.ActivateLicenseAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _licenseRepository.Setup(x => x.AddLicenseAsync(It.IsAny<License>(), It.IsAny<CancellationToken>()))
            .Callback<License, CancellationToken>((l, _) => l.LicenseID = 101)
            .Returns(Task.CompletedTask);
        _detainedRepository.Setup(x => x.AddAsync(It.IsAny<DetainedLicense>(), It.IsAny<CancellationToken>()))
            .Callback<DetainedLicense, CancellationToken>((d, _) => d.DetainID = 5)
            .Returns(Task.CompletedTask);
        _detainedRepository.Setup(x => x.IsLicenseDetainedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }

    private LicenseRenewalService CreateRenewalService() =>
        new(_licenseRepository.Object, _detainedRepository.Object, _applicationService.Object, _applicationTypeService.Object,
            _currentUserService.Object, _unitOfWork.Object, _renewalLogger.Object, _clock.Object);

    private LicenseReplacementService CreateReplacementService() =>
        new(_licenseRepository.Object, _applicationService.Object, _applicationTypeService.Object,
            _currentUserService.Object, _unitOfWork.Object, _replacementLogger.Object, _clock.Object);

    private DetainedLicenseService CreateDetainedService() =>
        new(_detainedRepository.Object, _licenseRepository.Object, _applicationService.Object,
            _applicationTypeService.Object, _unitOfWork.Object, _currentUserService.Object, _detainedLogger.Object, _clock.Object);

    private License CreateStandardLicense(bool isActive = true) =>
        new()
        {
            LicenseID = 10,
            DriverID = 20,
            LicenseClass = 3,
            IssueDate = _expirationDate.AddYears(-10),
            ExpirationDate = _expirationDate,
            IsActive = isActive,
            Driver = new Driver { DriverID = 20, PersonID = 30 },
            LicenseClassInfo = new LicenseClass
            {
                LicenseClassID = 3,
                ClassName = "Ordinary Driving License",
                DefaultValidityLength = 10,
                ClassFees = 20m
            }
        };

    [Theory]
    [InlineData("2026-11-02T12:00:00", "One day before expiration")]
    [InlineData("2026-11-03T00:00:00", "Exact expiration date at 00:00 Jordan time")]
    [InlineData("2026-11-03T12:00:00", "Exact expiration date at 12:00 Jordan time")]
    [InlineData("2026-11-03T23:59:59", "Exact expiration date at 23:59:59 Jordan time")]
    public async Task WhenLicenseIsStillValidInJordanTime_RenewalProhibited_ReplacementAndDetentionAllowed(
        string jordanTimeString,
        string scenarioDescription)
    {
        var jordanNow = DateTime.Parse(jordanTimeString);
        _clock.SetupGet(x => x.Now).Returns(jordanNow);

        var license = CreateStandardLicense(isActive: true);
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);
        _licenseRepository.Setup(x => x.GetForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);

        // 1. Renewal must be prohibited (early renewal not allowed)
        var renewalService = CreateRenewalService();
        var renewalResult = await renewalService.RenewLicenseAsync(10, "Early renewal attempt");
        Assert.True(renewalResult.IsFailure, $"Failed for {scenarioDescription}");
        Assert.Equal(ErrorType.Conflict, renewalResult.ErrorType);
        Assert.Equal("Cannot renew before expiration date.", renewalResult.Error);

        // 2. Replacement must be allowed (license is active and not expired)
        var replacementService = CreateReplacementService();
        var replacementResult = await replacementService.ReplaceLicenseAsync(10, "Lost License");
        Assert.True(replacementResult.IsSuccess, $"Replacement should succeed for {scenarioDescription}");

        // 3. Detention must be allowed
        _detainedRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DetainedLicense { DetainID = 1, LicenseID = 10, FineFees = 50m });
        var detainedService = CreateDetainedService();
        var detainResult = await detainedService.AddAsync(new CreateDetainedLicenseDto { LicenseID = 10, FineFees = 50m });
        Assert.True(detainResult.IsSuccess, $"Detention should succeed for {scenarioDescription}");
    }

    [Theory]
    [InlineData("2026-11-04T00:00:00", "First moment of the following Jordan calendar day")]
    [InlineData("2026-11-04T08:30:00", "Morning of the following Jordan calendar day")]
    public async Task WhenLicenseHasExpiredInJordanTime_RenewalAllowed_ReplacementAndDetentionProhibited(
        string jordanTimeString,
        string scenarioDescription)
    {
        var jordanNow = DateTime.Parse(jordanTimeString);
        _clock.SetupGet(x => x.Now).Returns(jordanNow);

        var license = CreateStandardLicense(isActive: true);
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);
        _licenseRepository.Setup(x => x.GetForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);

        // 1. Renewal must be allowed
        var renewalService = CreateRenewalService();
        var renewalResult = await renewalService.RenewLicenseAsync(10, "Valid renewal");
        Assert.True(renewalResult.IsSuccess, $"Renewal should succeed for {scenarioDescription}");

        // 2. Replacement must be prohibited (cannot replace an expired license)
        var replacementService = CreateReplacementService();
        var replacementResult = await replacementService.ReplaceLicenseAsync(10, "Lost License");
        Assert.True(replacementResult.IsFailure, $"Replacement should fail for {scenarioDescription}");
        Assert.Equal(ErrorType.Conflict, replacementResult.ErrorType);
        Assert.Equal("Cannot replace an expired license.", replacementResult.Error);

        // 3. Detention must be prohibited (an expired license cannot be detained)
        var detainedService = CreateDetainedService();
        var detainResult = await detainedService.AddAsync(new CreateDetainedLicenseDto { LicenseID = 10, FineFees = 50m });
        Assert.True(detainResult.IsFailure, $"Detention should fail for {scenarioDescription}");
        Assert.Equal(ErrorType.Conflict, detainResult.ErrorType);
        Assert.Equal("An expired license cannot be detained.", detainResult.Error);
    }

    [Fact]
    public async Task WhenUtcDateDiffersFromJordanDate_JordanLocalDateIsAuthoritative()
    {
        // Scenario: UTC is 2026-11-03 22:30:00 (still Nov 3 in UTC)
        // Jordan is UTC+3 -> 2026-11-04 01:30:00 (already Nov 4 in Jordan)
        // License expiration date is 2026-11-03.
        // Under Jordan local time, the license is EXPIRED starting Nov 4.
        var jordanNow = new DateTime(2026, 11, 4, 1, 30, 0);
        _clock.SetupGet(x => x.Now).Returns(jordanNow);

        var license = CreateStandardLicense(isActive: true);
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);
        _licenseRepository.Setup(x => x.GetForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);

        // Renewal is allowed because it is Nov 4 in Jordan
        var renewalService = CreateRenewalService();
        var renewalResult = await renewalService.RenewLicenseAsync(10, "Renewal after midnight Jordan");
        Assert.True(renewalResult.IsSuccess);

        // Replacement is prohibited
        var replacementService = CreateReplacementService();
        var replacementResult = await replacementService.ReplaceLicenseAsync(10, "Lost License");
        Assert.True(replacementResult.IsFailure);
        Assert.Equal("Cannot replace an expired license.", replacementResult.Error);

        // Detention is prohibited
        var detainedService = CreateDetainedService();
        var detainResult = await detainedService.AddAsync(new CreateDetainedLicenseDto { LicenseID = 10, FineFees = 50m });
        Assert.True(detainResult.IsFailure);
        Assert.Equal("An expired license cannot be detained.", detainResult.Error);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_OnExpirationDate_ReactivatesLicense()
    {
        // Detention occurred previously. Release happens on 2026-11-03 16:00:00 (still valid on expiration date)
        var jordanNow = new DateTime(2026, 11, 3, 16, 0, 0);
        _clock.SetupGet(x => x.Now).Returns(jordanNow);

        var license = CreateStandardLicense(isActive: false);
        var detention = new DetainedLicense
        {
            DetainID = 5,
            LicenseID = 10,
            DetainDate = new DateTime(2026, 11, 1),
            FineFees = 50m,
            IsReleased = false
        };

        _detainedRepository.Setup(x => x.GetByIdForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(detention);
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);
        _licenseRepository.Setup(x => x.GetForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);
        _licenseRepository.Setup(x => x.HasAnotherActiveLicenseAsync(20, 3, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var detainedService = CreateDetainedService();
        var result = await detainedService.ReleaseAsync(new ReleaseDetainedLicenseDto { DetainID = 5 });

        Assert.True(result.IsSuccess);
        Assert.True(detention.IsReleased);
        Assert.Equal(jordanNow, detention.ReleaseDate);

        // Must reactivate since license has not expired
        _licenseRepository.Verify(x => x.ActivateLicenseAsync(10, DateOnly.FromDateTime(jordanNow), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReleaseDetainedLicense_OnDayAfterExpiration_DoesNotReactivateLicense()
    {
        // Detention occurred previously. Release happens on 2026-11-04 10:00:00 (expired on Nov 4)
        var jordanNow = new DateTime(2026, 11, 4, 10, 0, 0);
        _clock.SetupGet(x => x.Now).Returns(jordanNow);

        var license = CreateStandardLicense(isActive: false);
        var detention = new DetainedLicense
        {
            DetainID = 5,
            LicenseID = 10,
            DetainDate = new DateTime(2026, 11, 1),
            FineFees = 50m,
            IsReleased = false
        };

        _detainedRepository.Setup(x => x.GetByIdForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(detention);
        _licenseRepository.Setup(x => x.GetLicenseByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);
        _licenseRepository.Setup(x => x.GetForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(license);
        _licenseRepository.Setup(x => x.HasAnotherActiveLicenseAsync(20, 3, 10, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var detainedService = CreateDetainedService();
        var result = await detainedService.ReleaseAsync(new ReleaseDetainedLicenseDto { DetainID = 5 });

        Assert.True(result.IsSuccess);
        Assert.True(detention.IsReleased);
        Assert.Equal(jordanNow, detention.ReleaseDate);

        // Must NOT reactivate since license expired while detained
        _licenseRepository.Verify(x => x.ActivateLicenseAsync(10, DateOnly.FromDateTime(jordanNow), It.IsAny<CancellationToken>()), Times.Never);
    }
}
