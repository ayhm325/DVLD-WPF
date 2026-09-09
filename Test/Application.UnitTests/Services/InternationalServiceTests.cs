using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DriverDTO;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace Application.UnitTests.Services;

public sealed class InternationalServiceTests
{
    private readonly Mock<IInternationalRepository> _repository = new();
    private readonly Mock<ILicenseQueryService> _licenseQueryService = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<IApplicationTypeService> _applicationTypeService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<ILogger<InternationalService>> _logger = new();

    private InternationalService CreateService()
    {
        return new InternationalService(
            _repository.Object,
            _licenseQueryService.Object,
            _applicationService.Object,
            _applicationTypeService.Object,
            _currentUserService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private void SetupAuthenticatedUser(int userId = 10)
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(userId);
    }

    private void SetupTransaction()
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(
                IsolationLevel.Serializable,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);
    }

    private void SetupRollback()
    {
        _transaction
            .Setup(x => x.RollbackAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupCommit()
    {
        _transaction
            .Setup(x => x.CommitAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupApplicationTypeSuccess()
    {
        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(6))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 6
                    }));
    }

    private void SetupLicenseQuerySuccess(LicenseDto? license = null)
    {
        _licenseQueryService
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<LicenseDto>.Success(
                    license ?? CreateValidLocalLicense()));
    }

    private void SetupLicenseQueryForLocalInfo(LicenseDto license)
    {
        _licenseQueryService
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<LicenseDto>.Success(license));
    }

    private static LicenseDto CreateValidLocalLicense(
        int licenseId = 100,
        int driverId = 20,
        int personId = 30)
    {
        return new LicenseDto
        {
            LicenseID = licenseId,
            ApplicationID = 500,
            DriverID = driverId,
            LicenseClassID = 3,
            LicenseClassName = "Ordinary",
            IssueDate = DateTime.UtcNow.AddYears(-1),
            ExpirationDate = DateTime.UtcNow.AddMonths(6),
            PaidFees = 20,
            IsActive = true,
            IssueReason = (byte)IssueReason.FirstTime,
            Driver = new DriverDto
            {
                DriverID = driverId,
                PersonID = personId,
                FullName = "Test Driver",
                NationalNo = "123456789",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                ImagePath = "driver.jpg"
            }
        };
    }

    private void SetupSuccessfulIssue()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<InternationalLicense>()))
            .Callback<InternationalLicense>(entity =>
            {
                entity.InternationalLicenseID = 900;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(Result.Success());

        SetupCommit();
        SetupRollback();
    }

    // =========================================================
    // GetAllAsync
    // =========================================================

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEntities_ReturnsMappedDtos()
    {
        var entities = new List<InternationalLicense>
        {
            new()
            {
                InternationalLicenseID = 1,
                ApplicationID = 10,
                DriverID = 20,
                IssuedUsingLocalLicenseID = 30,
                IssueDate = new DateTime(2026, 1, 1),
                ExpirationDate = new DateTime(2027, 1, 1),
                IsActive = true,
                CreatedByUserID = 5
            },
            new()
            {
                InternationalLicenseID = 2,
                ApplicationID = 11,
                DriverID = 21,
                IssuedUsingLocalLicenseID = 31,
                IssueDate = new DateTime(2026, 2, 1),
                ExpirationDate = new DateTime(2027, 2, 1),
                IsActive = false,
                CreatedByUserID = 6
            }
        };

        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(entities);

        var result = await CreateService().GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var value = result.Value!;

        Assert.Equal(2, value.Count);

        Assert.Equal(1, value[0].InternationalLicenseID);
        Assert.Equal(10, value[0].ApplicationID);
        Assert.Equal(20, value[0].DriverID);

        Assert.Equal(2, value[1].InternationalLicenseID);
        Assert.False(value[1].IsActive);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEmptyList_ReturnsEmptySuccess()
    {
        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<InternationalLicense>());

        var result = await CreateService().GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Empty(result.Value!);
    }

    // =========================================================
    // GetByIdAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_WhenIdIsInvalid_ReturnsValidationFailure(
        int id)
    {
        var result =
            await CreateService().GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid international license ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenLicenseDoesNotExist_ReturnsNotFound()
    {
        _repository
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync((InternationalLicense?)null);

        var result =
            await CreateService().GetByIdAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "International license not found.",
            result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenLicenseExists_ReturnsMappedDto()
    {
        var entity = new InternationalLicense
        {
            InternationalLicenseID = 100,
            ApplicationID = 200,
            DriverID = 300,
            IssuedUsingLocalLicenseID = 400,
            IssueDate = new DateTime(2026, 1, 1),
            ExpirationDate = new DateTime(2027, 1, 1),
            IsActive = true,
            CreatedByUserID = 50
        };

        _repository
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(entity);

        var result =
            await CreateService().GetByIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var value = result.Value!;

        Assert.Equal(100, value.InternationalLicenseID);
        Assert.Equal(200, value.ApplicationID);
        Assert.Equal(300, value.DriverID);
        Assert.Equal(400, value.IssuedUsingLocalLicenseID);
    }

    // =========================================================
    // GetByDriverIdAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByDriverIdAsync_WhenDriverIdIsInvalid_ReturnsValidationFailure(
        int driverId)
    {
        var result =
            await CreateService().GetByDriverIdAsync(driverId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid driver ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByDriverIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByDriverIdAsync_WhenRepositoryReturnsEntities_ReturnsMappedDtos()
    {
        _repository
            .Setup(x => x.GetByDriverIdAsync(20))
            .ReturnsAsync(
                new List<InternationalLicense>
                {
                    new()
                    {
                        InternationalLicenseID = 1,
                        ApplicationID = 2,
                        DriverID = 20,
                        IssuedUsingLocalLicenseID = 30,
                        IsActive = true
                    }
                });

        var result =
            await CreateService().GetByDriverIdAsync(20);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var value = result.Value!;

        Assert.Single(value);
        Assert.Equal(20, value[0].DriverID);
    }

    [Fact]
    public async Task GetByDriverIdAsync_WhenRepositoryReturnsEmptyList_ReturnsEmptySuccess()
    {
        _repository
            .Setup(x => x.GetByDriverIdAsync(20))
            .ReturnsAsync(
                new List<InternationalLicense>());

        var result =
            await CreateService().GetByDriverIdAsync(20);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Empty(result.Value!);
    }

    // =========================================================
    // GetByApplicationIdAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByApplicationIdAsync_WhenApplicationIdIsInvalid_ReturnsValidationFailure(
        int applicationId)
    {
        var result =
            await CreateService().GetByApplicationIdAsync(applicationId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid application ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByApplicationIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByApplicationIdAsync_WhenLicenseDoesNotExist_ReturnsNotFound()
    {
        _repository
            .Setup(x => x.GetByApplicationIdAsync(100))
            .ReturnsAsync((InternationalLicense?)null);

        var result =
            await CreateService().GetByApplicationIdAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "International license not found.",
            result.Error);
    }

    [Fact]
    public async Task GetByApplicationIdAsync_WhenLicenseExists_ReturnsSuccess()
    {
        _repository
            .Setup(x => x.GetByApplicationIdAsync(100))
            .ReturnsAsync(
                new InternationalLicense
                {
                    InternationalLicenseID = 1,
                    ApplicationID = 100,
                    DriverID = 20,
                    IssuedUsingLocalLicenseID = 30
                });

        var result =
            await CreateService().GetByApplicationIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(
            100,
            result.Value!.ApplicationID);
    }

    // =========================================================
    // GetByLocalLicenseIdAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByLocalLicenseIdAsync_WhenIdIsInvalid_ReturnsValidationFailure(
        int localLicenseId)
    {
        var result =
            await CreateService().GetByLocalLicenseIdAsync(localLicenseId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid local license ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByLocalLicenseIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByLocalLicenseIdAsync_WhenRepositoryReturnsEntities_ReturnsSuccess()
    {
        _repository
            .Setup(x => x.GetByLocalLicenseIdAsync(100))
            .ReturnsAsync(
                new List<InternationalLicense>
                {
                    new()
                    {
                        InternationalLicenseID = 1,
                        ApplicationID = 2,
                        DriverID = 3,
                        IssuedUsingLocalLicenseID = 100
                    }
                });

        var result =
            await CreateService().GetByLocalLicenseIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var value = result.Value!;

        Assert.Single(value);
        Assert.Equal(
            100,
            value[0].IssuedUsingLocalLicenseID);
    }

    // =========================================================
    // HasActiveInternationalLicenseAsync
    // =========================================================

    [Fact]
    public async Task HasActiveInternationalLicenseAsync_DelegatesToRepository()
    {
        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(true);

        var result =
            await CreateService()
                .HasActiveInternationalLicenseAsync(20);

        Assert.True(result);

        _repository.Verify(
            x => x.HasActiveInternationalLicenseAsync(20),
            Times.Once);
    }

    // =========================================================
    // IssueInternationalLicenseAsync
    // Validation / Authentication
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IssueInternationalLicenseAsync_WhenLocalLicenseIdIsInvalid_ReturnsValidationFailure(
        int localLicenseId)
    {
        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(localLicenseId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid local license ID.",
            result.Error);

        _applicationTypeService.Verify(
            x => x.GetApplicationTypeByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenUserIsNotAuthenticated_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

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
    public async Task IssueInternationalLicenseAsync_WhenUserIdIsInvalid_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);

        Assert.Equal(
            "Authenticated user is required.",
            result.Error);
    }

    // =========================================================
    // Application Type
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenApplicationTypeFails_PropagatesFailure()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(6))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromNotFound(
                    "Application type not found."));

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Application type not found.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenApplicationTypeValueIsNull_ReturnsNotFound()
    {
        SetupAuthenticatedUser();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(6))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(null!));

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "International application type not found.",
            result.Error);
    }

    // =========================================================
    // Local License
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenLicenseQueryFails_RollsBackAndPropagates()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        _licenseQueryService
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<LicenseDto>.FromNotFound(
                    "Local license not found."));

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Local license not found.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenLicenseValueIsNull_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        _licenseQueryService
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<LicenseDto>.Success(null!));

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Local license not found.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenLicenseClassIsNotThree_ReturnsConflictAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        var license = CreateValidLocalLicense();
        license.LicenseClassID = 2;

        SetupLicenseQuerySuccess(license);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Only class 3 licenses can be issued internationally.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenLicenseIsInactive_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        var license = CreateValidLocalLicense();
        license.IsActive = false;

        SetupLicenseQuerySuccess(license);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "The local license is not active.",
            result.Error);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenLicenseIsExpired_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        var license = CreateValidLocalLicense();
        license.ExpirationDate =
            DateTime.UtcNow.AddMinutes(-1);

        SetupLicenseQuerySuccess(license);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "The local license is expired.",
            result.Error);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenDriverIsMissing_ReturnsNotFound()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        var license = CreateValidLocalLicense();
        license.Driver = null;

        SetupLicenseQuerySuccess(license);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Driver information is not available.",
            result.Error);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenDriverPersonIdIsInvalid_ReturnsFailure()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        var license = CreateValidLocalLicense();
        license.Driver!.PersonID = 0;

        SetupLicenseQuerySuccess(license);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "The license has invalid driver information.",
            result.Error);
    }

    // =========================================================
    // Duplicate Checks
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenInternationalLicenseAlreadyExists_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(true);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "An international license already exists for this local license.",
            result.Error);

        _repository.Verify(
            x => x.HasActiveInternationalLicenseAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenDriverAlreadyHasActiveInternationalLicense_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(true);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "The driver already has an active international license.",
            result.Error);
    }

    // =========================================================
    // Application Creation
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenApplicationCreationFails_RollsBackAndPropagates()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.FromConflict(
                    "Application conflict."));

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Application conflict.",
            result.Error);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<InternationalLicense>()),
            Times.Never);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenApplicationIdIsInvalid_RollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(0));

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to create international application.",
            result.Error);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<InternationalLicense>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_CreatesApplicationWithCorrectData()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<InternationalLicense>()))
            .Callback<InternationalLicense>(x =>
                x.InternationalLicenseID = 900)
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(Result.Success());

        SetupCommit();

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsSuccess);

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(
                It.Is<CreateApplicationDto>(dto =>
                    dto.ApplicantPersonID == 30 &&
                    dto.ApplicationTypeID == 6)),
            Times.Once);
    }

    // =========================================================
    // Save
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenSaveReturnsZero_RollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<InternationalLicense>()))
            .Callback<InternationalLicense>(x =>
                x.InternationalLicenseID = 900)
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to save international license.",
            result.Error);

        _applicationService.Verify(
            x => x.CompleteApplicationAsync(
                It.IsAny<int>()),
            Times.Never);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenEntityIdIsNotGenerated_RollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<InternationalLicense>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to save international license.",
            result.Error);
    }

    // =========================================================
    // Entity Creation
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_CreatesInternationalLicenseWithCorrectData()
    {
        SetupAuthenticatedUser(55);
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        InternationalLicense? createdEntity = null;

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<InternationalLicense>()))
            .Callback<InternationalLicense>(x =>
            {
                createdEntity = x;
                x.InternationalLicenseID = 900;
            })
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(Result.Success());

        SetupCommit();

        var before = DateTime.UtcNow;

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        var after = DateTime.UtcNow;

        Assert.True(result.IsSuccess);
        Assert.Equal(900, result.Value);

        Assert.NotNull(createdEntity);

        var entity = createdEntity!;

        Assert.Equal(700, entity.ApplicationID);
        Assert.Equal(20, entity.DriverID);
        Assert.Equal(100, entity.IssuedUsingLocalLicenseID);
        Assert.True(entity.IsActive);
        Assert.Equal(55, entity.CreatedByUserID);

        Assert.InRange(
            entity.IssueDate,
            before,
            after);

        Assert.InRange(
            entity.ExpirationDate,
            before.AddYears(1),
            after.AddYears(1));
    }

    // =========================================================
    // Complete Application
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenCompletingApplicationFails_RollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupLicenseQuerySuccess();
        SetupRollback();

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.HasActiveInternationalLicenseAsync(20))
            .ReturnsAsync(false);

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<InternationalLicense>()))
            .Callback<InternationalLicense>(x =>
                x.InternationalLicenseID = 900)
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(
                Result.Conflict(
                    "Could not complete application."));

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Could not complete application.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Successful Workflow
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenEverythingIsValid_ReturnsCreatedLicenseId()
    {
        SetupSuccessfulIssue();

        var result =
            await CreateService()
                .IssueInternationalLicenseAsync(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(900, result.Value);

        _repository.Verify(
            x => x.AddAsync(
                It.Is<InternationalLicense>(license =>
                    license.ApplicationID == 700 &&
                    license.DriverID == 20 &&
                    license.IssuedUsingLocalLicenseID == 100 &&
                    license.IsActive &&
                    license.CreatedByUserID == 10)),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _applicationService.Verify(
            x => x.CompleteApplicationAsync(700),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // Exception / Rollback
    // =========================================================

    [Fact]
    public async Task IssueInternationalLicenseAsync_WhenUnexpectedExceptionOccurs_RollsBackAndRethrows()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupApplicationTypeSuccess();
        SetupRollback();

        _licenseQueryService
            .Setup(x => x.GetByIdAsync(100))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database failure."));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateService()
                    .IssueInternationalLicenseAsync(100));

        Assert.Equal(
            "Database failure.",
            exception.Message);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // GetLocalLicenseInfoAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLocalLicenseInfoAsync_WhenLicenseIdIsInvalid_ReturnsValidationFailure(
        int licenseId)
    {
        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(licenseId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "Invalid local license ID.",
            result.Error);

        _licenseQueryService.Verify(
            x => x.GetByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenLicenseQueryFails_PropagatesFailure()
    {
        _licenseQueryService
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<LicenseDto>.FromNotFound(
                    "Local license not found."));

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Local license not found.",
            result.Error);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenLicenseValueIsNull_ReturnsNotFound()
    {
        _licenseQueryService
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(
                Result<LicenseDto>.Success(null!));

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Local license not found.",
            result.Error);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenLicenseClassIsNotThree_ReturnsConflict()
    {
        var license = CreateValidLocalLicense();
        license.LicenseClassID = 2;

        SetupLicenseQueryForLocalInfo(license);

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Only class 3 licenses can be converted to an international license.",
            result.Error);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenLicenseIsInactive_ReturnsConflict()
    {
        var license = CreateValidLocalLicense();
        license.IsActive = false;

        SetupLicenseQueryForLocalInfo(license);

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "The local license is not active.",
            result.Error);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenLicenseIsExpired_ReturnsConflict()
    {
        var license = CreateValidLocalLicense();

        license.ExpirationDate =
            DateTime.UtcNow.AddMinutes(-1);

        SetupLicenseQueryForLocalInfo(license);

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "The local license is expired.",
            result.Error);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenDriverIsMissing_ReturnsNotFound()
    {
        var license = CreateValidLocalLicense();
        license.Driver = null;

        SetupLicenseQueryForLocalInfo(license);

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Driver information is not available.",
            result.Error);

        _repository.Verify(
            x => x.ExistsByLocalLicenseAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenInternationalLicenseAlreadyExists_ReturnsConflict()
    {
        var license = CreateValidLocalLicense();

        SetupLicenseQueryForLocalInfo(license);

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(true);

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "An international license already exists for this local license.",
            result.Error);
    }

    [Fact]
    public async Task GetLocalLicenseInfoAsync_WhenEverythingIsValid_ReturnsDriverLicenseInfo()
    {
        var license = CreateValidLocalLicense();

        SetupLicenseQueryForLocalInfo(license);

        _repository
            .Setup(x => x.ExistsByLocalLicenseAsync(100))
            .ReturnsAsync(false);

        var result =
            await CreateService()
                .GetLocalLicenseInfoAsync(100);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var value = result.Value!;

        Assert.Equal(100, value.LicenseId);
        Assert.Equal(20, value.DriverId);
        Assert.Equal(30, value.PersonID);
        Assert.Equal("Ordinary", value.LicenseClass);
        Assert.Equal("Test Driver", value.FullName);
        Assert.Equal("123456789", value.NationalNo);
        Assert.True(value.IsActive);
    }
}