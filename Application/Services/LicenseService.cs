using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseRepository _repository;
        private readonly ILocalDrivingLicenseApplicationService _lDLAppService;
        private readonly IApplicationService _applicationService;
        private readonly IDriverService _driverService;
        private readonly IPersonService _personService;
        private readonly IDetainedLicenseService _detainedLicenseService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILicenseClassService _licenseClassService;
        private readonly IApplicationTypeService _applicationTypeService;

        public LicenseService(
            ILicenseRepository repository,
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

        // =========================
        // REPLACE LICENSE
        // =========================
        public async Task<Result<int>> ReplaceLicenseAsync(
            int oldLicenseId,
            string replacementReason,
            int applicationTypeId)
        {
            // 1) Get old license
            var oldLicense = await _repository.GetLicenseByIdAsync(oldLicenseId);

            if (oldLicense == null)
                return Result<int>.Fail("الرخصة القديمة غير موجودة.");

            if (!oldLicense.IsActive)
                return Result<int>.Fail("لا يمكن استبدال رخصة غير فعالة.");

            // 2) Determine issue reason
            var reasonEnum = replacementReason == "Lost License"
                ? IssueReason.ReplacementForLost
                : IssueReason.ReplacementForDamaged;

            // 3) Get application type            
            var applicationTypeResult =await _applicationTypeService.GetApplicationTypeByIdAsync(applicationTypeId);

            if (applicationTypeResult.IsFailure)
                return Result<int>.Fail(applicationTypeResult.Error);

            var applicationType = applicationTypeResult.Value!;

            // 4) Create Application
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

            var appResult = await _applicationService.AddNewApplicationAsync(application);

            if (appResult.IsFailure)
                return Result<int>.Fail(appResult.Error);

            int applicationId = appResult.Value;

            // 5) Create new license
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

            var licenseResult = await AddAsync(newLicense);

            if (licenseResult.IsFailure)
                return Result<int>.Fail(licenseResult.Error);

            // 6) Deactivate old license
            oldLicense.IsActive = false;
            await _repository.UpdateLicenseAsync(oldLicense);

            // 7) Complete application
            application.ApplicationID = applicationId;
            application.ApplicationStatus = AppStatus.Completed;
            await _applicationService.UpdateApplicationAsync(application);

            return Result<int>.Success(licenseResult.Value);
        }

        // =========================
        // RENEW LICENSE
        // =========================
        public async Task<Result<int>> RenewLicenseAsync(int oldLicenseId, string? notes)
        {
            // 1) Get old license
            var oldLicense = await _repository.GetLicenseByIdAsync(oldLicenseId);

            if (oldLicense == null)
                return Result<int>.Fail("الرخصة القديمة غير موجودة.");

            // Business Validation
            if (!oldLicense.IsActive)
                return Result<int>.Fail("لا يمكن تجديد رخصة غير فعالة.");

            if (oldLicense.ExpirationDate > DateTime.Now)
                return Result<int>.Fail("لا يمكن تجديد الرخصة قبل تاريخ الانتهاء.");

            // 2) Get application type (Renew = 2)
            var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(2);

            if (applicationTypeResult.IsFailure)
                return Result<int>.Fail(applicationTypeResult.Error);

            var applicationType = applicationTypeResult.Value!;

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

            var appResult = await _applicationService.AddNewApplicationAsync(application);

            if (appResult.IsFailure)
                return Result<int>.Fail(appResult.Error);

            int applicationId = appResult.Value;

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
                IssueReason = (byte)IssueReason.Renew,
                CreatedByUserID = _currentUserService.UserId
            };

            var licenseResult = await AddAsync(newLicense);

            if (licenseResult.IsFailure)
                return Result<int>.Fail(licenseResult.Error);

            // 5) Deactivate old license
            oldLicense.IsActive = false;
            await _repository.UpdateLicenseAsync(oldLicense);

            // 6) Complete application
            application.ApplicationID = applicationId;
            application.ApplicationStatus = AppStatus.Completed;
            await _applicationService.UpdateApplicationAsync(application);

            return Result<int>.Success(licenseResult.Value);
        }

        // =========================
        // GET METHODS
        // =========================
        public async Task<Result<LicenseDto>> GetByIdAsync(int id)
        {
            var entity = await _repository.GetLicenseByIdAsync(id);

            if (entity == null)
                return Result<LicenseDto>.Fail("الرخصة غير موجودة.");

            return Result<LicenseDto>.Success(MapToDto(entity));
        }

        public async Task<Result<List<LicenseDto>>> GetAllAsync()
        {
            var list = await _repository.GetAllLicensesAsync();
            return Result<List<LicenseDto>>.Success(list.Select(MapToDto).ToList());
        }

        public async Task<Result<List<LicenseDto>>> GetByDriverIdAsync(int driverId)
        {
            var list = await _repository.GetLicensesByDriverIdAsync(driverId);
            return Result<List<LicenseDto>>.Success(list.Select(MapToDto).ToList());
        }

        public async Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(int applicationId)
        {
            var list = await _repository.GetLicensesByApplicationIdAsync(applicationId);
            return Result<List<LicenseDto>>.Success(list.Select(MapToDto).ToList());
        }

        public async Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(int licenseClassId)
        {
            var list = await _repository.GetLicensesByLicenseClassIdAsync(licenseClassId);
            return Result<List<LicenseDto>>.Success(list.Select(MapToDto).ToList());
        }

        public async Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(int personId)
        {
            var licenses = await _repository.GetLicensesByPersonIdAsync(personId);
            return Result<List<LicenseDto>>.Success(licenses.Select(MapToDto).ToList());
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
        public async Task<Result<int>> AddAsync(LicenseDto dto)
        {
            if (dto == null)
                return Result<int>.Fail("بيانات الرخصة مطلوبة.");

            var entity = MapToEntity(dto);
            var id = await _repository.AddLicenseAsync(entity);

            return Result<int>.Success(id);
        }

        public async Task<Result> UpdateAsync(LicenseDto dto)
        {
            if (dto == null)
                return Result.Failure("بيانات الرخصة مطلوبة.");

            var entity = MapToEntity(dto);
            var isSuccess = await _repository.UpdateLicenseAsync(entity);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في تحديث الرخصة.");
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var exists = await _repository.IsLicenseExistsAsync(id);

            if (!exists)
                return Result.Failure("الرخصة غير موجودة.");

            var isSuccess = await _repository.DeleteLicenseAsync(id);

            return isSuccess
                ? Result.Success()
                : Result.Failure("فشل في حذف الرخصة.");
        }

        // =========================
        // GET DETAILS
        // =========================
        public async Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(int localAppId)
        {
            var appIdResult = await _lDLAppService.GetApplicationIdByLocalIdAsync(localAppId);
            if (appIdResult.IsFailure)
                return Result<DriverLicenseInfoDto>.Fail(appIdResult.Error);

            var appId = appIdResult.Value;

            var appResult = await _applicationService.GetApplicationByIdAsync(appId);
            if (appResult.IsFailure)
                return Result<DriverLicenseInfoDto>.Fail(appResult.Error);

            var app = appResult.Value;

            var personResult = await _personService.GetPersonByIdAsync(app.ApplicantPersonID);

            if (personResult.IsFailure)
                return Result<DriverLicenseInfoDto>.Fail(personResult.Error);

            var person = personResult.Value!;

            if (person == null)
                return Result<DriverLicenseInfoDto>.Fail("الشخص غير موجود.");

            var licenses = await _repository.GetLicensesByApplicationIdAsync(appId);
            var license = licenses?.FirstOrDefault();

            if (license == null)
                return Result<DriverLicenseInfoDto>.Fail("الرخصة غير موجودة.");

            int driverId = 0;

            var driverResult = await _driverService.GetByPersonIdAsync(person.PersonId);

            if (driverResult.IsSuccess)
                driverId = driverResult.Value!.DriverID;

            return Result<DriverLicenseInfoDto>.Success(new DriverLicenseInfoDto
            {
                LicenseId = license.LicenseID,
                LicenseClass = license.LicenseClassInfo?.ClassName ?? "Unknown",
                IssueDate = license.IssueDate,
                ExpirationDate = license.ExpirationDate,
                IsActive = license.IsActive,
                IsDetained = await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID),
                IssueReason = license.IssueReason.ToString(),
                Notes = license.Notes ?? string.Empty,
                DriverId = driverId,
                FullName = person.FullName,
                NationalNo = person.NationalNo,
                DateOfBirth = person.DateOfBirth,
                Gender = person.Gender.ToString(),
                ImagePath = person.ImagePath
            });
        }

        public async Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(int licenseId)
        {
            var license = await _repository.GetLicenseByIdAsync(licenseId);

            if (license == null)
                return Result<DriverLicenseInfoDto>.Fail("الرخصة غير موجودة.");

            var person = license.Driver?.Person;

            if (person == null)
                return Result<DriverLicenseInfoDto>.Fail("بيانات الشخص غير موجودة.");

            return Result<DriverLicenseInfoDto>.Success(new DriverLicenseInfoDto
            {
                LicenseId = license.LicenseID,
                LicenseClass = license.LicenseClassInfo?.ClassName ?? "Unknown",
                IssueDate = license.IssueDate,
                ExpirationDate = license.ExpirationDate,
                IsActive = license.IsActive,
                IsDetained = await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID),
                IssueReason = ((IssueReason)license.IssueReason).ToString(),
                Notes = license.Notes,
                DriverId = license.DriverID,
                PersonID = person.PersonId,
                FullName = person.FullName,
                NationalNo = person.NationalNo,
                DateOfBirth = person.DateOfBirth,
                Gender = person.Gender.ToString(),
                ImagePath = person.ImagePath
            });
        }

        // =========================
        // ISSUE FIRST LICENSE
        // =========================
        public async Task<Result<int>> IssueFirstLicenseAsync(int localAppId, string? notes)
        {
            // 1) Get Local Driving License Application
            var localAppResult = await _lDLAppService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);

            if (localAppResult.IsFailure)
                return Result<int>.Fail(localAppResult.Error);

            var localApp = localAppResult.Value!;

            // 2) Get Main Application ID         
            var applicationIdResult = await _lDLAppService.GetApplicationIdByLocalIdAsync(localAppId);

            if (applicationIdResult.IsFailure)
                return Result<int>.Fail(applicationIdResult.Error);

            int applicationId = applicationIdResult.Value;

            // 3) Get Application
            var applicationResult = await _applicationService.GetApplicationByIdAsync(applicationId);

            if (applicationResult.IsFailure)
                return Result<int>.Fail(applicationResult.Error);

            var application = applicationResult.Value;

            // 4) Get Person
            var personResult = await _personService.GetPersonByIdAsync(application.ApplicantPersonID);

            if (personResult.IsFailure)
                return Result<int>.Fail(personResult.Error);

            var person = personResult.Value!;

            // 5) Get License Class
            int licenseClassId = localApp.LicenseClassID;

            if (licenseClassId <= 0)
                return Result<int>.Fail("فئة الرخصة غير صالحة.");

            var licenseClassResult = await _licenseClassService.GetLicenseClassByIdAsync(licenseClassId);

            if (licenseClassResult.IsFailure)
                return Result<int>.Fail(licenseClassResult.Error);

            var licenseClass = licenseClassResult.Value!;

            // 6) Create Driver if not exists
            int driverId;

            var driverResult = await _driverService.GetByPersonIdAsync(person.PersonId);

            if (driverResult.IsFailure)
            {
                var newDriver = new DriverDto
                {
                    PersonID = person.PersonId,
                    CreatedByUserID = _currentUserService.UserId,
                    CreatedDate = DateTime.Now
                };

                var addResult = await _driverService.AddAsync(newDriver);

                if (addResult.IsFailure)
                    return Result<int>.Fail(addResult.Error);

                driverId = addResult.Value!;
            }
            else
            {
                driverId = driverResult.Value!.DriverID;
            }

            // 7) Create License
            var licenseDto = new LicenseDto
            {
                ApplicationID = applicationId,
                DriverID = driverId,
                LicenseClassID = licenseClassId,
                IssueDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(licenseClass.DefaultValidityLength),
                Notes = notes,
                PaidFees = licenseClass.LicenseClassFees,
                IsActive = true,
                IssueReason = (byte)IssueReason.FirstTime,
                CreatedByUserID = _currentUserService.UserId
            };

            var licenseResult = await AddAsync(licenseDto);

            if (licenseResult.IsFailure)
                return Result<int>.Fail(licenseResult.Error);

            // 8) Update Application Status
            application.ApplicationStatus = AppStatus.Completed;
            application.PaidFees = licenseClass.LicenseClassFees;
            await _applicationService.UpdateApplicationAsync(application);

            return Result<int>.Success(licenseResult.Value);
        }

        // =========================
        // MAPPING
        // =========================
        private static LicenseDto MapToDto(License l)
        {
            return new LicenseDto
            {
                LicenseID = l.LicenseID,
                ApplicationID = l.ApplicationID,
                ApplicationInfo = l.Application != null ? $"App #{l.ApplicationID}" : null,
                DriverID = l.DriverID,
                DriverName = l.Driver?.Person?.FullName,
                LicenseClassID = l.LicenseClass,
                LicenseClassName = l.LicenseClassInfo?.ClassName,
                IssueDate = l.IssueDate,
                ExpirationDate = l.ExpirationDate,
                Notes = l.Notes,
                PaidFees = l.PaidFees,
                IsActive = l.IsActive,
                IssueReason = l.IssueReason,
                IssueReasonText = ((IssueReason)l.IssueReason).ToString(),
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
                IsActive = d.IsActive,
                IssueReason = d.IssueReason,
                CreatedByUserID = d.CreatedByUserID
            };
        }
    }
}