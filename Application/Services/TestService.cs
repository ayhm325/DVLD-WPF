using Application.Common.Results;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public class TestService : ITestService
{
    private readonly ITestRepository _repository;
    private readonly ITestAppointmentRepository _appointmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITestWorkflowService _workflowService;
    private readonly IUnitOfWork _unitOfWork;


public TestService(
    ITestRepository repository,
    ITestAppointmentRepository appointmentRepository,
    ICurrentUserService currentUserService,
    ITestWorkflowService workflowService,
    IUnitOfWork unitOfWork)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(nameof(repository));

        _appointmentRepository =
            appointmentRepository
            ?? throw new ArgumentNullException(nameof(appointmentRepository));

        _currentUserService =
            currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _workflowService =
            workflowService
            ?? throw new ArgumentNullException(nameof(workflowService));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<TestDto>> GetByIdAsync(int id)
    {
        var validation =
            TestValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<TestDto>
                .FromValidationFailure(validation.Error);
        }

        var entity =
            await _repository.GetByIdAsync(id);

        if (entity is null)
        {
            return Result<TestDto>
                .FromNotFound("Test not found.");
        }

        return Result<TestDto>
            .Success(TestMapper.ToDto(entity));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<TestDto>>> GetAllAsync()
    {
        var entities =
            await _repository.GetAllAsync();

        var dtos =
            entities
                .Select(TestMapper.ToDto)
                .ToList();

        return Result<List<TestDto>>
            .Success(dtos);
    }

    // =========================================================
    // GET BY TEST APPOINTMENT ID
    // =========================================================

    public async Task<Result<List<TestDto>>>
        GetByTestAppointmentIdAsync(int appointmentId)
    {
        var validation =
            TestValidator.ValidateAppointmentId(appointmentId);

        if (validation.IsFailure)
        {
            return Result<List<TestDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository
                .GetByTestAppointmentIdAsync(appointmentId);

        var dtos =
            entities
                .Select(TestMapper.ToDto)
                .ToList();

        return Result<List<TestDto>>
            .Success(dtos);
    }

    // =========================================================
    // GET BY USER ID
    // =========================================================

    public async Task<Result<List<TestDto>>>
        GetByUserIdAsync(int userId)
    {
        var validation =
            TestValidator.ValidateUserId(userId);

        if (validation.IsFailure)
        {
            return Result<List<TestDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository
                .GetByUserIdAsync(userId);

        var dtos =
            entities
                .Select(TestMapper.ToDto)
                .ToList();

        return Result<List<TestDto>>
            .Success(dtos);
    }

    // =========================================================
    // CHECK TEST EXISTS
    // =========================================================

    public async Task<bool> IsTestExistsAsync(int id)
    {
        var validation =
            TestValidator.ValidateId(id);

        if (validation.IsFailure)
            return false;

        return await _repository
            .IsTestExistsAsync(id);
    }

    // =========================================================
    // CHECK TEST ALREADY TAKEN
    // =========================================================

    public async Task<bool>
        IsTestAlreadyTakenAsync(int appointmentId)
    {
        var validation =
            TestValidator.ValidateAppointmentId(appointmentId);

        if (validation.IsFailure)
            return false;

        return await _repository
            .IsTestAlreadyTakenAsync(appointmentId);
    }

    // =========================================================
    // CREATE TEST RESULT
    // =========================================================

    public async Task<Result<int>> AddAsync(TestDto dto)
    {
        var validation =
            TestValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(validation.Error);
        }

        // -----------------------------------------------------
        // CURRENT USER
        // -----------------------------------------------------

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            return Result<int>.FromValidationFailure("You must be logged in first.");
        }

        // -----------------------------------------------------
        // APPOINTMENT
        // -----------------------------------------------------

        var appointment =
            await _appointmentRepository
                .GetByIdAsync(dto.TestAppointmentID);

        if (appointment is null)
        {
            return Result<int>
                .FromNotFound(
                    "Test appointment not found.");
        }

        // -----------------------------------------------------
        // PREVENT DUPLICATE RESULT
        // -----------------------------------------------------

        if (await _repository
            .IsTestAlreadyTakenAsync(dto.TestAppointmentID))
        {
            return Result<int>
                .FromConflict(
                    "A result already exists for this appointment.");
        }

        // -----------------------------------------------------
        // WORKFLOW
        //
        // This is the important protection.
        //
        // The test can only be taken when the appointment
        // belongs to the currently allowed step:
        //
        // Theory -> Written -> Practical
        // -----------------------------------------------------

        var canTakeTestResult =
            await _workflowService
                .CanTakeTestAsync(
                    dto.TestAppointmentID);

        if (canTakeTestResult.IsFailure)
        {
            return Result<int>
                .FromConflict(
                    canTakeTestResult.Error);
        }

        // -----------------------------------------------------
        // CREATE ENTITY
        // -----------------------------------------------------

        var entity =
            TestMapper.ToEntity(
                dto,
                _currentUserService.UserId);

        // -----------------------------------------------------
        // STAGE ENTITY
        // -----------------------------------------------------

        await _repository
            .AddAsync(entity);

        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0 ||
            entity.TestID <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to add test.");
        }

        return Result<int>
            .Success(entity.TestID);
    }

    // =========================================================
    // UPDATE TEST RESULT
    // =========================================================

    public async Task<Result> UpdateAsync(TestDto dto)
    {
        var validation =
            TestValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(validation.Error);
        }

        // -----------------------------------------------------
        // CURRENT USER
        // -----------------------------------------------------

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            return Result
                .ValidationFailure(
                    "You must be logged in first.");
        }

        // -----------------------------------------------------
        // GET EXISTING TEST
        // -----------------------------------------------------

        var entity =
            await _repository
                .GetByIdAsync(dto.TestID);

        if (entity is null)
        {
            return Result
                .NotFound("Test not found.");
        }

        // -----------------------------------------------------
        // APPOINTMENT CANNOT CHANGE
        // -----------------------------------------------------

        if (entity.TestAppointmentID !=
            dto.TestAppointmentID)
        {
            return Result
                .Conflict(
                    "Cannot change the linked appointment of a test result.");
        }

        // -----------------------------------------------------
        // APPOINTMENT MUST EXIST
        // -----------------------------------------------------

        var appointment =
            await _appointmentRepository
                .GetByIdAsync(
                    entity.TestAppointmentID);

        if (appointment is null)
        {
            return Result
                .NotFound(
                    "Test appointment not found.");
        }

        // -----------------------------------------------------
        // LOCKED APPOINTMENT
        // -----------------------------------------------------

        if (appointment.IsLocked)
        {
            return Result
                .Conflict(
                    "Cannot modify a result for a locked appointment.");
        }

        // -----------------------------------------------------
        // WORKFLOW
        //
        // Prevent modifying a test result when the appointment
        // is no longer valid according to the workflow.
        // -----------------------------------------------------

        var canTakeTestResult =
            await _workflowService
                .CanTakeTestAsync(
                    entity.TestAppointmentID);

        if (canTakeTestResult.IsFailure)
        {
            return Result
                .Conflict(
                    canTakeTestResult.Error);
        }

        // -----------------------------------------------------
        // UPDATE ENTITY
        // -----------------------------------------------------

        TestMapper.UpdateEntity(
            entity,
            dto);

        var updated =
            await _repository
                .UpdateAsync(entity);

        if (!updated)
        {
            return Result
                .Failure(
                    "Failed to update test.");
        }

        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result
                .Failure(
                    "No test changes were saved.");
        }

        return Result.Success();
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result> DeleteAsync(int id)
    {
        var validation =
            TestValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(validation.Error);
        }

        // -----------------------------------------------------
        // GET TEST
        // -----------------------------------------------------

        var entity =
            await _repository
                .GetByIdAsync(id);

        if (entity is null)
        {
            return Result
                .NotFound("Test not found.");
        }

        // -----------------------------------------------------
        // GET APPOINTMENT
        // -----------------------------------------------------

        var appointment =
            await _appointmentRepository
                .GetByIdAsync(
                    entity.TestAppointmentID);

        if (appointment is null)
        {
            return Result
                .NotFound(
                    "Test appointment not found.");
        }

        // -----------------------------------------------------
        // LOCKED RESULT
        // -----------------------------------------------------

        if (appointment.IsLocked)
        {
            return Result
                .Conflict(
                    "Cannot delete a result from a locked appointment.");
        }

        // -----------------------------------------------------
        // DELETE
        // -----------------------------------------------------

        var deleted =
            await _repository
                .DeleteAsync(id);

        if (!deleted)
        {
            return Result
                .Failure(
                    "Failed to delete test.");
        }

        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result
                .Failure(
                    "Failed to save test deletion.");
        }

        return Result.Success();
    }
}
