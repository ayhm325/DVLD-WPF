using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;

namespace Application.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly LicenseRepository _repository;

        private readonly ILocalDrivingLicenseApplicationService _lDLAppService;
        private readonly IApplicationService _applicationService;
        private readonly IDriverService _driverService; 
        private readonly IPersonService _personService;
        private readonly IDetainedLicenseService _detainedLicenseService;


        public LicenseService(LicenseRepository repository, ILocalDrivingLicenseApplicationService lDLAppService,
            IApplicationService applicationService, IDriverService driverService, IPersonService personService,
            IDetainedLicenseService detainedLicenseService)
        {
            _repository = repository;
            _lDLAppService = lDLAppService;
            _applicationService = applicationService;
            _driverService = driverService;
            _personService = personService;
            _detainedLicenseService = detainedLicenseService;
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

                IsActive = l.IsActive ,

                IssueReason = l.IssueReason ,
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
                IsActive = d.IsActive ,
                IssueReason = d.IssueReason ,
                CreatedByUserID = d.CreatedByUserID
            };
        }

        //===========================================

        public async Task<DriverLicenseInfoDto?> GetDetails(int localAppId)
        {
            var localApp = await _lDLAppService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);

            var appId = await _lDLAppService.GetApplicationIdByLocalIdAsync(localAppId);
            var app = await _applicationService.GetApplicationByIdAsync((int)appId);
            if (app == null) return null;

            var person = await _personService.GetPersonByIdAsync(app.ApplicantPersonID);
            if (person == null) return null;
          
            var driver = await _driverService.GetByPersonIdAsync(person.PersonId);
            if (driver == null) return null;
           
            var license = driver != null
                ? await _repository.GetByDriverIdAsync(driver.DriverID)
                : null;

            if (license == null) return null;


            return new DriverLicenseInfoDto
            {
                // License Info
                LicenseId = license?.LicenseID ?? 0,

                LicenseClass = localApp?.LicenseClassName ?? string.Empty,

                IssueDate = license?.IssueDate ?? default,
                ExpirationDate = license?.ExpirationDate ?? default,

                IsActive = license?.IsActive ?? false,

                IsDetained = license != null &&
             await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID),

                IssueReason = license?.IssueReason.ToString() ?? string.Empty,

                Notes = license?.Notes ?? string.Empty,

                // Driver Info
                DriverId = driver?.DriverID ?? 0,

                // Person Info
                FullName = person.FullName,
                NationalNo = person.NationalNo,
                DateOfBirth = person.DateOfBirth,
                Gender = person.Gender,
                ImagePath = person.ImagePath
            };
        }
        









    }
}