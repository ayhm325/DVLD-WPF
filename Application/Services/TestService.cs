using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Services;

public sealed class TestService(
    ITestRepository repository,
    ITestAppointmentRepository appointmentRepository,
    ICurrentUserService currentUserService,
    ITestWorkflowService workflowService,
    IUnitOfWork unitOfWork,
    ILogger<TestService> logger) : ITestService
{
    private readonly ITestRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly ITestAppointmentRepository _appointmentRepository =
        appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));

    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    private readonly ITestWorkflowService _workflowService =
        workflowService ?? throw new ArgumentNullException(nameof(workflowService));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    private readonly ILogger<TestService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

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
        var validation =
            TestValidator.ValidateAppointmentId(appointmentId);

        if (validation.IsFailure)
            return Result<List<TestDto>>.FromValidationFailure(
                validation.Error);

        var tests =
            await _repository.GetByTestAppointmentIdAsync(appointmentId);

        return Result<List<TestDto>>.Success(
            tests.Select(TestMapper.ToDto).ToList());
    }

    public async Task<Result<List<TestDto>>> GetByUserIdAsync(int userId)
    {
        var validation = TestValidator.ValidateUserId(userId);

        if (validation.IsFailure)
            return Result<List<TestDto>>.FromValidationFailure(
                validation.Error);

        var tests = await _repository.GetByUserIdAsync(userId);

        return Result<List<TestDto>>.Success(
            tests.Select(TestMapper.ToDto).ToList());
    }

    public async Task<Result<int>> AddAsync(SaveTestResultDto dto)
    {
        var validation = TestValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            return Result<int>.FromForbidden(
                "You must be logged in first.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var appointment =
                await _appointmentRepository.GetForUpdateAsync(
                    dto.TestAppointmentID);

            if (appointment is null)
            {
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromNotFound(
                        "Test appointment not found."));
            }

            if (appointment.IsLocked)
            {
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromConflict(
                        "This appointment is already locked."));
            }

            if (await _repository.IsTestAlreadyTakenAsync(
                    dto.TestAppointmentID))
            {
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromConflict(
                        "A result already exists for this appointment."));
            }

            var workflow =
                await _workflowService.CanTakeTestAsync(
                    dto.TestAppointmentID);

            if (workflow.IsFailure)
            {
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromResult(workflow));
            }

            var entity =
                TestMapper.ToEntity(
                    dto,
                    _currentUserService.UserId);

            await _repository.AddAsync(entity);

            appointment.IsLocked = true;

            var saved =
                await _unitOfWork.SaveChangesAsync();

            if (saved <= 0 || entity.TestID <= 0)
            {
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromFailure(
                        "Failed to save test result."));
            }

            await transaction.CommitAsync();

            return Result<int>.Success(entity.TestID);
        }
        catch (Exception)
        {
            await RollbackSafelyAsync(
                transaction,
                dto.TestAppointmentID);

            throw;
        }
    }

    private static async Task<T> RollbackAsync<T>(
        IUnitOfWorkTransaction transaction,
        T result)
    {
        await transaction.RollbackAsync();
        return result;
    }

    private async Task RollbackSafelyAsync(
        IUnitOfWorkTransaction transaction,
        int appointmentId)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(
                rollbackException,
                "Failed to rollback test result transaction for appointment {AppointmentId}.",
                appointmentId);
        }
    }
}
