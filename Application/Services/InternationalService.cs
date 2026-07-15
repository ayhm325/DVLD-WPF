using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class InternationalService : IInternationalService
    {
        private readonly InternationalRepository _repository;

        public InternationalService(InternationalRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<InternationalDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto);
        }

        public async Task<InternationalDto?> GetByIdAsync(int internationalLicenseId)
        {
            var entity = await _repository.GetByIdAsync(internationalLicenseId);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<InternationalDto>> GetByDriverIdAsync(int driverId)
        {
            var list = await _repository.GetByDriverIdAsync(driverId);
            return list.Select(MapToDto);
        }

        public async Task<InternationalDto?> GetByApplicationIdAsync(int applicationId)
        {
            var entity = await _repository.GetByApplicationIdAsync(applicationId);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<InternationalDto>> GetByLocalLicenseIdAsync(int localLicenseId)
        {
            var list = await _repository.GetByLocalLicenseIdAsync(localLicenseId);
            return list.Select(MapToDto);
        }

        public async Task<bool> HasActiveInternationalLicenseAsync(int driverId)
        {
            return await _repository.HasActiveInternationalLicenseAsync(driverId);
        }

        public async Task AddAsync(InternationalDto dto)
        {
            await _repository.AddAsync(MapToEntity(dto));
        }

        public async Task UpdateAsync(InternationalDto dto)
        {
            await _repository.UpdateAsync(MapToEntity(dto));
        }

        public async Task DeleteAsync(int internationalLicenseId)
        {
            await _repository.DeleteAsync(internationalLicenseId);
        }

        private static InternationalDto MapToDto(InternationalLicense entity)
        {
            return new InternationalDto
            {
                InternationalLicenseID = entity.InternationalLicenseID,
                ApplicationID = entity.ApplicationID,
                DriverID = entity.DriverID,
                IssuedUsingLocalLicenseID = entity.IssuedUsingLocalLicenseID,
                IssueDate = entity.IssueDate,
                ExpirationDate = entity.ExpirationDate,
                IsActive = entity.IsActive,
                CreatedByUserID = entity.CreatedByUserID,
                PersonID = entity.Driver?.PersonID ?? 0,
                FullName = entity.Driver?.Person?.FullName ?? string.Empty,
                DateOfBirth = entity.Driver?.Person?.DateOfBirth ?? DateTime.MinValue,
                ImagePath = entity.Driver?.Person?.ImagePath ?? string.Empty,
                NationalNo = entity.Driver?.Person?.NationalNo ?? string.Empty,
                Gender = entity.Driver?.Person?.Gender == Domain.Enums.Gender.Male? "Male": "Female"
            };
        }

        private static InternationalLicense MapToEntity(InternationalDto dto)
        {
            return new InternationalLicense
            {
                InternationalLicenseID = dto.InternationalLicenseID,
                ApplicationID = dto.ApplicationID,
                DriverID = dto.DriverID,
                IssuedUsingLocalLicenseID = dto.IssuedUsingLocalLicenseID,
                IssueDate = dto.IssueDate,
                ExpirationDate = dto.ExpirationDate,
                IsActive = dto.IsActive,
                CreatedByUserID = dto.CreatedByUserID                
            };
        }
    }
}