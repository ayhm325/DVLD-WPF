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
        private readonly LicenseRepository _licenseRepository;
        private readonly IApplicationService _applicationService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ICurrentUserService _currentUserService;

        public InternationalService(
            InternationalRepository repository,
            LicenseRepository licenseRepository,
            IApplicationService applicationService,
            IApplicationTypeService applicationTypeService,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _licenseRepository = licenseRepository;
            _applicationService = applicationService;
            _applicationTypeService = applicationTypeService;
            _currentUserService = currentUserService;
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
                Fees = entity.Application?.PaidFees ?? 0,
                CreatedByUserName = entity.CreatedByUser?.UserName ?? string.Empty,
                PersonID = entity.Driver?.PersonID ?? 0,
                FullName = entity.Driver?.Person?.FullName ?? string.Empty,
                DateOfBirth = entity.Driver?.Person?.DateOfBirth ?? DateTime.MinValue,
                ImagePath = entity.Driver?.Person?.ImagePath ?? string.Empty,
                NationalNo = entity.Driver?.Person?.NationalNo ?? string.Empty,
                Gender = entity.Driver?.Person?.Gender == Gender.Male ? "Male" : "Female"
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
        
        public async Task<bool> IssueInternationalLicenseAsync(int localLicenseId)
        {
            // 1. جلب الرخصة المحلية
            var license = await _licenseRepository.GetLicenseByIdAsync(localLicenseId);

            if (license == null)
                return false;

            // يجب أن تكون الرخصة فعالة
            if (!license.IsActive)
                return false;

            // 2. التأكد من عدم وجود رخصة دولية سارية
            var exists = await _repository.ExistsByLocalLicenseAsync(localLicenseId);

            if (exists)
                return false;

            // 3. جلب رسوم طلب الرخصة الدولية
            var applicationType = await _applicationTypeService.GetApplicationTypeByIdAsync(6);
            
            if (applicationType == null)
                return false;

            // 4. إنشاء الطلب
            var applicationId = await _applicationService.AddNewApplicationAsync(
                new ApplicationDto
                {
                    ApplicantPersonID = license.Driver!.PersonID,
                    ApplicationDate = DateTime.Now,
                    ApplicationTypeID = applicationType.ApplicationTypeId,
                    ApplicationStatus = AppStatus.New,
                    LastStatusDate = DateTime.Now,
                    PaidFees = applicationType.ApplicationTypeFees,
                    CreatedByUserID = _currentUserService.UserId
                });

            // 5. إنشاء الرخصة الدولية
            var internationalLicense = new InternationalLicense
            {
                ApplicationID = applicationId,
                DriverID = license.DriverID,
                IssuedUsingLocalLicenseID = license.LicenseID,
                IssueDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(1),
                IsActive = true,
                CreatedByUserID = _currentUserService.UserId
            };

            await _repository.AddAsync(internationalLicense);

            // 6. إنهاء الطلب
            await _applicationService.CompleteApplicationAsync(applicationId);

            return true;
        }


        public async Task<DriverLicenseInfoDto?> GetLocalLicenseInfoAsync(int licenseId)
        {
            var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);

            if (license == null)
                return null;

            if (license.LicenseClass != 3)
                return null;


            return new DriverLicenseInfoDto
            {
                LicenseId = license.LicenseID,
                DriverId = license.DriverID,
                LicenseClass = license.LicenseClassInfo?.ClassName ?? string.Empty,
                PersonID = license.Driver.PersonID,
                FullName = license.Driver?.Person?.FullName ?? string.Empty,
                NationalNo = license.Driver?.Person?.NationalNo ?? string.Empty,
                Gender = license.Driver?.Person?.Gender == Gender.Male
                    ? "Male"
                    : "Female",

                DateOfBirth = license.Driver?.Person?.DateOfBirth?? DateTime.MinValue,

                IssueDate = license.IssueDate,

                ExpirationDate = license.ExpirationDate,

                IsActive = license.IsActive,

                Notes = license.Notes,

                IssueReason = license.IssueReason.ToString(),

                ImagePath = license.Driver?.Person?.ImagePath ?? string.Empty
            };
        }

    }
}