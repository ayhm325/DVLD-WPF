using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TestAppointmentService : ITestAppointmentService
    {
        private readonly ITestAppointmentRepository _repository;
        private readonly ITestTypeRepository _testTypeRepository;
        private readonly ITestRepository _testRepository;
        private readonly IApplicationTypeService _applicationTypeService;

        public TestAppointmentService(
            ITestAppointmentRepository repository,
            ITestTypeRepository testTypeRepository,
            ITestRepository testRepository,
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

        public async Task<Result<TestAppointmentDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null)
                return Result<TestAppointmentDto>.Fail("الموعد غير موجود.");

            return Result<TestAppointmentDto>.Success(MapToDto(entity));
        }

        public async Task<Result<List<TestAppointmentDto>>> GetAllAsync()
        {
            var list = (await _repository.GetAllAsync())
                .Select(MapToDto)
                .ToList();

            return Result<List<TestAppointmentDto>>.Success(list);
        }

        public async Task<Result<List<TestAppointmentDto>>> GetByApplicationIdAsync(int applicationId)
        {
            var list = (await _repository.GetByApplicationIdAsync(applicationId))
                .Select(MapToDto)
                .ToList();

            return Result<List<TestAppointmentDto>>.Success(list);
        }

        public async Task<Result<List<TestAppointmentDto>>> GetByTestTypeIdAsync(TestTypeEnum testType)
        {
            var list = (await _repository.GetByTestTypeIdAsync(testType))
                .Select(MapToDto)
                .ToList();

            return Result<List<TestAppointmentDto>>.Success(list);
        }

        public async Task<Result<List<TestAppointmentDto>>> GetByCreatedUserIdAsync(int userId)
        {
            var list = (await _repository.GetByCreatedUserIdAsync(userId))
                .Select(MapToDto)
                .ToList();

            return Result<List<TestAppointmentDto>>.Success(list);
        }

        public async Task<Result<ScheduleTestDto>> GetScheduleInfoAsync(int testAppointmentId)
        {
            var data = await _repository.GetScheduleInfoAsync(testAppointmentId);

            if (data is null)
                return Result<ScheduleTestDto>.Fail("بيانات الموعد غير موجودة.");

            return Result<ScheduleTestDto>.Success(new ScheduleTestDto
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
            });
        }

        // =========================
        // BUSINESS HELPERS
        // =========================

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

        // =========================
        // COMMANDS
        // =========================

        public async Task<Result> AddAsync(TestAppointmentDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات الموعد مطلوبة.");

            if (await _repository.IsAppointmentAlreadyScheduledAsync(
                    dto.LocalDrivingLicenseApplicationID,
                    dto.TestTypeID))
            {
                return Result.Failure("يوجد موعد محجوز بالفعل لهذا الاختبار أو تم اجتياز الاختبار مسبقاً.");
            }

            if (await HasConflictAsync(dto.TestTypeID, dto.AppointmentDate))
            {
                return Result.Failure("التاريخ محجوز مسبقاً لاختبار آخر.");
            }

            var entity = MapToEntity(dto);
            var isSuccess = await _repository.AddAsync(entity);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في حجز الموعد.");
        }

        public async Task<Result> UpdateAsync(TestAppointmentDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات الموعد مطلوبة.");

            var entity = MapToEntity(dto);
            var isSuccess = await _repository.UpdateAsync(entity);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث الموعد.");
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var exists = await _repository.GetByIdAsync(id);

            if (exists is null)
                return Result.Failure("الموعد غير موجود.");

            await _repository.DeleteAsync(id);

            return Result.Success();
        }

        // =========================
        // SAVE TEST RESULT
        // =========================

        public async Task<Result> SaveTestResultAsync(TestDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات نتيجة الاختبار مطلوبة.");

            var testEntity = new Test
            {
                TestAppointmentID = dto.TestAppointmentID,
                TestResult = dto.TestResult,
                Notes = dto.Notes,
                CreatedByUserID = dto.CreatedByUserID
            };

            int newTestId = await _testRepository.AddAsync(testEntity);

            if (newTestId <= 0)
                return Result.Failure("فشل في حفظ نتيجة الاختبار.");

            var appointment = await _repository.GetByIdAsync(dto.TestAppointmentID);

            if (appointment is null)
                return Result.Failure("الموعد المرتبط بالاختبار غير موجود.");

            appointment.IsLocked = true;

            var isSuccess = await _repository.UpdateAsync(appointment);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في قفل الموعد بعد حفظ النتيجة.");
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

        // =========================
        // HELPERS
        // =========================

        public async Task<int> GetTrialCountAsync(int localAppId, int testTypeId)
        {
            var appointmentsResult = await GetByApplicationIdAsync(localAppId);

            if (appointmentsResult.IsFailure)
                return 0;

            return appointmentsResult.Value.Count(x => x.TestTypeID == testTypeId);
        }

        public async Task<decimal> GetTestTypeFeesAsync(int testTypeId)
        {
            var type = await _testTypeRepository.GetTestTypeByIdAsync(testTypeId);
            return type?.TestTypeFees ?? 0;
        }
    }
}