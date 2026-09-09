using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace Application.UnitTests.Services;

public sealed class TestAppointmentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();

    private readonly Mock<ITestAppointmentRepository> _repository = new();
    private readonly Mock<ITestTypeRepository> _testTypeRepository = new();

    private readonly Mock<ILocalDrivingLicenseApplicationService>
        _localApplicationService = new();

    private readonly Mock<IApplicationTypeService>
        _applicationTypeService = new();

    private readonly Mock<IApplicationService>
        _applicationService = new();

    private readonly Mock<ICurrentUserService>
        _currentUserService = new();

    private readonly Mock<ITestWorkflowService>
        _workflowService = new();

    private readonly Mock<ILogger<TestAppointmentService>>
        _logger = new();

    // =========================================================
    // FACTORY
    // =========================================================

    private TestAppointmentService CreateService()
    {
        _unitOfWork
            .Setup(x =>
                x.BeginTransactionAsync(
                    It.IsAny<IsolationLevel>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _transaction
            .Setup(x =>
                x.CommitAsync(
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transaction
            .Setup(x =>
                x.RollbackAsync(
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(true);

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(10);

        return new TestAppointmentService(
            _unitOfWork.Object,
            _repository.Object,
            _testTypeRepository.Object,
            _localApplicationService.Object,
            _applicationTypeService.Object,
            _currentUserService.Object,
            _workflowService.Object,
            _applicationService.Object,
            _logger.Object);
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static CreateTestAppointmentDto ValidCreateDto(
        TestTypeEnum type = TestTypeEnum.Theory)
    {
        return new CreateTestAppointmentDto
        {
            TestTypeID = (int)type,
            LocalDrivingLicenseApplicationID = 100,
            AppointmentDate = DateTime.UtcNow.AddDays(1)
        };
    }

    private static UpdateTestAppointmentDto ValidUpdateDto(
        int id = 1)
    {
        return new UpdateTestAppointmentDto
        {
            TestAppointmentID = id,
            AppointmentDate = DateTime.UtcNow.AddDays(2)
        };
    }

    private static TestType ValidTestType(
        TestTypeEnum type = TestTypeEnum.Theory,
        decimal fees = 25m)
    {
        return new TestType
        {
            TestTypeId = (int)type,
            TestTypeTitle = type.ToString(),
            TestTypeDescription = $"{type} test",
            TestTypeFees = fees
        };
    }

    private static TestAppointment ValidAppointment(
        int id = 1,
        int userId = 10,
        bool locked = false,
        DateTime? date = null)
    {
        return new TestAppointment
        {
            TestAppointmentID = id,
            TestTypeID = (int)TestTypeEnum.Theory,
            LocalDrivingLicenseApplicationID = 100,
            AppointmentDate =
                date ?? DateTime.UtcNow.AddDays(1),
            PaidFees = 25m,
            CreatedByUserID = userId,
            IsLocked = locked,

            TestType = ValidTestType(),

            User = new User
            {
                UserId = userId,
                UserName = "tester"
            }
        };
    }

    private void SetupBasicAddFlow()
    {
        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasUserConflictAsync(
                    10,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(false);
    }

    private void SetupBasicScheduleFlow()
    {
        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasUserConflictAsync(
                    10,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(0);
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    [Fact]
    public void Constructor_WhenUnitOfWorkIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                null!,
                _repository.Object,
                _testTypeRepository.Object,
                _localApplicationService.Object,
                _applicationTypeService.Object,
                _currentUserService.Object,
                _workflowService.Object,
                _applicationService.Object,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenRepositoryIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                null!,
                _testTypeRepository.Object,
                _localApplicationService.Object,
                _applicationTypeService.Object,
                _currentUserService.Object,
                _workflowService.Object,
                _applicationService.Object,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenTestTypeRepositoryIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                _repository.Object,
                null!,
                _localApplicationService.Object,
                _applicationTypeService.Object,
                _currentUserService.Object,
                _workflowService.Object,
                _applicationService.Object,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenLocalApplicationServiceIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                _repository.Object,
                _testTypeRepository.Object,
                null!,
                _applicationTypeService.Object,
                _currentUserService.Object,
                _workflowService.Object,
                _applicationService.Object,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenApplicationTypeServiceIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                _repository.Object,
                _testTypeRepository.Object,
                _localApplicationService.Object,
                null!,
                _currentUserService.Object,
                _workflowService.Object,
                _applicationService.Object,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenCurrentUserServiceIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                _repository.Object,
                _testTypeRepository.Object,
                _localApplicationService.Object,
                _applicationTypeService.Object,
                null!,
                _workflowService.Object,
                _applicationService.Object,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenWorkflowServiceIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                _repository.Object,
                _testTypeRepository.Object,
                _localApplicationService.Object,
                _applicationTypeService.Object,
                _currentUserService.Object,
                null!,
                _applicationService.Object,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenApplicationServiceIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                _repository.Object,
                _testTypeRepository.Object,
                _localApplicationService.Object,
                _applicationTypeService.Object,
                _currentUserService.Object,
                _workflowService.Object,
                null!,
                _logger.Object));
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestAppointmentService(
                _unitOfWork.Object,
                _repository.Object,
                _testTypeRepository.Object,
                _localApplicationService.Object,
                _applicationTypeService.Object,
                _currentUserService.Object,
                _workflowService.Object,
                _applicationService.Object,
                null!));
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetByIdAsync_WhenIdInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result = await service.GetByIdAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid test appointment ID.",
            result.Error);

        _repository.Verify(
            x => x.GetByIdAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetByIdAsync(99))
            .ReturnsAsync((TestAppointment?)null);

        var result = await service.GetByIdAsync(99);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Appointment not found.",
            result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsDto()
    {
        var service = CreateService();

        var entity = ValidAppointment(5);

        _repository
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(entity);

        var result = await service.GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value!.TestAppointmentID);
        Assert.Equal(
            entity.TestTypeID,
            result.Value.TestTypeID);
        Assert.Equal(
            entity.LocalDrivingLicenseApplicationID,
            result.Value.LocalDrivingLicenseApplicationID);
        Assert.Equal(
            entity.PaidFees,
            result.Value.PaidFees);
        Assert.Equal(
            entity.CreatedByUserID,
            result.Value.CreatedByUserID);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<TestAppointment>());

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAllAsync_WhenAppointmentsExist_ReturnsAll()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
            [
                ValidAppointment(1),
                ValidAppointment(2)
            ]);

        var result = await service.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
    }

    // =========================================================
    // GET BY LOCAL APPLICATION
    // =========================================================

    [Fact]
    public async Task GetByLocalApplicationAsync_WhenIdInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service
                .GetByLocalDrivingLicenseApplicationIdAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetByLocalApplicationAsync_WhenValid_ReturnsAppointments()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetByLocalDrivingLicenseApplicationIdAsync(100))
            .ReturnsAsync(
            [
                ValidAppointment(1),
                ValidAppointment(2)
            ]);

        var result =
            await service
                .GetByLocalDrivingLicenseApplicationIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
    }

    // =========================================================
    // GET BY TEST TYPE
    // =========================================================

    [Fact]
    public async Task GetByTestTypeIdAsync_WhenInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.GetByTestTypeIdAsync(
                (TestTypeEnum)999);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid test type.",
            result.Error);
    }

    [Fact]
    public async Task GetByTestTypeIdAsync_WhenValid_ReturnsAppointments()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetByTestTypeIdAsync(
                    TestTypeEnum.Theory))
            .ReturnsAsync(
            [
                ValidAppointment(1)
            ]);

        var result =
            await service.GetByTestTypeIdAsync(
                TestTypeEnum.Theory);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    // =========================================================
    // GET BY USER
    // =========================================================

    [Fact]
    public async Task GetByCreatedUserIdAsync_WhenInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.GetByCreatedUserIdAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid user ID.",
            result.Error);
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_WhenValid_ReturnsAppointments()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetByCreatedUserIdAsync(10))
            .ReturnsAsync(
            [
                ValidAppointment(1, 10),
                ValidAppointment(2, 10)
            ]);

        var result =
            await service.GetByCreatedUserIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    // =========================================================
    // SCHEDULE INFO
    // =========================================================

    [Fact]
    public async Task GetScheduleInfoAsync_WhenInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.GetScheduleInfoAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetScheduleInfoAsync_WhenNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetScheduleInfoAsync(100))
            .ReturnsAsync((TestAppointment?)null);

        var result =
            await service.GetScheduleInfoAsync(100);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Appointment data not found.",
            result.Error);
    }

    [Fact]
    public async Task GetScheduleInfoAsync_WhenFound_ReturnsScheduleDto()
    {
        var service = CreateService();

        var entity = ValidAppointment(5);

        _repository
            .Setup(x => x.GetScheduleInfoAsync(5))
            .ReturnsAsync(entity);

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(
                    100,
                    (int)TestTypeEnum.Theory))
            .ReturnsAsync(2);

        var result =
            await service.GetScheduleInfoAsync(5);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value!.AppointmentID);
        Assert.Equal(2, result.Value.Trial);
    }

    // =========================================================
    // TEST TYPE FEES
    // =========================================================

    [Fact]
    public async Task GetTestTypeFeesAsync_WhenInvalid_ReturnsZero()
    {
        var service = CreateService();

        var result =
            await service.GetTestTypeFeesAsync(999);

        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task GetTestTypeFeesAsync_WhenNotFound_ReturnsZero()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((TestType?)null);

        var result =
            await service.GetTestTypeFeesAsync(1);

        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task GetTestTypeFeesAsync_WhenFound_ReturnsFees()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(
                ValidTestType(
                    TestTypeEnum.Theory,
                    40m));

        var result =
            await service.GetTestTypeFeesAsync(1);

        Assert.Equal(40m, result);
    }

    // =========================================================
    // TRIAL COUNT
    // =========================================================

    [Fact]
    public async Task GetTrialCountAsync_WhenInvalidApplication_ReturnsZero()
    {
        var service = CreateService();

        var result =
            await service.GetTrialCountAsync(
                0,
                1);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetTrialCountAsync_WhenInvalidTestType_ReturnsZero()
    {
        var service = CreateService();

        var result =
            await service.GetTrialCountAsync(
                100,
                999);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetTrialCountAsync_WhenValid_ReturnsRepositoryValue()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(3);

        var result =
            await service.GetTrialCountAsync(100, 1);

        Assert.Equal(3, result);
    }

    // =========================================================
    // ALREADY SCHEDULED
    // =========================================================

    [Fact]
    public async Task IsAppointmentAlreadyScheduledAsync_DelegatesToRepository()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(100, 1))
            .ReturnsAsync(true);

        var result =
            await service.IsAppointmentAlreadyScheduledAsync(
                100,
                1);

        Assert.True(result);

        _repository.Verify(
            x =>
                x.IsAppointmentAlreadyScheduledAsync(100, 1),
            Times.Once);
    }

    // =========================================================
    // ADD - VALIDATION
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenDtoNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.AddAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Test appointment data is required.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenTestTypeInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var dto = ValidCreateDto();
        dto.TestTypeID = 999;

        var result =
            await service.AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task AddAsync_WhenApplicationIdInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var dto = ValidCreateDto();
        dto.LocalDrivingLicenseApplicationID = 0;

        var result =
            await service.AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task AddAsync_WhenDateDefault_ReturnsValidation()
    {
        var service = CreateService();

        var dto = ValidCreateDto();
        dto.AppointmentDate = default;

        var result =
            await service.AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment date is required.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenDateInPast_ReturnsValidation()
    {
        var service = CreateService();

        var dto = ValidCreateDto();
        dto.AppointmentDate =
            DateTime.UtcNow.AddMinutes(-1);

        var result =
            await service.AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains(
            "Appointment date must be in the future.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenRetakeIdInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var dto = ValidCreateDto();
        dto.RetakeTestApplicationID = 0;

        var result =
            await service.AddAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // =========================================================
    // ADD - AUTH
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenNotLoggedIn_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "You must be logged in first.",
            result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenUserIdInvalid_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.UserId)
            .Returns(0);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    // =========================================================
    // ADD - TEST TYPE
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenTestTypeNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((TestType?)null);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Test type not found.",
            result.Error);

        _unitOfWork.Verify(
            x =>
                x.BeginTransactionAsync(
                    It.IsAny<IsolationLevel>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // ADD - WORKFLOW
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenWorkflowRejects_ReturnsFailure()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.Conflict(
                    "Test cannot be scheduled."));

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Test cannot be scheduled.",
            result.Error);

        _repository.Verify(
            x => x.AddAsync(
                It.IsAny<TestAppointment>()),
            Times.Never);
    }

    // =========================================================
    // ADD - DUPLICATE
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenAlreadyScheduled_ReturnsConflict()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(true);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "An appointment already exists for this test.",
            result.Error);
    }

    // =========================================================
    // ADD - LOCAL CONFLICT
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenLocalConflict_ReturnsConflict()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(true);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "This application already has an appointment at this date and time.",
            result.Error);
    }

    // =========================================================
    // ADD - USER CONFLICT
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenUserConflict_ReturnsConflict()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasUserConflictAsync(
                    10,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(true);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The current user already has an appointment at this date and time.",
            result.Error);
    }

    // =========================================================
    // ADD - SUCCESS
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenValid_CreatesAndCommits()
    {
        var service = CreateService();

        SetupBasicAddFlow();

        TestAppointment? captured = null;

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Callback<TestAppointment>(
                entity =>
                {
                    captured = entity;
                    entity.TestAppointmentID = 500;
                })
            .Returns(Task.CompletedTask);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);

        Assert.Equal(
            100,
            captured!.LocalDrivingLicenseApplicationID);

        Assert.Equal(
            1,
            captured.TestTypeID);

        Assert.Equal(
            10,
            captured.CreatedByUserID);

        Assert.Equal(
            25m,
            captured.PaidFees);

        Assert.False(captured.IsLocked);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // ADD - SAVE FAILURE
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenSaveFails_ReturnsFailure()
    {
        var service = CreateService();

        SetupBasicAddFlow();

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Callback<TestAppointment>(
                x => x.TestAppointmentID = 500)
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to book appointment.",
            result.Error);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenGeneratedIdIsInvalid_ReturnsFailure()
    {
        var service = CreateService();

        SetupBasicAddFlow();

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Returns(Task.CompletedTask);

        var result =
            await service.AddAsync(
                ValidCreateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to book appointment.",
            result.Error);
    }

    // =========================================================
    // ADD - EXCEPTION
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenExceptionOccurs_RollsBackAndRethrows()
    {
        var service = CreateService();

        SetupBasicAddFlow();

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database error"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.AddAsync(
                    ValidCreateDto()));

        Assert.Equal(
            "Database error",
            exception.Message);

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
    // SCHEDULE - VALIDATION
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenApplicationIdInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.ScheduleAsync(
                0,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ScheduleAsync_WhenTestTypeInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.ScheduleAsync(
                100,
                999,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ScheduleAsync_WhenDateInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddMinutes(-1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // =========================================================
    // SCHEDULE - AUTH
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenNotAuthenticated_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "You must be logged in first.",
            result.Error);
    }

    // =========================================================
    // SCHEDULE - TEST TYPE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenTestTypeNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync((TestType?)null);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Test type not found.",
            result.Error);
    }

    // =========================================================
    // SCHEDULE - WORKFLOW
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenWorkflowRejects_RollsBack()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.Conflict(
                    "Workflow rejected."));

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - DUPLICATE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenAlreadyScheduled_RollsBack()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(true);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - LOCAL CONFLICT
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenLocalConflict_RollsBack()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(true);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - USER CONFLICT
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenUserConflict_RollsBack()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasUserConflictAsync(
                    10,
                    It.IsAny<DateTime>(),
                    null))
            .ReturnsAsync(true);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

        _transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - FIRST ATTEMPT
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_FirstAttempt_DoesNotCreateRetakeApplication()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        TestAppointment? captured = null;

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Callback<TestAppointment>(
                x =>
                {
                    captured = x;
                    x.TestAppointmentID = 700;
                })
            .Returns(Task.CompletedTask);

        var appointmentDate =
            DateTime.UtcNow.AddDays(1);

        var scheduleInfo =
            ValidAppointment(
                700,
                10,
                false,
                appointmentDate);

        _repository
            .Setup(x =>
                x.GetScheduleInfoAsync(700))
            .ReturnsAsync(scheduleInfo);

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(0);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                appointmentDate);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Null(
            captured!.RetakeTestApplicationID);

        _applicationService.Verify(
            x =>
                x.GetBasicInfoAsync(
                    It.IsAny<int>()),
            Times.Never);

        _applicationService.Verify(
            x =>
                x.AddNewApplicationAsync(
                    It.IsAny<CreateApplicationDto>()),
            Times.Never);

        _transaction.Verify(
            x =>
                x.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - RETAKE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_Retake_CreatesRetakeApplication()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(1);

        _localApplicationService
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(100))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .Success(
                        new LocalDrivingLicenseApplicationListDto
                        {
                            ApplicantPersonID = 55,
                            LocalDrivingLicenseApplicationID = 100,
                            LicenseClassID = 1,
                            LicenseClassName = "A",
                            FullName = "Test Person",
                            ApplicationDate = DateTime.UtcNow,
                            PassedTest = 0,
                            ApplicationStatus = AppStatus.New,
                            ApplicationFees = 50m,
                            LicenseClassFees = 100m,
                            HasLicense = false
                        }));

        _applicationService
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(900));

        TestAppointment? captured = null;

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Callback<TestAppointment>(
                x =>
                {
                    captured = x;
                    x.TestAppointmentID = 800;
                })
            .Returns(Task.CompletedTask);

        var date =
            DateTime.UtcNow.AddDays(1);

        _repository
            .Setup(x =>
                x.GetScheduleInfoAsync(800))
            .ReturnsAsync(
                ValidAppointment(
                    800,
                    10,
                    false,
                    date));

        var result =
            await service.ScheduleAsync(
                100,
                1,
                date);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);

        Assert.Equal(
            900,
            captured!.RetakeTestApplicationID);

        _applicationService.Verify(
            x =>
                x.AddNewApplicationAsync(
                    It.Is<CreateApplicationDto>(
                        dto =>
                            dto.ApplicantPersonID == 55 &&
                            dto.ApplicationTypeID == 7)),
            Times.Once);

        _transaction.Verify(
            x =>
                x.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - RETAKE LOCAL APPLICATION FAILURE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_Retake_WhenLocalApplicationFails_RollsBack()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(1);

        _localApplicationService
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(100))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .FromNotFound(
                        "Local application not found."));

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);

        _transaction.Verify(
            x =>
                x.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _applicationService.Verify(
            x =>
                x.AddNewApplicationAsync(
                    It.IsAny<CreateApplicationDto>()),
            Times.Never);
    }

    // =========================================================
    // SCHEDULE - RETAKE LOCAL APPLICATION NULL
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_Retake_WhenLocalApplicationValueNull_ReturnsNotFound()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(1);

        _localApplicationService
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(100))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .Success(null!));

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "Local driving license application was not found.",
            result.Error);

        _transaction.Verify(
            x =>
                x.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - RETAKE APPLICANT SOURCE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_Retake_UsesLocalApplicationApplicantPersonId()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(1);

        _localApplicationService
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(100))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .Success(
                        new LocalDrivingLicenseApplicationListDto
                        {
                            ApplicantPersonID = 55,
                            LocalDrivingLicenseApplicationID = 100,
                            LicenseClassID = 1,
                            LicenseClassName = "A",
                            FullName = "Test Person",
                            ApplicationDate = DateTime.UtcNow,
                            PassedTest = 0,
                            ApplicationStatus = AppStatus.New,
                            ApplicationFees = 50m,
                            LicenseClassFees = 100m,
                            HasLicense = false
                        }));

        _applicationService
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.Success(900));

        TestAppointment? captured = null;

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Callback<TestAppointment>(
                x =>
                {
                    captured = x;
                    x.TestAppointmentID = 800;
                })
            .Returns(Task.CompletedTask);

        var date =
            DateTime.UtcNow.AddDays(1);

        _repository
            .Setup(x =>
                x.GetScheduleInfoAsync(800))
            .ReturnsAsync(
                ValidAppointment(
                    800,
                    10,
                    false,
                    date));

        var result =
            await service.ScheduleAsync(
                100,
                1,
                date);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);

        _applicationService.Verify(
            x =>
                x.AddNewApplicationAsync(
                    It.Is<CreateApplicationDto>(
                        dto =>
                            dto.ApplicantPersonID == 55 &&
                            dto.ApplicationTypeID == 7)),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - RETAKE APPLICATION FAILURE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_Retake_WhenCreateApplicationFails_RollsBack()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        _repository
            .Setup(x =>
                x.GetTrialCountAsync(100, 1))
            .ReturnsAsync(1);

        _localApplicationService
            .Setup(x =>
                x.GetLocalDrivingLicenseApplicationByIdAsync(100))
            .ReturnsAsync(
                Result<LocalDrivingLicenseApplicationListDto>
                    .Success(
                        new LocalDrivingLicenseApplicationListDto
                        {
                            ApplicantPersonID = 55,
                            LocalDrivingLicenseApplicationID = 100,
                            LicenseClassID = 1,
                            LicenseClassName = "A",
                            FullName = "Test Person",
                            ApplicationDate = DateTime.UtcNow,
                            PassedTest = 0,
                            ApplicationStatus = AppStatus.New,
                            ApplicationFees = 50m,
                            LicenseClassFees = 100m,
                            HasLicense = false
                        }));

        _applicationService
            .Setup(x =>
                x.AddNewApplicationAsync(
                    It.IsAny<CreateApplicationDto>()))
            .ReturnsAsync(
                Result<int>.FromFailure(
                    "Failed to create retake application."));

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Failure,
            result.ErrorType);

        _transaction.Verify(
            x =>
                x.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _repository.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()),
            Times.Never);
    }

    // =========================================================
    // SCHEDULE - SAVE FAILURE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenSaveFails_RollsBack()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Callback<TestAppointment>(
                x => x.TestAppointmentID = 700)
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);

        _transaction.Verify(
            x =>
                x.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x =>
                x.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // SCHEDULE - GENERATED ID FAILURE
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenGeneratedIdInvalid_RollsBack()
    {
        var service = CreateService();

        SetupBasicScheduleFlow();

        _repository
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<TestAppointment>()))
            .Returns(Task.CompletedTask);

        var result =
            await service.ScheduleAsync(
                100,
                1,
                DateTime.UtcNow.AddDays(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);

        _transaction.Verify(
            x =>
                x.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // SCHEDULE - EXCEPTION
    // =========================================================

    [Fact]
    public async Task ScheduleAsync_WhenExceptionOccurs_RollsBackAndRethrows()
    {
        var service = CreateService();

        _testTypeRepository
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(ValidTestType());

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.IsAppointmentAlreadyScheduledAsync(
                    100,
                    1))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database error"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ScheduleAsync(
                    100,
                    1,
                    DateTime.UtcNow.AddDays(1)));

        Assert.Equal(
            "Database error",
            exception.Message);

        _transaction.Verify(
            x =>
                x.RollbackAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x =>
                x.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // UPDATE - VALIDATION
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenDtoNull_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task UpdateAsync_WhenIdInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.UpdateAsync(
                ValidUpdateDto(0));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task UpdateAsync_WhenDateInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var dto = ValidUpdateDto();
        dto.AppointmentDate = default;

        var result =
            await service.UpdateAsync(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // =========================================================
    // UPDATE - AUTH
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenNotAuthenticated_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        var result =
            await service.UpdateAsync(
                ValidUpdateDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "You must be logged in first.",
            result.Error);
    }

    // =========================================================
    // UPDATE - NOT FOUND
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(
                (TestAppointment?)null);

        var result =
            await service.UpdateAsync(
                ValidUpdateDto(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Appointment not found.",
            result.Error);
    }

    // =========================================================
    // UPDATE - OWNERSHIP
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenAnotherUserOwnsAppointment_ReturnsForbidden()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(
                ValidAppointment(
                    1,
                    99));

        var result =
            await service.UpdateAsync(
                ValidUpdateDto(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "You are not allowed to modify this appointment.",
            result.Error);
    }

    // =========================================================
    // UPDATE - LOCKED
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenLocked_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(
                ValidAppointment(
                    1,
                    10,
                    true));

        var result =
            await service.UpdateAsync(
                ValidUpdateDto(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot modify a locked appointment.",
            result.Error);
    }

    // =========================================================
    // UPDATE - SAME DATE
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenDateUnchanged_CommitsWithoutSave()
    {
        var service = CreateService();

        var date =
            DateTime.UtcNow.AddDays(1);

        var appointment =
            ValidAppointment(
                1,
                10,
                false,
                date);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        var dto = new UpdateTestAppointmentDto
        {
            TestAppointmentID = 1,
            AppointmentDate = date
        };

        var result =
            await service.UpdateAsync(dto);

        Assert.True(result.IsSuccess);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // UPDATE - WORKFLOW
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenWorkflowRejects_ReturnsFailure()
    {
        var service = CreateService();

        var appointment =
            ValidAppointment(1);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(
                Result.Conflict(
                    "Workflow rejected."));

        var result =
            await service.UpdateAsync(
                ValidUpdateDto(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    // =========================================================
    // UPDATE - LOCAL CONFLICT
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenLocalConflict_ReturnsConflict()
    {
        var service = CreateService();

        var appointment =
            ValidAppointment(1);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    1))
            .ReturnsAsync(true);

        var result =
            await service.UpdateAsync(
                ValidUpdateDto(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "This application already has another appointment at this date and time.",
            result.Error);
    }

    // =========================================================
    // UPDATE - USER CONFLICT
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenUserConflict_ReturnsConflict()
    {
        var service = CreateService();

        var appointment =
            ValidAppointment(1);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    It.IsAny<DateTime>(),
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasUserConflictAsync(
                    10,
                    It.IsAny<DateTime>(),
                    1))
            .ReturnsAsync(true);

        var result =
            await service.UpdateAsync(
                ValidUpdateDto(1));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The current user already has another appointment at this date and time.",
            result.Error);
    }

    // =========================================================
    // UPDATE - SUCCESS
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesAndCommits()
    {
        var service = CreateService();

        var appointment =
            ValidAppointment(1);

        var newDate =
            DateTime.UtcNow.AddDays(5);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    newDate,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasUserConflictAsync(
                    10,
                    newDate,
                    1))
            .ReturnsAsync(false);

        var result =
            await service.UpdateAsync(
                new UpdateTestAppointmentDto
                {
                    TestAppointmentID = 1,
                    AppointmentDate = newDate
                });

        Assert.True(result.IsSuccess);
        Assert.Equal(
            newDate,
            appointment.AppointmentDate);

        _unitOfWork.Verify(
            x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x =>
                x.CommitAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // UPDATE - SAVE FAILURE
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenSaveFails_ReturnsFailure()
    {
        var service = CreateService();

        var appointment =
            ValidAppointment(1);

        var newDate =
            DateTime.UtcNow.AddDays(5);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        _workflowService
            .Setup(x =>
                x.CanScheduleTestAsync(
                    100,
                    TestTypeEnum.Theory))
            .ReturnsAsync(Result.Success());

        _repository
            .Setup(x =>
                x.HasLocalApplicationConflictAsync(
                    100,
                    newDate,
                    1))
            .ReturnsAsync(false);

        _repository
            .Setup(x =>
                x.HasUserConflictAsync(
                    10,
                    newDate,
                    1))
            .ReturnsAsync(false);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.UpdateAsync(
                new UpdateTestAppointmentDto
                {
                    TestAppointmentID = 1,
                    AppointmentDate = newDate
                });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to update appointment.",
            result.Error);
    }

    // =========================================================
    // UPDATE - EXCEPTION
    // =========================================================

    [Fact]
    public async Task UpdateAsync_WhenExceptionOccurs_RollsBackAndRethrows()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database error"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.UpdateAsync(
                    ValidUpdateDto(1)));

        Assert.Equal(
            "Database error",
            exception.Message);

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
    // DELETE - VALIDATION
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenIdInvalid_ReturnsValidation()
    {
        var service = CreateService();

        var result =
            await service.DeleteAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    // =========================================================
    // DELETE - AUTH
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenNotAuthenticated_ReturnsForbidden()
    {
        var service = CreateService();

        _currentUserService
            .SetupGet(x => x.IsLoggedIn)
            .Returns(false);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "You must be logged in first.",
            result.Error);
    }

    // =========================================================
    // DELETE - NOT FOUND
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(
                (TestAppointment?)null);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Appointment not found.",
            result.Error);
    }

    // =========================================================
    // DELETE - OWNERSHIP
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenAnotherUserOwnsAppointment_ReturnsForbidden()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(
                ValidAppointment(
                    1,
                    99));

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal(
            "You are not allowed to delete this appointment.",
            result.Error);
    }

    // =========================================================
    // DELETE - LOCKED
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenLocked_ReturnsConflict()
    {
        var service = CreateService();

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(
                ValidAppointment(
                    1,
                    10,
                    true));

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Cannot delete a locked appointment.",
            result.Error);
    }

    // =========================================================
    // DELETE - SUCCESS
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenValid_DeletesAndSaves()
    {
        var service = CreateService();

        var appointment =
            ValidAppointment(1);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsSuccess);

        _repository.Verify(
            x => x.Delete(appointment),
            Times.Once);

        _unitOfWork.Verify(
            x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // DELETE - SAVE FAILURE
    // =========================================================

    [Fact]
    public async Task DeleteAsync_WhenSaveFails_ReturnsFailure()
    {
        var service = CreateService();

        var appointment =
            ValidAppointment(1);

        _repository
            .Setup(x =>
                x.GetForUpdateAsync(1))
            .ReturnsAsync(appointment);

        _unitOfWork
            .Setup(x =>
                x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result =
            await service.DeleteAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal(
            "Failed to delete appointment.",
            result.Error);

        _repository.Verify(
            x => x.Delete(appointment),
            Times.Once);
    }
}