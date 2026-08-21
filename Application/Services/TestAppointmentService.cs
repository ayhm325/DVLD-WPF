using Application.Common.Results;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class TestAppointmentService : ITestAppointmentService
{
    private readonly ITestAppointmentRepository _repository;
    private readonly ITestTypeRepository _testTypeRepository;
    private readonly ITestRepository _testRepository;
    private readonly ICurrentUserService _currentUserService;

    public TestAppointmentService(
        ITestAppointmentRepository repository,
        ITestTypeRepository testTypeRepository,
        ITestRepository testRepository,
        ICurrentUserService currentUserService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _testTypeRepository = testTypeRepository ?? throw new ArgumentNullException(nameof(testTypeRepository));
        _testRepository = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    // GET
    public async Task<Result<TestAppointmentDto>> GetByIdAsync(int id)
    {
        var validation = TestAppointmentValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<TestAppointmentDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result<TestAppointmentDto>.FromFailure("Appointment not found.");

        return Result<TestAppointmentDto>.Success(MapToDto(entity));
    }

    public async Task<Result<List<TestAppointmentDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return Result<List<TestAppointmentDto>>.Success(entities.Select(MapToDto).ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = TestAppointmentValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromFailure(validation.Error);

        var entities = await _repository.GetByApplicationIdAsync(applicationId);
        return Result<List<TestAppointmentDto>>.Success(entities.Select(MapToDto).ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(TestTypeEnum testType)
    {
        var validation = TestAppointmentValidator.ValidateTestTypeId((int)testType);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromFailure(validation.Error);

        var entities = await _repository.GetByTestTypeIdAsync(testType);
        return Result<List<TestAppointmentDto>>.Success(entities.Select(MapToDto).ToList());
    }

    public async Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(int userId)
    {
        var validation = TestAppointmentValidator.ValidateUserId(userId);
        if (validation.IsFailure)
            return Result<List<TestAppointmentDto>>.FromFailure(validation.Error);

        var entities = await _repository.GetByCreatedUserIdAsync(userId);
        return Result<List<TestAppointmentDto>>.Success(entities.Select(MapToDto).ToList());
    }

    // SCHEDULE INFO
    public async Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(int testAppointmentId)
    {
        var validation = TestAppointmentValidator.ValidateId(testAppointmentId);
        if (validation.IsFailure)
            return Result<ScheduleTestDto>.FromFailure(validation.Error);

        var data = await _repository.GetScheduleInfoAsync(testAppointmentId);
        if (data is null)
            return Result<ScheduleTestDto>.FromFailure("Appointment data not found.");

        var dto = new ScheduleTestDto
        {
            AppointmentID = data.TestAppointmentID,
            RetakeTestApplicationID = data.RetakeTestApplicationID,
            LocalDrivingLicenseApplicationID = data.LocalDrivingLicenseApplicationID,
            LicenseClassName = data.LocalDrivingLicenseApplication?.LicenseClass?.ClassName,
            FullName = data.LocalDrivingLicenseApplication?.Application?.Person?.FullName,
            Trial = 0,
            Date = data.AppointmentDate,
            Fees = data.TestType?.TestTypeFees ?? data.PaidFees,
            TestTypeID = data.TestTypeID,
            RetakerFees = data.RetakeTestApplication != null ? data.TestType?.TestTypeFees ?? 0 : 0,
            TestID = data.Test?.TestID ?? 0,
            Result = data.Test?.TestResult ?? false,
            Notes = data.Test?.Notes
        };

        dto.Trial = await GetTrialCountAsync(data.LocalDrivingLicenseApplicationID, data.TestTypeID);
        return Result<ScheduleTestDto>.Success(dto);
    }

    // BUSINESS HELPERS
    public Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime)
        => _repository.HasConflictAsync(testTypeId, dateTime);

    public Task<bool> HasUserConflictAsync(int userId, DateTime dateTime)
        => _repository.HasUserConflictAsync(userId, dateTime);

    public Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime)
        => _repository.HasApplicationConflictAsync(applicationId, dateTime);

    public Task<bool> HasPassedAllTestsAsync(int appId)
        => _repository.HasPassedAllTestsAsync(appId);

    public Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId)
        => _repository.IsAppointmentAlreadyScheduledAsync(localAppId, testTypeId);

    // CREATE
    public async Task<Result> AddAsync(CreateTestAppointmentDto dto)
    {
        var validation = TestAppointmentValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return validation;

        var alreadyScheduled = await _repository.IsAppointmentAlreadyScheduledAsync(
            dto.LocalDrivingLicenseApplicationID, dto.TestTypeID);
        if (alreadyScheduled)
            return Result.Failure("An appointment already exists for this test or the test has been passed.");

        var hasConflict = await HasConflictAsync(dto.TestTypeID, dto.AppointmentDate);
        if (hasConflict)
            return Result.Failure("The selected date is already booked for another test.");

        var testType = await _testTypeRepository.GetTestTypeByIdAsync(dto.TestTypeID);
        if (testType is null)
            return Result.Failure("Test type not found.");

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.Failure("You must be logged in first.");

        var entity = new TestAppointment
        {
            TestTypeID = dto.TestTypeID,
            LocalDrivingLicenseApplicationID = dto.LocalDrivingLicenseApplicationID,
            AppointmentDate = dto.AppointmentDate,
            PaidFees = testType.TestTypeFees,
            CreatedByUserID = _currentUserService.UserId,
            IsLocked = false,
            RetakeTestApplicationID = dto.RetakeTestApplicationID
        };

        var isSuccess = await _repository.AddAsync(entity);
        return isSuccess ? Result.Success() : Result.Failure("Failed to book appointment.");
    }

    // UPDATE
    public async Task<Result> UpdateAsync(UpdateTestAppointmentDto dto)
    {
        var validation = TestAppointmentValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return validation;

        var entity = await _repository.GetByIdAsync(dto.TestAppointmentID);
        if (entity is null)
            return Result.Failure("Appointment not found.");

        if (entity.IsLocked)
            return Result.Failure("Cannot modify a locked appointment.");

        if (entity.AppointmentDate == dto.AppointmentDate)
            return Result.Success();

        var hasConflict = await HasConflictAsync(entity.TestTypeID, dto.AppointmentDate);
        if (hasConflict)
            return Result.Failure("The new date is already booked for another test.");

        entity.AppointmentDate = dto.AppointmentDate;
        var isSuccess = await _repository.UpdateAsync(entity);
        return isSuccess ? Result.Success() : Result.Failure("Failed to update appointment.");
    }

    // DELETE
    public async Task<Result> DeleteAsync(int id)
    {
        var validation = TestAppointmentValidator.ValidateId(id);
        if (validation.IsFailure)
            return validation;

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result.Failure("Appointment not found.");

        if (entity.IsLocked)
            return Result.Failure("Cannot delete a locked appointment.");

        await _repository.DeleteAsync(id);
        return Result.Success();
    }

    // SAVE TEST RESULT
    public async Task<Result> SaveTestResultAsync(SaveTestResultDto dto)
    {
        var validation = TestAppointmentValidator.ValidateSaveTestResult(dto);
        if (validation.IsFailure)
            return validation;

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.Failure("You must be logged in first.");

        var appointment = await _repository.GetByIdAsync(dto.TestAppointmentID);
        if (appointment is null)
            return Result.Failure("Test appointment not found.");

        if (appointment.IsLocked)
            return Result.Failure("A result has already been saved for this test.");

        var testEntity = new Test
        {
            TestAppointmentID = dto.TestAppointmentID,
            TestResult = dto.TestResult,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            CreatedByUserID = _currentUserService.UserId
        };

        var newTestId = await _testRepository.AddAsync(testEntity);
        if (newTestId <= 0)
            return Result.Failure("Failed to save test result.");

        appointment.IsLocked = true;
        var isSuccess = await _repository.UpdateAsync(appointment);
        if (!isSuccess)
            return Result.Failure("Failed to lock appointment after saving result.");

        return Result.Success();
    }

    // MAPPING
    private static TestAppointmentDto MapToDto(TestAppointment entity)
    {
        var result = TestResultType.NotTaken;
        if (entity.Test is not null)
            result = entity.Test.TestResult ? TestResultType.Pass : TestResultType.Fail;

        return new TestAppointmentDto
        {
            TestAppointmentID = entity.TestAppointmentID,
            TestTypeID = entity.TestTypeID,
            TestTypeName = entity.TestType?.TestTypeTitle ?? string.Empty,
            LocalDrivingLicenseApplicationID = entity.LocalDrivingLicenseApplicationID,
            AppointmentDate = entity.AppointmentDate,
            PaidFees = entity.PaidFees,
            CreatedByUserID = entity.CreatedByUserID,
            CreatedByUserName = entity.User?.UserName ?? "N/A",
            IsLocked = entity.IsLocked,
            RetakeTestApplicationID = entity.RetakeTestApplicationID,
            TestResult = result
        };
    }

    // HELPERS
    public async Task<int> GetTrialCountAsync(int localAppId, int testTypeId)
    {
        var appValidation = TestAppointmentValidator.ValidateApplicationId(localAppId);
        if (appValidation.IsFailure) return 0;

        var typeValidation = TestAppointmentValidator.ValidateTestTypeId(testTypeId);
        if (typeValidation.IsFailure) return 0;

        var appointmentsResult = await GetByApplicationIdAsync(localAppId);
        if (appointmentsResult.IsFailure || appointmentsResult.Value is null)
            return 0;

        return appointmentsResult.Value.Count(x => x.TestTypeID == testTypeId);
    }

    public async Task<decimal> GetTestTypeFeesAsync(int testTypeId)
    {
        var validation = TestAppointmentValidator.ValidateTestTypeId(testTypeId);
        if (validation.IsFailure) return 0;

        var type = await _testTypeRepository.GetTestTypeByIdAsync(testTypeId);
        return type?.TestTypeFees ?? 0;
    }
}