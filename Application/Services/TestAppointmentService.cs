using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class TestAppointmentService : ITestAppointmentService
    {
        private readonly TestAppointmentRepository _repository;
        private readonly TestTypeRepository _testTypeRepository;
        private readonly TestRepository _testRepository;
        private readonly IApplicationTypeService _applicationTypeService;

        public TestAppointmentService(
            TestAppointmentRepository repository,
            TestTypeRepository testTypeRepository,
            TestRepository testRepository,
            IApplicationTypeService applicationTypeService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _testTypeRepository = testTypeRepository ?? throw new ArgumentNullException(nameof(testTypeRepository));
            _testRepository = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
            _applicationTypeService = applicationTypeService;
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<TestAppointmentDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : MapToDto(entity);
        }

        public async Task<List<TestAppointmentDto>> GetAllAsync()
        {
            var data = await _repository.GetAllAsync();
            return data.Select(MapToDto).ToList();
        }

        public async Task<List<TestAppointmentDto>> GetByApplicationIdAsync(int applicationId)
        {
            var data = await _repository.GetByApplicationIdAsync(applicationId);
            return data.Select(MapToDto).ToList();
        }

        public async Task<List<TestAppointmentDto>> GetByTestTypeIdAsync(TestTypeEnum testType)
        {
            var data = await _repository.GetByTestTypeIdAsync(testType);
            return data.Select(MapToDto).ToList();
        }

        public async Task<List<TestAppointmentDto>> GetByCreatedUserIdAsync(int userId)
        {
            var data = await _repository.GetByCreatedUserIdAsync(userId);
            return data.Select(MapToDto).ToList();
        }

        public async Task<ScheduleTestDto?> GetScheduleInfoAsync(int testAppointmentId)
        {
            var data = await _repository.GetScheduleInfoAsync(testAppointmentId);

            if (data is null)
                return null;

            return new ScheduleTestDto
            {
                AppointmentID = data.TestAppointmentID,
                LocalDrivingLicenseApplicationID = data.LocalDrivingLicenseApplicationID,
                LicenseClassName = data.LocalDrivingLicenseApplication?.LicenseClass?.ClassName,
                FullName = data.LocalDrivingLicenseApplication?.Application?.Person?.FullName,
                Date = data.AppointmentDate,
                TestTypeID = data.TestTypeID,
                Fees = data.TestType?.TestTypeFees ?? data.PaidFees,
                RetakerFees = data.RetakeTestApplication != null
                    ? data.TestType?.TestTypeFees ?? 0
                    : 0
            };
        }

        // =========================
        // BUSINESS
        // =========================

        public Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime)
            => _repository.HasConflictAsync(testTypeId, dateTime);

        public Task<bool> HasUserConflictAsync(int userId, DateTime dateTime)
            => _repository.HasUserConflictAsync(userId, dateTime);

        public Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime)
            => _repository.HasApplicationConflictAsync(applicationId, dateTime);

        public Task<bool> IsAppointmentAlreadyScheduledAsync(int localAppId, int testTypeId)
            => _repository.IsAppointmentAlreadyScheduledAsync(localAppId, testTypeId);

        // =========================
        // COMMANDS
        // =========================

        public async Task<bool> AddAsync(TestAppointmentDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (await _repository.IsAppointmentAlreadyScheduledAsync(dto.LocalDrivingLicenseApplicationID, dto.TestTypeID))
                throw new InvalidOperationException("Appointment already exists or test already passed.");

            if (await HasConflictAsync(dto.TestTypeID, dto.AppointmentDate))
                throw new InvalidOperationException("Date is already reserved.");

            var entity = MapToEntity(dto);
            return await _repository.AddAsync(entity);
        }

        public async Task<bool> UpdateAsync(TestAppointmentDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            var entity = MapToEntity(dto);
            return await _repository.UpdateAsync(entity);
        }

        public Task DeleteAsync(int id)
            => _repository.DeleteAsync(id);

        // =========================
        // SAVE TEST RESULT
        // =========================

        public async Task<bool> SaveTestResultAsync(TestDto dto)
        {
            var testEntity = new Test
            {
                TestAppointmentID = dto.TestAppointmentID,
                TestResult = dto.TestResult,
                Notes = dto.Notes,
                CreatedByUserID = dto.CreatedByUserID
            };

            int newTestId = await _testRepository.AddAsync(testEntity);

            if (newTestId <= 0)
                return false;

            var appointment = await _repository.GetByIdAsync(dto.TestAppointmentID);

            if (appointment is null)
                return false;

            appointment.IsLocked = true;

            await _repository.UpdateAsync(appointment);

            return true;
        }

        // =========================
        // MAPPING
        // =========================

        private static TestAppointmentDto MapToDto(TestAppointment entity)
        {
            var result = TestResultType.Fail;

            if (entity.Test != null)
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

        private static TestAppointment MapToEntity(TestAppointmentDto dto)
        {
            return new TestAppointment
            {
                TestAppointmentID = dto.TestAppointmentID,
                TestTypeID = dto.TestTypeID,
                LocalDrivingLicenseApplicationID = dto.LocalDrivingLicenseApplicationID,
                AppointmentDate = dto.AppointmentDate,
                PaidFees = dto.PaidFees,
                CreatedByUserID = dto.CreatedByUserID,
                IsLocked = dto.IsLocked,
                RetakeTestApplicationID = dto.RetakeTestApplicationID
            };
        }

        public async Task<int> GetTrialCountAsync(int localAppId, int testTypeId)
        {
            var appointments = await GetByApplicationIdAsync(localAppId);

            return appointments.Count(x => x.TestTypeID == testTypeId);
        }

        public async Task<decimal> GetTestTypeFeesAsync(int testTypeId)
        {
            var type = await _testTypeRepository.GetTestTypeByIdAsync(testTypeId);
            return type?.TestTypeFees ?? 0;
        }
    }
}