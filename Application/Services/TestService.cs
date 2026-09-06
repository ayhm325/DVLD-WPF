using Application.Common.Results;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public sealed class TestService : ITestService
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
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _appointmentRepository = appointmentRepository
            ?? throw new ArgumentNullException(nameof(appointmentRepository));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _workflowService = workflowService
            ?? throw new ArgumentNullException(nameof(workflowService));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<TestDto>> GetByIdAsync(int id)
    {
        var validation = TestValidator.ValidateId(id);

        if (validation.IsFailure)
            return Result<TestDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);

        return entity is null
            ? Result<TestDto>.FromNotFound("Test not found.")
            : Result<TestDto>.Success(TestMapper.ToDto(entity));
    }

    public async Task<Result<List<TestDto>>> GetAllAsync()
    {
        var dtos = (await _repository.GetAllAsync())
            .Select(TestMapper.ToDto)
            .ToList();

        return Result<List<TestDto>>.Success(dtos);
    }

    public async Task<Result<List<TestDto>>> GetByTestAppointmentIdAsync(
        int appointmentId)
    {
        var validation =
            TestValidator.ValidateAppointmentId(appointmentId);

        if (validation.IsFailure)
            return Result<List<TestDto>>
                .FromValidationFailure(validation.Error);

        var dtos = (await _repository
                .GetByTestAppointmentIdAsync(appointmentId))
            .Select(TestMapper.ToDto)
            .ToList();

        return Result<List<TestDto>>.Success(dtos);
    }

    public async Task<Result<List<TestDto>>> GetByUserIdAsync(int userId)
    {
        var validation = TestValidator.ValidateUserId(userId);

        if (validation.IsFailure)
            return Result<List<TestDto>>
                .FromValidationFailure(validation.Error);

        var dtos = (await _repository.GetByUserIdAsync(userId))
            .Select(TestMapper.ToDto)
            .ToList();

        return Result<List<TestDto>>.Success(dtos);
    }

    public Task<bool> IsTestExistsAsync(int id) =>
        TestValidator.ValidateId(id).IsFailure
            ? Task.FromResult(false)
            : _repository.IsTestExistsAsync(id);

    public Task<bool> IsTestAlreadyTakenAsync(int appointmentId) =>
        TestValidator.ValidateAppointmentId(appointmentId).IsFailure
            ? Task.FromResult(false)
            : _repository.IsTestAlreadyTakenAsync(appointmentId);

    public async Task<Result<int>> AddAsync(TestDto dto)
    {
        var validation = TestValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<int>.FromValidationFailure(
                "You must be logged in first.");

        var appointment =
            await _appointmentRepository.GetByIdAsync(
                dto.TestAppointmentID);

        if (appointment is null)
            return Result<int>.FromNotFound(
                "Test appointment not found.");

        if (appointment.IsLocked)
            return Result<int>.FromConflict(
                "This appointment is already locked.");

        if (await _repository.IsTestAlreadyTakenAsync(
                dto.TestAppointmentID))
        {
            return Result<int>.FromConflict(
                "A result already exists for this appointment.");
        }

        var canTakeTest =
            await _workflowService.CanTakeTestAsync(
                dto.TestAppointmentID);

        if (canTakeTest.IsFailure)
            return Result<int>.FromConflict(
                canTakeTest.Error);

        var entity = TestMapper.ToEntity(
            dto,
            _currentUserService.UserId);

        await _repository.AddAsync(entity);

        appointment.IsLocked = true;

        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0 || entity.TestID <= 0)
            return Result<int>.FromFailure(
                "Failed to add test.");

        return Result<int>.Success(entity.TestID);
    }

    public async Task<Result> UpdateAsync(TestDto dto)
    {
        var validation = TestValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result.ValidationFailure(
                "You must be logged in first.");

        var entity =
            await _repository.GetForUpdateAsync(dto.TestID);

        if (entity is null)
            return Result.NotFound("Test not found.");

        if (entity.TestAppointmentID != dto.TestAppointmentID)
            return Result.Conflict(
                "Cannot change the linked appointment of a test result.");

        var appointment =
            await _appointmentRepository.GetByIdAsync(
                entity.TestAppointmentID);

        if (appointment is null)
            return Result.NotFound(
                "Test appointment not found.");

        if (appointment.IsLocked)
            return Result.Conflict(
                "Cannot modify a result for a locked appointment.");

        TestMapper.UpdateEntity(entity, dto);

        return await SaveAsync("Failed to update test.");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var validation = TestValidator.ValidateId(id);

        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var entity = await _repository.GetForUpdateAsync(id);

        if (entity is null)
            return Result.NotFound("Test not found.");

        var appointment =
            await _appointmentRepository.GetByIdAsync(
                entity.TestAppointmentID);

        if (appointment is null)
            return Result.NotFound(
                "Test appointment not found.");

        if (appointment.IsLocked)
            return Result.Conflict(
                "Cannot delete a result from a locked appointment.");

        _repository.Delete(entity);

        return await SaveAsync("Failed to delete test.");
    }

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn &&
        _currentUserService.UserId > 0;

    private async Task<Result> SaveAsync(string errorMessage)
    {
        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure(errorMessage);
    }
}
