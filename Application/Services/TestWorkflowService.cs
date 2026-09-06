using Application.Common.Results;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

public sealed class TestWorkflowService(
    ITestAppointmentRepository repository)
    : ITestWorkflowService
{
    private readonly ITestAppointmentRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private static readonly TestTypeEnum[] RequiredTests =
    [
        TestTypeEnum.Theory,
        TestTypeEnum.Written,
        TestTypeEnum.Practical
    ];

    public async Task<Result> CanScheduleTestAsync(
        int localAppId,
        TestTypeEnum testType)
    {
        var validation = ValidateTestRequest(localAppId, testType);

        if (validation.IsFailure)
            return validation;

        var status =
            await _repository.GetApplicationStatusAsync(localAppId);

        if (status is null)
            return Result.NotFound(
                "Local driving license application not found.");

        if (status != AppStatus.New)
            return Result.Conflict(
                "Tests can only be scheduled for an active application.");

        return await ValidateNextTestAsync(
            localAppId,
            testType,
            "scheduled");
    }

    public async Task<Result<TestTypeEnum>> GetNextTestTypeAsync(
        int localAppId)
    {
        if (localAppId <= 0)
        {
            return Result<TestTypeEnum>.FromValidationFailure(
                "Invalid local driving license application ID.");
        }

        var status =
            await _repository.GetApplicationStatusAsync(localAppId);

        if (status is null)
        {
            return Result<TestTypeEnum>.FromNotFound(
                "Local driving license application not found.");
        }

        if (status != AppStatus.New)
        {
            return Result<TestTypeEnum>.FromConflict(
                "Tests can only be taken for an active application.");
        }

        var passedTests =
            await _repository.GetPassedTestTypeIdsAsync(localAppId);

        foreach (var testType in RequiredTests)
        {
            if (!passedTests.Contains((int)testType))
                return Result<TestTypeEnum>.Success(testType);
        }

        return Result<TestTypeEnum>.FromConflict(
            "All required tests have already been passed.");
    }

    public async Task<Result> CanTakeTestAsync(
        int testAppointmentId)
    {
        if (testAppointmentId <= 0)
        {
            return Result.ValidationFailure(
                "Invalid test appointment ID.");
        }

        var appointment =
            await _repository.GetByIdAsync(testAppointmentId);

        if (appointment is null)
        {
            return Result.NotFound(
                "Test appointment not found.");
        }

        if (appointment.IsLocked)
        {
            return Result.Conflict(
                "This appointment is already locked.");
        }

        if (appointment.AppointmentDate > DateTime.UtcNow)
        {
            return Result.Conflict(
                "The appointment date has not arrived yet.");
        }

        var status =
            await _repository.GetApplicationStatusAsync(
                appointment.LocalDrivingLicenseApplicationID);

        if (status is null)
        {
            return Result.NotFound(
                "Local driving license application not found.");
        }

        if (status != AppStatus.New)
        {
            return Result.Conflict(
                "Tests can only be taken for an active application.");
        }

        if (!Enum.IsDefined(
                typeof(TestTypeEnum),
                appointment.TestTypeID))
        {
            return Result.ValidationFailure(
                "Invalid test type.");
        }

        return await ValidateNextTestAsync(
            appointment.LocalDrivingLicenseApplicationID,
            (TestTypeEnum)appointment.TestTypeID,
            "taken");
    }

    public async Task<bool> HasPassedAllTestsAsync(
        int localAppId)
    {
        if (localAppId <= 0)
            return false;

        var status =
            await _repository.GetApplicationStatusAsync(localAppId);

        if (status is null)
            return false;

        var passedTests =
            await _repository.GetPassedTestTypeIdsAsync(localAppId);

        return RequiredTests.All(
            testType =>
                passedTests.Contains((int)testType));
    }

    private async Task<Result> ValidateNextTestAsync(
        int localAppId,
        TestTypeEnum testType,
        string action)
    {
        var nextTest =
            await GetNextTestTypeAsync(localAppId);

        if (nextTest.IsFailure)
            return Result.FromFailure(nextTest);

        return testType == nextTest.Value
            ? Result.Success()
            : Result.Conflict(
                $"The {GetTestName(testType)} test cannot be {action} yet. " +
                $"The next required test is {GetTestName(nextTest.Value)}.");
    }

    private static Result ValidateTestRequest(
        int localAppId,
        TestTypeEnum testType)
    {
        if (localAppId <= 0)
            return Result.ValidationFailure(
                "Invalid local driving license application ID.");

        return Enum.IsDefined(testType)
            ? Result.Success()
            : Result.ValidationFailure(
                "Invalid test type.");
    }

    private static string GetTestName(
        TestTypeEnum testType) =>
        testType switch
        {
            TestTypeEnum.Theory => "Theory",
            TestTypeEnum.Written => "Written",
            TestTypeEnum.Practical => "Practical",
            _ => "Unknown"
        };
}