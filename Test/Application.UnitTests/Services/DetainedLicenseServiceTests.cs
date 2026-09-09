using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace Application.UnitTests.Services;

public sealed class DetainedLicenseServiceTests
{
    private readonly Mock<IDetainedLicenseRepository> _repository = new();
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<IApplicationTypeService> _applicationTypeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<ILogger<DetainedLicenseService>> _logger = new();

    private DetainedLicenseService CreateService()
    {
        return new DetainedLicenseService(
            _repository.Object,
            _licenseRepository.Object,
            _applicationService.Object,
            _applicationTypeService.Object,
            _unitOfWork.Object,
            _currentUserService.Object,
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

    private void SetupValidLicense(
        int licenseId = 100,
        int driverId = 20,
        int personId = 30)
    {
        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(licenseId))
            .ReturnsAsync(
                new License
                {
                    LicenseID = licenseId,
                    DriverID = driverId,
                    LicenseClass = 3,
                    IssueDate = DateTime.UtcNow.AddYears(-1),
                    ExpirationDate = DateTime.UtcNow.AddMonths(6),
                    IsActive = true,
                    Driver = new Driver
                    {
                        DriverID = driverId,
                        PersonID = personId
                    }
                });
    }

    private void SetupValidDetention(int detainId = 50)
    {
        _repository
            .Setup(x => x.GetByIdAsync(detainId))
            .ReturnsAsync(
                new DetainedLicense
                {
                    DetainID = detainId,
                    LicenseID = 100,
                    DetainDate = DateTime.UtcNow.AddDays(-5),
                    FineFees = 50,
                    CreatedByUserID = 10,
                    IsReleased = false
                });
    }

    private void SetupValidReleaseDetention(int detainId = 50)
    {
        _repository
            .Setup(x => x.GetByIdForUpdateAsync(detainId))
            .ReturnsAsync(
                new DetainedLicense
                {
                    DetainID = detainId,
                    LicenseID = 100,
                    DetainDate = DateTime.UtcNow.AddDays(-5),
                    FineFees = 50,
                    CreatedByUserID = 10,
                    IsReleased = false
                });
    }

    // =========================================================
    // GetAllAsync
    // =========================================================

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEntities_ReturnsMappedDtos()
    {
        var entities = new List<DetainedLicense>
        {
            new()
            {
                DetainID = 1,
                LicenseID = 100,
                DetainDate = new DateTime(2026, 1, 1),
                FineFees = 50,
                CreatedByUserID = 10,
                IsReleased = false
            },
            new()
            {
                DetainID = 2,
                LicenseID = 200,
                DetainDate = new DateTime(2026, 2, 1),
                FineFees = 100,
                CreatedByUserID = 20,
                IsReleased = true,
                ReleaseDate = new DateTime(2026, 2, 10),
                ReleasedByUserID = 30,
                ReleaseApplicationID = 40
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

        Assert.Equal(1, value[0].DetainID);
        Assert.Equal(100, value[0].LicenseID);
        Assert.Equal(50, value[0].FineFees);
        Assert.False(value[0].IsReleased);

        Assert.Equal(2, value[1].DetainID);
        Assert.True(value[1].IsReleased);
        Assert.Equal(30, value[1].ReleasedByUserID);
        Assert.Equal(40, value[1].ReleaseApplicationID);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEmptyList_ReturnsEmptySuccess()
    {
        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<DetainedLicense>());

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
        var result = await CreateService().GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid detained license ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDetentionDoesNotExist_ReturnsNotFound()
    {
        _repository
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync((DetainedLicense?)null);

        var result = await CreateService().GetByIdAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Detained license not found.",
            result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDetentionExists_ReturnsMappedDto()
    {
        var entity = new DetainedLicense
        {
            DetainID = 100,
            LicenseID = 200,
            DetainDate = new DateTime(2026, 1, 1),
            FineFees = 75,
            CreatedByUserID = 10,
            IsReleased = false
        };

        _repository
            .Setup(x => x.GetByIdAsync(100))
            .ReturnsAsync(entity);

        var result = await CreateService().GetByIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var value = result.Value!;

        Assert.Equal(100, value.DetainID);
        Assert.Equal(200, value.LicenseID);
        Assert.Equal(75, value.FineFees);
        Assert.Equal(10, value.CreatedByUserID);
        Assert.False(value.IsReleased);
    }

    // =========================================================
    // GetActiveDetainByLicenseIdAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetActiveDetainByLicenseIdAsync_WhenLicenseIdIsInvalid_ReturnsValidationFailure(
        int licenseId)
    {
        var result =
            await CreateService()
                .GetActiveDetainByLicenseIdAsync(licenseId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "Invalid license ID.",
            result.Error);

        _repository.Verify(
            x => x.GetActiveDetainByLicenseIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetActiveDetainByLicenseIdAsync_WhenNoActiveDetentionExists_ReturnsNotFound()
    {
        _repository
            .Setup(x => x.GetActiveDetainByLicenseIdAsync(100))
            .ReturnsAsync((DetainedLicense?)null);

        var result =
            await CreateService()
                .GetActiveDetainByLicenseIdAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "No active detention found for this license.",
            result.Error);
    }

    [Fact]
    public async Task GetActiveDetainByLicenseIdAsync_WhenActiveDetentionExists_ReturnsSuccess()
    {
        var entity = new DetainedLicense
        {
            DetainID = 50,
            LicenseID = 100,
            DetainDate = new DateTime(2026, 1, 1),
            FineFees = 100,
            CreatedByUserID = 10,
            IsReleased = false
        };

        _repository
            .Setup(x => x.GetActiveDetainByLicenseIdAsync(100))
            .ReturnsAsync(entity);

        var result =
            await CreateService()
                .GetActiveDetainByLicenseIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(50, result.Value!.DetainID);
        Assert.Equal(100, result.Value.LicenseID);
    }

    // =========================================================
    // IsLicenseDetainedAsync
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IsLicenseDetainedAsync_WhenLicenseIdIsInvalid_ReturnsFalse(
        int licenseId)
    {
        var result =
            await CreateService()
                .IsLicenseDetainedAsync(licenseId);

        Assert.False(result);

        _repository.Verify(
            x => x.IsLicenseDetainedAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IsLicenseDetainedAsync_WhenRepositoryReturnsTrue_ReturnsTrue()
    {
        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(true);

        var result =
            await CreateService()
                .IsLicenseDetainedAsync(100);

        Assert.True(result);
    }

    [Fact]
    public async Task IsLicenseDetainedAsync_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(false);

        var result =
            await CreateService()
                .IsLicenseDetainedAsync(100);

        Assert.False(result);
    }

    // =========================================================
    // AddAsync - Validation
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenDtoIsNull_ReturnsValidationFailure()
    {
        var result =
            await CreateService()
                .AddAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "Detained license data is required.",
            result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddAsync_WhenLicenseIdIsInvalid_ReturnsValidationFailure(
        int licenseId)
    {
        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = licenseId,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Contains(
            "A valid license is required.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenFineFeesAreNegative_ReturnsValidationFailure()
    {
        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = -1
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Contains(
            "Fine fees cannot be negative.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenFineFeesExceedMaximum_ReturnsValidationFailure()
    {
        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 10_000_000_000_000_000m
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Contains(
            "Fine fees exceed the allowed value.",
            result.Error);
    }

    // =========================================================
    // AddAsync - Authentication
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenUserIsNotAuthenticated_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);

        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenUserIdIsInvalid_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);
    }

    // =========================================================
    // AddAsync - License checks
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenLicenseDoesNotExist_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(100))
            .ReturnsAsync((License?)null);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "License not found.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenLicenseIsInactive_ReturnsConflictAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(100))
            .ReturnsAsync(
                new License
                {
                    LicenseID = 100,
                    IsActive = false,
                    ExpirationDate = DateTime.UtcNow.AddMonths(6)
                });

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Only an active license can be detained.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenLicenseIsExpired_ReturnsConflictAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(100))
            .ReturnsAsync(
                new License
                {
                    LicenseID = 100,
                    IsActive = true,
                    ExpirationDate =
                        DateTime.UtcNow.AddMinutes(-1)
                });

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "An expired license cannot be detained.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenLicenseAlreadyDetained_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidLicense();

        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(true);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "License is already detained.",
            result.Error);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<DetainedLicense>()),
            Times.Never);
    }

    // =========================================================
    // AddAsync - Create / Deactivate
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenDeactivateLicenseFails_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidLicense();

        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<DetainedLicense>()))
            .Returns(Task.CompletedTask);

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(100))
            .ReturnsAsync(false);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 75
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to deactivate the license.",
            result.Error);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_CreatesDetentionWithCorrectData()
    {
        SetupAuthenticatedUser(55);
        SetupTransaction();
        SetupRollback();
        SetupCommit();

        SetupValidLicense();

        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(false);

        DetainedLicense? createdEntity = null;

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<DetainedLicense>()))
            .Callback<DetainedLicense>(entity =>
            {
                createdEntity = entity;
                entity.DetainID = 500;
            })
            .Returns(Task.CompletedTask);

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var savedEntity = new DetainedLicense
        {
            DetainID = 500,
            LicenseID = 100,
            DetainDate = DateTime.UtcNow,
            FineFees = 75,
            CreatedByUserID = 55,
            IsReleased = false
        };

        _repository
            .Setup(x => x.GetByIdAsync(500))
            .ReturnsAsync(savedEntity);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 75
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.NotNull(createdEntity);

        var entity = createdEntity!;

        Assert.Equal(100, entity.LicenseID);
        Assert.Equal(75, entity.FineFees);
        Assert.Equal(55, entity.CreatedByUserID);
        Assert.False(entity.IsReleased);
        Assert.Null(entity.ReleaseDate);
        Assert.Null(entity.ReleasedByUserID);
        Assert.Null(entity.ReleaseApplicationID);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<DetainedLicense>()),
            Times.Once);

        _licenseRepository.Verify(
            x => x.DeactivateLicenseAsync(100),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenSaveReturnsZero_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidLicense();

        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<DetainedLicense>()))
            .Callback<DetainedLicense>(entity =>
                entity.DetainID = 500)
            .Returns(Task.CompletedTask);

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to save detained license.",
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
    public async Task AddAsync_WhenDetainIdIsNotGenerated_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidLicense();

        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<DetainedLicense>()))
            .Returns(Task.CompletedTask);

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to save detained license.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenSavedEntityCannotBeRetrieved_ReturnsFailureAfterCommit()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupCommit();

        SetupValidLicense();

        _repository
            .Setup(x => x.IsLicenseDetainedAsync(100))
            .ReturnsAsync(false);

        _repository
            .Setup(x => x.AddAsync(
                It.IsAny<DetainedLicense>()))
            .Callback<DetainedLicense>(entity =>
                entity.DetainID = 500)
            .Returns(Task.CompletedTask);

        _licenseRepository
            .Setup(x => x.DeactivateLicenseAsync(100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repository
            .Setup(x => x.GetByIdAsync(500))
            .ReturnsAsync((DetainedLicense?)null);

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var result =
            await CreateService()
                .AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Unable to retrieve created detained license.",
            result.Error);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // AddAsync - Exception
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenUnexpectedExceptionOccurs_RollsBackAndRethrows()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(100))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database failure."));

        var dto = new CreateDetainedLicenseDto
        {
            LicenseID = 100,
            FineFees = 50
        };

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateService().AddAsync(dto));

        Assert.Equal(
            "Database failure.",
            exception.Message);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // ReleaseAsync - Validation
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenDtoIsNull_ReturnsValidationFailure()
    {
        var result =
            await CreateService()
                .ReleaseAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "Release data is required.",
            result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ReleaseAsync_WhenDetainIdIsInvalid_ReturnsValidationFailure(
        int detainId)
    {
        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = detainId
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);

        Assert.Equal(
            "A valid detention ID is required.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // ReleaseAsync - Authentication
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenUserIsNotAuthenticated_ReturnsForbidden()
    {
        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Forbidden,
            result.ErrorType);

        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // ReleaseAsync - Detention checks
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenDetentionDoesNotExist_ReturnsNotFoundAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        _repository
            .Setup(x => x.GetByIdForUpdateAsync(50))
            .ReturnsAsync((DetainedLicense?)null);

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Detained license not found.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseAsync_WhenAlreadyReleased_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        _repository
            .Setup(x => x.GetByIdForUpdateAsync(50))
            .ReturnsAsync(
                new DetainedLicense
                {
                    DetainID = 50,
                    LicenseID = 100,
                    IsReleased = true
                });

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "License is already released.",
            result.Error);
    }

    // =========================================================
    // ReleaseAsync - Associated license
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenAssociatedLicenseDoesNotExist_ReturnsNotFound()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(100))
            .ReturnsAsync((License?)null);

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Associated license not found.",
            result.Error);
    }

    // =========================================================
    // ReleaseAsync - Application Type
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenApplicationTypeFails_PropagatesFailure()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.FromNotFound(
                    "Application type not found."));

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Application type not found.",
            result.Error);
    }

    [Fact]
    public async Task ReleaseAsync_WhenApplicationTypeValueIsNull_ReturnsNotFound()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(null!));

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Release application type not found.",
            result.Error);
    }

    // =========================================================
    // ReleaseAsync - Driver
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenDriverIsMissing_ReturnsNotFound()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(100))
            .ReturnsAsync(
                new License
                {
                    LicenseID = 100,
                    DriverID = 20,
                    LicenseClass = 3,
                    IsActive = true,
                    ExpirationDate =
                        DateTime.UtcNow.AddMonths(6),
                    Driver = null!
                });

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        Assert.Equal(
            "Driver information is not available.",
            result.Error);
    }

    // =========================================================
    // ReleaseAsync - Application creation
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenApplicationCreationFails_RollsBackAndPropagates()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.FromConflict(
                    "Application conflict."));

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Application conflict.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseAsync_WhenApplicationIdIsInvalid_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(0));

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to create release application.",
            result.Error);
    }

    [Fact]
    public async Task ReleaseAsync_CreatesApplicationWithCorrectData()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(Result.Success());

        SetupCommit();

        var dto = new ReleaseDetainedLicenseDto
        {
            DetainID = 50
        };

        var result =
            await CreateService()
                .ReleaseAsync(dto);

        Assert.True(result.IsSuccess);

        _applicationService.Verify(
            x => x.AddNewApplicationAsync(
                It.Is<CreateApplicationDto>(application =>
                    application.ApplicantPersonID == 30 &&
                    application.ApplicationTypeID == 5)),
            Times.Once);
    }

    // =========================================================
    // ReleaseAsync - License activation
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenAnotherActiveLicenseExists_DoesNotActivateLicense()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(true);

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
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.ActivateLicenseAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ReleaseAsync_WhenLicenseIsExpired_DoesNotActivateLicense()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();

        _licenseRepository
            .Setup(x => x.GetLicenseByIdAsync(100))
            .ReturnsAsync(
                new License
                {
                    LicenseID = 100,
                    DriverID = 20,
                    LicenseClass = 3,
                    IsActive = false,
                    ExpirationDate =
                        DateTime.UtcNow.AddMinutes(-1),
                    Driver = new Driver
                    {
                        DriverID = 20,
                        PersonID = 30
                    }
                });

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(false);

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
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.ActivateLicenseAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task ReleaseAsync_WhenNoAnotherActiveLicenseAndLicenseValid_ActivatesLicense()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(false);

        _licenseRepository
            .Setup(x => x.ActivateLicenseAsync(100))
            .ReturnsAsync(true);

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
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        Assert.True(result.IsSuccess);

        _licenseRepository.Verify(
            x => x.ActivateLicenseAsync(100),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseAsync_WhenActivationFails_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(false);

        _licenseRepository
            .Setup(x => x.ActivateLicenseAsync(100))
            .ReturnsAsync(false);

        var result =
            await CreateService()
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to restore the license state.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // ReleaseAsync - Save
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenSaveFails_ReturnsFailureAndRollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await CreateService()
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        Assert.Equal(
            "Failed to save license release.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _applicationService.Verify(
            x => x.CompleteApplicationAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    // =========================================================
    // ReleaseAsync - Complete Application
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenCompletingApplicationFails_RollsBack()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(
                Result.Conflict(
                    "Could not complete release application."));

        var result =
            await CreateService()
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Conflict,
            result.ErrorType);

        Assert.Equal(
            "Could not complete release application.",
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
    // ReleaseAsync - Entity State
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_UpdatesDetentionWithCorrectReleaseData()
    {
        SetupAuthenticatedUser(55);
        SetupTransaction();
        SetupRollback();
        SetupCommit();

        var detention = new DetainedLicense
        {
            DetainID = 50,
            LicenseID = 100,
            DetainDate = DateTime.UtcNow.AddDays(-5),
            FineFees = 50,
            CreatedByUserID = 10,
            IsReleased = false
        };

        _repository
            .Setup(x => x.GetByIdForUpdateAsync(50))
            .ReturnsAsync(detention);

        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(Result.Success());

        var before = DateTime.UtcNow;

        var result =
            await CreateService()
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        var after = DateTime.UtcNow;

        Assert.True(result.IsSuccess);

        Assert.True(detention.IsReleased);
        Assert.NotNull(detention.ReleaseDate);
        Assert.Equal(55, detention.ReleasedByUserID);
        Assert.Equal(700, detention.ReleaseApplicationID);

        Assert.InRange(
            detention.ReleaseDate!.Value,
            before,
            after);
    }

    // =========================================================
    // ReleaseAsync - Success
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenEverythingIsValid_CommitsAndReturnsSuccess()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();
        SetupCommit();

        SetupValidReleaseDetention();
        SetupValidLicense();

        _applicationTypeService
            .Setup(x => x.GetApplicationTypeByIdAsync(5))
            .ReturnsAsync(
                Result<ApplicationTypeDto>.Success(
                    new ApplicationTypeDto
                    {
                        ApplicationTypeId = 5
                    }));

        _applicationService
            .Setup(x => x.AddNewApplicationAsync(
                It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(700));

        _licenseRepository
            .Setup(x => x.HasAnotherActiveLicenseAsync(
                20,
                3,
                100))
            .ReturnsAsync(true);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationService
            .Setup(x => x.CompleteApplicationAsync(700))
            .ReturnsAsync(Result.Success());

        var result =
            await CreateService()
                .ReleaseAsync(
                    new ReleaseDetainedLicenseDto
                    {
                        DetainID = 50
                    });

        Assert.True(result.IsSuccess);

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
    // ReleaseAsync - Exception
    // =========================================================

    [Fact]
    public async Task ReleaseAsync_WhenUnexpectedExceptionOccurs_RollsBackAndRethrows()
    {
        SetupAuthenticatedUser();
        SetupTransaction();
        SetupRollback();

        _repository
            .Setup(x => x.GetByIdForUpdateAsync(50))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database failure."));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateService()
                    .ReleaseAsync(
                        new ReleaseDetainedLicenseDto
                        {
                            DetainID = 50
                        }));

        Assert.Equal(
            "Database failure.",
            exception.Message);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}