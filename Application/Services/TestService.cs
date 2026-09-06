using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using System.Data;

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
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
        var tests = await _repository.GetAllAsync();

        return Result<List<TestDto>>.Success(
            tests.Select(TestMapper.ToDto).ToList());
    }

    public async Task<Result<List<TestDto>>> GetByTestAppointmentIdAsync(
        int appointmentId)
    {
        var validation = TestValidator.ValidateAppointmentId(appointmentId);

        if (validation.IsFailure)
            return Result<List<TestDto>>.FromValidationFailure(validation.Error);

        var tests = await _repository.GetByTestAppointmentIdAsync(appointmentId);

        return Result<List<TestDto>>.Success(
            tests.Select(TestMapper.ToDto).ToList());
    }

    public async Task<Result<List<TestDto>>> GetByUserIdAsync(int userId)
    {
        var validation = TestValidator.ValidateUserId(userId);

        if (validation.IsFailure)
            return Result<List<TestDto>>.FromValidationFailure(validation.Error);

        var tests = await _repository.GetByUserIdAsync(userId);

        return Result<List<TestDto>>.Success(
            tests.Select(TestMapper.ToDto).ToList());
    }

    public async Task<Result<int>> AddAsync(SaveTestResultDto dto)
    {
        var validation = TestValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<int>.FromFailure(
                "You must be logged in first.");

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var appointment = await _appointmentRepository.GetForUpdateAsync(
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

            var workflow = await _workflowService.CanTakeTestAsync(
                dto.TestAppointmentID);

            if (workflow.IsFailure)
                return Result<int>.FromFailure(workflow.Error);

            var entity = TestMapper.ToEntity(
                dto,
                _currentUserService.UserId);

            await _repository.AddAsync(entity);

            appointment.IsLocked = true;

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                entity.TestID <= 0)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure(
                    "Failed to save test result.");
            }

            await transaction.CommitAsync();

            return Result<int>.Success(entity.TestID);
        }
        catch
        {
            await transaction.RollbackAsync();
            return Result<int>.FromFailure(
                "Failed to save test result.");
        }
    }

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn &&
        _currentUserService.UserId > 0;
}