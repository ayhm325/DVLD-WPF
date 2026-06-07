using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Repositories;
using Domain.Entities;

namespace Application.Services
{
    public class LocalDrivingLicenseApplicationService : ILocalDrivingLicenseApplicationService
    {
        private readonly LocalDrivingLicenseApplicationRepository _repository;

        public LocalDrivingLicenseApplicationService(LocalDrivingLicenseApplicationRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<LocalDrivingLicenseApplicationDto>> GetAllLocalDrivingLicenseApplicationsAsync()
        {
            var entities = await _repository.GetAllAsync();
            var dtoList = new List<LocalDrivingLicenseApplicationDto>();

            foreach (var e in entities)
            {
                int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);
                dtoList.Add(MapToDto(e, count));
            }
            return dtoList;
        }

        public async Task<LocalDrivingLicenseApplicationDto?> GetLocalDrivingLicenseApplicationByIdAsync(int id)
        {
            var e = await _repository.GetByIdAsync(id);
            if (e == null) return null;

            int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);
            return MapToDto(e, count);
        }

        public async Task<int> AddLocalDrivingLicenseApplicationAsync(LocalDrivingLicenseApplicationCreateUpdateDto dto)
        {
            var result = await _repository.AddFullApplicationAsync(dto.PersonId, dto.LicenseClassId, dto.CreatedByUserId);
            return result.ApplicationID;
        }

        public async Task<bool> UpdateLocalDrivingLicenseApplicationAsync(int id, LocalDrivingLicenseApplicationCreateUpdateDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;
            existing.LicenseClassID = dto.LicenseClassId;
            return await _repository.UpdateAsync(existing);
        }

        public async Task<bool> DeleteLocalDrivingLicenseApplicationAsync(int id) => await _repository.DeleteAsync(id);

        public async Task<List<LocalDrivingLicenseApplicationDto>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId)
        {
            var list = await _repository.GetByPersonIdAsync(applicantPersonId);
            return await MapListToDtoAsync(list);
        }

        private async Task<List<LocalDrivingLicenseApplicationDto>> MapListToDtoAsync(List<LocalDrivingLicenseApplication> list)
        {
            var dtoList = new List<LocalDrivingLicenseApplicationDto>();
            foreach (var e in list)
            {
                int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);
                dtoList.Add(MapToDto(e, count));
            }
            return dtoList;
        }

        public async Task<List<LocalDrivingLicenseApplicationDto>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId)
        {
            var list = await _repository.GetByApplicationIdAsync(applicationId);
            return await MapListToDtoAsync(list);
        }

        public async Task<List<LocalDrivingLicenseApplicationDto>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId)
        {
            var list = await _repository.GetByLicenseClassIdAsync(licenseClassId);
            return await MapListToDtoAsync(list);
        }

        public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id)
        {
            return await _repository.GetByIdAsync(id) != null;
        }

        private LocalDrivingLicenseApplicationDto MapToDto(LocalDrivingLicenseApplication e, int passedTestCount)
        {
            return new LocalDrivingLicenseApplicationDto
            {
                ApplicationID = e.LocalDrivingLicenseApplicationID,
                FullName = $"{e.Application?.Person?.FirstName} {e.Application?.Person?.SecondName} {e.Application?.Person?.ThirdName} {e.Application?.Person?.LastName}".Trim(),
                DrivingClass = e.LicenseClass?.ClassName ?? "N/A",
                NationalNo = e.Application?.Person?.NationalNo ?? "N/A",
                ApplicationDate = e.Application?.ApplicationDate ?? DateTime.MinValue,
                ApplicationStatus = e.Application?.ApplicationStatus ?? 0,
                PassedTest = passedTestCount
            };
        }
    }
}