using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Application.Services
{
    public class TestService : ITestService
    {
        private readonly TestRepository _repository;

        public TestService(TestRepository repository)
        {
            _repository = repository;
        }

        // =========================
        // GET
        // =========================

        public async Task<TestDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : MapToDto(entity);
        }

        public async Task<List<TestDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<TestDto>> GetByTestAppointmentIdAsync(int appointmentId)
        {
            var list = await _repository.GetByTestAppointmentIdAsync(appointmentId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<TestDto>> GetByUserIdAsync(int userId)
        {
            var list = await _repository.GetByUserIdAsync(userId);
            return list.Select(MapToDto).ToList();
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

        public async Task<int> AddAsync(TestDto dto)
        {
            var entity = MapToEntity(dto);
            return await _repository.AddAsync(entity);
        }

        public async Task<bool> UpdateAsync(TestDto dto)
        {
            var entity = MapToEntity(dto);
            return await _repository.UpdateAsync(entity);
        }

        public Task<bool> DeleteAsync(int id)
            => _repository.DeleteAsync(id);

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