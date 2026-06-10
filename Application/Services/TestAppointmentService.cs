using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class TestAppointmentService : ITestAppointmentService
    {
        private readonly TestAppointmentRepository _repository;

        public TestAppointmentService(TestAppointmentRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<TestAppointmentDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);

            return entity is null
                ? null
                : MapToDto(entity);
        }

        public async Task<List<TestAppointmentDto>> GetAllAsync()
        {
            var appointments = await _repository.GetAllAsync();

            return appointments
                .Select(MapToDto)
                .ToList();
        }

        public async Task<List<TestAppointmentDto>> GetByApplicationIdAsync(int applicationId)
        {
            var appointments = await _repository.GetByApplicationIdAsync(applicationId);

            return appointments
                .Select(MapToDto)
                .ToList();
        }

        public async Task<List<TestAppointmentDto>> GetByTestTypeIdAsync(int testTypeId)
        {
            var appointments = await _repository.GetByTestTypeIdAsync(testTypeId);

            return appointments
                .Select(MapToDto)
                .ToList();
        }

        public async Task<List<TestAppointmentDto>> GetByCreatedUserIdAsync(int userId)
        {
            var appointments = await _repository.GetByCreatedUserIdAsync(userId);

            return appointments
                .Select(MapToDto)
                .ToList();
        }

        // =========================
        // BUSINESS RULES
        // =========================

        public Task<bool> HasConflictAsync(int testTypeId, DateTime dateTime)
        {
            return _repository.HasConflictAsync(testTypeId, dateTime);
        }

        public Task<bool> HasUserConflictAsync(int userId, DateTime dateTime)
        {
            return _repository.HasUserConflictAsync(userId, dateTime);
        }

        public Task<bool> HasApplicationConflictAsync(int applicationId, DateTime dateTime)
        {
            return _repository.HasApplicationConflictAsync(applicationId, dateTime);
        }

        // =========================
        // COMMAND OPERATIONS
        // =========================

        public async Task AddAsync(TestAppointmentDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            if (await HasConflictAsync(dto.TestTypeID, dto.AppointmentDate))
                throw new InvalidOperationException(
                    "An appointment already exists for this test type at the selected time.");

            var entity = MapToEntity(dto);

            await _repository.AddAsync(entity);
        }

        public async Task UpdateAsync(TestAppointmentDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            var entity = MapToEntity(dto);

            await _repository.UpdateAsync(entity);
        }

        public Task DeleteAsync(int id)
        {
            return _repository.DeleteAsync(id);
        }

        // =========================
        // MAPPING
        // =========================

        private static TestAppointmentDto MapToDto(TestAppointment entity)
        {
            return new TestAppointmentDto
            {
                TestAppointmentID = entity.TestAppointmentID,

                TestTypeID = entity.TestTypeID,
                TestTypeName = entity.TestType?.TestTypeTitle ?? string.Empty,

                LocalDrivingLicenseApplicationID = entity.LocalDrivingLicenseApplicationID,

                AppointmentDate = entity.AppointmentDate,

                PaidFees = entity.PaidFees,

                CreatedByUserID = entity.CreatedByUserID,
                CreatedByUserName = entity.User?.UserName ?? string.Empty,

                IsLocked = entity.IsLocked,

                RetakeTestApplicationID = entity.RetakeTestApplicationID
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
    }
}