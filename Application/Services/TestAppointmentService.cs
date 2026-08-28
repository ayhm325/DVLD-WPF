using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class TestAppointmentService : ITestAppointmentService
{
    private readonly ITestAppointmentRepository _repository;
    private readonly ITestTypeRepository _testTypeRepository;
    private readonly ITestRepository _testRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITestWorkflowService _workflowService;

    public TestAppointmentService(
        ITestAppointmentRepository repository,
        ITestTypeRepository testTypeRepository,
        ITestRepository testRepository,
        ICurrentUserService currentUserService,
        ITestWorkflowService workflowService)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(
                nameof(repository));

        _testTypeRepository =
            testTypeRepository
            ?? throw new ArgumentNullException(
                nameof(testTypeRepository));

        _testRepository =
            testRepository
            ?? throw new ArgumentNullException(
                nameof(testRepository));

        _currentUserService =
            currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _workflowService =
            workflowService
            ?? throw new ArgumentNullException(
                nameof(workflowService));
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<TestAppointmentDto>>
        GetByIdAsync(int id)
    {
        var validation =
            TestAppointmentValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<TestAppointmentDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            await _repository.GetByIdAsync(id);

        if (entity is null)
        {
            return Result<TestAppointmentDto>
                .FromNotFound(
                    "Appointment not found.");
        }

        return Result<TestAppointmentDto>
            .Success(
                TestAppointmentMapper.ToDto(entity));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<TestAppointmentDto>>>
        GetAllAsync()
    {
        var entities =
            await _repository.GetAllAsync();

        var dtos =
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList();

        return Result<List<TestAppointmentDto>>
            .Success(dtos);
    }


    // =========================================================
    // GET BY APPLICATION
    // =========================================================

    public async Task<Result<List<TestAppointmentDto>>>
        GetByApplicationIdAsync(
            int applicationId)
    {
        var validation =
            TestAppointmentValidator
                .ValidateApplicationId(
                    applicationId);

        if (validation.IsFailure)
        {
            return Result<List<TestAppointmentDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByApplicationIdAsync(
                    applicationId);

        var dtos =
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList();

        return Result<List<TestAppointmentDto>>
            .Success(dtos);
    }


    // =========================================================
    // GET BY TEST TYPE
    // =========================================================

    public async Task<Result<List<TestAppointmentDto>>>
        GetByTestTypeIdAsync(
            TestTypeEnum testType)
    {
        var validation =
            TestAppointmentValidator
                .ValidateTestTypeId(
                    (int)testType);

        if (validation.IsFailure)
        {
            return Result<List<TestAppointmentDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByTestTypeIdAsync(
                    testType);

        var dtos =
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList();

        return Result<List<TestAppointmentDto>>
            .Success(dtos);
    }


    // =========================================================
    // GET BY CREATED USER
    // =========================================================

    public async Task<Result<List<TestAppointmentDto>>>
        GetByCreatedUserIdAsync(
            int userId)
    {
        var validation =
            TestAppointmentValidator
                .ValidateUserId(userId);

        if (validation.IsFailure)
        {
            return Result<List<TestAppointmentDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByCreatedUserIdAsync(
                    userId);

        var dtos =
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList();

        return Result<List<TestAppointmentDto>>
            .Success(dtos);
    }


    // =========================================================
    // GET SCHEDULE INFO
    // =========================================================

    public async Task<Result<ScheduleTestDto>>
        GetScheduleInfoAsync(
            int testAppointmentId)
    {
        var validation =
            TestAppointmentValidator
                .ValidateId(testAppointmentId);

        if (validation.IsFailure)
        {
            return Result<ScheduleTestDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            await _repository
                .GetScheduleInfoAsync(
                    testAppointmentId);

        if (entity is null)
        {
            return Result<ScheduleTestDto>
                .FromNotFound(
                    "Appointment data not found.");
        }

        var trial =
            await GetTrialCountAsync(
                entity.LocalDrivingLicenseApplicationID,
                entity.TestTypeID);

        var dto =
            TestAppointmentMapper.ToScheduleDto(
                entity,
                trial);

        return Result<ScheduleTestDto>
            .Success(dto);
    }


    // =========================================================
    // BUSINESS HELPERS
    // =========================================================

    public async Task<bool>
    HasConflictAsync(
        int localAppId,
        int testTypeId,
        DateTime dateTime,
        int? excludeAppointmentId = null)
    {
        return await _repository
            .HasConflictAsync(
                localAppId,
                testTypeId,
                dateTime,
                excludeAppointmentId);
    }


    public async Task<bool>
        HasUserConflictAsync(
            int userId,
            DateTime dateTime)
    {
        return await _repository
            .HasUserConflictAsync(
                userId,
                dateTime);
    }


    public async Task<bool>
        HasApplicationConflictAsync(
            int applicationId,
            DateTime dateTime)
    {
        return await _repository
            .HasApplicationConflictAsync(
                applicationId,
                dateTime);
    }


    public async Task<bool>
        IsAppointmentAlreadyScheduledAsync(
            int localAppId,
            int testTypeId)
    {
        return await _repository
            .IsAppointmentAlreadyScheduledAsync(
                localAppId,
                testTypeId);
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result>
        AddAsync(
            CreateTestAppointmentDto dto)
    {
        var validation =
            TestAppointmentValidator
                .ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }


        // -----------------------------------------------------
        // WORKFLOW
        // Theory -> Written -> Practical
        // -----------------------------------------------------

        if (!Enum.IsDefined(
                typeof(TestTypeEnum),
                dto.TestTypeID))
        {
            return Result.ValidationFailure(
                "Invalid test type.");
        }

        var workflowResult =
            await _workflowService
                .CanScheduleTestAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    (TestTypeEnum)dto.TestTypeID);

        if (workflowResult.IsFailure)
        {
            return Result.Conflict(
                workflowResult.Error);
        }


        // -----------------------------------------------------
        // DUPLICATE / PASSED TEST
        // -----------------------------------------------------

        var alreadyScheduled =
            await _repository
                .IsAppointmentAlreadyScheduledAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    dto.TestTypeID);

        if (alreadyScheduled)
        {
            return Result.Conflict(
                "An appointment already exists for this test " +
                "or the test has already been passed.");
        }


        // -----------------------------------------------------
        // TEST TYPE
        // -----------------------------------------------------

        var testType =
            await _testTypeRepository
                .GetTestTypeByIdAsync(
                    dto.TestTypeID);

        if (testType is null)
        {
            return Result.NotFound(
                "Test type not found.");
        }


        // -----------------------------------------------------
        // USER
        // -----------------------------------------------------

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            return Result.ValidationFailure(
                "You must be logged in first.");
        }


        // -----------------------------------------------------
        // CREATE ENTITY
        // -----------------------------------------------------

        var entity =
            TestAppointmentMapper.ToEntity(
                dto,
                testType.TestTypeFees,
                _currentUserService.UserId);


        var isSuccess =
            await _repository.AddAsync(entity);

        return isSuccess
            ? Result.Success()
            : Result.Failure(
                "Failed to book appointment.");
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result> UpdateAsync(UpdateTestAppointmentDto dto)
    {
        var validation =
            TestAppointmentValidator
                .ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var entity =
            await _repository
                .GetByIdAsync(
                    dto.TestAppointmentID);

        if (entity is null)
        {
            return Result.NotFound(
                "Appointment not found.");
        }

        if (entity.IsLocked)
        {
            return Result.Conflict(
                "Cannot modify a locked appointment.");
        }

        // Same date/time → nothing to update
        if (entity.AppointmentDate ==
            dto.AppointmentDate)
        {
            return Result.Success();
        }

        // =========================================================
        // CHECK CONFLICT
        // Same Local Application + Same Test Type + Same Date/Time
        // Exclude the current appointment because this is an Edit.
        // =========================================================

        var hasConflict =
            await HasConflictAsync(
                entity.LocalDrivingLicenseApplicationID,
                entity.TestTypeID,
                dto.AppointmentDate,
                entity.TestAppointmentID);

        if (hasConflict)
        {
            return Result.Conflict(
                "The new date is already booked for another test.");
        }

        // =========================================================
        // UPDATE
        // =========================================================

        entity.AppointmentDate =
            dto.AppointmentDate;

        var isSuccess =
            await _repository
                .UpdateAsync(entity);

        return isSuccess
            ? Result.Success()
            : Result.Failure(
                "Failed to update appointment.");
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteAsync(int id)
    {
        var validation =
            TestAppointmentValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }


        var entity =
            await _repository
                .GetByIdAsync(id);

        if (entity is null)
        {
            return Result.NotFound(
                "Appointment not found.");
        }


        if (entity.IsLocked)
        {
            return Result.Conflict(
                "Cannot delete a locked appointment.");
        }


        await _repository.DeleteAsync(id);

        return Result.Success();
    }


    // =========================================================
    // SAVE TEST RESULT
    // =========================================================

    public async Task<Result>
        SaveTestResultAsync(
            SaveTestResultDto dto)
    {
        var validation =
            TestAppointmentValidator
                .ValidateSaveTestResult(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }


        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            return Result.ValidationFailure(
                "You must be logged in first.");
        }


        var appointment =
            await _repository
                .GetByIdAsync(
                    dto.TestAppointmentID);

        if (appointment is null)
        {
            return Result.NotFound(
                "Test appointment not found.");
        }


        if (appointment.IsLocked)
        {
            return Result.Conflict(
                "A result has already been saved for this test.");
        }


        var testEntity =
            new Domain.Entities.Test
            {
                TestAppointmentID =
                    dto.TestAppointmentID,

                TestResult =
                    dto.TestResult,

                Notes =
                    string.IsNullOrWhiteSpace(dto.Notes)
                        ? null
                        : dto.Notes.Trim(),

                CreatedByUserID =
                    _currentUserService.UserId
            };


        var newTestId =
            await _testRepository
                .AddAsync(testEntity);

        if (newTestId <= 0)
        {
            return Result.Failure(
                "Failed to save test result.");
        }


        appointment.IsLocked =
            true;


        var isSuccess =
            await _repository
                .UpdateAsync(appointment);

        if (!isSuccess)
        {
            return Result.Failure(
                "Failed to lock appointment after saving result.");
        }


        return Result.Success();
    }


    // =========================================================
    // GET TRIAL COUNT
    // =========================================================

    public async Task<int>
        GetTrialCountAsync(
            int localAppId,
            int testTypeId)
    {
        var appValidation =
            TestAppointmentValidator
                .ValidateApplicationId(
                    localAppId);

        if (appValidation.IsFailure)
            return 0;


        var typeValidation =
            TestAppointmentValidator
                .ValidateTestTypeId(
                    testTypeId);

        if (typeValidation.IsFailure)
            return 0;


        var appointmentsResult =
            await GetByApplicationIdAsync(
                localAppId);

        if (appointmentsResult.IsFailure ||
            appointmentsResult.Value is null)
        {
            return 0;
        }


        return appointmentsResult.Value
            .Count(x =>
                x.TestTypeID == testTypeId);
    }


    // =========================================================
    // GET TEST TYPE FEES
    // =========================================================

    public async Task<decimal>
        GetTestTypeFeesAsync(
            int testTypeId)
    {
        var validation =
            TestAppointmentValidator
                .ValidateTestTypeId(
                    testTypeId);

        if (validation.IsFailure)
            return 0;


        var type =
            await _testTypeRepository
                .GetTestTypeByIdAsync(
                    testTypeId);

        return type?.TestTypeFees ?? 0;
    }
}