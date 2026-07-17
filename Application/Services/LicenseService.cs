using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
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
        private readonly ICurrentUserService _currentUserService;
        private readonly ILicenseClassService _licenseClassService;
        private readonly IApplicationTypeService _applicationTypeService;


        public LicenseService(
                LicenseRepository repository,
                ILocalDrivingLicenseApplicationService lDLAppService,
                IApplicationService applicationService,
                IDriverService driverService,
                IPersonService personService,
                IDetainedLicenseService detainedLicenseService,
                ICurrentUserService currentUserService,
                ILicenseClassService licenseClassService,
                IApplicationTypeService applicationTypeService)
        {
            _repository = repository;
            _lDLAppService = lDLAppService;
            _applicationService = applicationService;
            _driverService = driverService;
            _personService = personService;
            _detainedLicenseService = detainedLicenseService;
            _currentUserService = currentUserService;
            _licenseClassService = licenseClassService;
            _applicationTypeService = applicationTypeService;
        }

        // Replace License
        public async Task<int> ReplaceLicenseAsync(int oldLicenseId, string replacementReason, int applicationTypeId)
        {
            var oldLicense = await _repository.GetLicenseByIdAsync(oldLicenseId);

            if (oldLicense == null)
                throw new Exception("Old license not found.");

            if (!oldLicense.IsActive)
                throw new Exception("Cannot replace inactive license.");

            var reasonEnum = replacementReason == "Lost License"
                     ? Domain.Enums.IssueReason.ReplacementForLost
                     : Domain.Enums.IssueReason.ReplacementForDamaged;

            var applicationType = await _applicationTypeService.GetApplicationTypeByIdAsync(applicationTypeId);

            if (applicationType == null)
                throw new Exception("Replacement application type not found.");

            var application = new ApplicationDto
            {
                ApplicantPersonID = oldLicense.Driver.PersonID,
                ApplicationTypeID = applicationTypeId,
                ApplicationDate = DateTime.Now,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = DateTime.Now,
                PaidFees = applicationType.ApplicationTypeFees,
                CreatedByUserID = _currentUserService.UserId
            };

            int applicationId = await _applicationService.AddNewApplicationAsync(application);

            var newLicense = new LicenseDto
            {
                ApplicationID = applicationId,
                DriverID = oldLicense.DriverID,
                LicenseClassID = oldLicense.LicenseClass,
                IssueDate = DateTime.Now,
                ExpirationDate = oldLicense.ExpirationDate,
                PaidFees = oldLicense.LicenseClassInfo.ClassFees,
                Notes = replacementReason,
                IsActive = true,
                IssueReason = (byte)reasonEnum,
                CreatedByUserID = _currentUserService.UserId
            };
            int newLicenseId = await AddAsync(newLicense);
            // Deactivate old license
            oldLicense.IsActive = false;
            await _repository.UpdateLicenseAsync(oldLicense);

            application.ApplicationID = applicationId;
            application.ApplicationStatus = AppStatus.Completed;
            await _applicationService.UpdateApplicationAsync(application);

            return newLicenseId;
        }

        // Renew License
        public async Task<int> RenewLicenseAsync(int oldLicenseId, string? notes)
        {
            // 1) Get old license
            var oldLicense = await _repository.GetLicenseByIdAsync(oldLicenseId);

            if (oldLicense == null)
                throw new Exception("Old license not found");

            // ==========================
            // Business Validation
            // ==========================
            if (!oldLicense.IsActive)
                throw new Exception("Cannot renew an inactive license.");

            if (oldLicense.ExpirationDate > DateTime.Now)
                throw new Exception("Cannot renew license before expiration date.");

            // 2) Get application type
            var applicationType =
                await _applicationTypeService.GetApplicationTypeByIdAsync(2);

            if (applicationType == null)
                throw new Exception("Application type not found");

            var totalFees = applicationType.ApplicationTypeFees + oldLicense.LicenseClassInfo.ClassFees;

            // 3) Create Application            
            var application = new ApplicationDto
            {
                ApplicantPersonID = oldLicense.Driver.PersonID,
                ApplicationTypeID = 2,
                ApplicationDate = DateTime.Now,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = DateTime.Now,
                PaidFees = applicationType.ApplicationTypeFees,
                CreatedByUserID = _currentUserService.UserId
            };

            int applicationId = await _applicationService.AddNewApplicationAsync(application);

            // 4) Create new license
            var newLicense = new LicenseDto
            {
                ApplicationID = applicationId,
                DriverID = oldLicense.DriverID,
                LicenseClassID = oldLicense.LicenseClass,
                IssueDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(oldLicense.LicenseClassInfo.DefaultValidityLength),
                PaidFees = oldLicense.LicenseClassInfo.ClassFees,
                Notes = notes,
                IsActive = true,
                IssueReason = (byte)Domain.Enums.IssueReason.Renew,
                CreatedByUserID = _currentUserService.UserId
            };

            int newLicenseId = await AddAsync(newLicense);

            // ==========================
            // Deactivate old license
            // ==========================
            oldLicense.IsActive = false;

            await _repository.UpdateLicenseAsync(oldLicense);

            // 5) Complete application
            application.ApplicationID = applicationId;

            application.ApplicationStatus = AppStatus.Completed;

            await _applicationService.UpdateApplicationAsync(application);

            return newLicenseId;
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

        public async Task<List<LicenseDto>> GetLicensesByPersonIdAsync(int personId)
        {
            var licenses = await _repository.GetLicensesByPersonIdAsync(personId);
            return licenses.Select(MapToDto).ToList();
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
                DriverName = l.Driver?.Person != null? l.Driver.Person.FullName: null,

                LicenseClassID = l.LicenseClass,
                LicenseClassName = l.LicenseClassInfo?.ClassName,

                IssueDate = l.IssueDate,
                ExpirationDate = l.ExpirationDate,

                Notes = l.Notes,
                PaidFees = l.PaidFees,

                IsActive = l.IsActive ,

                IssueReason = l.IssueReason ,
                IssueReasonText = ((Domain.Enums.IssueReason)l.IssueReason).ToString(),

                CreatedByUserID = l.CreatedByUserID,
                CreatedByUserName = l.CreatedByUser?.UserName ?? "Unknown"
            };
        }

        private static License MapToEntity(LicenseDto d)
        {
            return new License
            {
                LicenseID = d.LicenseID,
                ApplicationID = d.ApplicationID,
                DriverID = d.DriverID,
                LicenseClass = d.LicenseClassID,
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
            var appId = await _lDLAppService.GetApplicationIdByLocalIdAsync(localAppId);
            if (!appId.HasValue) return null;

            var app = await _applicationService.GetApplicationByIdAsync(appId.Value);
            if (app == null) return null;

            var person = await _personService.GetPersonByIdAsync(app.ApplicantPersonID);
            if (person == null) return null;

            // تأكد هنا أن الـ Repository يعيد كائن الرخصة مع الـ LicenseClassInfo
            var licenses = await _repository.GetLicensesByApplicationIdAsync(appId.Value);
            var license = licenses?.FirstOrDefault();

            if (license == null) return null;

            var driver = await _driverService.GetByPersonIdAsync(person.PersonId);

            return new DriverLicenseInfoDto
            {
                LicenseId = license.LicenseID,

                // التصحيح هنا: جلب الاسم من الكائن المرتبط وليس تحويل الـ ID لنص
                LicenseClass = license.LicenseClassInfo?.ClassName ?? "Unknown",

                IssueDate = license.IssueDate,
                ExpirationDate = license.ExpirationDate,
                IsActive = license.IsActive,
                IsDetained = await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID),
                IssueReason = license.IssueReason.ToString(),
                Notes = license.Notes ?? string.Empty,
                DriverId = driver?.DriverID ?? 0,
                FullName = person.FullName,
                NationalNo = person.NationalNo,
                DateOfBirth = person.DateOfBirth,
                Gender = person.Gender,
                ImagePath = person.ImagePath
            };
        }
        //===========================================
        // Get License Details By License ID
        //===========================================
        public async Task<DriverLicenseInfoDto?> GetLicenseDetailsByIdAsync(int licenseId)
        {
            var license = await _repository.GetLicenseByIdAsync(licenseId);

            if (license == null)
                return null;


            var person = license.Driver?.Person;

            if (person == null)
                return null;


            return new DriverLicenseInfoDto
            {
                // License Info
                LicenseId = license.LicenseID,
                LicenseClass =license.LicenseClassInfo?.ClassName ?? "Unknown",
                IssueDate = license.IssueDate,
                ExpirationDate = license.ExpirationDate,
                IsActive = license.IsActive,
                IsDetained =await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID),
                IssueReason = ((Domain.Enums.IssueReason)license.IssueReason).ToString(),
                Notes = license.Notes,
                // Driver Info
                DriverId = license.DriverID,
                // Person Info
                PersonID = person.PersonId,
                FullName = person.FullName,
                NationalNo = person.NationalNo,
                DateOfBirth = person.DateOfBirth,
                Gender = person.Gender.ToString(),
                ImagePath = person.ImagePath
            };
        }

        public async Task<int> IssueFirstLicenseAsync(int localAppId, string? notes)
        {
            // 1) Get Local Driving License Application
            var localApp = await _lDLAppService
                .GetLocalDrivingLicenseApplicationByIdAsync(localAppId);

            if (localApp == null)
                throw new Exception("Local application not found.");


            // 2) Get Main Application ID
            var applicationId = await _lDLAppService
                .GetApplicationIdByLocalIdAsync(localAppId);

            if (!applicationId.HasValue)
                throw new Exception("Application not found.");


            // 3) Get Application
            var application = await _applicationService
                .GetApplicationByIdAsync(applicationId.Value);

            if (application == null)
                throw new Exception("Application not found.");


            // 4) Get Person
            var person = await _personService
                .GetPersonByIdAsync(application.ApplicantPersonID);

            if (person == null)
                throw new Exception("Person not found.");

            // 5) Get License Class
            int licenseClassId = localApp.LicenseClassID;

            if (licenseClassId <= 0)
                throw new Exception("Invalid License Class.");

            var licenseClass = await _licenseClassService
                .GetLicenseClassByIdAsync(licenseClassId);


            if (licenseClass == null)
                throw new Exception("License class not found.");

            // 6) Create Driver if not exists
            var driver = await _driverService.GetByPersonIdAsync(person.PersonId);

            int driverId;


            if (driver == null)
            {
                var newDriver = new DriverDto
                {
                    PersonID = person.PersonId,
                    CreatedByUserID = _currentUserService.UserId,
                    CreatedDate = DateTime.Now
                };

                driverId = await _driverService.AddAsync(newDriver);
            }
            else
            {
                driverId = driver.DriverID;
            }

            // 7) Create License
            var licenseDto = new LicenseDto
            {
                ApplicationID = applicationId.Value,

                DriverID = driverId,

                LicenseClassID = licenseClassId,

                IssueDate = DateTime.Now,

                ExpirationDate = DateTime.Now
                    .AddYears(licenseClass.DefaultValidityLength),

                Notes = notes,

                PaidFees = licenseClass.LicenseClassFees,

                IsActive = true,

                IssueReason = (byte)Domain.Enums.IssueReason.FirstTime,

                CreatedByUserID = _currentUserService.UserId
            };

            int licenseId = await AddAsync(licenseDto);

            // 8) Update Application Status
            application.ApplicationStatus = AppStatus.Completed;

            application.PaidFees = licenseClass.LicenseClassFees;

            await _applicationService.UpdateApplicationAsync(application);

            return licenseId;
        }


    }
}