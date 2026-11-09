using Application.Validators;
using Application.Common.Results;
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
using Xunit;

namespace Application.UnitTests.Services;

public sealed class AppointmentBookingConcurrencyEngineTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<ITestAppointmentRepository> _repository = new();
    private readonly Mock<ITestTypeRepository> _testTypeRepository = new();
    private readonly Mock<ILocalDrivingLicenseApplicationService> _localApplicationService = new();
    private readonly Mock<IApplicationTypeService> _applicationTypeService = new();
    private readonly Mock<IApplicationService> _applicationService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ITestWorkflowService> _workflowService = new();
    private readonly Mock<ILogger<TestAppointmentService>> _logger = new();
    private readonly Common.FakeJordanClock _clock = new();

    public AppointmentBookingConcurrencyEngineTests()
    {
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Func<IUnitOfWorkTransaction, Task<Result>> op, IsolationLevel _, CancellationToken __) =>
            {
                var res = await op(_transaction.Object);
                if (res.IsFailure)
                    await _transaction.Object.RollbackAsync();
                return res;
            });

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<IUnitOfWorkTransaction, Task<Result<int>>>>(),
                It.IsAny<IsolationLevel>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Func<IUnitOfWorkTransaction, Task<Result<int>>> op, IsolationLevel _, CancellationToken __) =>
            {
                var res = await op(_transaction.Object);
                if (res.IsFailure)
                    await _transaction.Object.RollbackAsync();
                return res;
            });

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _currentUserService.Setup(x => x.IsLoggedIn).Returns(true);
        _currentUserService.Setup(x => x.UserId).Returns(10);
    }

    private TestAppointmentService CreateService() =>
        new(
            _unitOfWork.Object,
            _repository.Object,
            _testTypeRepository.Object,
            _localApplicationService.Object,
            _applicationTypeService.Object,
            _currentUserService.Object,
            _workflowService.Object,
            _applicationService.Object,
            _logger.Object,
            _clock);

    private static DateTime GetFutureJordanSlot(int daysAhead = 2, int slotHour = 8, int slotMinute = 0)
    {
        var jordanNow = Common.FakeJordanClock.DefaultJordanNow;
        var date = jordanNow.Date.AddDays(daysAhead);
        while (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
        {
            date = date.AddDays(1);
        }
        return new DateTime(date.Year, date.Month, date.Day, slotHour, slotMinute, 0, DateTimeKind.Unspecified);
    }

    // =========================================================================
    // 1. EMPLOYEE INDEPENDENCE
    // =========================================================================

    [Fact]
    public async Task ScheduleAsync_DifferentEmployees_CanScheduleDifferentTestTypesAtSameSlot()
    {
        // Arrange: Employee 10 is booking Vision at 08:00; Employee 20 is booking Written at 08:00
        var slot = GetFutureJordanSlot(2, 8, 0);
        var service = CreateService();

        _testTypeRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new TestType { TestTypeId = 1, TestTypeFees = 20m });
        _workflowService.Setup(x => x.CanScheduleTestAsync(100, TestTypeEnum.Theory, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(x => x.LockLocalApplicationForSchedulingAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.GetApplicantPersonIdByLocalAppIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repository.Setup(x => x.AcquireSchedulingLocksAsync(1, 1, slot, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.IsAppointmentAlreadyScheduledAsync(100, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.IsSlotOccupiedAsync(1, slot, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.HasApplicantSameDayConflictAsync(1, slot, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.GetTrialCountAsync(100, 1, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        TestAppointment? captured = null;
        _repository.Setup(x => x.AddAsync(It.IsAny<TestAppointment>(), It.IsAny<CancellationToken>()))
            .Callback<TestAppointment, CancellationToken>((a, _) => { a.TestAppointmentID = 501; captured = a; })
            .Returns(Task.CompletedTask);

        _repository.Setup(x => x.GetScheduleInfoAsync(501, It.IsAny<CancellationToken>())).ReturnsAsync(new TestAppointment
        {
            TestAppointmentID = 501,
            AppointmentDate = slot,
            TestTypeID = 1,
            CreatedByUserID = 10,
            LocalDrivingLicenseApplication = new LocalDrivingLicenseApplication
            {
                LocalDrivingLicenseApplicationID = 100,
                Application = new ApplicationD { Person = new Person { FirstName = "John", LastName = "Doe" } },
                LicenseClass = new LicenseClass { ClassName = "Class 3" }
            }
        });

        // Act
        var result = await service.ScheduleAsync(100, 1, slot);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(10, captured.CreatedByUserID); // Logged in employee ID recorded for audit
        Assert.Equal(slot, captured.AppointmentDate);
    }

    [Fact]
    public async Task UpdateAsync_AuthorizedEmployee_CanUpdateAppointmentCreatedByAnotherEmployee()
    {
        // Arrange: Appointment was created by employee 99. Current user is employee 10.
        var originalSlot = GetFutureJordanSlot(2, 8, 0);
        var newSlot = GetFutureJordanSlot(2, 8, 40);
        var service = CreateService();

        var existing = new TestAppointment
        {
            TestAppointmentID = 42,
            LocalDrivingLicenseApplicationID = 100,
            TestTypeID = 1,
            AppointmentDate = originalSlot,
            CreatedByUserID = 99, // Another employee
            IsLocked = false
        };

        _repository.Setup(x => x.GetForUpdateAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _workflowService.Setup(x => x.CanScheduleTestAsync(100, TestTypeEnum.Theory, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(x => x.GetApplicantPersonIdByLocalAppIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _repository.Setup(x => x.AcquireSchedulingLocksAsync(5, 1, newSlot, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.IsSlotOccupiedAsync(1, newSlot, 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.HasApplicantSameDayConflictAsync(5, newSlot, 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await service.UpdateAsync(new UpdateTestAppointmentDto
        {
            TestAppointmentID = 42,
            AppointmentDate = newSlot
        });

        // Assert: Succeeds because creator ownership restriction is eliminated; audit ID 99 preserved
        Assert.True(result.IsSuccess);
        Assert.Equal(newSlot, existing.AppointmentDate);
        Assert.Equal(99, existing.CreatedByUserID);
    }

    // =========================================================================
    // 2. GLOBAL TEST-TYPE SLOT OCCUPANCY
    // =========================================================================

    [Fact]
    public async Task ScheduleAsync_SameTestTypeSameSlot_RejectsDuplicate()
    {
        // Arrange: Vision test at 08:00 is already booked by another applicant
        var slot = GetFutureJordanSlot(3, 8, 0);
        var service = CreateService();

        _testTypeRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new TestType { TestTypeId = 1, TestTypeFees = 20m });
        _workflowService.Setup(x => x.CanScheduleTestAsync(101, TestTypeEnum.Theory, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(x => x.LockLocalApplicationForSchedulingAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.GetApplicantPersonIdByLocalAppIdAsync(101, It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _repository.Setup(x => x.AcquireSchedulingLocksAsync(2, 1, slot, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.IsAppointmentAlreadyScheduledAsync(101, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.IsSlotOccupiedAsync(1, slot, null, It.IsAny<CancellationToken>())).ReturnsAsync(true); // Occupied!

        // Act
        var result = await service.ScheduleAsync(101, 1, slot);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("This slot is already booked for this test type.", result.Error);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SameAppointmentSameSlot_DoesNotConflictWithItself()
    {
        // Arrange: Update without changing date commits without error
        var slot = GetFutureJordanSlot(3, 8, 0);
        var service = CreateService();

        var existing = new TestAppointment
        {
            TestAppointmentID = 15,
            LocalDrivingLicenseApplicationID = 100,
            TestTypeID = 1,
            AppointmentDate = slot,
            CreatedByUserID = 10,
            IsLocked = false
        };

        _repository.Setup(x => x.GetForUpdateAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        // Act
        var result = await service.UpdateAsync(new UpdateTestAppointmentDto
        {
            TestAppointmentID = 15,
            AppointmentDate = slot
        });

        // Assert
        Assert.True(result.IsSuccess);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ConflictingSlotOccupied_RejectedWithoutModifyingOriginalAppointment()
    {
        // Arrange
        var originalSlot = GetFutureJordanSlot(3, 8, 0);
        var occupiedSlot = GetFutureJordanSlot(3, 8, 40);
        var service = CreateService();

        var existing = new TestAppointment
        {
            TestAppointmentID = 15,
            LocalDrivingLicenseApplicationID = 100,
            TestTypeID = 1,
            AppointmentDate = originalSlot,
            CreatedByUserID = 10,
            IsLocked = false
        };

        _repository.Setup(x => x.GetForUpdateAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _workflowService.Setup(x => x.CanScheduleTestAsync(100, TestTypeEnum.Theory, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(x => x.GetApplicantPersonIdByLocalAppIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _repository.Setup(x => x.AcquireSchedulingLocksAsync(5, 1, occupiedSlot, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.IsSlotOccupiedAsync(1, occupiedSlot, 15, It.IsAny<CancellationToken>())).ReturnsAsync(true); // Occupied by someone else!

        // Act
        var result = await service.UpdateAsync(new UpdateTestAppointmentDto
        {
            TestAppointmentID = 15,
            AppointmentDate = occupiedSlot
        });

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("This slot is already booked for this test type.", result.Error);
        Assert.Equal(originalSlot, existing.AppointmentDate); // Unchanged!
    }

    // =========================================================================
    // 3. APPLICANT IDENTITY & SAME-DAY RESTRICTION
    // =========================================================================

    [Fact]
    public async Task ScheduleAsync_SameApplicant_CannotBookTwoDifferentTestTypesOnSameDay()
    {
        // Arrange: Applicant already has Vision test at 08:00. Tries to book Written test at 09:20 on same day.
        var slotVision = GetFutureJordanSlot(4, 8, 0);
        var slotWritten = GetFutureJordanSlot(4, 9, 20);
        var service = CreateService();

        _testTypeRepository.Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(new TestType { TestTypeId = 2, TestTypeFees = 20m });
        _workflowService.Setup(x => x.CanScheduleTestAsync(100, TestTypeEnum.Written, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(x => x.LockLocalApplicationForSchedulingAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.GetApplicantPersonIdByLocalAppIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(77); // ApplicantPersonID = 77
        _repository.Setup(x => x.AcquireSchedulingLocksAsync(77, 2, slotWritten, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.IsAppointmentAlreadyScheduledAsync(100, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.IsSlotOccupiedAsync(2, slotWritten, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.HasApplicantSameDayConflictAsync(77, slotWritten, null, It.IsAny<CancellationToken>())).ReturnsAsync(true); // Conflict on same day!

        // Act
        var result = await service.ScheduleAsync(100, 2, slotWritten);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("The applicant already has an appointment or test scheduled on this calendar day.", result.Error);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_SameApplicantDifferentLocalApplications_RejectedOnSameDay()
    {
        // Arrange: Applicant has LocalApp 100 (Class 3) and LocalApp 200 (Class 1).
        // Both belong to ApplicantPersonID = 77. Booking on same calendar day must be rejected.
        var slot = GetFutureJordanSlot(5, 10, 0);
        var service = CreateService();

        _testTypeRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new TestType { TestTypeId = 1, TestTypeFees = 20m });
        _workflowService.Setup(x => x.CanScheduleTestAsync(200, TestTypeEnum.Theory, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(x => x.LockLocalApplicationForSchedulingAsync(200, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.GetApplicantPersonIdByLocalAppIdAsync(200, It.IsAny<CancellationToken>())).ReturnsAsync(77); // Resolves to same person 77
        _repository.Setup(x => x.AcquireSchedulingLocksAsync(77, 1, slot, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.IsAppointmentAlreadyScheduledAsync(200, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.IsSlotOccupiedAsync(1, slot, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.HasApplicantSameDayConflictAsync(77, slot, null, It.IsAny<CancellationToken>())).ReturnsAsync(true); // Found appointment under LocalApp 100

        // Act
        var result = await service.ScheduleAsync(200, 1, slot);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("The applicant already has an appointment or test scheduled on this calendar day.", result.Error);
    }

    // =========================================================================
    // 4. CONCURRENCY LOCK ACQUISITION & FAILURE HANDLING
    // =========================================================================

    [Fact]
    public async Task ScheduleAsync_WhenLockAcquisitionFails_ReturnsConflictAndRollsBack()
    {
        // Arrange: Lock acquisition times out or fails
        var slot = GetFutureJordanSlot(2, 8, 0);
        var service = CreateService();

        _testTypeRepository.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new TestType { TestTypeId = 1, TestTypeFees = 20m });
        _workflowService.Setup(x => x.CanScheduleTestAsync(100, TestTypeEnum.Theory, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(x => x.LockLocalApplicationForSchedulingAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(x => x.GetApplicantPersonIdByLocalAppIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _repository.Setup(x => x.IsAppointmentAlreadyScheduledAsync(100, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repository.Setup(x => x.AcquireSchedulingLocksAsync(5, 1, slot, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false); // Lock timeout!

        // Act
        var result = await service.ScheduleAsync(100, 1, slot);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Contains("Unable to acquire scheduling lock", result.Error);
        _transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // 5. JORDAN FACILITY TIME COMPARISONS IN TEST WORKFLOW
    // =========================================================================

    [Fact]
    public async Task CanTakeTestAsync_WhenAppointmentDateArrivedInJordanFacilityTime_ReturnsSuccess()
    {
        // Arrange
        var mockRepo = new Mock<ITestAppointmentRepository>();
        var workflowService = new TestWorkflowService(mockRepo.Object, _clock);

        // Current facility time
        var currentJordanTime = _clock.Now;

        // Appointment date was 10 minutes ago in Jordan facility time (has arrived!)
        var arrivedAppointment = new TestAppointment
        {
            TestAppointmentID = 10,
            LocalDrivingLicenseApplicationID = 100,
            TestTypeID = (int)TestTypeEnum.Theory,
            AppointmentDate = currentJordanTime.AddMinutes(-10),
            IsLocked = false
        };

        mockRepo.Setup(x => x.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(arrivedAppointment);
        mockRepo.Setup(x => x.GetApplicationStatusAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(AppStatus.New);
        mockRepo.Setup(x => x.GetPassedTestTypeIdsAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(new HashSet<int>());

        // Act
        var result = await workflowService.CanTakeTestAsync(10);

        // Assert: Succeeds because Jordan facility time comparison confirms the appointment date arrived!
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task CanTakeTestAsync_WhenAppointmentDateInFutureInJordanFacilityTime_ReturnsConflict()
    {
        // Arrange
        var mockRepo = new Mock<ITestAppointmentRepository>();
        var workflowService = new TestWorkflowService(mockRepo.Object, _clock);

        var currentJordanTime = _clock.Now;

        // Appointment date is 15 minutes in the future in Jordan facility time
        var futureAppointment = new TestAppointment
        {
            TestAppointmentID = 11,
            LocalDrivingLicenseApplicationID = 100,
            TestTypeID = (int)TestTypeEnum.Theory,
            AppointmentDate = currentJordanTime.AddMinutes(15),
            IsLocked = false
        };

        mockRepo.Setup(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(futureAppointment);

        // Act
        var result = await workflowService.CanTakeTestAsync(11);

        // Assert: Fails with date has not arrived yet
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal("The appointment date has not arrived yet.", result.Error);
    }
}
