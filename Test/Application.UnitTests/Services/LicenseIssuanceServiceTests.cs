using System.Data;
using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DriverDTO;
using Application.DTOs.LicenseDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.UnitTests.Services;

public class LicenseIssuanceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILicenseRepository> _licenseRepositoryMock;
    private readonly Mock<ILocalDrivingLicenseApplicationService> _localApplicationServiceMock;
    private readonly Mock<IApplicationService> _applicationServiceMock;
    private readonly Mock<IDriverService> _driverServiceMock;
    private readonly Mock<IPersonService> _personServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILicenseClassService> _licenseClassServiceMock;
    private readonly Mock<ITestWorkflowService> _testWorkflowServiceMock;
    private readonly Mock<ILogger<LicenseIssuanceService>> _loggerMock;
    private readonly Mock<IUnitOfWorkTransaction> _transactionMock;

    private readonly LicenseIssuanceService _sut;

    public LicenseIssuanceServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _licenseRepositoryMock = new Mock<ILicenseRepository>();
        _localApplicationServiceMock =
            new Mock<ILocalDrivingLicenseApplicationService>();
        _applicationServiceMock =
            new Mock<IApplicationService>();
        _driverServiceMock =
            new Mock<IDriverService>();
        _personServiceMock =
            new Mock<IPersonService>();
        _currentUserServiceMock =
            new Mock<ICurrentUserService>();
        _licenseClassServiceMock =
            new Mock<ILicenseClassService>();
        _testWorkflowServiceMock =
            new Mock<ITestWorkflowService>();
        _loggerMock =
            new Mock<ILogger<LicenseIssuanceService>>();
        _transactionMock =
            new Mock<IUnitOfWorkTransaction>();

        _sut = new LicenseIssuanceService(
            _unitOfWorkMock.Object,
            _licenseRepositoryMock.Object,
            _localApplicationServiceMock.Object,
            _applicationServiceMock.Object,
            _driverServiceMock.Object,
            _personServiceMock.Object,
            _currentUserServiceMock.Object,
            _licenseClassServiceMock.Object,
            _testWorkflowServiceMock.Object,
            _loggerMock.Object);
    }


    [Fact]
    public async Task IssueFirstLicenseAsync_InvalidLocalApplicationId_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 0;

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid license ID.",
            result.Error);

        _currentUserServiceMock.VerifyGet(
            service => service.IsLoggedIn,
            Times.Never);

        _localApplicationServiceMock.Verify(
            service =>
                service.GetLocalDrivingLicenseApplicationByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_UserNotAuthenticated_ReturnsForbidden()
    {
        // Arrange
        const int localAppId = 10;

        _currentUserServiceMock
            .SetupGet(service => service.IsLoggedIn)
            .Returns(false);

        _currentUserServiceMock
            .SetupGet(service => service.UserId)
            .Returns(0);

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "Authenticated user is required.",
            result.Error);

        _localApplicationServiceMock.Verify(
            service =>
                service.GetLocalDrivingLicenseApplicationByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }


    [Fact]
    public async Task IssueFirstLicenseAsync_LocalApplicationNotFound_ReturnsNotFound()
    {
        // Arrange
        const int localAppId = 10;

        SetupAuthenticatedUser();

        _localApplicationServiceMock
            .Setup(service =>
                service.GetLocalDrivingLicenseApplicationByIdAsync(
                    localAppId))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .FromNotFound("Local application not found."));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Local application not found.",
            result.Error);

        _localApplicationServiceMock.Verify(
            service =>
                service.GetApplicationIdByLocalIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_ApplicationIdLookupFails_PropagatesFailure()
    {
        // Arrange
        const int localAppId = 10;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(localAppId);

        _localApplicationServiceMock
            .Setup(service =>
                service.GetApplicationIdByLocalIdAsync(localAppId))
            .ReturnsAsync(
                Result<int>.FromNotFound(
                    "Application was not found."));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application was not found.",
            result.Error);

        _applicationServiceMock.Verify(
            service =>
                service.GetApplicationForIssuanceAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_ApplicationNotFound_ReturnsNotFound()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(localAppId);

        _localApplicationServiceMock
            .Setup(service =>
                service.GetApplicationIdByLocalIdAsync(localAppId))
            .ReturnsAsync(
                Result<int>.Success(applicationId));

        _applicationServiceMock
            .Setup(service =>
                service.GetApplicationByIdAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.FromNotFound(
                    "Application was not found."));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Application was not found.",
            result.Error);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_NotNewApplicationType_ReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(localAppId);
        SetupValidApplicationLookup(applicationId);

        var application = CreateApplication(
            applicationId,
            applicationTypeId: 2);

        _applicationServiceMock
            .Setup(service =>
                service.GetApplicationByIdAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(application));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "First-time license issuance is only allowed for a new local driving license application.",
            result.Error);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_InvalidApplicant_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(localAppId);
        SetupValidApplicationLookup(applicationId);

        var application = CreateApplication(
            applicationId,
            applicantPersonId: 0);

        _applicationServiceMock
            .Setup(service =>
                service.GetApplicationByIdAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(application));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "The application does not have a valid applicant.",
            result.Error);

        _personServiceMock.Verify(
            service =>
                service.GetPersonByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_PersonNotFound_ReturnsNotFound()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(localAppId);
        SetupValidApplicationLookup(applicationId);

        var application = CreateApplication(
            applicationId,
            applicantPersonId: personId);

        _applicationServiceMock
            .Setup(service =>
                service.GetApplicationForIssuanceAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(application));

        _personServiceMock
            .Setup(service =>
                service.GetPersonByIdAsync(personId))
            .ReturnsAsync(
                Result<PersonDto>.FromNotFound(
                    "Applicant person was not found."));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Applicant person was not found.",
            result.Error);

        _licenseClassServiceMock.Verify(
            service =>
                service.GetLicenseClassByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }


    [Fact]
    public async Task IssueFirstLicenseAsync_InvalidLicenseClassId_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(
            localAppId,
            licenseClassId: 0);

        SetupValidApplicationLookup(
            applicationId,
            applicantPersonId: personId);

        _personServiceMock
            .Setup(service =>
                service.GetPersonByIdAsync(personId))
            .ReturnsAsync(
                Result<PersonDto>.Success(
                    CreatePerson(personId)));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid license class ID.",
            result.Error);

        _licenseClassServiceMock.Verify(
            service =>
                service.GetLicenseClassByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_LicenseClassNotFound_ReturnsNotFound()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;
        const int licenseClassId = 3;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(
            localAppId,
            licenseClassId);

        SetupValidApplicationLookup(
            applicationId,
            applicantPersonId: personId);

        SetupPerson(personId);

        _licenseClassServiceMock
            .Setup(service =>
                service.GetLicenseClassByIdAsync(
                    licenseClassId))
            .ReturnsAsync(
                Result<LicenseClassDto>.FromNotFound(
                    "License class was not found."));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "License class was not found.",
            result.Error);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(
                    It.IsAny<IsolationLevel>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_InvalidValidityPeriod_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;
        const int licenseClassId = 3;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(
            localAppId,
            licenseClassId);

        SetupValidApplicationLookup(
            applicationId,
            applicantPersonId: personId);

        SetupPerson(personId);

        _licenseClassServiceMock
            .Setup(service =>
                service.GetLicenseClassByIdAsync(
                    licenseClassId))
            .ReturnsAsync(
                Result<LicenseClassDto>.Success(
                    CreateLicenseClass(
                        licenseClassId,
                        validityLength: 0)));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "License class has an invalid validity period.",
            result.Error);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(
                    It.IsAny<IsolationLevel>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_NegativeLicenseFees_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;
        const int licenseClassId = 3;

        SetupAuthenticatedUser();
        SetupValidLocalApplication(
            localAppId,
            licenseClassId);

        SetupValidApplicationLookup(
            applicationId,
            applicantPersonId: personId);

        SetupPerson(personId);

        _licenseClassServiceMock
            .Setup(service =>
                service.GetLicenseClassByIdAsync(
                    licenseClassId))
            .ReturnsAsync(
                Result<LicenseClassDto>.Success(
                    CreateLicenseClass(
                        licenseClassId,
                        fees: -1)));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "License class has invalid fees.",
            result.Error);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(
                    It.IsAny<IsolationLevel>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task IssueFirstLicenseAsync_ApplicationNotNewInsideTransaction_RollsBackAndReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _applicationServiceMock
            .Setup(service =>
                service.GetApplicationForIssuanceAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(
                    CreateApplication(applicationId, personId, AppStatus.Cancelled)));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The application is not in a valid state for license issuance.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_NotAllTestsPassed_RollsBackAndReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _applicationServiceMock
            .SetupSequence(service =>
                service.GetApplicationForIssuanceAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(
                    CreateApplication(
                        applicationId,
                        applicantPersonId: personId,
                        status: AppStatus.New)))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(
                    CreateApplication(
                        applicationId,
                        applicantPersonId: personId,
                        status: AppStatus.New)));

        _testWorkflowServiceMock
            .Setup(service =>
                service.HasPassedAllTestsAsync(localAppId))
            .ReturnsAsync(false);

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The applicant has not passed all required tests.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _licenseRepositoryMock.Verify(
            repository =>
                repository.AddLicenseAsync(
                    It.IsAny<Domain.Entities.License>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_ApplicationAlreadyHasLicense_RollsBackAndReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(true);

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "A license has already been issued for this application.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }


    [Fact]
    public async Task IssueFirstLicenseAsync_ExistingDriverWithInvalidId_RollsBackAndReturnsFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(false);

        _driverServiceMock
            .Setup(service =>
                service.GetByPersonIdAsync(personId))
            .ReturnsAsync(
                Result<DriverDto>.Success(
                    new DriverDto
                    {
                        DriverID = 0,
                        PersonID = personId
                    }));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Driver information was returned incorrectly.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_DriverNotFound_CreatesDriver()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(false);

        _driverServiceMock
            .Setup(service =>
                service.GetByPersonIdAsync(personId))
            .ReturnsAsync(
                Result<DriverDto>.FromNotFound(
                    "Driver not found."));

        _driverServiceMock
            .Setup(service =>
                service.AddAsync(
                    It.Is<CreateDriverDto>(
                        dto => dto.PersonID == personId)))
            .ReturnsAsync(
                Result<int>.Success(500));

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsActiveLicenseExistsAsync(
                    500,
                    3))
            .ReturnsAsync(false);

        SetupSuccessfulLicenseSave();
        SetupSuccessfulCompletion(applicationId);

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.True(result.IsSuccess);

        _driverServiceMock.Verify(
            service =>
                service.AddAsync(
                    It.Is<CreateDriverDto>(
                        dto => dto.PersonID == personId)),
            Times.Once);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_DriverCreationFails_RollsBackAndPropagatesFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(false);

        _driverServiceMock
            .Setup(service =>
                service.GetByPersonIdAsync(personId))
            .ReturnsAsync(
                Result<DriverDto>.FromNotFound(
                    "Driver not found."));

        _driverServiceMock
            .Setup(service =>
                service.AddAsync(
                    It.IsAny<CreateDriverDto>()))
            .ReturnsAsync(
                Result<int>.FromFailure(
                    "Failed to create driver."));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to create driver.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_ActiveLicenseExists_RollsBackAndReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;
        const int driverId = 500;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(false);

        _driverServiceMock
            .Setup(service =>
                service.GetByPersonIdAsync(personId))
            .ReturnsAsync(
                Result<DriverDto>.Success(
                    new DriverDto
                    {
                        DriverID = driverId,
                        PersonID = personId
                    }));

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsActiveLicenseExistsAsync(
                    driverId,
                    3))
            .ReturnsAsync(true);

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The driver already has an active license for this license class.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _licenseRepositoryMock.Verify(
            repository =>
                repository.AddLicenseAsync(
                    It.IsAny<Domain.Entities.License>()),
            Times.Never);
    }


    [Fact]
    public async Task IssueFirstLicenseAsync_SaveFails_RollsBackAndReturnsFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;
        const int driverId = 500;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(false);

        SetupExistingDriver(personId, driverId);

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsActiveLicenseExistsAsync(
                    driverId,
                    3))
            .ReturnsAsync(false);

        _licenseRepositoryMock
            .Setup(repository =>
                repository.AddLicenseAsync(
                    It.IsAny<Domain.Entities.License>()))
            .Callback<Domain.Entities.License>(
                license => license.LicenseID = 1000)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                " Test notes ");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to save the driving license.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_ApplicationCompletionFails_RollsBackAndPropagatesFailure()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;
        const int driverId = 500;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(false);

        SetupExistingDriver(personId, driverId);

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsActiveLicenseExistsAsync(
                    driverId,
                    3))
            .ReturnsAsync(false);

        _licenseRepositoryMock
            .Setup(repository =>
                repository.AddLicenseAsync(
                    It.IsAny<Domain.Entities.License>()))
            .Callback<Domain.Entities.License>(
                license => license.LicenseID = 1000)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationServiceMock
            .Setup(service =>
                service.CompleteApplicationAsync(applicationId))
            .ReturnsAsync(
                Result.Failure("Failed to complete application."));

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to complete application.",
            result.Error);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueFirstLicenseAsync_ValidRequest_ReturnsLicenseIdAndCommitsTransaction()
    {
        // Arrange
        const int localAppId = 10;
        const int applicationId = 100;
        const int personId = 50;
        const int driverId = 500;
        const int licenseClassId = 3;
        const int licenseId = 1000;

        SetupValidPreTransactionState(
            localAppId,
            applicationId,
            personId,
            licenseClassId);

        SetupTransaction();

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsApplicationHasLicenseAsync(applicationId))
            .ReturnsAsync(false);

        SetupExistingDriver(personId, driverId);

        _licenseRepositoryMock
            .Setup(repository =>
                repository.IsActiveLicenseExistsAsync(
                    driverId,
                    licenseClassId))
            .ReturnsAsync(false);

        Domain.Entities.License? createdLicense = null;

        _licenseRepositoryMock
            .Setup(repository =>
                repository.AddLicenseAsync(
                    It.IsAny<Domain.Entities.License>()))
            .Callback<Domain.Entities.License>(
                license =>
                {
                    createdLicense = license;
                    license.LicenseID = licenseId;
                })
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationServiceMock
            .Setup(service =>
                service.CompleteApplicationAsync(applicationId))
            .ReturnsAsync(Result.Success());

        // Act
        Result<int> result =
            await _sut.IssueFirstLicenseAsync(
                localAppId,
                "   First license   ");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(licenseId, result.Value);

        Assert.NotNull(createdLicense);

        Assert.Equal(
            applicationId,
            createdLicense!.ApplicationID);

        Assert.Equal(
            driverId,
            createdLicense.DriverID);

        Assert.Equal(
            licenseClassId,
            createdLicense.LicenseClass);

        Assert.Equal(
            IssueReason.FirstTime,
            createdLicense.IssueReason);

        Assert.Equal(
            "First license",
            createdLicense.Notes);

        Assert.Equal(
            1,
            createdLicense.CreatedByUserID);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _applicationServiceMock.Verify(
            service =>
                service.CompleteApplicationAsync(applicationId),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionMock.Verify(
            transaction =>
                transaction.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }


    private void SetupAuthenticatedUser()
    {
        _currentUserServiceMock
            .SetupGet(service => service.IsLoggedIn)
            .Returns(true);

        _currentUserServiceMock
            .SetupGet(service => service.UserId)
            .Returns(1);
    }

    private void SetupTransaction()
    {
        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
    }

    private void SetupValidPreTransactionState(
        int localAppId,
        int applicationId,
        int personId,
        int licenseClassId = 3)
    {
        SetupAuthenticatedUser();

        SetupValidLocalApplication(
            localAppId,
            licenseClassId);

        SetupValidApplicationLookup(
            applicationId,
            personId);

        SetupPerson(personId);

        _licenseClassServiceMock
            .Setup(service =>
                service.GetLicenseClassByIdAsync(
                    licenseClassId))
            .ReturnsAsync(
                Result<LicenseClassDto>.Success(
                    CreateLicenseClass(
                        licenseClassId)));

        _applicationServiceMock
            .Setup(service => service.GetApplicationForIssuanceAsync(applicationId))
            .ReturnsAsync(Result<ApplicationDto>.Success(CreateApplication(applicationId, personId)));

        _testWorkflowServiceMock
            .Setup(service => service.HasPassedAllTestsAsync(localAppId))
            .ReturnsAsync(true);
    }

    private void SetupValidLocalApplication(
        int localAppId,
        int licenseClassId = 3)
    {
        _localApplicationServiceMock
            .Setup(service =>
                service.GetLocalDrivingLicenseApplicationByIdAsync(
                    localAppId))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>.Success(
                    new LocalDrivingLicenseApplicationListDto
                    {
                        LocalDrivingLicenseApplicationID = localAppId,
                        LicenseClassID = licenseClassId,
                        ApplicantPersonID = 50,
                        ApplicationStatus = AppStatus.New
                    }));
    }

    private void SetupValidApplicationLookup(
    int applicationId,
    int applicantPersonId = 50)
    {
        _localApplicationServiceMock
            .Setup(service =>
                service.GetApplicationIdByLocalIdAsync(
                    It.IsAny<int>()))
            .ReturnsAsync(
                Result<int>.Success(applicationId));

        var application = CreateApplication(
            applicationId,
            applicantPersonId);

        _applicationServiceMock
            .Setup(service =>
                service.GetApplicationByIdAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(application));

        _applicationServiceMock
            .Setup(service =>
                service.GetApplicationForIssuanceAsync(applicationId))
            .ReturnsAsync(
                Result<ApplicationDto>.Success(application));
    }

    private void SetupPerson(int personId)
    {
        _personServiceMock
            .Setup(service =>
                service.GetPersonByIdAsync(personId))
            .ReturnsAsync(
                Result<PersonDto>.Success(
                    CreatePerson(personId)));
    }

    private void SetupExistingDriver(
        int personId,
        int driverId)
    {
        _driverServiceMock
            .Setup(service =>
                service.GetByPersonIdAsync(personId))
            .ReturnsAsync(
                Result<DriverDto>.Success(
                    new DriverDto
                    {
                        DriverID = driverId,
                        PersonID = personId,
                        FullName = "Test Person",
                        NationalNo = "1234567890"
                    }));
    }

    private void SetupSuccessfulLicenseSave()
    {
        _licenseRepositoryMock
            .Setup(repository =>
                repository.AddLicenseAsync(
                    It.IsAny<Domain.Entities.License>()))
            .Callback<Domain.Entities.License>(
                license => license.LicenseID = 1000)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(unitOfWork =>
                unitOfWork.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _applicationServiceMock
            .Setup(service =>
                service.CompleteApplicationAsync(
                    It.IsAny<int>()))
            .ReturnsAsync(Result.Success());
    }

    private void SetupSuccessfulCompletion(
        int applicationId)
    {
        _applicationServiceMock
            .Setup(service =>
                service.CompleteApplicationAsync(applicationId))
            .ReturnsAsync(Result.Success());
    }

    private static ApplicationDto CreateApplication(
        int applicationId,
        int applicantPersonId = 50,
        AppStatus status = AppStatus.New,
        int applicationTypeId = 1)
    {
        return new ApplicationDto
        {
            ApplicationID = applicationId,
            ApplicantPersonID = applicantPersonId,
            ApplicationDate = DateTime.UtcNow.AddDays(-10),
            ApplicationTypeID = applicationTypeId,
            ApplicationStatus = status,
            LastStatusDate = DateTime.UtcNow.AddDays(-10),
            PaidFees = 20,
            CreatedByUserID = 1,
            CreatedByUserName = "testuser"
        };
    }

    private static PersonDto CreatePerson(
        int personId)
    {
        return new PersonDto
        {
            PersonId = personId,
            NationalNo = "1234567890",
            FirstName = "Test",
            SecondName = "Person",
            ThirdName = null,
            LastName = "User",
            FullName = "Test Person User",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            Address = "Amman",
            Phone = "0791234567",
            Email = "test@example.com",
            NationalityCountryID = 1,
            CountryName = "Jordan",
            ImagePath = null
        };
    }

    private static LicenseClassDto CreateLicenseClass(
        int licenseClassId,
        byte validityLength = 10,
        decimal fees = 50)
    {
        return new LicenseClassDto
        {
            LicenseClassID = licenseClassId,
            LicenseClassName = "Private",
            LicenseClassDescription = "Private vehicle license",
            MinAllowedAge = 18,
            DefaultValidityLength = validityLength,
            LicenseClassFees = fees
        };
    }
}
