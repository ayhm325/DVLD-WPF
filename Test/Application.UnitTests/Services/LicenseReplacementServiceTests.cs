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

public class LicenseReplacementServiceTests
{
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<IApplicationTypeService> _applicationTypeService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<LicenseReplacementService>> _logger = new();

    private LicenseReplacementService CreateSut()
    {
        return new LicenseReplacementService(
            _licenseRepository.Object,
            _applicationService.Object,
            _applicationTypeService.Object,
            _currentUserService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    // =========================================================
    // Validation
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenLicenseIdIsInvalid_ReturnsValidationFailure()
    {
        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            0,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        _currentUserService.Verify(
            x => x.IsLoggedIn,
            Times.Never);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenReasonIsNull_ReturnsValidationFailure()
    {
        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            null!);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "Replacement reason is required.",
            result.Error);

        _currentUserService.Verify(
            x => x.IsLoggedIn,
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenReasonIsWhitespace_ReturnsValidationFailure()
    {
        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "     ");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "Replacement reason is required.",
            result.Error);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenUserIsNotAuthenticated_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(1);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);

        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenUserIdIsInvalid_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);

        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenReasonIsInvalid_ReturnsValidationFailure()
    {
        SetupAuthenticatedUser();

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Stolen License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "Invalid replacement reason. Allowed reasons are Lost License or Damaged License.",
            result.Error);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenReasonHasDifferentCasing_AcceptsReason()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(false);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "lost license");

        Assert.True(result.IsFailure);

        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(3),
            Times.Once);
    }

    // =========================================================
    // Application Type
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenLostReason_RequestsLostApplicationType()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(3))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromNotFound(
                    "Application type not found."));

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(3),
            Times.Once);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(4),
            Times.Never);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenDamagedReason_RequestsDamagedApplicationType()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(4))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromNotFound(
                    "Application type not found."));

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Damaged License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(4),
            Times.Once);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(3),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenApplicationTypeFails_PropagatesFailure()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(3))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromConflict(
                    "Replacement application type conflict."));

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Replacement application type conflict.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenApplicationTypeValueIsNull_ReturnsNotFound()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(3))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(null!));

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Replacement application type not found.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Old License
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenLicenseDoesNotExist_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync((License?)null);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "License not found.",
            result.Error);

        VerifyRollback(transaction);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenLicenseIsInactive_ReturnsConflictAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();
        oldLicense.IsActive = false;

        SetupOldLicense(oldLicense);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Cannot replace an inactive license.",
            result.Error);

        VerifyRollback(transaction);

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenDriverIsMissing_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();
        oldLicense.Driver = null!;

        SetupOldLicense(oldLicense);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Driver information is not available.",
            result.Error);

        VerifyRollback(transaction);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenLicenseClassIsMissing_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();
        oldLicense.LicenseClassInfo = null!;

        SetupOldLicense(oldLicense);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "License class information is not available.",
            result.Error);

        VerifyRollback(transaction);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenLicenseIsExpired_ReturnsConflictAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();
        oldLicense.ExpirationDate =
            DateTime.UtcNow.AddDays(-1);

        SetupOldLicense(oldLicense);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Cannot replace an expired license.",
            result.Error);

        VerifyRollback(transaction);

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()),
            Times.Never);
    }

    // =========================================================
    // Renewal Application Creation
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenApplicationCreationFails_PropagatesFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.FromConflict(
                    "Replacement application already exists."));

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Replacement application already exists.",
            result.Error);

        VerifyRollback(transaction);

        _licenseRepository.Verify(
            x => x.DeactivateLicenseAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenApplicationIdIsInvalid_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(0));

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to create replacement application.",
            result.Error);

        VerifyRollback(transaction);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenCreatingApplication_SendsCorrectApplicantAndType()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(false);

        var sut = CreateSut();

        await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(
                It.Is<CreateApplicationDto>(dto =>
                    dto.ApplicantPersonID ==
                        oldLicense.Driver.PersonID &&
                    dto.ApplicationTypeID == 3)),
            Times.Once);
    }

    // =========================================================
    // Deactivation
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenDeactivationFails_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(false);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to deactivate the old license.",
            result.Error);

        VerifyRollback(transaction);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.IsAny<License>()),
            Times.Never);
    }

    // =========================================================
    // Save
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenSaveReturnsZero_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to save the replacement license.",
            result.Error);

        VerifyRollback(transaction);

        _applicationService.Verify(
            x => x.CompleteApplicationAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenNewLicenseIdIsNotGenerated_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to save the replacement license.",
            result.Error);

        VerifyRollback(transaction);
    }

    // =========================================================
    // Complete Application
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenCompletingApplicationFails_PropagatesFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(
                Result<int>.FromFailure(
                    "Application could not be completed."));

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Application could not be completed.",
            result.Error);

        VerifyRollback(transaction);

        transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Successful Lost Replacement
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenLostLicenseIsValid_ReturnsNewLicenseIdAndCommits()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsSuccess);
        Assert.Equal(500, result.Value);

        VerifySuccessfulReplacement(
            transaction,
            oldLicense,
            IssueReason.ReplacementForLost,
            3);
    }

    // =========================================================
    // Successful Damaged Replacement
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenDamagedLicenseIsValid_ReturnsNewLicenseIdAndCommits()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(4);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 600;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Damaged License");

        Assert.True(result.IsSuccess);
        Assert.Equal(600, result.Value);

        VerifySuccessfulReplacement(
            transaction,
            oldLicense,
            IssueReason.ReplacementForDamaged,
            4);
    }

    // =========================================================
    // Trimmed Reason
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenReasonHasWhitespace_TrimsReason()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "   Lost License   ");

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.Notes == "Lost License")),
            Times.Once);
    }

    // =========================================================
    // License Mapping
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenValid_CopiesDriverAndLicenseClass()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.DriverID ==
                        oldLicense.DriverID &&
                    license.LicenseClass ==
                        oldLicense.LicenseClass)),
            Times.Once);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenValid_CopiesExpirationDate()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.ExpirationDate ==
                        oldLicense.ExpirationDate)),
            Times.Once);
    }

    [Fact]
    public async Task ReplaceLicenseAsync_WhenValid_UsesLicenseClassFees()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        var oldLicense = CreateActiveUnexpiredLicense();

        SetupOldLicense(oldLicense);

        SetupSuccessfulReplacementApplication();

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(10))
            .ReturnsAsync(true);

        _licenseRepository
            .Setup(x => x.AddLicenseAsync(
                It.IsAny<License>()))
            .Callback<License>(license =>
            {
                license.LicenseID = 500;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(100))
            .ReturnsAsync(Result.Success());

        var sut = CreateSut();

        var result = await sut.ReplaceLicenseAsync(
            10,
            "Lost License");

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.PaidFees ==
                        oldLicense.LicenseClassInfo.ClassFees)),
            Times.Once);
    }

    // =========================================================
    // Exception
    // =========================================================

    [Fact]
    public async Task ReplaceLicenseAsync_WhenExceptionOccurs_RollsBackAndRethrows()
    {
        SetupAuthenticatedUser();
        SetupValidApplicationType(3);

        var transaction = CreateTransactionMock();
        SetupTransaction(transaction);

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database failure."));

        var sut = CreateSut();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.ReplaceLicenseAsync(
                    10,
                    "Lost License"));

        Assert.Equal(
            "Database failure.",
            exception.Message);

        transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
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

    private void SetupValidApplicationType(
        int applicationTypeId)
    {
        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(
                applicationTypeId))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId =
                            applicationTypeId
                    }));
    }

    private Mock<IUnitOfWorkTransaction>
        CreateTransactionMock()
    {
        var transaction =
            new Mock<IUnitOfWorkTransaction>();

        transaction
            .Setup(x => x.RollbackAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        transaction
            .Setup(x => x.CommitAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return transaction;
    }

    private void SetupTransaction(
        Mock<IUnitOfWorkTransaction> transaction)
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(
                IsolationLevel.Serializable,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
    }

    private void VerifyRollback(
        Mock<IUnitOfWorkTransaction> transaction)
    {
        transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupOldLicense(
        License license)
    {
        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(10))
            .ReturnsAsync(license);
    }

    private void SetupSuccessfulReplacementApplication()
    {
        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(100));
    }

    private License CreateActiveUnexpiredLicense()
    {
        return new License
        {
            LicenseID = 10,
            ApplicationID = 50,
            DriverID = 20,
            LicenseClass = 1,
            IssueDate =
                DateTime.UtcNow.AddYears(-2),
            ExpirationDate =
                DateTime.UtcNow.AddDays(30),
            PaidFees = 50,
            IsActive = true,
            IssueReason = IssueReason.FirstTime,

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

    private void VerifySuccessfulReplacement(
        Mock<IUnitOfWorkTransaction> transaction,
        License oldLicense,
        IssueReason expectedIssueReason,
        int expectedApplicationTypeId)
    {
        _licenseRepository.Verify(
            x => x.DeactivateLicenseAsync(10),
            Times.Once);

        _licenseRepository.Verify(
            x => x.AddLicenseAsync(
                It.Is<License>(license =>
                    license.DriverID ==
                        oldLicense.DriverID &&
                    license.LicenseClass ==
                        oldLicense.LicenseClass &&
                    license.ExpirationDate ==
                        oldLicense.ExpirationDate &&
                    license.PaidFees ==
                        oldLicense.LicenseClassInfo.ClassFees &&
                    license.IssueReason ==
                        expectedIssueReason &&
                    license.CreatedByUserID == 1 &&
                    license.Notes != null)),
            Times.Once);

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(
                It.Is<CreateApplicationDto>(dto =>
                    dto.ApplicantPersonID ==
                        oldLicense.Driver.PersonID &&
                    dto.ApplicationTypeID ==
                        expectedApplicationTypeId)),
            Times.Once);

        _applicationService.Verify(
            x => x.CompleteApplicationAsync(100),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}