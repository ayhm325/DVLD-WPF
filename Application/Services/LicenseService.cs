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
        private readonly ICurrentUserService _currentUserService;


        public LicenseService(
                LicenseRepository repository,
                ILocalDrivingLicenseApplicationService lDLAppService,
                IApplicationService applicationService,
                IDriverService driverService,
                IPersonService personService,
                IDetainedLicenseService detainedLicenseService,
                ICurrentUserService currentUserService)
        {
            _repository = repository;
            _lDLAppService = lDLAppService;
            _applicationService = applicationService;
            _driverService = driverService;
            _personService = personService;
            _detainedLicenseService = detainedLicenseService;
            _currentUserService = currentUserService;
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
                    ? $"{l.Driver.Person.FirstName ?? ""}"
                    : null,

                LicenseClassID = l.LicenseClass,
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


        public async Task<int> IssueFirstLicenseAsync(int localAppId, string? notes)
        {
            // 1. جلب البيانات الأساسية للطلب المحلي
            var localApp = await _lDLAppService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);
            if (localApp == null) throw new Exception("Local application not found.");

            // 2. جلب بيانات التطبيق المرتبط (Application)
            var applicationId = await _lDLAppService.GetApplicationIdByLocalIdAsync(localAppId);
            if (!applicationId.HasValue) throw new Exception("Application not found.");

            var application = await _applicationService.GetApplicationByIdAsync(applicationId.Value);
            var person = await _personService.GetPersonByIdAsync(application.ApplicantPersonID);
            if (person == null) throw new Exception("Person not found.");

            // 3. معالجة وجود السائق
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

            // 4. التصحيح الجوهري: الحصول على LicenseClassID بشكل مباشر وموثوق
            // بما أننا نعلم أن localApp يحتوي على البيانات، سنقوم بالتحقق من القيمة قبل الاستخدام.
            // إذا استمر الخطأ، تأكد من أن GetLocalDrivingLicenseApplicationByIdAsync يملأ هذا الحقل.
            int licenseClassId = localApp.LicenseClassID;

            if (licenseClassId <= 0)
            {
                // إذا وصلت القيمة هنا وهي 0، فهذا يعني أن الـ Mapper لا يقوم بجلبها من قاعدة البيانات
                throw new Exception($"Invalid License Class. The application (ID: {localAppId}) does not have a valid LicenseClassID (Value: {licenseClassId}).");
            }

            // 5. إنشاء كائن الرخصة
            var dto = new LicenseDto
            {
                ApplicationID = applicationId.Value,
                DriverID = driverId,
                LicenseClassID = licenseClassId, // استخدام المتغير الموثق
                IssueDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(10),
                Notes = notes,
                PaidFees = 0,
                IsActive = true,
                IssueReason = 1, // First Time
                CreatedByUserID = _currentUserService.UserId
            };

            // 6. الحفظ
            return await AddAsync(dto);
        }









    }
}