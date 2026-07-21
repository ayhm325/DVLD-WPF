using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class TestService : ITestService
    {
        private readonly ITestRepository _repository;

        public TestService(ITestRepository repository)
        {
            _repository = repository;
        }

        // =========================
        // GET
        // =========================

        public async Task<Result<TestDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity is null)
                return Result<TestDto>.Fail("الاختبار غير موجود.");

            return Result<TestDto>.Success(MapToDto(entity));
        }

        public async Task<Result<List<TestDto>>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();

            return Result<List<TestDto>>.Success(
                list.Select(MapToDto).ToList());
        }

        public async Task<Result<List<TestDto>>> GetByTestAppointmentIdAsync(int appointmentId)
        {
            var list = await _repository.GetByTestAppointmentIdAsync(appointmentId);

            return Result<List<TestDto>>.Success(
                list.Select(MapToDto).ToList());
        }

        public async Task<Result<List<TestDto>>> GetByUserIdAsync(int userId)
        {
            var list = await _repository.GetByUserIdAsync(userId);

            return Result<List<TestDto>>.Success(
                list.Select(MapToDto).ToList());
        }

        // =========================
        // CHECKS
        // =========================

        public Task<bool> IsTestExistsAsync(int id)
            => _repository.IsTestExistsAsync(id);

        public Task<bool> IsTestAlreadyTakenAsync(int appointmentId)
            => _repository.IsTestAlreadyTakenAsync(appointmentId);

        // =========================
        // COMMANDS
        // =========================

        public async Task<Result<int>> AddAsync(TestDto dto)
        {
            if (dto == null)
                return Result<int>.Fail("بيانات الاختبار مطلوبة.");

            var entity = MapToEntity(dto);
            int id = await _repository.AddAsync(entity);

            if (id <= 0)
                return Result<int>.Fail("فشل في إضافة الاختبار.");

            return Result<int>.Success(id);
        }

        public async Task<Result> UpdateAsync(TestDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات الاختبار مطلوبة.");

            var entity = MapToEntity(dto);
            var isSuccess = await _repository.UpdateAsync(entity);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث الاختبار.");
        }

        public async Task<Result> DeleteAsync(int id)
        {
            if (!await _repository.IsTestExistsAsync(id))
                return Result.Failure("الاختبار غير موجود.");

            var isSuccess = await _repository.DeleteAsync(id);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في حذف الاختبار.");
        }

        // =========================
        // MAPPING
        // =========================

        private static TestDto MapToDto(Test t)
        {
            return new TestDto
            {
                TestID = t.TestID,
                TestAppointmentID = t.TestAppointmentID,
                TestResult = t.TestResult,
                Notes = t.Notes,
                CreatedByUserID = t.CreatedByUserID,
                CreatedByUserName = t.User?.UserName,

                TestTypeName = t.TestAppointment?.TestType?.ToString(),
                AppointmentDate = t.TestAppointment?.AppointmentDate
            };
        }

        private static Test MapToEntity(TestDto d)
        {
            return new Test
            {
                TestID = d.TestID,
                TestAppointmentID = d.TestAppointmentID,
                TestResult = d.TestResult,
                Notes = d.Notes,
                CreatedByUserID = d.CreatedByUserID
            };
        }
    }
}