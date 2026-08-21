using Application.Common.Results;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

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
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    // GET
    public async Task<Result<TestDto>> GetByIdAsync(int id)
    {
        var validation = TestValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<TestDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result<TestDto>.FromFailure("Test not found.");

        return Result<TestDto>.Success(MapToDto(entity));
    }

    public async Task<Result<List<TestDto>>> GetAllAsync()
    {
        var list = await _repository.GetAllAsync();
        return Result<List<TestDto>>.Success(list.Select(MapToDto).ToList());
    }

    public async Task<Result<List<TestDto>>> GetByTestAppointmentIdAsync(int appointmentId)
    {
        var validation = TestValidator.ValidateAppointmentId(appointmentId);
        if (validation.IsFailure)
            return Result<List<TestDto>>.FromFailure(validation.Error);

        var list = await _repository.GetByTestAppointmentIdAsync(appointmentId);
        return Result<List<TestDto>>.Success(list.Select(MapToDto).ToList());
    }

    public async Task<Result<List<TestDto>>> GetByUserIdAsync(int userId)
    {
        var validation = TestValidator.ValidateUserId(userId);
        if (validation.IsFailure)
            return Result<List<TestDto>>.FromFailure(validation.Error);

        var list = await _repository.GetByUserIdAsync(userId);
        return Result<List<TestDto>>.Success(list.Select(MapToDto).ToList());
    }

    // CHECKS
    public async Task<bool> IsTestExistsAsync(int id)
    {
        var validation = TestValidator.ValidateId(id);
        if (validation.IsFailure) return false;
        return await _repository.IsTestExistsAsync(id);
    }

    public async Task<bool> IsTestAlreadyTakenAsync(int appointmentId)
    {
        var validation = TestValidator.ValidateAppointmentId(appointmentId);
        if (validation.IsFailure) return false;
        return await _repository.IsTestAlreadyTakenAsync(appointmentId);
    }

    // CREATE
    public async Task<Result<int>> AddAsync(TestDto dto)
    {
        var validation = TestValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        var appointment = await _appointmentRepository.GetByIdAsync(dto.TestAppointmentID);
        if (appointment is null)
            return Result<int>.FromFailure("Test appointment not found.");

        if (await _repository.IsTestAlreadyTakenAsync(dto.TestAppointmentID))
            return Result<int>.FromFailure("A result already exists for this appointment.");

        var entity = new Test
        {
            TestAppointmentID = dto.TestAppointmentID,
            TestResult = dto.TestResult,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            CreatedByUserID = _currentUserService.UserId
        };

        var id = await _repository.AddAsync(entity);
        if (id <= 0)
            return Result<int>.FromFailure("Failed to add test.");

        return Result<int>.Success(id);
    }

    // UPDATE
    public async Task<Result> UpdateAsync(TestDto dto)
    {
        var validation = TestValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return validation;

        var entity = await _repository.GetByIdAsync(dto.TestID);
        if (entity is null)
            return Result.Failure("Test not found.");

        if (entity.TestAppointmentID != dto.TestAppointmentID)
            return Result.Failure("Cannot change the linked appointment of a test result.");

        entity.TestResult = dto.TestResult;
        entity.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        var isSuccess = await _repository.UpdateAsync(entity);
        return isSuccess ? Result.Success() : Result.Failure("Failed to update test.");
    }

    // DELETE
    public async Task<Result> DeleteAsync(int id)
    {
        var validation = TestValidator.ValidateId(id);
        if (validation.IsFailure)
            return validation;

        if (!await _repository.IsTestExistsAsync(id))
            return Result.Failure("Test not found.");

        var isSuccess = await _repository.DeleteAsync(id);
        return isSuccess ? Result.Success() : Result.Failure("Failed to delete test.");
    }

    // MAPPING
    private static TestDto MapToDto(Test entity)
    {
        return new TestDto
        {
            TestID = entity.TestID,
            TestAppointmentID = entity.TestAppointmentID,
            TestResult = entity.TestResult,
            Notes = entity.Notes,
            CreatedByUserID = entity.CreatedByUserID,
            CreatedByUserName = entity.User?.UserName,
            TestTypeName = entity.TestAppointment?.TestType?.TestTypeTitle,
            AppointmentDate = entity.TestAppointment?.AppointmentDate
        };
    }
}