using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class TestAppointmentService : ITestAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITestAppointmentRepository _repository;
    private readonly ITestTypeRepository _testTypeRepository;
    private readonly ITestRepository _testRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITestWorkflowService _workflowService;

    public TestAppointmentService(
        IUnitOfWork unitOfWork,
        ITestAppointmentRepository repository,
        ITestTypeRepository testTypeRepository,
        ITestRepository testRepository,
        ICurrentUserService currentUserService,
        ITestWorkflowService workflowService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _testTypeRepository = testTypeRepository ?? throw new ArgumentNullException(nameof(testTypeRepository));
        _testRepository = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
    }

    // ===== GET BY ID =====

    public async Task<Result<TestAppointmentDto>> GetByIdAsync(int id)
    {
        var validation = TestAppointmentValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<TestAppointmentDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result<TestAppointmentDto>.FromNotFound("Appointment not found.");

        return Result<TestAppointmentDto>.Success(TestAppointmentMapper.ToDto(entity));
    }

    // ===== GET ALL =====

    public async Task<Result<List<TestAppointmentDto>>> GetAllAsync()
    {
        var dtos = (await _repository.GetAllAsync())
            .Select(TestAppointmentMapper.ToDto)
            .ToList();

        return Result<List<TestAppointmentDto>>.Success(dtos);
    }

    // ===== GET BY LOCAL DRIVING LICENSE APPLICATION =====

    public async Task<Result<List<TestAppointmentDto>>> GetByLocalDrivingLicenseApplicationIdAsync(
        int localDrivingLicenseApplicationId)
    {
        var validation = TestAppointmentValidator.ValidateApplicationId(localDrivingLicenseApplicationId);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromValidationFailure(validation.Error);

        var dtos = (await _repository.GetByLocalDrivingLicenseApplicationIdAsync(localDrivingLicenseApplicationId))
            .Select(TestAppointmentMapper.ToDto)
            .ToList();

        return Result<List<TestAppointmentDto>>.Success(dtos);
    }

    // ===== GET BY TEST TYPE =====

    public async Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(TestTypeEnum testType)
    {
        var validation = TestAppointmentValidator.ValidateTestTypeId((int)testType);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromValidationFailure(validation.Error);

        var dtos = (await _repository.GetByTestTypeIdAsync(testType))
            .Select(TestAppointmentMapper.ToDto)
            .ToList();

        return Result<List<TestAppointmentDto>>.Success(dtos);
    }

    // ===== GET BY CREATED USER =====

    public async Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(int userId)
    {
        var validation = TestAppointmentValidator.ValidateUserId(userId);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromValidationFailure(validation.Error);

        var dtos = (await _repository.GetByCreatedUserIdAsync(userId))
            .Select(TestAppointmentMapper.ToDto)
            .ToList();

        return Result<List<TestAppointmentDto>>.Success(dtos);
    }

    // ===== GET SCHEDULE INFO =====

    public async Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(int testAppointmentId)
    {
        var validation = TestAppointmentValidator.ValidateId(testAppointmentId);
        if (validation.IsFailure)
            return Result<ScheduleTestDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetScheduleInfoAsync(testAppointmentId);
        if (entity is null)
            return Result<ScheduleTestDto>.FromNotFound("Appointment data not found.");

        var trial = await GetTrialCountAsync(entity.LocalDrivingLicenseApplicationID, entity.TestTypeID);

        return Result<ScheduleTestDto>.Success(
            TestAppointmentMapper.ToScheduleDto(entity, trial));
    }

    // ===== BUSINESS HELPERS =====

    public Task<bool> HasConflictAsync(int localAppId, int testTypeId, DateTime dateTime, int? excludeAppointmentId = null)
        => _repository.HasConflictAsync(localAppId, testTypeId, dateTime, excludeAppointmentId);

    public Task<bool> HasUserConflictAsync(int userId, DateTime dateTime, int? excludeAppointmentId = null)
        => _repository.HasUserConflictAsync(userId, dateTime, excludeAppointmentId);

    public Task<bool> HasLocalApplicationConflictAsync(int localAppId, DateTime dateTime, int? excludeAppointmentId = null)
        => _repository.HasLocalApplicationConflictAsync(localAppId, dateTime, excludeAppointmentId);

    public Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId)
        => _repository.IsAppointmentAlreadyScheduledAsync(localAppId, testTypeId);

    // ===== CREATE =====

    public async Task<Result> AddAsync(CreateTestAppointmentDto dto)
    {
        var validation = TestAppointmentValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.ValidationFailure("You must be logged in first.");

        // Validate test type enum
        if (!Enum.IsDefined(typeof(TestTypeEnum), dto.TestTypeID))
            return Result.ValidationFailure("Invalid test type.");

        // Workflow: Theory -> Written -> Practical
        var workflowResult = await _workflowService.CanScheduleTestAsync(
            dto.LocalDrivingLicenseApplicationID, (TestTypeEnum)dto.TestTypeID);

        if (workflowResult.IsFailure)
            return Result.Conflict(workflowResult.Error);

        // Duplicate / already passed
        if (await _repository.IsAppointmentAlreadyScheduledAsync(
                dto.LocalDrivingLicenseApplicationID, dto.TestTypeID))
            return Result.Conflict("An appointment already exists for this test or the test has already been passed.");

        // Test type must exist
        var testType = await _testTypeRepository.GetTestTypeByIdAsync(dto.TestTypeID);
        if (testType is null)
            return Result.NotFound("Test type not found.");

        // Local application conflict
        if (await _repository.HasLocalApplicationConflictAsync(
                dto.LocalDrivingLicenseApplicationID, dto.AppointmentDate))
            return Result.Conflict("This application already has an appointment at this date and time.");

        // User conflict
        if (await _repository.HasUserConflictAsync(_currentUserService.UserId, dto.AppointmentDate))
            return Result.Conflict("The current user already has an appointment at this date and time.");

        // Create entity
        var entity = TestAppointmentMapper.ToEntity(dto, testType.TestTypeFees, _currentUserService.UserId);

        if (!await _repository.AddAsync(entity))
            return Result.Failure("Failed to prepare appointment.");

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure("Failed to book appointment.");
    }

    // ===== UPDATE =====

    public async Task<Result> UpdateAsync(UpdateTestAppointmentDto dto)
    {
        var validation = TestAppointmentValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.ValidationFailure("You must be logged in first.");

        var entity = await _repository.GetByIdAsync(dto.TestAppointmentID);
        if (entity is null)
            return Result.NotFound("Appointment not found.");

        if (entity.IsLocked)
            return Result.Conflict("Cannot modify a locked appointment.");

        if (!Enum.IsDefined(typeof(TestTypeEnum), entity.TestTypeID))
            return Result.ValidationFailure("Invalid test type.");

        // Re-check workflow (prevents stale appointments)
        var workflowResult = await _workflowService.CanScheduleTestAsync(
            entity.LocalDrivingLicenseApplicationID, (TestTypeEnum)entity.TestTypeID);

        if (workflowResult.IsFailure)
            return Result.Conflict(workflowResult.Error);

        // No-op if same date
        if (entity.AppointmentDate == dto.AppointmentDate)
            return Result.Success();

        // Conflicts (exclude current appointment)
        if (await HasConflictAsync(entity.LocalDrivingLicenseApplicationID, entity.TestTypeID,
                dto.AppointmentDate, entity.TestAppointmentID))
            return Result.Conflict("The new date is already booked for another test.");

        if (await HasLocalApplicationConflictAsync(
                entity.LocalDrivingLicenseApplicationID, dto.AppointmentDate, entity.TestAppointmentID))
            return Result.Conflict("This application already has another appointment at this date and time.");

        if (await HasUserConflictAsync(entity.CreatedByUserID, dto.AppointmentDate, entity.TestAppointmentID))
            return Result.Conflict("The current user already has another appointment at this date and time.");

        // Apply update
        entity.AppointmentDate = dto.AppointmentDate;

        if (!await _repository.UpdateAsync(entity))
            return Result.Failure("Failed to prepare appointment update.");

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure("Failed to update appointment.");
    }

    // ===== DELETE =====

    public async Task<Result> DeleteAsync(int id)
    {
        var validation = TestAppointmentValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.ValidationFailure("You must be logged in first.");

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result.NotFound("Appointment not found.");

        if (entity.IsLocked)
            return Result.Conflict("Cannot delete a locked appointment.");

        await _repository.DeleteAsync(id);

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure("Failed to delete appointment.");
    }

    // ===== SAVE TEST RESULT =====

    public async Task<Result> SaveTestResultAsync(SaveTestResultDto dto)
    {
        var validation = TestAppointmentValidator.ValidateSaveTestResult(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.ValidationFailure("You must be logged in first.");

        var appointment = await _repository.GetByIdAsync(dto.TestAppointmentID);
        if (appointment is null)
            return Result.NotFound("Test appointment not found.");

        if (appointment.IsLocked)
            return Result.Conflict("A result has already been saved for this test.");

        // Workflow gate
        var canTakeTestResult = await _workflowService.CanTakeTestAsync(dto.TestAppointmentID);
        if (canTakeTestResult.IsFailure)
            return canTakeTestResult;

        // Prevent duplicate result
        if (await _testRepository.IsTestAlreadyTakenAsync(dto.TestAppointmentID))
            return Result.Conflict("A result has already been saved for this test.");

        // Transactional: create test result + lock appointment
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var testEntity = new Domain.Entities.Test
            {
                TestAppointmentID = dto.TestAppointmentID,
                TestResult = dto.TestResult,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedByUserID = _currentUserService.UserId
            };

            await _testRepository.AddAsync(testEntity);

            appointment.IsLocked = true;
            if (!await _repository.UpdateAsync(appointment))
            {
                await transaction.RollbackAsync();
                return Result.Failure("Failed to lock appointment.");
            }

            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                await transaction.RollbackAsync();
                return Result.Failure("Failed to save test result.");
            }

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ===== GET TRIAL COUNT =====

    public async Task<int> GetTrialCountAsync(int localAppId, int testTypeId)
    {
        if (TestAppointmentValidator.ValidateApplicationId(localAppId).IsFailure)
            return 0;

        if (TestAppointmentValidator.ValidateTestTypeId(testTypeId).IsFailure)
            return 0;

        var appointmentsResult = await GetByLocalDrivingLicenseApplicationIdAsync(localAppId);
        if (appointmentsResult.IsFailure || appointmentsResult.Value is null)
            return 0;

        return appointmentsResult.Value.Count(x => x.TestTypeID == testTypeId);
    }

    // ===== GET TEST TYPE FEES =====

    public async Task<decimal> GetTestTypeFeesAsync(int testTypeId)
    {
        if (TestAppointmentValidator.ValidateTestTypeId(testTypeId).IsFailure)
            return 0;

        var type = await _testTypeRepository.GetTestTypeByIdAsync(testTypeId);
        return type?.TestTypeFees ?? 0;
    }
}