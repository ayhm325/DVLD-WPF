using Application.Common.Results;
using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

public class TestWorkflowService : ITestWorkflowService
{
    private readonly ITestAppointmentRepository _appointmentRepository;

    public TestWorkflowService(
        ITestAppointmentRepository appointmentRepository)
    {
        _appointmentRepository =
            appointmentRepository
            ?? throw new ArgumentNullException(
                nameof(appointmentRepository));
    }


    // =========================================================
    // CAN SCHEDULE TEST
    // =========================================================

    public async Task<Result> CanScheduleTestAsync(
        int localAppId,
        TestTypeEnum testType)
    {
        if (localAppId <= 0)
        {
            return Result.ValidationFailure(
                "Invalid local driving license application ID.");
        }

        if (!Enum.IsDefined(testType))
        {
            return Result.ValidationFailure(
                "Invalid test type.");
        }


        // -----------------------------------------------------
        // APPLICATION STATUS
        // Only New applications can schedule tests.
        // -----------------------------------------------------

        var applicationStatus =
            await _appointmentRepository
                .GetApplicationStatusAsync(localAppId);

        if (applicationStatus is null)
        {
            return Result.NotFound(
                "Local driving license application not found.");
        }

        if (applicationStatus != AppStatus.New)
        {
            return Result.Conflict(
                "Tests can only be scheduled for an active application.");
        }


        // -----------------------------------------------------
        // WORKFLOW
        // Theory -> Written -> Practical
        // -----------------------------------------------------

        var nextTestResult =
            await GetNextTestTypeAsync(localAppId);

        if (nextTestResult.IsFailure)
        {
            return Result.Conflict(
                nextTestResult.Error);
        }


        var nextTest =
            nextTestResult.Value;


        if (testType != nextTest)
        {
            return Result.Conflict(
                $"The {GetTestName(testType)} test cannot be scheduled yet. " +
                $"The next required test is {GetTestName(nextTest)}.");
        }


        return Result.Success();
    }


    // =========================================================
    // GET NEXT TEST
    // =========================================================

    public async Task<Result<TestTypeEnum>> GetNextTestTypeAsync(
        int localAppId)
    {
        if (localAppId <= 0)
        {
            return Result<TestTypeEnum>.FromValidationFailure(
                "Invalid local driving license application ID.");
        }


        // -----------------------------------------------------
        // THEORY
        // -----------------------------------------------------

        if (!await HasPassedTestAsync(
                localAppId,
                TestTypeEnum.Theory))
        {
            return Result<TestTypeEnum>.Success(
                TestTypeEnum.Theory);
        }


        // -----------------------------------------------------
        // WRITTEN
        // -----------------------------------------------------

        if (!await HasPassedTestAsync(
                localAppId,
                TestTypeEnum.Written))
        {
            return Result<TestTypeEnum>.Success(
                TestTypeEnum.Written);
        }


        // -----------------------------------------------------
        // PRACTICAL
        // -----------------------------------------------------

        if (!await HasPassedTestAsync(
                localAppId,
                TestTypeEnum.Practical))
        {
            return Result<TestTypeEnum>.Success(
                TestTypeEnum.Practical);
        }


        // -----------------------------------------------------
        // ALL PASSED
        // -----------------------------------------------------

        return Result<TestTypeEnum>.FromConflict(
            "All required tests have already been passed.");
    }


    // =========================================================
    // CAN TAKE TEST
    // =========================================================

    public async Task<Result> CanTakeTestAsync(
        int testAppointmentId)
    {
        if (testAppointmentId <= 0)
        {
            return Result.ValidationFailure(
                "Invalid test appointment ID.");
        }


        // -----------------------------------------------------
        // GET APPOINTMENT
        // -----------------------------------------------------

        var appointment =
            await _appointmentRepository
                .GetByIdAsync(testAppointmentId);


        if (appointment is null)
        {
            return Result.NotFound(
                "Test appointment not found.");
        }


        // -----------------------------------------------------
        // LOCKED APPOINTMENT
        // -----------------------------------------------------

        if (appointment.IsLocked)
        {
            return Result.Conflict(
                "This appointment is already locked.");
        }


        // -----------------------------------------------------
        // APPOINTMENT DATE
        // -----------------------------------------------------

        if (appointment.AppointmentDate > DateTime.Now)
        {
            return Result.Conflict(
                "The appointment date has not arrived yet.");
        }


        // -----------------------------------------------------
        // GET LOCAL APPLICATION
        // -----------------------------------------------------

        var localAppId =
            appointment.LocalDrivingLicenseApplicationID;


        // -----------------------------------------------------
        // APPLICATION STATUS
        // Only New applications can take tests.
        // -----------------------------------------------------

        var applicationStatus =
            await _appointmentRepository
                .GetApplicationStatusAsync(localAppId);

        if (applicationStatus is null)
        {
            return Result.NotFound(
                "Local driving license application not found.");
        }

        if (applicationStatus != AppStatus.New)
        {
            return Result.Conflict(
                "Tests can only be taken for an active application.");
        }


        var testType =
            (TestTypeEnum)appointment.TestTypeID;


        // -----------------------------------------------------
        // VALIDATE TEST TYPE
        // -----------------------------------------------------

        if (!Enum.IsDefined(testType))
        {
            return Result.ValidationFailure(
                "Invalid test type.");
        }


        // -----------------------------------------------------
        // MAKE SURE THIS IS THE CORRECT TEST
        // -----------------------------------------------------

        var nextTestResult =
            await GetNextTestTypeAsync(localAppId);


        if (nextTestResult.IsFailure)
        {
            return Result.Conflict(
                nextTestResult.Error);
        }


        var nextTest =
            nextTestResult.Value;


        if (testType != nextTest)
        {
            return Result.Conflict(
                $"The {GetTestName(testType)} test cannot be taken yet. " +
                $"The next required test is {GetTestName(nextTest)}.");
        }


        return Result.Success();
    }


    // =========================================================
    // HAS PASSED ALL TESTS
    // =========================================================

    public async Task<bool> HasPassedAllTestsAsync(
        int localAppId)
    {
        if (localAppId <= 0)
            return false;


        if (!await HasPassedTestAsync(
                localAppId,
                TestTypeEnum.Theory))
        {
            return false;
        }


        if (!await HasPassedTestAsync(
                localAppId,
                TestTypeEnum.Written))
        {
            return false;
        }


        return await HasPassedTestAsync(
            localAppId,
            TestTypeEnum.Practical);
    }


    // =========================================================
    // CHECK PASSED TEST
    // =========================================================

    private async Task<bool> HasPassedTestAsync(
        int localAppId,
        TestTypeEnum testType)
    {
        var appointments =
    await _appointmentRepository
        .GetByLocalDrivingLicenseApplicationIdAsync(localAppId);


        return appointments.Any(a =>
            a.TestTypeID == (int)testType &&
            a.Test is not null &&
            a.Test.TestResult);
    }


    // =========================================================
    // TEST NAME
    // =========================================================

    private static string GetTestName(
        TestTypeEnum testType)
    {
        return testType switch
        {
            TestTypeEnum.Theory =>
                "Theory",

            TestTypeEnum.Written =>
                "Written",

            TestTypeEnum.Practical =>
                "Practical",

            _ =>
                "Unknown"
        };
    }
}