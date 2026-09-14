using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace Application.UnitTests.Services;

public sealed class TestServiceTests
{
    private readonly Mock<ITestRepository> _repository = new();
    private readonly Mock<ITestAppointmentRepository> _appointmentRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ITestWorkflowService> _workflowService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<ILogger<TestService>> _logger = new();

    public TestServiceTests()
    {
        _transaction
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transaction
            .Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _currentUserService.SetupGet(x => x.IsLoggedIn).Returns(true);
        _currentUserService.SetupGet(x => x.UserId).Returns(10);
    }

    private TestService CreateService()
    {
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(ExecuteTransactionAsync);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new TestService(
            _repository.Object,
            _appointmentRepository.Object,
            _currentUserService.Object,
            _workflowService.Object,
            _unitOfWork.Object,
            _logger.Object);
    }

    private async Task<Result<int>> ExecuteTransactionAsync(
        Func<IUnitOfWorkTransaction, Task<Result<int>>> operation,
        IsolationLevel _,
        CancellationToken __)
    {
        try
        {
            return await operation(_transaction.Object);
        }
        catch
        {
            await _transaction.Object.RollbackAsync();
            throw;
        }
    }

    private static Test CreateTest(
        int id = 1,
        int appointmentId = 100,
        bool result = true,
        string? notes = " Passed ") =>
        new()
        {
            TestID = id,
            TestAppointmentID = appointmentId,
            TestResult = result,
            Notes = notes,
            CreatedByUserID = 10
        };

    private static TestAppointment CreateAppointment(
        int id = 100,
        bool locked = false) =>
        new()
        {
            TestAppointmentID = id,
            TestTypeID = 1,
            LocalDrivingLicenseApplicationID = 200,
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            PaidFees = 50m,
            CreatedByUserID = 10,
            IsLocked = locked
        };

    private static SaveTestResultDto ValidDto(
        int appointmentId = 100,
        bool result = true,
        string? notes = "Passed") =>
        new()
        {
            TestAppointmentID = appointmentId,
            TestResult = result,
            Notes = notes
        };

    private void SetupValidAddFlow()
    {
        _appointmentRepository
            .Setup(x => x.GetForUpdateAsync(100))
            .ReturnsAsync(CreateAppointment());

        _repository
            .Setup(x => x.IsTestAlreadyTakenAsync(100))
            .ReturnsAsync(false);

        _workflowService
            .Setup(x => x.CanTakeTestAsync(100))
            .ReturnsAsync(Result.Success());

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetByIdAsync_WhenIdInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetByIdAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid test ID.", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        _repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Test?)null);

        var result = await CreateService().GetByIdAsync(1);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Test not found.", result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedTest()
    {
        _repository
            .Setup(x => x.GetByIdAsync(5))
            .ReturnsAsync(CreateTest(5, 50, true, "  passed  "));

        var result = await CreateService().GetByIdAsync(5);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value!.TestID);
        Assert.Equal(50, result.Value.TestAppointmentID);
        Assert.True(result.Value.TestResult);
        Assert.Equal("  passed  ", result.Value.Notes);
        Assert.Equal(10, result.Value.CreatedByUserID);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsTests_ReturnsMappedList()
    {
        _repository.Setup(x => x.GetAllAsync()).ReturnsAsync(
        [
            CreateTest(1, 100, true, "Passed"),
            CreateTest(2, 101, false, "Failed")
        ]);

        var result = await CreateService().GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(1, result.Value[0].TestID);
        Assert.True(result.Value[0].TestResult);
        Assert.Equal(2, result.Value[1].TestID);
        Assert.False(result.Value[1].TestResult);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        _repository.Setup(x => x.GetAllAsync()).ReturnsAsync([]);

        var result = await CreateService().GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    // =========================================================
    // GET BY APPOINTMENT
    // =========================================================

    [Fact]
    public async Task GetByTestAppointmentIdAsync_WhenIdInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetByTestAppointmentIdAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid test appointment ID.", result.Error);
    }

    [Fact]
    public async Task GetByTestAppointmentIdAsync_WhenValid_ReturnsMappedTests()
    {
        _repository
            .Setup(x => x.GetByTestAppointmentIdAsync(100))
            .ReturnsAsync(
            [
                CreateTest(1, 100),
                CreateTest(2, 100, false)
            ]);

        var result = await CreateService().GetByTestAppointmentIdAsync(100);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, x => Assert.Equal(100, x.TestAppointmentID));
    }

    // =========================================================
    // GET BY USER
    // =========================================================

    [Fact]
    public async Task GetByUserIdAsync_WhenIdInvalid_ReturnsValidation()
    {
        var result = await CreateService().GetByUserIdAsync(0);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid user ID.", result.Error);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenValid_ReturnsMappedTests()
    {
        _repository
            .Setup(x => x.GetByUserIdAsync(10))
            .ReturnsAsync(
            [
                CreateTest(1, 100),
                CreateTest(2, 101)
            ]);

        var result = await CreateService().GetByUserIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    // =========================================================
    // ADD - VALIDATION
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenDtoNull_ReturnsValidation()
    {
        var result = await CreateService().AddAsync(null!);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Test result data is required.", result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenAppointmentIdInvalid_ReturnsValidation()
    {
        var result = await CreateService().AddAsync(ValidDto(0));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Invalid test appointment ID.", result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenNotesTooLong_ReturnsValidation()
    {
        var result = await CreateService()
            .AddAsync(ValidDto(notes: new string('x', 501)));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Test notes cannot exceed 500 characters.", result.Error);
    }

    [Fact]
    public async Task AddAsync_WhenNotesExactly500Characters_IsValid()
    {
        SetupValidAddFlow();

        Test? captured = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .Callback<Test>(x =>
            {
                captured = x;
                x.TestID = 700;
            })
            .Returns(Task.CompletedTask);

        var result = await CreateService()
            .AddAsync(ValidDto(notes: new string('x', 500)));

        Assert.True(result.IsSuccess);
        Assert.Equal(700, result.Value);
        Assert.NotNull(captured);
        Assert.Equal(500, captured!.Notes!.Length);
    }

    // =========================================================
    // ADD - AUTH
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenNotLoggedIn_ReturnsForbidden()
    {
        _currentUserService.SetupGet(x => x.IsLoggedIn).Returns(false);

        var result = await CreateService().AddAsync(ValidDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("You must be logged in first.", result.Error);

        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenUserIdInvalid_ReturnsForbidden()
    {
        _currentUserService.SetupGet(x => x.UserId).Returns(0);

        var result = await CreateService().AddAsync(ValidDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        Assert.Equal("You must be logged in first.", result.Error);
    }

    // =========================================================
    // ADD - APPOINTMENT
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenAppointmentNotFound_ReturnsNotFoundAndRollsBack()
    {
        _appointmentRepository
            .Setup(x => x.GetForUpdateAsync(100))
            .ReturnsAsync((TestAppointment?)null);

        var result = await CreateService().AddAsync(ValidDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal("Test appointment not found.", result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenAppointmentLocked_ReturnsConflictAndRollsBack()
    {
        _appointmentRepository
            .Setup(x => x.GetForUpdateAsync(100))
            .ReturnsAsync(CreateAppointment(locked: true));

        var result = await CreateService().AddAsync(ValidDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("This appointment is already locked.", result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenResultAlreadyExists_ReturnsConflictAndRollsBack()
    {
        _appointmentRepository
            .Setup(x => x.GetForUpdateAsync(100))
            .ReturnsAsync(CreateAppointment());

        _repository
            .Setup(x => x.IsTestAlreadyTakenAsync(100))
            .ReturnsAsync(true);

        var result = await CreateService().AddAsync(ValidDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "A result already exists for this appointment.",
            result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _workflowService.Verify(
            x => x.CanTakeTestAsync(100),
            Times.Never);
    }

    // =========================================================
    // ADD - WORKFLOW
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenWorkflowRejects_ReturnsWorkflowFailure()
    {
        _appointmentRepository
            .Setup(x => x.GetForUpdateAsync(100))
            .ReturnsAsync(CreateAppointment());

        _repository
            .Setup(x => x.IsTestAlreadyTakenAsync(100))
            .ReturnsAsync(false);

        _workflowService
            .Setup(x => x.CanTakeTestAsync(100))
            .ReturnsAsync(Result.Conflict("Test cannot be taken."));

        var result = await CreateService().AddAsync(ValidDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("Test cannot be taken.", result.Error);

        _repository.Verify(
            x => x.AddAsync(It.IsAny<Test>()),
            Times.Never);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // ADD - SUCCESS
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenValid_CreatesResultLocksAppointmentAndCommits()
    {
        SetupValidAddFlow();

        var appointment = CreateAppointment();
        Test? captured = null;

        _appointmentRepository
            .Setup(x => x.GetForUpdateAsync(100))
            .ReturnsAsync(appointment);

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .Callback<Test>(x =>
            {
                captured = x;
                x.TestID = 700;
            })
            .Returns(Task.CompletedTask);

        var result = await CreateService()
            .AddAsync(ValidDto(result: true, notes: "  Passed  "));

        Assert.True(result.IsSuccess);
        Assert.Equal(700, result.Value);
        Assert.NotNull(captured);
        Assert.Equal(100, captured!.TestAppointmentID);
        Assert.True(captured.TestResult);
        Assert.Equal("Passed", captured.Notes);
        Assert.Equal(10, captured.CreatedByUserID);
        Assert.True(appointment.IsLocked);

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenResultIsFailed_StillCreatesAndLocksAppointment()
    {
        SetupValidAddFlow();

        Test? captured = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .Callback<Test>(x =>
            {
                captured = x;
                x.TestID = 701;
            })
            .Returns(Task.CompletedTask);

        var result = await CreateService()
            .AddAsync(ValidDto(result: false, notes: "Failed"));

        Assert.True(result.IsSuccess);
        Assert.Equal(701, result.Value);
        Assert.NotNull(captured);
        Assert.False(captured!.TestResult);

        _appointmentRepository.Verify(
            x => x.GetForUpdateAsync(100),
            Times.Once);
    }

    // =========================================================
    // ADD - SAVE FAILURE
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenSaveFails_ReturnsFailureAndRollsBack()
    {
        SetupValidAddFlow();

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .Callback<Test>(x => x.TestID = 700)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var result = await service.AddAsync(ValidDto());

        _unitOfWork.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Failed to save test result.", result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenGeneratedTestIdInvalid_ReturnsFailureAndRollsBack()
    {
        SetupValidAddFlow();

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .Returns(Task.CompletedTask);

        var result = await CreateService().AddAsync(ValidDto());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Failure, result.ErrorType);
        Assert.Equal("Failed to save test result.", result.Error);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================
    // ADD - EXCEPTION
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenRepositoryThrows_RollsBackAndRethrows()
    {
        SetupValidAddFlow();

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().AddAsync(ValidDto()));

        Assert.Equal("Database error", exception.Message);

        _transaction.Verify(
            x => x.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _transaction.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================
    // ADD - ENTITY MAPPING
    // =========================================================

    [Fact]
    public async Task AddAsync_TrimsNotesBeforeSaving()
    {
        SetupValidAddFlow();

        Test? captured = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .Callback<Test>(x =>
            {
                captured = x;
                x.TestID = 702;
            })
            .Returns(Task.CompletedTask);

        var result = await CreateService()
            .AddAsync(ValidDto(notes: "   Some notes   "));

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal("Some notes", captured!.Notes);
    }

    [Fact]
    public async Task AddAsync_WhenNotesAreWhitespace_SavesNullNotes()
    {
        SetupValidAddFlow();

        Test? captured = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Test>()))
            .Callback<Test>(x =>
            {
                captured = x;
                x.TestID = 703;
            })
            .Returns(Task.CompletedTask);

        var result = await CreateService()
            .AddAsync(ValidDto(notes: "   "));

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Null(captured!.Notes);
    }
}