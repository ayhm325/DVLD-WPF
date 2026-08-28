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

    public TestService(
        ITestRepository repository,
        ITestAppointmentRepository appointmentRepository,
        ICurrentUserService currentUserService)
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
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<TestDto>>
        GetByIdAsync(int id)
    {
        var validation =
            TestValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<TestDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            await _repository.GetByIdAsync(id);

        if (entity is null)
        {
            return Result<TestDto>
                .FromNotFound(
                    "Test not found.");
        }

        return Result<TestDto>
            .Success(
                TestMapper.ToDto(entity));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<TestDto>>>
        GetAllAsync()
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
        GetByTestAppointmentIdAsync(
            int appointmentId)
    {
        var validation =
            TestValidator
                .ValidateAppointmentId(
                    appointmentId);

        if (validation.IsFailure)
        {
            return Result<List<TestDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByTestAppointmentIdAsync(
                    appointmentId);

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
                .FromValidationFailure(
                    validation.Error);
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
    // CHECKS
    // =========================================================

    public async Task<bool>
        IsTestExistsAsync(int id)
    {
        var validation =
            TestValidator.ValidateId(id);

        if (validation.IsFailure)
            return false;

        return await _repository
            .IsTestExistsAsync(id);
    }


    public async Task<bool>
        IsTestAlreadyTakenAsync(
            int appointmentId)
    {
        var validation =
            TestValidator
                .ValidateAppointmentId(
                    appointmentId);

        if (validation.IsFailure)
            return false;

        return await _repository
            .IsTestAlreadyTakenAsync(
                appointmentId);
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<int>>
        AddAsync(TestDto dto)
    {
        var validation =
            TestValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    validation.Error);
        }


        // -----------------------------------------------------
        // Appointment must exist
        // -----------------------------------------------------

        var appointment =
            await _appointmentRepository
                .GetByIdAsync(
                    dto.TestAppointmentID);

        if (appointment is null)
        {
            return Result<int>
                .FromNotFound(
                    "Test appointment not found.");
        }


        // -----------------------------------------------------
        // Prevent duplicate test result
        // -----------------------------------------------------

        if (await _repository
            .IsTestAlreadyTakenAsync(
                dto.TestAppointmentID))
        {
            return Result<int>
                .FromConflict(
                    "A result already exists for this appointment.");
        }


        // -----------------------------------------------------
        // DTO -> Entity
        // -----------------------------------------------------

        var entity =
            TestMapper.ToEntity(
                dto,
                _currentUserService.UserId);


        // -----------------------------------------------------
        // CREATE
        // -----------------------------------------------------

        var id =
            await _repository.AddAsync(entity);

        if (id <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to add test.");
        }

        return Result<int>
            .Success(id);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateAsync(TestDto dto)
    {
        var validation =
            TestValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }


        var entity =
            await _repository
                .GetByIdAsync(
                    dto.TestID);

        if (entity is null)
        {
            return Result
                .NotFound(
                    "Test not found.");
        }


        // -----------------------------------------------------
        // Appointment cannot be changed
        // -----------------------------------------------------

        if (entity.TestAppointmentID !=
            dto.TestAppointmentID)
        {
            return Result
                .Conflict(
                    "Cannot change the linked appointment of a test result.");
        }


        // -----------------------------------------------------
        // Update entity
        // -----------------------------------------------------

        TestMapper.UpdateEntity(
            entity,
            dto);


        var updated =
            await _repository
                .UpdateAsync(entity);

        return updated
            ? Result.Success()
            : Result.Failure(
                "Failed to update test.");
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteAsync(int id)
    {
        var validation =
            TestValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }


        if (!await _repository
            .IsTestExistsAsync(id))
        {
            return Result
                .NotFound(
                    "Test not found.");
        }


        var deleted =
            await _repository
                .DeleteAsync(id);

        return deleted
            ? Result.Success()
            : Result.Failure(
                "Failed to delete test.");
    }
}