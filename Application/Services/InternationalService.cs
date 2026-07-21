using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;


namespace Application.Services
{
    public class InternationalService : IInternationalService
    {
        private readonly IInternationalRepository _repository;
        private readonly ILicenseService _licenseService;
        private readonly IApplicationService _applicationService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ICurrentUserService _currentUserService;

        public InternationalService(
            IInternationalRepository repository,
            ILicenseService licenseService,
            IApplicationService applicationService,
            IApplicationTypeService applicationTypeService,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _licenseService = licenseService;
            _applicationService = applicationService;
            _applicationTypeService = applicationTypeService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<InternationalDto>>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();

            return Result<List<InternationalDto>>.Success(
                list.Select(MapToDto).ToList());
        }

        public async Task<Result<InternationalDto>> GetByIdAsync(int internationalLicenseId)
        {
            var entity = await _repository.GetByIdAsync(internationalLicenseId);

            if (entity == null)
                return Result<InternationalDto>.Fail(
                    "International license not found.");

            return Result<InternationalDto>.Success(MapToDto(entity));
        }

        public async Task<Result<List<InternationalDto>>> GetByDriverIdAsync(int driverId)
        {
            var list = await _repository.GetByDriverIdAsync(driverId);

            return Result<List<InternationalDto>>.Success(
                list.Select(MapToDto).ToList());
        }

        public async Task<Result<InternationalDto>> GetByApplicationIdAsync(int applicationId)
        {
            var entity = await _repository.GetByApplicationIdAsync(applicationId);

            if (entity == null)
                return Result<InternationalDto>.Fail(
                    "International license not found.");

            return Result<InternationalDto>.Success(MapToDto(entity));
        }

        public async Task<Result<List<InternationalDto>>> GetByLocalLicenseIdAsync(int localLicenseId)
        {
            var list = await _repository.GetByLocalLicenseIdAsync(localLicenseId);

            return Result<List<InternationalDto>>.Success(
                list.Select(MapToDto).ToList());
        }

        public async Task<bool> HasActiveInternationalLicenseAsync(int driverId)
        {
            return await _repository.HasActiveInternationalLicenseAsync(driverId);
        }

        public async Task<Result> AddAsync(InternationalDto dto)
        {
            if (dto == null)
                return Result.Failure("International license data is required.");

            await _repository.AddAsync(MapToEntity(dto));

            return Result.Success();
        }

        public async Task<Result> UpdateAsync(InternationalDto dto)
        {
            if (dto == null)
                return Result.Failure("International license data is required.");

            await _repository.UpdateAsync(MapToEntity(dto));

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(int internationalLicenseId)
        {
            var entity = await _repository.GetByIdAsync(internationalLicenseId);

            if (entity == null)
                return Result.Failure("International license not found.");

            await _repository.DeleteAsync(internationalLicenseId);

            return Result.Success();
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

        public async Task<Result<int>> IssueInternationalLicenseAsync(int localLicenseId)
        {
            var licenseResult = await _licenseService.GetByIdAsync(localLicenseId);

            if (licenseResult.IsFailure)
                return Result<int>.Fail(licenseResult.Error);

            var license = licenseResult.Value!;


            if (!license.IsActive)
                return Result<int>.Fail("License is not active.");


            var exists = await _repository.ExistsByLocalLicenseAsync(localLicenseId);

            if (exists)
                return Result<int>.Fail(
                    "An active international license already exists.");


            var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(6);

            if (applicationTypeResult.IsFailure)
                return Result<int>.Fail(applicationTypeResult.Error);

            var applicationType = applicationTypeResult.Value!;

            var applicationResult =
                await _applicationService.AddNewApplicationAsync(
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


            if (applicationResult.IsFailure)
                return Result<int>.Fail(applicationResult.Error);


            int applicationId = applicationResult.Value;


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


            await _applicationService
                .CompleteApplicationAsync(applicationId);


            return Result<int>.Success(
                internationalLicense.InternationalLicenseID);
        }


        public async Task<Result<DriverLicenseInfoDto>> GetLocalLicenseInfoAsync(int licenseId)
        {
            var licenseResult = await _licenseService.GetByIdAsync(licenseId);

            if (licenseResult.IsFailure)
                return Result<DriverLicenseInfoDto>.Fail(
                    licenseResult.Error);


            var license = licenseResult.Value!;


            if (license.LicenseClassID != 3)
                return Result<DriverLicenseInfoDto>.Fail(
                    "Only class 3 licenses can be converted.");


            var dto = new DriverLicenseInfoDto
            {
                LicenseId = license.LicenseID,
                DriverId = license.DriverID,
                LicenseClass = license.LicenseClassName,
                PersonID = license.Driver.PersonID,
                FullName = license.Driver?.FullName ?? string.Empty,
                NationalNo = license.Driver?.NationalNo ?? string.Empty,

                Gender = license.Driver?.Gender == Gender.Male
                    ? "Male"
                    : "Female",

                DateOfBirth = license.Driver?.DateOfBirth
                    ?? DateTime.MinValue,

                IssueDate = license.IssueDate,

                ExpirationDate = license.ExpirationDate,

                IsActive = license.IsActive,

                Notes = license.Notes,

                IssueReason = license.IssueReason.ToString(),

                ImagePath = license.Driver?.ImagePath ?? string.Empty
            };


            return Result<DriverLicenseInfoDto>.Success(dto);
        }

    }
}