using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace Application.UnitTests.Services;

public class LicenseRenewalServiceTests
{
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<IApplicationTypeService> _applicationTypeService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<LicenseRenewalService>> _logger = new();

    private LicenseRenewalService CreateSut()
    {
        return new LicenseRenewalService(
            _licenseRepository.Object,
            _applicationService.Object,
            _applicationTypeService.Object,
            _currentUserService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    // =========================================================
    // Validation / Authentication
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenLicenseIdIsInvalid_ReturnsValidationFailure()
    {
        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(0, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenUserIsNotAuthenticated_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(1);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("Authenticated user is required.", result.Error);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(It.IsAny<int>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenUserIdIsInvalid_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("Authenticated user is required.", result.Error);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    // =========================================================
    // Application Type
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenApplicationTypeFails_PropagatesFailure()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(2))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromNotFound(
                    "Application type not found."));

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Application type not found.", result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenApplicationTypeValueIsNull_ReturnsNotFound()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(2))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(null!));

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Renewal application type not found.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Old License Preconditions
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenOldLicenseDoesNotExist_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync((License?)null);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Old license not found.", result.Error);

        VerifyRollback(transaction);
        VerifyCommitNeverCalled(transaction);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenOldLicenseIsInactive_ReturnsConflictAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        oldLicense.IsActive = false;

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(oldLicense);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot renew an inactive license.",
            result.Error);

        VerifyRollback(transaction);

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(It.IsAny<CreateApplicationDto>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenOldLicenseHasNotExpired_ReturnsConflictAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        oldLicense.ExpirationDate = DateTime.UtcNow.AddDays(10);

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(oldLicense);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot renew before expiration date.",
            result.Error);

        VerifyRollback(transaction);

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(It.IsAny<CreateApplicationDto>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenDriverInformationIsMissing_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        oldLicense.Driver = null!;

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(oldLicense);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Driver information is not available.",
            result.Error);

        VerifyRollback(transaction);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenLicenseClassInformationIsMissing_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        oldLicense.LicenseClassInfo = null!;

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(oldLicense);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "License class information is not available.",
            result.Error);

        VerifyRollback(transaction);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenValidityPeriodIsInvalid_ReturnsValidationFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        oldLicense.LicenseClassInfo.DefaultValidityLength = 0;

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(oldLicense);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "License class has an invalid validity period.",
            result.Error);

        VerifyRollback(transaction);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenLicenseFeesAreInvalid_ReturnsValidationFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        oldLicense.LicenseClassInfo.ClassFees = -1;

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(oldLicense);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "License class has invalid fees.",
            result.Error);

        VerifyRollback(transaction);
    }

    // =========================================================
    // Renewal Application
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenCreatingApplicationFails_PropagatesFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.FromConflict(
                    "Application already exists."));

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Application already exists.",
            result.Error);

        VerifyRollback(transaction);

        _licenseRepository.Verify(
            x => x.DeactivateLicenseAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenCreatedApplicationIdIsInvalid_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(Result<int>.Success(0));

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to create renewal application.",
            result.Error);

        VerifyRollback(transaction);
    }

    // =========================================================
    // Deactivation
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenDeactivationFails_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        SetupSuccessfulRenewalApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(false);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to deactivate old license.",
            result.Error);

        VerifyRollback(transaction);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(It.IsAny<License>()),
            Times.Never);
    }

    // =========================================================
    // Save
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenSaveChangesFails_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        SetupSuccessfulRenewalApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(It.IsAny<License>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to save the renewed license.",
            result.Error);

        VerifyRollback(transaction);

        _applicationService.Verify(
            x => x.CompleteApplicationAsync(It.IsAny<int>()),
            Times.Never);
    }

    // =========================================================
    // Complete Application
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenCompletingApplicationFails_PropagatesFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        SetupSuccessfulRenewalApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(It.IsAny<License>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(
                Result.Conflict(
                    "Application could not be completed."));

        var newLicenseId = 500;

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = newLicenseId;
            })
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Application could not be completed.",
            result.Error);

        VerifyRollback(transaction);

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Successful Renewal
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenValid_ReturnsNewLicenseIdAndCommits()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        SetupSuccessfulRenewalApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(10, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(500, result.Value);

        _licenseRepository.Verify(
            x => x.DeactivateLicenseAsync(10),
            Times.Once);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.DriverID == oldLicense.DriverID &&
                    license.LicenseClass == oldLicense.LicenseClass &&
                    license.PaidFees == oldLicense.LicenseClassInfo.ClassFees &&
                    license.IssueReason == IssueReason.Renew &&
                    license.CreatedByUserID == 1 &&
                    license.Notes == null)),
            Times.Once);

        _applicationService.Verify(
            x => x.CompleteApplicationAsync(100),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenNotesContainWhitespace_TrimsNotes()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        SetupSuccessfulRenewalApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(
            10,
            "   Renewal notes   ");

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.Notes == "Renewal notes")),
            Times.Once);
    }

    [Fact]
    public async Task RenewLicenseAsync_WhenNotesAreWhitespace_SavesNullNotes()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        SetupSuccessfulRenewalApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.RenewLicenseAsync(
            10,
            "     ");

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.Notes == null)),
            Times.Once);
    }

    // =========================================================
    // Exception / Rollback
    // =========================================================

    [Fact]
    public async Task RenewLicenseAsync_WhenExceptionOccurs_RollsBackAndRethrows()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType();

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateExpiredLicense();
        SetupOldLicense(oldLicense);

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ThrowsAsync(
                new InvalidOperationException("Database failure."));

        var sut = CreateSut();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RenewLicenseAsync(10, null));

        Assert.Equal("Database failure.", exception.Message);

        transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private void SetupAuthenticatedUser()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(1);
    }

    private void SetupValidApplicationType()
    {
        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(2))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 2
                    }));
    }

    private void SetupTransaction(Mock<IUnitOfWorkTransaction> transaction)
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(
                IsolationLevel.Serializable,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
    }

    private Mock<IUnitOfWorkTransaction> CreateTransactionMock()
    {
        var transaction = new Mock<IUnitOfWorkTransaction>();

        transaction
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transaction
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return transaction;
    }

    private void VerifyRollback(
        Mock<IUnitOfWorkTransaction> transaction)
    {
        transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifyCommitNeverCalled(
        Mock<IUnitOfWorkTransaction> transaction)
    {
        transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupOldLicense(License license)
    {
        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(license);
    }

    private void SetupSuccessfulRenewalApplication()
    {
        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(100));
    }

    private License CreateExpiredLicense()
    {
        return new License
        {
            LicenseID = 10,
            DriverID = 20,
            LicenseClass = 1,
            IssueDate = DateTime.UtcNow.AddYears(-6),
            ExpirationDate = DateTime.UtcNow.AddDays(-1),
            IsActive = true,

            Driver = new Driver
            {
                DriverID = 20,
                PersonID = 30
            },

            LicenseClassInfo = new LicenseClass
            {
                LicenseClassID = 1,
                ClassName = "Test Class",
                ClassDescription = "Test",
                MinimumAllowedAge = 18,
                DefaultValidityLength = 5,
                ClassFees = 50
            }
        };
    }
}