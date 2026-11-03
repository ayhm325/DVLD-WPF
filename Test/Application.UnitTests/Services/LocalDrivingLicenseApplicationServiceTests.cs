using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;
using System.Data;

namespace Application.UnitTests.Services;

public sealed class LocalDrivingLicenseApplicationServiceTests
{
    private readonly Mock<ILocalDrivingLicenseApplicationRepository> _repository = new();
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<IApplicationRepository> _applicationRepository = new();
    private readonly Mock<IApplicationTypeRepository> _applicationTypeRepository = new();
    private readonly Mock<ILicenseClassService> _licenseClassService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<ITestAppointmentRepository> _testAppointmentRepository = new();
    private readonly Mock<IPersonRepository> _personRepository = new();
    private readonly Mock<IJordanClock> _clock = new();

    private LocalDrivingLicenseApplicationService CreateService(
        bool withAppointmentRepo = false,
        bool withPersonRepo = true,
        bool withClock = true) => new(
        _repository.Object, _licenseRepository.Object, _unitOfWork.Object,
        _applicationRepository.Object, _applicationTypeRepository.Object,
        _licenseClassService.Object, _currentUserService.Object,
        _applicationService.Object,
        withAppointmentRepo ? _testAppointmentRepository.Object : null,
        withPersonRepo ? _personRepository.Object : null,
        withClock ? _clock.Object : null);

    private void SetupTransactionWithResult<T>()
    {
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<T>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (
                Func<IUnitOfWorkTransaction, Task<Result<T>>> operation,
                IsolationLevel _,
                CancellationToken __) =>
            {
                var result = await operation(_transaction.Object);
                if (result.IsFailure)
                {
                    await _transaction.Object.RollbackAsync();
                }

                return result;
            });
    }

    private void SetupTransaction()
    {
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (
                Func<IUnitOfWorkTransaction, Task<Result>> operation,
                IsolationLevel _,
                CancellationToken __) =>
            {
                var result = await operation(_transaction.Object);
                if (result.IsFailure)
                {
                    await _transaction.Object.RollbackAsync();
                }

                return result;
            });
    }

    private static LocalDrivingLicenseApplication CreateEntity(
        int id = 1, int applicationId = 10, int licenseClassId = 3) => new()
        {
            LocalDrivingLicenseApplicationID = id,
            ApplicationID = applicationId,
            LicenseClassID = licenseClassId,
            Application = new ApplicationD
            {
                ApplicationID = applicationId,
                ApplicantPersonID = 20,
                ApplicationTypeID = 1,
                ApplicationStatus = AppStatus.New,
                PaidFees = 25m,
                ApplicationDate = new DateTime(2026, 1, 1)
            }
        };

    [Fact]
    public void Constructor_WhenAnyDependencyIsNull_ThrowsArgumentNullException()
    {
        Assert.Equal("repository", Assert.Throws<ArgumentNullException>(() =>
            new LocalDrivingLicenseApplicationService(
                null!, _licenseRepository.Object, _unitOfWork.Object,
                _applicationRepository.Object, _applicationTypeRepository.Object,
                _licenseClassService.Object, _currentUserService.Object,
                _applicationService.Object)).ParamName);

        Assert.Equal("licenseRepository", Assert.Throws<ArgumentNullException>(() =>
            new LocalDrivingLicenseApplicationService(
                _repository.Object, null!, _unitOfWork.Object,
                _applicationRepository.Object, _applicationTypeRepository.Object,
                _licenseClassService.Object, _currentUserService.Object,
                _applicationService.Object)).ParamName);

        Assert.Equal("unitOfWork", Assert.Throws<ArgumentNullException>(() =>
            new LocalDrivingLicenseApplicationService(
                _repository.Object, _licenseRepository.Object, null!,
                _applicationRepository.Object, _applicationTypeRepository.Object,
                _licenseClassService.Object, _currentUserService.Object,
                _applicationService.Object)).ParamName);
    }

    [Fact]
    public async Task GetAll_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            CreateService().GetAllLocalDrivingLicenseApplicationsAsync(null!));

        _repository.Verify(x => x.GetAllAsync(It.IsAny<PaginationRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsPagedResult()
    {
        var entities = new[] { CreateEntity(1, 10, 3), CreateEntity(2, 20, 4) };

        _repository.Setup(x => x.GetAllAsync(It.IsAny<PaginationRequest>()))
            .ReturnsAsync(new PagedResult<LocalDrivingLicenseApplication>
            {
                Items = entities,
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 25
            });

        _repository.Setup(x => x.GetPassedTestCountsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, int> { [1] = 2, [2] = 3 });

        _licenseRepository.Setup(x => x.GetApplicationIdsWithLicensesAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync([10]);

        var result = await CreateService().GetAllLocalDrivingLicenseApplicationsAsync(
            new PaginationRequest { PageNumber = 2, PageSize = 10 });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var page = result.Value!;
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal(2, page.Items.Count);

        _repository.Verify(x => x.GetAllAsync(
            It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 10)), Times.Once);

        _repository.Verify(x => x.GetPassedTestCountsAsync(
            It.Is<IEnumerable<int>>(ids => ids.Contains(1) && ids.Contains(2))), Times.Once);

        _licenseRepository.Verify(x => x.GetApplicationIdsWithLicensesAsync(
            It.Is<IEnumerable<int>>(ids => ids.Contains(10) && ids.Contains(20))), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetLocalDrivingLicenseApplicationByIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid local driving license application ID.", result.Error);
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().GetLocalDrivingLicenseApplicationByIdAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetNewFees_WhenApplicationTypeNotFound_ReturnsNotFound()
    {
        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync((ApplicationType?)null);

        var result = await CreateService().GetNewLocalDrivingLicenseApplicationFeesAsync();

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Application type not found.", result.Error);
    }

    [Fact]
    public async Task GetNewFees_WhenApplicationTypeExists_ReturnsFees()
    {
        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 42.5m });

        var result = await CreateService().GetNewLocalDrivingLicenseApplicationFeesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(42.5m, result.Value);
    }

    [Fact]
    public async Task GetApplicationIdByLocalId_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetApplicationIdByLocalIdAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _repository.Verify(x => x.GetApplicationIdByLocalIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetApplicationIdByLocalId_WhenMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetApplicationIdByLocalIdAsync(5)).ReturnsAsync((int?)null);

        var result = await CreateService().GetApplicationIdByLocalIdAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Main application not found for this local application.", result.Error);
    }

    [Fact]
    public async Task GetApplicationIdByLocalId_WhenFound_ReturnsId()
    {
        _repository.Setup(x => x.GetApplicationIdByLocalIdAsync(5)).ReturnsAsync(25);

        var result = await CreateService().GetApplicationIdByLocalIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value);
    }

    [Fact]
    public async Task Exists_WhenIdInvalid_ReturnsFalse()
    {
        Assert.False(await CreateService().IsLocalDrivingLicenseApplicationExistsAsync(0));
        _repository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Exists_WhenEntityExists_ReturnsTrue()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));

        Assert.True(await CreateService().IsLocalDrivingLicenseApplicationExistsAsync(5));
    }

    [Fact]
    public async Task Exists_WhenEntityMissing_ReturnsFalse()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        Assert.False(await CreateService().IsLocalDrivingLicenseApplicationExistsAsync(5));
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetApplicationBasicInfoAsync(0);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenLocalApplicationMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().GetApplicationBasicInfoAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetApplicationBasicInfo_WhenApplicationNavigationMissing_ReturnsNotFound()
    {
        var entity = CreateEntity(5);
        entity.Application = null!;
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(entity);

        var result = await CreateService().GetApplicationBasicInfoAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Main application information not found.", result.Error);
    }

    [Fact]
    public async Task Cancel_WhenInvalidId_ReturnsValidation()
    {
        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(0);

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _applicationService.Verify(
            x => x.CancelApplicationAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_WhenApplicationMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Cancel_WhenNavigationMissing_ReturnsFailure()
    {
        var entity = CreateEntity(5);
        entity.Application = null!;
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(entity);

        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Main application information is missing.", result.Error);
    }

    [Fact]
    public async Task Cancel_WhenValid_DelegatesToApplicationService()
    {
        SetupTransaction();
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        var expected = Result.Success();
        _applicationService.Setup(x => x.CancelApplicationAsync(10)).ReturnsAsync(expected);

        var result = await CreateService().CancelLocalDrivingLicenseApplicationAsync(5);

        Assert.True(result.IsSuccess);
        _applicationService.Verify(x => x.CancelApplicationAsync(10), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenValid_WithActiveAppointments_CascadesCancellation()
    {
        SetupTransaction();
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        _applicationService.Setup(x => x.CancelApplicationAsync(10)).ReturnsAsync(Result.Success());

        var activeAppointment = new TestAppointment
        {
            TestAppointmentID = 101,
            LocalDrivingLicenseApplicationID = 5,
            TestTypeID = 1,
            AppointmentDate = DateTime.UtcNow.AddDays(3),
            PaidFees = 20,
            CreatedByUserID = 2,
            IsLocked = false,
            Status = AppointmentStatus.Scheduled
        };

        _testAppointmentRepository
            .Setup(x => x.GetActiveScheduledByLocalAppIdForUpdateAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TestAppointment> { activeAppointment });

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService(withAppointmentRepo: true).CancelLocalDrivingLicenseApplicationAsync(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.Cancelled, activeAppointment.Status);
        Assert.True(activeAppointment.IsLocked);
        Assert.NotNull(activeAppointment.CancelledAt);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task Update_WhenValid_ReadsForUpdateInsideTransactionAndCommits()
    {
        SetupTransaction();
        var entity = CreateEntity(5, 10, 3);
        bool inTransaction = false;

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result>>>(),
                IsolationLevel.Serializable,
                It.IsAny<CancellationToken>()))
            .Returns(async (
                Func<IUnitOfWorkTransaction, Task<Result>> operation,
                IsolationLevel _,
                CancellationToken __) =>
            {
                inTransaction = true;
                return await operation(_transaction.Object);
            });

        _repository.Setup(x => x.GetForUpdateAsync(5))
            .ReturnsAsync(() =>
            {
                Assert.True(inTransaction);
                return entity;
            });

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(4))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 4 }));

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 4))
            .ReturnsAsync((int?)null);

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await CreateService().UpdateLocalDrivingLicenseApplicationAsync(
            5,
            new UpdateLocalDrivingLicenseApplicationDto { LicenseClassID = 4 });

        Assert.True(result.IsSuccess);
        Assert.Equal(4, entity.LicenseClassID);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenDuplicateExists_ReturnsConflictAndRollsBack()
    {
        SetupTransaction();
        var entity = CreateEntity(5, 10, 3);

        _repository.Setup(x => x.GetForUpdateAsync(5))
            .ReturnsAsync(entity);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(4))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 4 }));

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 4))
            .ReturnsAsync(99);

        var result = await CreateService().UpdateLocalDrivingLicenseApplicationAsync(
            5,
            new UpdateLocalDrivingLicenseApplicationDto { LicenseClassID = 4 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(3, entity.LicenseClassID);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenInvalidId_ReturnsValidation()
    {
        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(0);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync((LocalDrivingLicenseApplication?)null);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Delete_WhenApplicationStatusIsNotNew_ReturnsConflict()
    {
        var entity = CreateEntity(5);
        entity.Application!.ApplicationStatus = AppStatus.Cancelled;
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(entity);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Only a New application can be deleted.", result.Error);
        _repository.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenRepositoryDeleteFails_ReturnsFailure()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        _repository.Setup(x => x.DeleteAsync(5)).ReturnsAsync(false);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Failed to delete local driving license application.", result.Error);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WhenDeleteAndSaveSucceed_ReturnsSuccess()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        _repository.Setup(x => x.DeleteAsync(5)).ReturnsAsync(true);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.True(result.IsSuccess);
        _repository.Verify(x => x.DeleteAsync(5), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenSaveReturnsZero_ReturnsFailure()
    {
        _repository.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(CreateEntity(5));
        _repository.Setup(x => x.DeleteAsync(5)).ReturnsAsync(true);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().DeleteLocalDrivingLicenseApplicationAsync(5);

        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to save local driving license application deletion.",
            result.Error);
    }
    [Fact]
    public async Task Create_WhenApplicantDoesNotExist_ReturnsNotFound()
    {
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);
        _personRepository.Setup(x => x.GetPersonByIdAsync(999))
            .ReturnsAsync((Person?)null);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(999, 3);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Contains("Applicant person was not found", result.Error);
        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(
            It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
            It.IsAny<IsolationLevel>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_WhenApplicantIdIsInvalid_ReturnsValidationFailure(int invalidApplicantId)
    {
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(invalidApplicantId, 3);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        _personRepository.Verify(x => x.GetPersonByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenApplicantYoungerThanMinimumAge_ReturnsValidationFailure()
    {
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var referenceDate = new DateTime(2026, 11, 3, 10, 0, 0);
        _clock.Setup(x => x.Now).Returns(referenceDate);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2009, 11, 4),
            FirstName = "Ali",
            LastName = "Hassan"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains("minimum age requirement", result.Error);
        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(
            It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
            It.IsAny<IsolationLevel>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenApplicantExactlyAtMinimumAge_Succeeds()
    {
        SetupTransactionWithResult<int>();
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var referenceDate = new DateTime(2026, 11, 3, 10, 0, 0);
        _clock.Setup(x => x.Now).Returns(referenceDate);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2008, 11, 3),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m });

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 3)).ReturnsAsync((int?)null);

        _applicationRepository.Setup(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>()))
            .Callback<ApplicationD>(app => app.ApplicationID = 100)
            .Returns(Task.CompletedTask);

        _repository.Setup(x => x.AddAsync(It.IsAny<LocalDrivingLicenseApplication>()))
            .Callback<LocalDrivingLicenseApplication>(app => app.LocalDrivingLicenseApplicationID = 50)
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(50, result.Value);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenApplicantOlderThanMinimumAge_Succeeds()
    {
        SetupTransactionWithResult<int>();
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var referenceDate = new DateTime(2026, 11, 3, 10, 0, 0);
        _clock.Setup(x => x.Now).Returns(referenceDate);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(1995, 5, 20),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m });

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 3)).ReturnsAsync((int?)null);

        _applicationRepository.Setup(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>()))
            .Callback<ApplicationD>(app => app.ApplicationID = 101)
            .Returns(Task.CompletedTask);

        _repository.Setup(x => x.AddAsync(It.IsAny<LocalDrivingLicenseApplication>()))
            .Callback<LocalDrivingLicenseApplication>(app => app.LocalDrivingLicenseApplicationID = 51)
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(51, result.Value);
    }

    [Fact]
    public async Task Create_WhenBirthdayIsTodayInJordanTime_Succeeds()
    {
        SetupTransactionWithResult<int>();
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var jordanFacilityTime = new DateTime(2026, 11, 3, 23, 45, 0);
        _clock.Setup(x => x.Now).Returns(jordanFacilityTime);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2008, 11, 3),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m });

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 3)).ReturnsAsync((int?)null);

        _applicationRepository.Setup(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>()))
            .Callback<ApplicationD>(app => app.ApplicationID = 102)
            .Returns(Task.CompletedTask);

        _repository.Setup(x => x.AddAsync(It.IsAny<LocalDrivingLicenseApplication>()))
            .Callback<LocalDrivingLicenseApplication>(app => app.LocalDrivingLicenseApplicationID = 52)
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(52, result.Value);
    }

    [Fact]
    public async Task Create_WhenBirthdayIsTomorrowInJordanTime_ReturnsValidationFailure()
    {
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var jordanFacilityTime = new DateTime(2026, 11, 3, 23, 45, 0);
        _clock.Setup(x => x.Now).Returns(jordanFacilityTime);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2008, 11, 4),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains("minimum age requirement", result.Error);
    }

    [Theory]
    [InlineData(2026, 2, 27, 18, false)]
    [InlineData(2026, 2, 28, 18, true)]
    [InlineData(2028, 2, 28, 20, false)]
    [InlineData(2028, 2, 29, 20, true)]
    public async Task Create_WhenLeapYearBirthday_CalculatesAgeCorrectly(
        int testYear, int testMonth, int testDay, int minAllowedAge, bool expectedSuccess)
    {
        SetupTransactionWithResult<int>();
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var referenceDate = new DateTime(testYear, testMonth, testDay, 12, 0, 0);
        _clock.Setup(x => x.Now).Returns(referenceDate);

        var leapDayPerson = new Person
        {
            PersonId = 25,
            DateOfBirth = new DateTime(2008, 2, 29),
            FirstName = "Sami",
            LastName = "Nader"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(25)).ReturnsAsync(leapDayPerson);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = (byte)minAllowedAge }));

        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m });

        _repository.Setup(x => x.HasDuplicateApplicationAsync(25, 3)).ReturnsAsync((int?)null);

        _applicationRepository.Setup(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>()))
            .Callback<ApplicationD>(app => app.ApplicationID = 103)
            .Returns(Task.CompletedTask);

        _repository.Setup(x => x.AddAsync(It.IsAny<LocalDrivingLicenseApplication>()))
            .Callback<LocalDrivingLicenseApplication>(app => app.LocalDrivingLicenseApplicationID = 53)
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(25, 3);

        if (expectedSuccess)
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(53, result.Value);
        }
        else
        {
            Assert.True(result.IsFailure);
            Assert.Equal(ErrorType.Validation, result.ErrorType);
            Assert.Contains("minimum age requirement", result.Error);
        }
    }

    [Fact]
    public async Task Create_WhenLicenseClassNotFound_ReturnsNotFound()
    {
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2000, 1, 1),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(99))
            .ReturnsAsync(Result<LicenseClassDto>.FromNotFound("License class not found."));

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 99);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Create_WhenValid_SetsApplicationDateToJordanFacilityTime()
    {
        SetupTransactionWithResult<int>();
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var jordanFacilityTime = new DateTime(2026, 11, 3, 14, 25, 30);
        _clock.Setup(x => x.Now).Returns(jordanFacilityTime);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2000, 1, 1),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m });

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 3)).ReturnsAsync((int?)null);

        ApplicationD? capturedApplication = null;
        _applicationRepository.Setup(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>()))
            .Callback<ApplicationD>(app =>
            {
                capturedApplication = app;
                app.ApplicationID = 200;
            })
            .Returns(Task.CompletedTask);

        _repository.Setup(x => x.AddAsync(It.IsAny<LocalDrivingLicenseApplication>()))
            .Callback<LocalDrivingLicenseApplication>(app => app.LocalDrivingLicenseApplicationID = 75)
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedApplication);
        Assert.Equal(jordanFacilityTime, capturedApplication.ApplicationDate);
        Assert.Equal(jordanFacilityTime, capturedApplication.LastStatusDate);
        Assert.Equal(AppStatus.New, capturedApplication.ApplicationStatus);
        Assert.Equal(15m, capturedApplication.PaidFees);
        Assert.Equal(2, capturedApplication.CreatedByUserID);
    }

    [Fact]
    public async Task Create_WhenDuplicateApplicationExists_ReturnsConflict()
    {
        SetupTransactionWithResult<int>();
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var referenceDate = new DateTime(2026, 11, 3, 10, 0, 0);
        _clock.Setup(x => x.Now).Returns(referenceDate);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2000, 1, 1),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m });

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 3)).ReturnsAsync(88);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Contains("already exists", result.Error);
        _applicationRepository.Verify(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>()), Times.Never);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WhenSaveChangesFails_RollsBackTransaction()
    {
        SetupTransactionWithResult<int>();
        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(2);

        var referenceDate = new DateTime(2026, 11, 3, 10, 0, 0);
        _clock.Setup(x => x.Now).Returns(referenceDate);

        var person = new Person
        {
            PersonId = 20,
            DateOfBirth = new DateTime(2000, 1, 1),
            FirstName = "Ahmad",
            LastName = "Khalil"
        };
        _personRepository.Setup(x => x.GetPersonByIdAsync(20)).ReturnsAsync(person);

        _licenseClassService.Setup(x => x.GetLicenseClassByIdAsync(3))
            .ReturnsAsync(Result<LicenseClassDto>.Success(new LicenseClassDto { LicenseClassID = 3, MinAllowedAge = 18 }));

        _applicationTypeRepository.Setup(x => x.GetApplicationTypeByIdAsync(1))
            .ReturnsAsync(new ApplicationType { ApplicationTypeId = 1, ApplicationFees = 15m });

        _repository.Setup(x => x.HasDuplicateApplicationAsync(20, 3)).ReturnsAsync((int?)null);

        _applicationRepository.Setup(x => x.AddNewApplicationAsync(It.IsAny<ApplicationD>()))
            .Returns(Task.CompletedTask);

        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await CreateService().CreateLocalDrivingLicenseApplicationAsync(20, 3);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Contains("Failed to create", result.Error);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
