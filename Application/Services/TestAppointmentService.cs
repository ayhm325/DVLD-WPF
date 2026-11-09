using Application.Common.Results;
using Application.DTOs;
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
    ILocalDrivingLicenseApplicationService localApplicationService,
    IApplicationTypeService applicationTypeService,
    ICurrentUserService currentUserService,
    ITestWorkflowService workflowService,
    IApplicationService applicationService,
    ILogger<TestAppointmentService> logger,
    IJordanClock clock) : ITestAppointmentService
{
    private const int RetakeApplicationTypeId = 7;

    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ITestAppointmentRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ITestTypeRepository _testTypeRepository = testTypeRepository ?? throw new ArgumentNullException(nameof(testTypeRepository));
    private readonly ILocalDrivingLicenseApplicationService _localApplicationService = localApplicationService ?? throw new ArgumentNullException(nameof(localApplicationService));
    private readonly IApplicationTypeService _applicationTypeService = applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));
    private readonly ICurrentUserService _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    private readonly ITestWorkflowService _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
    private readonly IApplicationService _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    private readonly ILogger<TestAppointmentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IJordanClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<TestAppointmentDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<TestAppointmentDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        return entity is null
            ? Result<TestAppointmentDto>.FromNotFound("Appointment not found.")
            : Result<TestAppointmentDto>.Success(TestAppointmentMapper.ToDto(entity));
    }

    public async Task<Result<List<TestAppointmentDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);
        return Result<List<TestAppointmentDto>>.Success(
            entities.Select(TestAppointmentMapper.ToDto).ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>> GetByLocalDrivingLicenseApplicationIdAsync(int localAppId, CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateApplicationId(localAppId);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByLocalDrivingLicenseApplicationIdAsync(localAppId, cancellationToken);

        return Result<List<TestAppointmentDto>>.Success(
            entities.Select(TestAppointmentMapper.ToDto).ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(int testType, CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateTestTypeId((int)testType);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByTestTypeIdAsync((TestTypeEnum)testType, cancellationToken);

        return Result<List<TestAppointmentDto>>.Success(
            entities.Select(TestAppointmentMapper.ToDto).ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateUserId(userId);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByCreatedUserIdAsync(userId, cancellationToken);

        return Result<List<TestAppointmentDto>>.Success(
            entities.Select(TestAppointmentMapper.ToDto).ToList());
    }

    public async Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateId(appointmentId);
        if (validation.IsFailure)
            return Result<ScheduleTestDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetScheduleInfoAsync(appointmentId, cancellationToken);
        if (entity is null)
            return Result<ScheduleTestDto>.FromNotFound("Appointment data not found.");

        var priorAttempts = await _repository.GetTrialCountAsync(
            entity.LocalDrivingLicenseApplicationID,
            entity.TestTypeID,
            cancellationToken);

        var isRecordedAttempt = entity.Status is AppointmentStatus.Completed or AppointmentStatus.NoShow || entity.Test is not null;
        var trial = isRecordedAttempt ? Math.Max(1, priorAttempts) : priorAttempts + 1;

        return Result<ScheduleTestDto>.Success(
            TestAppointmentMapper.ToScheduleDto(entity, trial));
    }

    public async Task<Result<ScheduleTestDto>> GetSchedulePreparationAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default)
    {
        var appValidation = TestAppointmentValidator.ValidateApplicationId(localAppId);
        if (appValidation.IsFailure)
            return Result<ScheduleTestDto>.FromValidationFailure(appValidation.Error);

        var typeValidation = TestAppointmentValidator.ValidateTestTypeId(testTypeId);
        if (typeValidation.IsFailure)
            return Result<ScheduleTestDto>.FromValidationFailure(typeValidation.Error);

        var localResult =
            await _localApplicationService
                .GetLocalDrivingLicenseApplicationByIdAsync(localAppId, cancellationToken);

        if (localResult.IsFailure)
            return Result<ScheduleTestDto>.FromResult(localResult);

        var localApplication = localResult.Value;

        if (localApplication is null)
            return Result<ScheduleTestDto>.FromNotFound(
                "Local driving license application was not found.");

        var testType = await _testTypeRepository.GetByIdAsync(testTypeId, cancellationToken);

        if (testType is null)
            return Result<ScheduleTestDto>.FromNotFound("Test type not found.");

        var trial = await _repository.GetTrialCountAsync(localAppId, testTypeId, cancellationToken) + 1;
        var retakerFees = 0m;

        if (trial > 1)
        {
            var retakeResult =
                await _applicationTypeService
                    .GetApplicationTypeByIdAsync(RetakeApplicationTypeId, cancellationToken);

            if (retakeResult.IsFailure)
                return Result<ScheduleTestDto>.FromResult(retakeResult);

            retakerFees = retakeResult.Value?.ApplicationTypeFees ?? 0;
        }

        return Result<ScheduleTestDto>.Success(new ScheduleTestDto
        {
            AppointmentID = 0,
            RetakeTestApplicationID = null,
            LocalDrivingLicenseApplicationID = localAppId,
            LicenseClassName = localApplication.LicenseClassName,
            FullName = localApplication.FullName,
            Trial = trial,
            Date = _clock.Today.AddDays(1).ToDateTime(TimeOnly.MinValue),
            Fees = testType.TestTypeFees,
            TestTypeID = testTypeId,
            RetakerFees = retakerFees,
            TestID = 0,
            Result = false,
            Notes = null
        });
    }

    public async Task<Result> AddAsync(CreateTestAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateCreate(dto, _clock.Now);
        if (validation.IsFailure)
            return validation;

        if (!IsAuthenticated())
            return Result.Forbidden("You must be logged in first.");

        var testType = await _testTypeRepository.GetByIdAsync(dto.TestTypeID, cancellationToken);
        if (testType is null)
            return Result.NotFound("Test type not found.");

        return await ExecuteTransactionAsync(async transaction =>
        {
            if (!await _repository.LockLocalApplicationForSchedulingAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    cancellationToken))
                return Result.NotFound(
                    "Local driving license application not found.");

            var workflow = await _workflowService.CanScheduleTestAsync(
                dto.LocalDrivingLicenseApplicationID,
                (TestTypeEnum)dto.TestTypeID,
                cancellationToken);

            if (workflow.IsFailure)
                return workflow;

            if (await _repository.IsAppointmentAlreadyScheduledAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    dto.TestTypeID,
                    cancellationToken))
                return Result.Conflict(
                    "An appointment already exists for this test.");

            var applicantPersonId = await _repository.GetApplicantPersonIdByLocalAppIdAsync(
                dto.LocalDrivingLicenseApplicationID,
                cancellationToken);

            if (!applicantPersonId.HasValue)
            {
                var localApp = await _localApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    cancellationToken);
                if (localApp.IsSuccess && localApp.Value is not null)
                {
                    applicantPersonId = localApp.Value.ApplicantPersonID;
                }
            }

            if (!applicantPersonId.HasValue)
                return Result.NotFound("Applicant person not found.");

            if (!await _repository.AcquireSchedulingLocksAsync(
                    applicantPersonId.Value,
                    dto.TestTypeID,
                    dto.AppointmentDate,
                    5000,
                    cancellationToken))
                return Result.Conflict("Unable to acquire scheduling lock for the selected slot. Please try again.");

            if (await _repository.IsSlotOccupiedAsync(
                    dto.TestTypeID,
                    dto.AppointmentDate,
                    null,
                    cancellationToken))
                return Result.Conflict(
                    "This slot is already booked for this test type.");

            if (await _repository.HasApplicantSameDayConflictAsync(
                    applicantPersonId.Value,
                    dto.AppointmentDate,
                    null,
                    cancellationToken))
                return Result.Conflict(
                    "The applicant already has an appointment or test scheduled on this calendar day.");

            var entity = TestAppointmentMapper.ToEntity(
                dto,
                testType.TestTypeFees,
                _currentUserService.UserId);

            await _repository.AddAsync(entity, cancellationToken);

            if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0 ||
                entity.TestAppointmentID <= 0)
                return Result.Failure("Failed to book appointment.");

            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }

    public async Task<Result<ScheduleTestDto>> ScheduleAsync(
        int localAppId,
        int testTypeId,
        DateTime appointmentDate,
        CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateSchedule(
            localAppId,
            testTypeId,
            appointmentDate,
            _clock.Now);

        if (validation.IsFailure)
            return Result<ScheduleTestDto>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<ScheduleTestDto>.FromForbidden(
                "You must be logged in first.");

        var testType = await _testTypeRepository.GetByIdAsync(testTypeId, cancellationToken);

        if (testType is null)
            return Result<ScheduleTestDto>.FromNotFound(
                "Test type not found.");

        var appointmentId = await ExecuteTransactionAsync(async transaction =>
        {
            if (!await _repository.LockLocalApplicationForSchedulingAsync(localAppId, cancellationToken))
                return Result<int>.FromNotFound(
                    "Local driving license application was not found.");

            var workflow = await _workflowService.CanScheduleTestAsync(
                localAppId,
                (TestTypeEnum)testTypeId,
                cancellationToken);

            if (workflow.IsFailure)
                return Result<int>.FromResult(workflow);

            if (await _repository.IsAppointmentAlreadyScheduledAsync(
                    localAppId,
                    testTypeId,
                    cancellationToken))
                return Result<int>.FromConflict(
                    "An appointment already exists for this test.");

            var applicantPersonId = await _repository.GetApplicantPersonIdByLocalAppIdAsync(localAppId, cancellationToken);
            if (!applicantPersonId.HasValue)
            {
                var localApp = await _localApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId, cancellationToken);
                if (localApp.IsSuccess && localApp.Value is not null)
                {
                    applicantPersonId = localApp.Value.ApplicantPersonID;
                }
            }

            if (!applicantPersonId.HasValue)
                return Result<int>.FromNotFound("Applicant person not found.");

            if (!await _repository.AcquireSchedulingLocksAsync(
                    applicantPersonId.Value,
                    testTypeId,
                    appointmentDate,
                    5000,
                    cancellationToken))
                return Result<int>.FromConflict("Unable to acquire scheduling lock for the selected slot. Please try again.");

            if (await _repository.IsSlotOccupiedAsync(
                    testTypeId,
                    appointmentDate,
                    null,
                    cancellationToken))
                return Result<int>.FromConflict(
                    "This slot is already booked for this test type.");

            if (await _repository.HasApplicantSameDayConflictAsync(
                    applicantPersonId.Value,
                    appointmentDate,
                    null,
                    cancellationToken))
                return Result<int>.FromConflict(
                    "The applicant already has an appointment or test scheduled on this calendar day.");

            var trial = await _repository.GetTrialCountAsync(
                localAppId,
                testTypeId,
                cancellationToken) + 1;

            int? retakeApplicationId = null;

            if (trial > 1)
            {
                var localResult =
                    await _localApplicationService
                        .GetLocalDrivingLicenseApplicationByIdAsync(localAppId, cancellationToken);

                if (localResult.IsFailure)
                    return Result<int>.FromResult(localResult);

                if (localResult.Value is null)
                    return Result<int>.FromNotFound(
                        "Local driving license application was not found.");

                var retakeResult =
                    await _applicationService.AddNewApplicationAsync(
                        new CreateApplicationDto
                        {
                            ApplicantPersonID = localResult.Value.ApplicantPersonID,
                            ApplicationTypeID = RetakeApplicationTypeId
                        },
                        cancellationToken);

                if (retakeResult.IsFailure)
                    return Result<int>.FromResult(retakeResult);

                retakeApplicationId = retakeResult.Value;
            }

            var appointment = TestAppointmentMapper.ToEntity(
                new CreateTestAppointmentDto
                {
                    TestTypeID = testTypeId,
                    LocalDrivingLicenseApplicationID = localAppId,
                    AppointmentDate = appointmentDate,
                    RetakeTestApplicationID = retakeApplicationId
                },
                testType.TestTypeFees,
                _currentUserService.UserId);

            await _repository.AddAsync(appointment, cancellationToken);

            if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0 ||
                appointment.TestAppointmentID <= 0)
                return Result<int>.FromFailure(
                    "Failed to book appointment.");

            await transaction.CommitAsync(cancellationToken);

            return Result<int>.Success(appointment.TestAppointmentID);
        }, cancellationToken);

        if (appointmentId.IsFailure)
            return Result<ScheduleTestDto>.FromResult(appointmentId);

        return await GetScheduleInfoAsync(appointmentId.Value, cancellationToken);
    }

    public async Task<Result> UpdateAsync(UpdateTestAppointmentDto dto, CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var validation = TestAppointmentValidator.ValidateUpdate(dto, now);
        if (validation.IsFailure)
            return validation;

        if (!IsAuthenticated())
            return Result.Forbidden("You must be logged in first.");

        return await ExecuteTransactionAsync(async transaction =>
        {
            var entity = await _repository.GetForUpdateAsync(
                dto.TestAppointmentID,
                cancellationToken);

            if (entity is null)
                return Result.NotFound("Appointment not found.");

            if (entity.Status == AppointmentStatus.Cancelled)
                return Result.Conflict("Cannot modify a cancelled appointment.");

            if (entity.Status == AppointmentStatus.Completed || entity.Test is not null)
                return Result.Conflict("Cannot modify a completed appointment.");

            if (entity.Status == AppointmentStatus.NoShow)
                return Result.Conflict("Cannot modify a no-show appointment.");

            if (entity.Status == null)
                return Result.Conflict("Appointment has an unresolved lifecycle status and cannot be modified.");

            if (entity.IsLocked)
                return Result.Conflict(
                    "Cannot modify a locked appointment.");

            var cutoff = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(
                entity.AppointmentDate,
                _clock.Now);
            if (cutoff.IsFailure)
                return cutoff;

            if (entity.AppointmentDate == dto.AppointmentDate)
            {
                await transaction.CommitAsync(cancellationToken);
                return Result.Success();
            }

            var workflow = await _workflowService.CanScheduleTestAsync(
                entity.LocalDrivingLicenseApplicationID,
                (TestTypeEnum)entity.TestTypeID,
                cancellationToken);

            if (workflow.IsFailure)
                return workflow;

            var applicantPersonId = await _repository.GetApplicantPersonIdByLocalAppIdAsync(
                entity.LocalDrivingLicenseApplicationID,
                cancellationToken);
            if (!applicantPersonId.HasValue)
            {
                var localApp = await _localApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(
                    entity.LocalDrivingLicenseApplicationID,
                    cancellationToken);
                if (localApp.IsSuccess && localApp.Value is not null)
                {
                    applicantPersonId = localApp.Value.ApplicantPersonID;
                }
            }

            if (!applicantPersonId.HasValue)
                return Result.NotFound("Applicant person not found.");

            if (!await _repository.AcquireSchedulingLocksAsync(
                    applicantPersonId.Value,
                    entity.TestTypeID,
                    dto.AppointmentDate,
                    5000,
                    cancellationToken))
                return Result.Conflict("Unable to acquire scheduling lock for the selected slot. Please try again.");

            if (await _repository.IsSlotOccupiedAsync(
                    entity.TestTypeID,
                    dto.AppointmentDate,
                    entity.TestAppointmentID,
                    cancellationToken))
                return Result.Conflict(
                    "This slot is already booked for this test type.");

            if (await _repository.HasApplicantSameDayConflictAsync(
                    applicantPersonId.Value,
                    dto.AppointmentDate,
                    entity.TestAppointmentID,
                    cancellationToken))
                return Result.Conflict(
                    "The applicant already has an appointment or test scheduled on this calendar day.");

            entity.AppointmentDate = dto.AppointmentDate;

            if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0)
                return Result.Failure("Failed to update appointment.");

            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }

    public async Task<Result> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateId(id);
        if (validation.IsFailure)
            return validation;

        if (!IsAuthenticated())
            return Result.Forbidden("You must be logged in first.");

        return await ExecuteTransactionAsync(async transaction =>
        {
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken);

            if (entity is null)
                return Result.NotFound("Appointment not found.");

            if (entity.Status == AppointmentStatus.Cancelled)
                return Result.Conflict("Appointment is already cancelled.");

            if (entity.Status == AppointmentStatus.Completed || entity.Test is not null)
                return Result.Conflict("Cannot cancel a completed appointment.");

            if (entity.Status == AppointmentStatus.NoShow)
                return Result.Conflict("Cannot cancel a no-show appointment.");

            if (entity.Status == null)
                return Result.Conflict("Appointment has an unresolved lifecycle status and cannot be cancelled.");

            if (entity.IsLocked)
                return Result.Conflict("Cannot cancel a locked appointment.");

            var nowJordan = _clock.Now;
            var cutoff = TestAppointmentValidator.ValidateModificationOrCancellationCutoff(
                entity.AppointmentDate,
                nowJordan);
            if (cutoff.IsFailure)
                return cutoff;

            entity.Status = AppointmentStatus.Cancelled;
            entity.CancelledAt = nowJordan;
            entity.CancelledByUserID = _currentUserService.UserId;
            entity.IsLocked = true;

            if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0)
                return Result.Failure("Failed to cancel appointment.");

            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }

    public Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default) => CancelAsync(id, cancellationToken);

    public async Task<int> GetTrialCountAsync(int localAppId, int testTypeId, CancellationToken cancellationToken = default) =>
        TestAppointmentValidator.ValidateApplicationId(localAppId).IsFailure ||
        TestAppointmentValidator.ValidateTestTypeId(testTypeId).IsFailure
            ? 0
            : await _repository.GetTrialCountAsync(localAppId, testTypeId, cancellationToken);

    public async Task<decimal> GetTestTypeFeesAsync(int testTypeId, CancellationToken cancellationToken = default)
    {
        if (TestAppointmentValidator.ValidateTestTypeId(testTypeId).IsFailure)
            return 0;

        var type = await _testTypeRepository.GetByIdAsync(testTypeId, cancellationToken);
        return type?.TestTypeFees ?? 0;
    }

    public Task<bool> IsAppointmentAlreadyScheduledAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default) =>
        _repository.IsAppointmentAlreadyScheduledAsync(
            localAppId,
            testTypeId,
            cancellationToken);

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn && _currentUserService.UserId > 0;

    private async Task<T> ExecuteTransactionAsync<T>(
        Func<IUnitOfWorkTransaction, Task<T>> operation,
        CancellationToken cancellationToken = default) =>
        await _unitOfWork.ExecuteInTransactionAsync(
            operation,
            IsolationLevel.ReadCommitted,
            cancellationToken);

    public async Task<Result<AppointmentAvailabilityDto>> GetAvailabilityAsync(
        int localAppId,
        int testTypeId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var validation = TestAppointmentValidator.ValidateAvailabilityRequest(
            localAppId,
            testTypeId,
            startDate,
            endDate);

        if (validation.IsFailure)
            return Result<AppointmentAvailabilityDto>.FromValidationFailure(validation.Error);

        var workflow = await _workflowService.CanScheduleTestAsync(
            localAppId,
            (TestTypeEnum)testTypeId,
            cancellationToken);

        if (workflow.IsFailure)
            return Result<AppointmentAvailabilityDto>.FromResult(workflow);

        if (await _repository.IsAppointmentAlreadyScheduledAsync(
                localAppId,
                testTypeId,
                cancellationToken))
            return Result<AppointmentAvailabilityDto>.FromConflict(
                "An appointment already exists for this test.");

        var applicantPersonId = await _repository.GetApplicantPersonIdByLocalAppIdAsync(
            localAppId,
            cancellationToken);

        if (!applicantPersonId.HasValue)
        {
            var localApp = await _localApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(
                localAppId,
                cancellationToken);
            if (localApp.IsSuccess && localApp.Value is not null)
            {
                applicantPersonId = localApp.Value.ApplicantPersonID;
            }
        }

        if (!applicantPersonId.HasValue)
            return Result<AppointmentAvailabilityDto>.FromNotFound("Applicant person not found.");

        var currentJordanTime = _clock.Now;
        var currentJordanDate = _clock.Today.ToDateTime(TimeOnly.MinValue);

        var startFacilityDate = TestAppointmentValidator.ToFacilityTime(startDate).Date;
        var endFacilityDate = TestAppointmentValidator.ToFacilityTime(endDate).Date;

        var minimumBookingDate = currentJordanDate.AddDays(2);
        if (endFacilityDate < minimumBookingDate)
        {
            return Result<AppointmentAvailabilityDto>.Success(new AppointmentAvailabilityDto
            {
                LocalAppId = localAppId,
                TestTypeId = testTypeId,
                AvailableDays = []
            });
        }

        var effectiveStartDate = startFacilityDate < minimumBookingDate ? minimumBookingDate : startFacilityDate;
        var rangeStart = effectiveStartDate;
        var rangeEnd = endFacilityDate.AddDays(1);

        var (occupiedSlots, applicantDates, lastTestDate) = await _repository.GetAvailabilityDataAsync(
            testTypeId,
            applicantPersonId.Value,
            localAppId,
            rangeStart,
            rangeEnd,
            cancellationToken);

        DateTime? earliestEligibleDate = null;
        if (lastTestDate.HasValue)
        {
            earliestEligibleDate = GetFirstWorkingDayAfter(lastTestDate.Value);
        }

        var availableDays = new List<AppointmentDayAvailabilityDto>();

        for (var date = effectiveStartDate; date <= endFacilityDate; date = date.AddDays(1))
        {
            if (date < minimumBookingDate)
                continue;

            if (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
                continue;

            if (earliestEligibleDate.HasValue && date < earliestEligibleDate.Value)
                continue;

            if (applicantDates.Contains(date))
                continue;

            var daySlots = new List<AppointmentSlotDto>();

            foreach (var slotTime in TestAppointmentValidator.ValidSlotTimes)
            {
                var slotDateTime = date.Add(slotTime);

                if (slotDateTime <= currentJordanTime)
                    continue;

                if (occupiedSlots.Contains(slotDateTime))
                    continue;

                daySlots.Add(new AppointmentSlotDto
                {
                    StartTime = slotTime.ToString(@"hh\:mm"),
                    EndTime = (slotTime + TimeSpan.FromMinutes(40)).ToString(@"hh\:mm")
                });
            }

            if (daySlots.Count > 0)
            {
                availableDays.Add(new AppointmentDayAvailabilityDto
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Slots = daySlots
                });
            }
        }

        return Result<AppointmentAvailabilityDto>.Success(new AppointmentAvailabilityDto
        {
            LocalAppId = localAppId,
            TestTypeId = testTypeId,
            AvailableDays = availableDays
        });
    }

    private static DateTime GetFirstWorkingDayAfter(DateTime date)
    {
        var next = date.Date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday)
        {
            next = next.AddDays(1);
        }
        return next;
    }

    public Task<ExaminationAttemptStats> GetAttemptStatsAsync(
        int localAppId,
        int testTypeId,
        CancellationToken cancellationToken = default) =>
        _repository.GetAttemptStatsAsync(localAppId, testTypeId, cancellationToken);
}
