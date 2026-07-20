using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class LocalDrivingLicenseApplicationService : ILocalDrivingLicenseApplicationService
    {
        private readonly ILocalDrivingLicenseApplicationRepository _repository;
        private readonly ILicenseRepository _licenseRepository;

        public LocalDrivingLicenseApplicationService(ILocalDrivingLicenseApplicationRepository repository, ILicenseRepository licenseRepository)
        {
            _repository = repository;
            _licenseRepository = licenseRepository;
        }

        public async Task<List<LocalDrivingLicenseApplicationListDto>> GetAllLocalDrivingLicenseApplicationsAsync()
        {
            var entities = await _repository.GetAllAsync();
            var dtoList = new List<LocalDrivingLicenseApplicationListDto>();

            foreach (var e in entities)
            {
                int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);

                bool hasLicense = await _licenseRepository
                    .IsApplicationHasLicenseAsync(e.ApplicationID);

                dtoList.Add(MapToDto(e, count, hasLicense));
            }
            return dtoList;
        }

        public async Task<LocalDrivingLicenseApplicationListDto?> GetLocalDrivingLicenseApplicationByIdAsync(int id)
        {
            var e = await _repository.GetByIdAsync(id);
            if (e == null)
                return null;

            int count = await _repository.GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);

            bool hasLicense = await _licenseRepository
                .IsApplicationHasLicenseAsync(e.ApplicationID);

            return MapToDto(e, count, hasLicense);
        }

        public async Task<int> AddLocalDrivingLicenseApplicationAsync(LocalDrivingLicenseApplicationCreateUpdateDto dto)
        {
            var entity = new LocalDrivingLicenseApplication
            {
                ApplicationID = dto.ApplicatonId,
                LicenseClassID = dto.LicenseClassId,                              
            };

            return await _repository.CreateLocalDrivingLicenseApplicationAsync(entity);
        }

        public async Task<bool> UpdateLocalDrivingLicenseApplicationAsync(int id, LocalDrivingLicenseApplicationCreateUpdateDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;
            existing.LicenseClassID = dto.LicenseClassId;
            return await _repository.UpdateAsync(existing);
        }

        public async Task<bool> DeleteLocalDrivingLicenseApplicationAsync(int id) => await _repository.DeleteAsync(id);

        public async Task<List<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId)
        {
            var list = await _repository.GetByPersonIdAsync(applicantPersonId);
            return await MapListToDtoAsync(list);
        }

        private async Task<List<LocalDrivingLicenseApplicationListDto>> MapListToDtoAsync(
    List<LocalDrivingLicenseApplication> list)
        {
            var dtoList = new List<LocalDrivingLicenseApplicationListDto>();

            foreach (var e in list)
            {
                int count = await _repository
                    .GetPassedTestCountAsync(e.LocalDrivingLicenseApplicationID);

                bool hasLicense = await _licenseRepository
                    .IsApplicationHasLicenseAsync(e.ApplicationID);

                dtoList.Add(MapToDto(e, count, hasLicense));
            }

            return dtoList;
        }

        public async Task<List<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId)
        {
            var list = await _repository.GetByApplicationIdAsync(applicationId);
            return await MapListToDtoAsync(list);
        }

        public async Task<List<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId)
        {
            var list = await _repository.GetByLicenseClassIdAsync(licenseClassId);
            return await MapListToDtoAsync(list);
        }

        public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id)
        {
            return await _repository.GetByIdAsync(id) != null;
        }

        private LocalDrivingLicenseApplicationListDto MapToDto(LocalDrivingLicenseApplication e, int passedTestCount, bool hasLicense)
        {
            return new LocalDrivingLicenseApplicationListDto
            {
                LocalDrivingLicenseApplicationID = e.LocalDrivingLicenseApplicationID,
                LicenseClassID = e.LicenseClassID,
                LicenseClassName = e.LicenseClass?.ClassName ?? "N/A",
                NationalNo = e.Application?.Person?.NationalNo ?? "N/A",
                Fees = e.LicenseClass?.ClassFees ?? 0,
                FullName =
                    $"{e.Application?.Person?.FirstName} " +
                    $"{e.Application?.Person?.SecondName} " +
                    $"{e.Application?.Person?.ThirdName} " +
                    $"{e.Application?.Person?.LastName}".Trim(),
                ApplicationDate = e.Application?.ApplicationDate ?? DateTime.MinValue,
                PassedTest = passedTestCount,                
                ApplicationStatus = e.Application is not null && Enum.IsDefined(typeof(AppStatus), e.Application.ApplicationStatus)
                ? (AppStatus)e.Application.ApplicationStatus
                : AppStatus.Cancelled,
                HasLicense = hasLicense,
                ApplicantPersonID = e.Application?.Person?.PersonId ?? 0

            };
        }

        public Task<int?> GetApplicationIdByLocalIdAsync(int localId)
        {
            return _repository.GetApplicationIdByLocalIdAsync(localId);
        }
    }
}