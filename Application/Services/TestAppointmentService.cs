using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Services;

public sealed class TestAppointmentService(
    IUnitOfWork unitOfWork,
    ITestAppointmentRepository repository,
    ITestTypeRepository testTypeRepository,
    ICurrentUserService currentUserService,
    ITestWorkflowService workflowService,
    IApplicationService applicationService,
    ILogger<TestAppointmentService> logger)
    : ITestAppointmentService
{
    private const int RetakeApplicationTypeId = 7;

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork
        ?? throw new ArgumentNullException(nameof(unitOfWork));

    private readonly ITestAppointmentRepository _repository =
        repository
        ?? throw new ArgumentNullException(nameof(repository));

    private readonly ITestTypeRepository _testTypeRepository =
        testTypeRepository
        ?? throw new ArgumentNullException(nameof(testTypeRepository));

    private readonly ICurrentUserService _currentUserService =
        currentUserService
        ?? throw new ArgumentNullException(nameof(currentUserService));

    private readonly ITestWorkflowService _workflowService =
        workflowService
        ?? throw new ArgumentNullException(nameof(workflowService));

    private readonly IApplicationService _applicationService =
        applicationService
        ?? throw new ArgumentNullException(nameof(applicationService));

    private readonly ILogger<TestAppointmentService> _logger =
        logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<TestAppointmentDto>> GetByIdAsync(
        int id)
    {
        var validation =
            TestAppointmentValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<TestAppointmentDto>
                .FromValidationFailure(validation.Error);
        }

        var entity =
            await _repository.GetByIdAsync(id);

        return entity is null
            ? Result<TestAppointmentDto>.FromNotFound(
                "Appointment not found.")
            : Result<TestAppointmentDto>.Success(
                TestAppointmentMapper.ToDto(entity));
    }

    public async Task<Result<List<TestAppointmentDto>>> GetAllAsync()
    {
        var entities =
            await _repository.GetAllAsync();

        return Result<List<TestAppointmentDto>>.Success(
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>>
        GetByLocalDrivingLicenseApplicationIdAsync(
            int localAppId)
    {
        var validation =
            TestAppointmentValidator
                .ValidateApplicationId(localAppId);

        if (validation.IsFailure)
        {
            return Result<List<TestAppointmentDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository
                .GetByLocalDrivingLicenseApplicationIdAsync(
                    localAppId);

        return Result<List<TestAppointmentDto>>.Success(
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>>
        GetByTestTypeIdAsync(
            TestTypeEnum testType)
    {
        var validation =
            TestAppointmentValidator
                .ValidateTestTypeId((int)testType);

        if (validation.IsFailure)
        {
            return Result<List<TestAppointmentDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByTestTypeIdAsync(testType);

        return Result<List<TestAppointmentDto>>.Success(
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>>
        GetByCreatedUserIdAsync(
            int userId)
    {
        var validation =
            TestAppointmentValidator.ValidateUserId(userId);

        if (validation.IsFailure)
        {
            return Result<List<TestAppointmentDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByCreatedUserIdAsync(userId);

        return Result<List<TestAppointmentDto>>.Success(
            entities
                .Select(TestAppointmentMapper.ToDto)
                .ToList());
    }

    public async Task<Result<ScheduleTestDto>>
        GetScheduleInfoAsync(
            int appointmentId)
    {
        var validation =
            TestAppointmentValidator.ValidateId(
                appointmentId);

        if (validation.IsFailure)
        {
            return Result<ScheduleTestDto>
                .FromValidationFailure(validation.Error);
        }

        var entity =
            await _repository.GetScheduleInfoAsync(
                appointmentId);

        if (entity is null)
        {
            return Result<ScheduleTestDto>.FromNotFound(
                "Appointment data not found.");
        }

        var trial =
            await GetTrialCountAsync(
                entity.LocalDrivingLicenseApplicationID,
                entity.TestTypeID);

        return Result<ScheduleTestDto>.Success(
            TestAppointmentMapper.ToScheduleDto(
                entity,
                trial));
    }

    public async Task<Result> AddAsync(
        CreateTestAppointmentDto dto)
    {
        var validation =
            TestAppointmentValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return validation;

        if (!IsAuthenticated())
        {
            return Result.Forbidden(
                "You must be logged in first.");
        }

        var testType =
            await _testTypeRepository.GetByIdAsync(
                dto.TestTypeID);

        if (testType is null)
        {
            return Result.NotFound(
                "Test type not found.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var workflow =
                await _workflowService.CanScheduleTestAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    (TestTypeEnum)dto.TestTypeID);

            if (workflow.IsFailure)
                return workflow;

            if (await _repository
                .IsAppointmentAlreadyScheduledAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    dto.TestTypeID))
            {
                return Result.Conflict(
                    "An appointment already exists for this test.");
            }

            if (await _repository
                .HasLocalApplicationConflictAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    dto.AppointmentDate))
            {
                return Result.Conflict(
                    "This application already has an appointment at this date and time.");
            }

            if (await _repository
                .HasUserConflictAsync(
                    _currentUserService.UserId,
                    dto.AppointmentDate))
            {
                return Result.Conflict(
                    "The current user already has an appointment at this date and time.");
            }

            var entity =
                TestAppointmentMapper.ToEntity(
                    dto,
                    testType.TestTypeFees,
                    _currentUserService.UserId);

            await _repository.AddAsync(entity);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                entity.TestAppointmentID <= 0)
            {
                return Result.Failure(
                    "Failed to book appointment.");
            }

            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await RollbackSafelyAsync(
                transaction,
                dto.LocalDrivingLicenseApplicationID);

            _logger.LogError(
                ex,
                "Error booking test appointment for local application {LocalApplicationId}.",
                dto.LocalDrivingLicenseApplicationID);

            return Result.Failure(
                "An unexpected error occurred while booking the appointment.");
        }
    }

    public async Task<Result<ScheduleTestDto>>
        ScheduleAsync(
            int localAppId,
            int testTypeId,
            DateTime appointmentDate)
    {
        var validation =
            TestAppointmentValidator.ValidateSchedule(
                localAppId,
                testTypeId,
                appointmentDate);

        if (validation.IsFailure)
        {
            return Result<ScheduleTestDto>
                .FromValidationFailure(
                    validation.Error);
        }

        if (!IsAuthenticated())
        {
            return Result<ScheduleTestDto>.FromForbidden(
                "You must be logged in first.");
        }

        var testType =
            await _testTypeRepository.GetByIdAsync(
                testTypeId);

        if (testType is null)
        {
            return Result<ScheduleTestDto>.FromNotFound(
                "Test type not found.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var workflow =
                await _workflowService.CanScheduleTestAsync(
                    localAppId,
                    (TestTypeEnum)testTypeId);

            if (workflow.IsFailure)
            {
                await transaction.RollbackAsync();

                return Result<ScheduleTestDto>.FromResult(
                    workflow);
            }

            if (await _repository
                .IsAppointmentAlreadyScheduledAsync(
                    localAppId,
                    testTypeId))
            {
                await transaction.RollbackAsync();

                return Result<ScheduleTestDto>.FromConflict(
                    "An appointment already exists for this test.");
            }

            if (await _repository
                .HasLocalApplicationConflictAsync(
                    localAppId,
                    appointmentDate))
            {
                await transaction.RollbackAsync();

                return Result<ScheduleTestDto>.FromConflict(
                    "This application already has an appointment at this date and time.");
            }

            if (await _repository
                .HasUserConflictAsync(
                    _currentUserService.UserId,
                    appointmentDate))
            {
                await transaction.RollbackAsync();

                return Result<ScheduleTestDto>.FromConflict(
                    "The current user already has an appointment at this date and time.");
            }

            var trial =
                await _repository.GetTrialCountAsync(
                    localAppId,
                    testTypeId) + 1;

            var isRetake =
                trial > 1;

            int? retakeApplicationId = null;

            if (isRetake)
            {
                var localApplicationResult =
                    await _applicationService
                        .GetBasicInfoAsync(localAppId);

                if (localApplicationResult.IsFailure)
                {
                    await transaction.RollbackAsync();

                    return Result<ScheduleTestDto>.FromResult(
                        localApplicationResult);
                }

                var localApplication =
                    localApplicationResult.Value;

                if (localApplication is null)
                {
                    await transaction.RollbackAsync();

                    return Result<ScheduleTestDto>.FromNotFound(
                        "Local driving license application was not found.");
                }

                var createApplicationDto =
                    new CreateApplicationDto
                    {
                        ApplicantPersonID =
                            localApplication.ApplicantPersonID,

                        ApplicationTypeID =
                            RetakeApplicationTypeId
                    };

                var retakeResult =
                    await _applicationService
                        .AddNewApplicationAsync(
                            createApplicationDto);

                if (retakeResult.IsFailure)
                {
                    await transaction.RollbackAsync();

                    return Result<ScheduleTestDto>.FromResult(
                        retakeResult);
                }

                retakeApplicationId =
                    retakeResult.Value;
            }

            var appointment =
                TestAppointmentMapper.ToEntity(
                    new CreateTestAppointmentDto
                    {
                        TestTypeID =
                            testTypeId,

                        LocalDrivingLicenseApplicationID =
                            localAppId,

                        AppointmentDate =
                            appointmentDate,

                        RetakeTestApplicationID =
                            retakeApplicationId
                    },
                    testType.TestTypeFees,
                    _currentUserService.UserId);

            await _repository.AddAsync(
                appointment);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                appointment.TestAppointmentID <= 0)
            {
                await transaction.RollbackAsync();

                return Result<ScheduleTestDto>.FromFailure(
                    "Failed to book appointment.");
            }

            await transaction.CommitAsync();

            return await GetScheduleInfoAsync(
                appointment.TestAppointmentID);
        }
        catch (Exception ex)
        {
            await RollbackSafelyAsync(
                transaction,
                localAppId);

            _logger.LogError(
                ex,
                "Error scheduling test for local application {LocalApplicationId}.",
                localAppId);

            return Result<ScheduleTestDto>.FromFailure(
                "An unexpected error occurred while scheduling the test.");
        }
    }

    public async Task<Result> UpdateAsync(
        UpdateTestAppointmentDto dto)
    {
        var validation =
            TestAppointmentValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
            return validation;

        if (!IsAuthenticated())
        {
            return Result.Forbidden(
                "You must be logged in first.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var entity =
                await _repository.GetForUpdateAsync(
                    dto.TestAppointmentID);

            if (entity is null)
                return Result.NotFound(
                    "Appointment not found.");

            if (entity.CreatedByUserID !=
                _currentUserService.UserId)
            {
                return Result.Forbidden(
                    "You are not allowed to modify this appointment.");
            }

            if (entity.IsLocked)
                return Result.Conflict(
                    "Cannot modify a locked appointment.");

            if (entity.AppointmentDate ==
                dto.AppointmentDate)
            {
                await transaction.CommitAsync();

                return Result.Success();
            }

            var workflow =
                await _workflowService.CanScheduleTestAsync(
                    entity.LocalDrivingLicenseApplicationID,
                    (TestTypeEnum)entity.TestTypeID);

            if (workflow.IsFailure)
                return workflow;

            if (await _repository
                .HasLocalApplicationConflictAsync(
                    entity.LocalDrivingLicenseApplicationID,
                    dto.AppointmentDate,
                    entity.TestAppointmentID))
            {
                return Result.Conflict(
                    "This application already has another appointment at this date and time.");
            }

            if (await _repository
                .HasUserConflictAsync(
                    entity.CreatedByUserID,
                    dto.AppointmentDate,
                    entity.TestAppointmentID))
            {
                return Result.Conflict(
                    "The current user already has another appointment at this date and time.");
            }

            entity.AppointmentDate =
                dto.AppointmentDate;

            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                return Result.Failure(
                    "Failed to update appointment.");
            }

            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await RollbackSafelyAsync(
                transaction,
                dto.TestAppointmentID);

            _logger.LogError(
                ex,
                "Error updating test appointment {AppointmentId}.",
                dto.TestAppointmentID);

            return Result.Failure(
                "An unexpected error occurred while updating the appointment.");
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var validation =
            TestAppointmentValidator.ValidateId(id);

        if (validation.IsFailure)
            return validation;

        if (!IsAuthenticated())
        {
            return Result.Forbidden(
                "You must be logged in first.");
        }

        var entity =
            await _repository.GetForUpdateAsync(id);

        if (entity is null)
            return Result.NotFound(
                "Appointment not found.");

        if (entity.CreatedByUserID !=
            _currentUserService.UserId)
        {
            return Result.Forbidden(
                "You are not allowed to delete this appointment.");
        }

        if (entity.IsLocked)
            return Result.Conflict(
                "Cannot delete a locked appointment.");

        _repository.Delete(entity);

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to delete appointment.");
    }

    public async Task<int> GetTrialCountAsync(
        int localAppId,
        int testTypeId)
    {
        if (TestAppointmentValidator
                .ValidateApplicationId(localAppId)
                .IsFailure ||
            TestAppointmentValidator
                .ValidateTestTypeId(testTypeId)
                .IsFailure)
        {
            return 0;
        }

        return await _repository.GetTrialCountAsync(
            localAppId,
            testTypeId);
    }

    public async Task<decimal> GetTestTypeFeesAsync(
        int testTypeId)
    {
        if (TestAppointmentValidator
                .ValidateTestTypeId(testTypeId)
                .IsFailure)
        {
            return 0;
        }

        var type =
            await _testTypeRepository
                .GetByIdAsync(testTypeId);

        return type?.TestTypeFees ?? 0;
    }

    public Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId) =>
        _repository.IsAppointmentAlreadyScheduledAsync(
            localAppId,
            testTypeId);

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn &&
        _currentUserService.UserId > 0;

    private async Task RollbackSafelyAsync(
        IUnitOfWorkTransaction transaction,
        int id)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(
                rollbackException,
                "Rollback failed for test appointment operation {Id}.",
                id);
        }
    }
}