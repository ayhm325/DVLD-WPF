using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly LicenseRepository _repository;

        public LicenseService(LicenseRepository repository)
        {
            _repository = repository;
        }

        // =========================
        // GET
        // =========================

        public async Task<LicenseDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetLicenseByIdAsync(id);
            return entity is null ? null : MapToDto(entity);
        }

        public async Task<List<LicenseDto>> GetAllAsync()
        {
            var list = await _repository.GetAllLicensesAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<LicenseDto>> GetByDriverIdAsync(int driverId)
        {
            var list = await _repository.GetLicensesByDriverIdAsync(driverId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<LicenseDto>> GetByApplicationIdAsync(int applicationId)
        {
            var list = await _repository.GetLicensesByApplicationIdAsync(applicationId);
            return list.Select(MapToDto).ToList();
        }

        public async Task<List<LicenseDto>> GetByLicenseClassIdAsync(int licenseClassId)
        {
            var list = await _repository.GetLicensesByLicenseClassIdAsync(licenseClassId);
            return list.Select(MapToDto).ToList();
        }

        // =========================
        // CHECKS
        // =========================

        public Task<bool> IsLicenseExistsAsync(int id)
            => _repository.IsLicenseExistsAsync(id);

        public Task<bool> IsDriverHasLicenseAsync(int driverId)
            => _repository.IsDriverHasLicenseAsync(driverId);

        public Task<bool> IsApplicationHasLicenseAsync(int applicationId)
            => _repository.IsApplicationHasLicenseAsync(applicationId);

        // =========================
        // COMMANDS
        // =========================

        public async Task<int> AddAsync(LicenseDto dto)
        {
            var entity = MapToEntity(dto);
            return await _repository.AddLicenseAsync(entity);
        }

        public async Task<bool> UpdateAsync(LicenseDto dto)
        {
            var entity = MapToEntity(dto);
            return await _repository.UpdateLicenseAsync(entity);
        }

        public Task<bool> DeleteAsync(int id)
            => _repository.DeleteLicenseAsync(id);

        // =========================
        // MAPPING
        // =========================

        private static LicenseDto MapToDto(License l)
        {
            return new LicenseDto
            {
                LicenseID = l.LicenseID,
                ApplicationID = l.ApplicationID,
                ApplicationInfo = l.Application != null
                    ? $"App #{l.ApplicationID}"
                    : null,

                DriverID = l.DriverID,
                DriverName = l.Driver != null
                    ? $"{l.Driver.FirstName ?? ""}"
                    : null,

                LicenseClassId = l.LicenseClassId,
                LicenseClassName = l.LicenseClass != null
                    ? l.LicenseClass.ToString()
                    : null,

                IssueDate = l.IssueDate,
                ExpirationDate = l.ExpirationDate,

                Notes = l.Notes,
                PaidFees = l.PaidFees,

                IsActive = l.IsActive,

                IssueReason = l.IssueReason,
                IssueReasonText = l.IssueReason.ToString(),

                CreatedByUserID = l.CreatedByUserID,
                CreatedByUserName = l.CreatedByUser?.UserName
            };
        }

        private static License MapToEntity(LicenseDto d)
        {
            return new License
            {
                LicenseID = d.LicenseID,
                ApplicationID = d.ApplicationID,
                DriverID = d.DriverID,
                LicenseClassId = d.LicenseClassId,
                IssueDate = d.IssueDate,
                ExpirationDate = d.ExpirationDate,
                Notes = d.Notes,
                PaidFees = d.PaidFees,
                IsActive = d.IsActive,
                IssueReason = d.IssueReason,
                CreatedByUserID = d.CreatedByUserID
            };
        }
    }
}