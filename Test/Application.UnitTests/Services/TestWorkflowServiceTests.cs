using Application.Common.Results;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Application.UnitTests.Services;

public class TestWorkflowServiceTests
{
    private readonly Mock<ITestAppointmentRepository> _repositoryMock;
    private readonly TestWorkflowService _sut;

    public TestWorkflowServiceTests()
    {
        _repositoryMock = new Mock<ITestAppointmentRepository>();

        _sut = new TestWorkflowService(
            _repositoryMock.Object);
    }

    // =========================================================
    // GetNextTestTypeAsync
    // =========================================================

    [Fact]
    public async Task GetNextTestTypeAsync_InvalidApplicationId_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 0;

        // Act
        Result<TestTypeEnum> result =
            await _sut.GetNextTestTypeAsync(localAppId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid local driving license application ID.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetApplicationStatusAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetNextTestTypeAsync_ApplicationNotFound_ReturnsNotFound()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync((AppStatus?)null);

        // Act
        Result<TestTypeEnum> result =
            await _sut.GetNextTestTypeAsync(localAppId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Local driving license application not found.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetPassedTestTypeIdsAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Theory]
    [InlineData(AppStatus.Cancelled)]
    [InlineData(AppStatus.Completed)]
    public async Task GetNextTestTypeAsync_InactiveApplication_ReturnsConflict(
        AppStatus status)
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(status);

        // Act
        Result<TestTypeEnum> result =
            await _sut.GetNextTestTypeAsync(localAppId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Tests can only be taken for an active application.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetPassedTestTypeIdsAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetNextTestTypeAsync_NoTestsPassed_ReturnsTheory()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        Result<TestTypeEnum> result =
            await _sut.GetNextTestTypeAsync(localAppId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
        Assert.Equal(TestTypeEnum.Theory, result.Value);

        _repositoryMock.Verify(
            repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId),
            Times.Once);
    }

    [Fact]
    public async Task GetNextTestTypeAsync_TheoryPassed_ReturnsWritten()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory
                });

        // Act
        Result<TestTypeEnum> result =
            await _sut.GetNextTestTypeAsync(localAppId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TestTypeEnum.Written, result.Value);
    }

    [Fact]
    public async Task GetNextTestTypeAsync_TheoryAndWrittenPassed_ReturnsPractical()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory,
                    (int)TestTypeEnum.Written
                });

        // Act
        Result<TestTypeEnum> result =
            await _sut.GetNextTestTypeAsync(localAppId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TestTypeEnum.Practical, result.Value);
    }

    [Fact]
    public async Task GetNextTestTypeAsync_AllTestsPassed_ReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory,
                    (int)TestTypeEnum.Written,
                    (int)TestTypeEnum.Practical
                });

        // Act
        Result<TestTypeEnum> result =
            await _sut.GetNextTestTypeAsync(localAppId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "All required tests have already been passed.",
            result.Error);
    }

    // =========================================================
    // CanScheduleTestAsync
    // =========================================================

    [Fact]
    public async Task CanScheduleTestAsync_InvalidApplicationId_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 0;

        // Act
        Result result =
            await _sut.CanScheduleTestAsync(
                localAppId,
                TestTypeEnum.Theory);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid local driving license application ID.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetApplicationStatusAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanScheduleTestAsync_InvalidTestType_ReturnsValidationFailure()
    {
        // Arrange
        const int localAppId = 10;
        var invalidTestType = (TestTypeEnum)99;

        // Act
        Result result =
            await _sut.CanScheduleTestAsync(
                localAppId,
                invalidTestType);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid test type.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetApplicationStatusAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanScheduleTestAsync_ApplicationNotFound_ReturnsNotFound()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync((AppStatus?)null);

        // Act
        Result result =
            await _sut.CanScheduleTestAsync(
                localAppId,
                TestTypeEnum.Theory);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Local driving license application not found.",
            result.Error);
    }

    [Theory]
    [InlineData(AppStatus.Cancelled)]
    [InlineData(AppStatus.Completed)]
    public async Task CanScheduleTestAsync_InactiveApplication_ReturnsConflict(
        AppStatus status)
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(status);

        // Act
        Result result =
            await _sut.CanScheduleTestAsync(
                localAppId,
                TestTypeEnum.Theory);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Tests can only be scheduled for an active application.",
            result.Error);
    }

    [Fact]
    public async Task CanScheduleTestAsync_WrongNextTest_ReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory
                });

        // Written is next, but we try to schedule Theory.
        // Act
        Result result =
            await _sut.CanScheduleTestAsync(
                localAppId,
                TestTypeEnum.Theory);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The Theory test cannot be scheduled yet. The next required test is Written.",
            result.Error);
    }

    [Fact]
    public async Task CanScheduleTestAsync_CorrectNextTest_ReturnsSuccess()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        Result result =
            await _sut.CanScheduleTestAsync(
                localAppId,
                TestTypeEnum.Theory);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    [Fact]
    public async Task CanScheduleTestAsync_AllTestsPassed_ReturnsConflict()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory,
                    (int)TestTypeEnum.Written,
                    (int)TestTypeEnum.Practical
                });

        // Act
        Result result =
            await _sut.CanScheduleTestAsync(
                localAppId,
                TestTypeEnum.Practical);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "All required tests have already been passed.",
            result.Error);
    }

    // =========================================================
    // CanTakeTestAsync
    // =========================================================

    [Fact]
    public async Task CanTakeTestAsync_InvalidAppointmentId_ReturnsValidationFailure()
    {
        // Arrange
        const int appointmentId = 0;

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid test appointment ID.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetByIdAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanTakeTestAsync_AppointmentNotFound_ReturnsNotFound()
    {
        // Arrange
        const int appointmentId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync((TestAppointment?)null);

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Test appointment not found.",
            result.Error);
    }

    [Fact]
    public async Task CanTakeTestAsync_LockedAppointment_ReturnsConflict()
    {
        // Arrange
        const int appointmentId = 10;

        var appointment = CreateAppointment(
            appointmentId,
            isLocked: true);

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "This appointment is already locked.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetApplicationStatusAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanTakeTestAsync_FutureAppointment_ReturnsConflict()
    {
        // Arrange
        const int appointmentId = 10;

        var appointment = CreateAppointment(
            appointmentId,
            appointmentDate: DateTime.UtcNow.AddMinutes(30));

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The appointment date has not arrived yet.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetApplicationStatusAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanTakeTestAsync_ApplicationNotFound_ReturnsNotFound()
    {
        // Arrange
        const int appointmentId = 10;
        const int localAppId = 20;

        var appointment = CreateAppointment(
            appointmentId,
            localAppId: localAppId,
            appointmentDate: DateTime.UtcNow.AddMinutes(-30));

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync((AppStatus?)null);

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(
            "Local driving license application not found.",
            result.Error);
    }

    [Theory]
    [InlineData(AppStatus.Cancelled)]
    [InlineData(AppStatus.Completed)]
    public async Task CanTakeTestAsync_InactiveApplication_ReturnsConflict(
        AppStatus status)
    {
        // Arrange
        const int appointmentId = 10;
        const int localAppId = 20;

        var appointment = CreateAppointment(
            appointmentId,
            localAppId: localAppId,
            appointmentDate: DateTime.UtcNow.AddMinutes(-30));

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(status);

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "Tests can only be taken for an active application.",
            result.Error);
    }

    [Fact]
    public async Task CanTakeTestAsync_InvalidTestType_ReturnsValidationFailure()
    {
        // Arrange
        const int appointmentId = 10;
        const int localAppId = 20;

        var appointment = CreateAppointment(
            appointmentId,
            localAppId: localAppId,
            testTypeId: 99,
            appointmentDate: DateTime.UtcNow.AddMinutes(-30));

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "Invalid test type.",
            result.Error);

        _repositoryMock.Verify(
            repository =>
                repository.GetPassedTestTypeIdsAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task CanTakeTestAsync_WrongNextTest_ReturnsConflict()
    {
        // Arrange
        const int appointmentId = 10;
        const int localAppId = 20;

        var appointment = CreateAppointment(
            appointmentId,
            localAppId: localAppId,
            testTypeId: (int)TestTypeEnum.Theory,
            appointmentDate: DateTime.UtcNow.AddMinutes(-30));

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory
                });

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
        Assert.Equal(
            "The Theory test cannot be taken yet. The next required test is Written.",
            result.Error);
    }

    [Fact]
    public async Task CanTakeTestAsync_CorrectNextTest_ReturnsSuccess()
    {
        // Arrange
        const int appointmentId = 10;
        const int localAppId = 20;

        var appointment = CreateAppointment(
            appointmentId,
            localAppId: localAppId,
            testTypeId: (int)TestTypeEnum.Theory,
            appointmentDate: DateTime.UtcNow.AddMinutes(-30));

        _repositoryMock
            .Setup(repository =>
                repository.GetByIdAsync(appointmentId))
            .ReturnsAsync(appointment);

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        Result result =
            await _sut.CanTakeTestAsync(appointmentId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ErrorType.None, result.ErrorType);
    }

    // =========================================================
    // HasPassedAllTestsAsync
    // =========================================================

    [Fact]
    public async Task HasPassedAllTestsAsync_InvalidApplicationId_ReturnsFalse()
    {
        // Arrange
        const int localAppId = 0;

        // Act
        bool result =
            await _sut.HasPassedAllTestsAsync(localAppId);

        // Assert
        Assert.False(result);

        _repositoryMock.Verify(
            repository =>
                repository.GetApplicationStatusAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task HasPassedAllTestsAsync_ApplicationNotFound_ReturnsFalse()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync((AppStatus?)null);

        // Act
        bool result =
            await _sut.HasPassedAllTestsAsync(localAppId);

        // Assert
        Assert.False(result);

        _repositoryMock.Verify(
            repository =>
                repository.GetPassedTestTypeIdsAsync(
                    It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task HasPassedAllTestsAsync_NotAllTestsPassed_ReturnsFalse()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory,
                    (int)TestTypeEnum.Written
                });

        // Act
        bool result =
            await _sut.HasPassedAllTestsAsync(localAppId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasPassedAllTestsAsync_AllTestsPassed_ReturnsTrue()
    {
        // Arrange
        const int localAppId = 10;

        _repositoryMock
            .Setup(repository =>
                repository.GetApplicationStatusAsync(localAppId))
            .ReturnsAsync(AppStatus.New);

        _repositoryMock
            .Setup(repository =>
                repository.GetPassedTestTypeIdsAsync(localAppId))
            .ReturnsAsync(
                new HashSet<int>
                {
                    (int)TestTypeEnum.Theory,
                    (int)TestTypeEnum.Written,
                    (int)TestTypeEnum.Practical
                });

        // Act
        bool result =
            await _sut.HasPassedAllTestsAsync(localAppId);

        // Assert
        Assert.True(result);
    }

    // =========================================================
    // Helpers
    // =========================================================

    private static TestAppointment CreateAppointment(
        int appointmentId = 10,
        int localAppId = 20,
        int testTypeId = (int)TestTypeEnum.Theory,
        DateTime? appointmentDate = null,
        bool isLocked = false)
    {
        return new TestAppointment
        {
            TestAppointmentID = appointmentId,
            TestTypeID = testTypeId,
            LocalDrivingLicenseApplicationID = localAppId,
            AppointmentDate =
                appointmentDate ?? DateTime.UtcNow.AddMinutes(-30),
            PaidFees = 10,
            CreatedByUserID = 1,
            IsLocked = isLocked
        };
    }
}