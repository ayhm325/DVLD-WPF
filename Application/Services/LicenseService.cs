using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DriverDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _repository;
    private readonly ILocalDrivingLicenseApplicationService _localDrivingLicenseApplicationService;
    private readonly IApplicationService _applicationService;
    private readonly IDriverService _driverService;
    private readonly IPersonService _personService;
    private readonly IDetainedLicenseService _detainedLicenseService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILicenseClassService _licenseClassService;
    private readonly IApplicationTypeService _applicationTypeService;

    public LicenseService(
        ILicenseRepository repository,
        ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
        IApplicationService applicationService,
        IDriverService driverService,
        IPersonService personService,
        IDetainedLicenseService detainedLicenseService,
        ICurrentUserService currentUserService,
        ILicenseClassService licenseClassService,
        IApplicationTypeService applicationTypeService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _localDrivingLicenseApplicationService = localDrivingLicenseApplicationService ?? throw new ArgumentNullException(nameof(localDrivingLicenseApplicationService));
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
        _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        _detainedLicenseService = detainedLicenseService ?? throw new ArgumentNullException(nameof(detainedLicenseService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _licenseClassService = licenseClassService ?? throw new ArgumentNullException(nameof(licenseClassService));
        _applicationTypeService = applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));
    }

    // GET BY ID
    public async Task<Result<LicenseDto>> GetByIdAsync(int id)
    {
        var validation = LicenseValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<LicenseDto>.FromFailure(validation.Error);

        var license = await _repository.GetLicenseByIdAsync(id);
        if (license is null)
            return Result<LicenseDto>.FromFailure("License not found.");

        return Result<LicenseDto>.Success(MapToDto(license));
    }

    // GET ALL
    public async Task<Result<List<LicenseDto>>> GetAllAsync()
    {
        var licenses = await _repository.GetAllLicensesAsync();
        return Result<List<LicenseDto>>.Success(licenses.Select(MapToDto).ToList());
    }

    // GET BY DRIVER ID
    public async Task<Result<List<LicenseDto>>> GetByDriverIdAsync(int driverId)
    {
        var validation = LicenseValidator.ValidateDriverId(driverId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromFailure(validation.Error);

        var licenses = await _repository.GetLicensesByDriverIdAsync(driverId);
        return Result<List<LicenseDto>>.Success(licenses.Select(MapToDto).ToList());
    }

    // GET BY APPLICATION ID
    public async Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = LicenseValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromFailure(validation.Error);

        var licenses = await _repository.GetLicensesByApplicationIdAsync(applicationId);
        return Result<List<LicenseDto>>.Success(licenses.Select(MapToDto).ToList());
    }

    // GET BY LICENSE CLASS ID
    public async Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(int licenseClassId)
    {
        var validation = LicenseValidator.ValidateLicenseClassId(licenseClassId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromFailure(validation.Error);

        var licenses = await _repository.GetLicensesByLicenseClassIdAsync(licenseClassId);
        return Result<List<LicenseDto>>.Success(licenses.Select(MapToDto).ToList());
    }

    // GET BY PERSON ID
    public async Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return Result<List<LicenseDto>>.FromFailure("Invalid person ID.");

        var licenses = await _repository.GetLicensesByPersonIdAsync(personId);
        return Result<List<LicenseDto>>.Success(licenses.Select(MapToDto).ToList());
    }

    // GET DETAILS BY LOCAL APPLICATION ID
    public async Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(int localAppId)
    {
        if (localAppId <= 0)
            return Result<DriverLicenseInfoDto>.FromFailure("Invalid local application ID.");

        // Get Application ID
        var applicationIdResult = await _localDrivingLicenseApplicationService.GetApplicationIdByLocalIdAsync(localAppId);
        if (applicationIdResult.IsFailure)
            return Result<DriverLicenseInfoDto>.FromFailure(applicationIdResult.Error);
        var applicationId = applicationIdResult.Value;

        // Get Application
        var applicationResult = await _applicationService.GetApplicationByIdAsync(applicationId);
        if (applicationResult.IsFailure)
            return Result<DriverLicenseInfoDto>.FromFailure(applicationResult.Error);
        var application = applicationResult.Value!;

        // Get Person
        var personResult = await _personService.GetPersonByIdAsync(application.ApplicantPersonID);
        if (personResult.IsFailure)
            return Result<DriverLicenseInfoDto>.FromFailure(personResult.Error);
        var person = personResult.Value!;

        // Get License
        var licenses = await _repository.GetLicensesByApplicationIdAsync(applicationId);
        var license = licenses.FirstOrDefault();
        if (license is null)
            return Result<DriverLicenseInfoDto>.FromFailure("License not found.");

        // Get Driver
        var driverResult = await _driverService.GetByPersonIdAsync(person.PersonId);
        var driverId = driverResult.IsSuccess ? driverResult.Value!.DriverID : 0;

        // Check Detention
        var isDetained = await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID);

        return Result<DriverLicenseInfoDto>.Success(new DriverLicenseInfoDto
        {
            LicenseId = license.LicenseID,
            LicenseClass = license.LicenseClassInfo?.ClassName ?? "Unknown",
            IssueDate = license.IssueDate,
            ExpirationDate = license.ExpirationDate,
            IsActive = license.IsActive,
            IsDetained = isDetained,
            IssueReason = ((IssueReason)license.IssueReason).ToString(),
            Notes = license.Notes,
            LicenseClassFees = license.LicenseClassInfo?.ClassFees ?? 0,
            DriverId = driverId,
            PersonID = person.PersonId,
            FullName = person.FullName,
            NationalNo = person.NationalNo,
            DateOfBirth = person.DateOfBirth,
            Gender = person.Gender.ToString(),
            ImagePath = person.ImagePath
        });
    }

    // GET LICENSE DETAILS BY LICENSE ID
    public async Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(int licenseId)
    {
        var validation = LicenseValidator.ValidateId(licenseId);
        if (validation.IsFailure)
            return Result<DriverLicenseInfoDto>.FromFailure(validation.Error);

        var license = await _repository.GetLicenseByIdAsync(licenseId);
        if (license is null)
            return Result<DriverLicenseInfoDto>.FromFailure("License not found.");

        var person = license.Driver?.Person;
        if (person is null)
            return Result<DriverLicenseInfoDto>.FromFailure("Person information not found.");

        var isDetained = await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID);

        return Result<DriverLicenseInfoDto>.Success(new DriverLicenseInfoDto
        {
            LicenseId = license.LicenseID,
            LicenseClass = license.LicenseClassInfo?.ClassName ?? "Unknown",
            IssueDate = license.IssueDate,
            ExpirationDate = license.ExpirationDate,
            IsActive = license.IsActive,
            IsDetained = isDetained,
            IssueReason = ((IssueReason)license.IssueReason).ToString(),
            Notes = license.Notes,
            LicenseClassFees = license.LicenseClassInfo?.ClassFees ?? 0,
            DriverId = license.DriverID,
            PersonID = person.PersonId,
            FullName = person.FullName,
            NationalNo = person.NationalNo,
            DateOfBirth = person.DateOfBirth,
            Gender = person.Gender.ToString(),
            ImagePath = person.ImagePath
        });
    }

    // ISSUE FIRST LICENSE
    public async Task<Result<int>> IssueFirstLicenseAsync(int localAppId, string? notes)
    {
        if (localAppId <= 0)
            return Result<int>.FromFailure("Invalid local application ID.");

        // Get Local App
        var localAppResult = await _localDrivingLicenseApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);
        if (localAppResult.IsFailure)
            return Result<int>.FromFailure(localAppResult.Error);
        var localApp = localAppResult.Value!;

        // Get Application ID
        var applicationIdResult = await _localDrivingLicenseApplicationService.GetApplicationIdByLocalIdAsync(localAppId);
        if (applicationIdResult.IsFailure)
            return Result<int>.FromFailure(applicationIdResult.Error);
        var applicationId = applicationIdResult.Value;

        // Get Application
        var applicationResult = await _applicationService.GetApplicationByIdAsync(applicationId);
        if (applicationResult.IsFailure)
            return Result<int>.FromFailure(applicationResult.Error);
        var application = applicationResult.Value!;

        // Get Person
        var personResult = await _personService.GetPersonByIdAsync(application.ApplicantPersonID);
        if (personResult.IsFailure)
            return Result<int>.FromFailure(personResult.Error);
        var person = personResult.Value!;

        // Get License Class
        var licenseClassId = localApp.LicenseClassID;
        var licenseClassValidation = LicenseValidator.ValidateLicenseClassId(licenseClassId);
        if (licenseClassValidation.IsFailure)
            return Result<int>.FromFailure(licenseClassValidation.Error);

        var licenseClassResult = await _licenseClassService.GetLicenseClassByIdAsync(licenseClassId);
        if (licenseClassResult.IsFailure)
            return Result<int>.FromFailure(licenseClassResult.Error);
        var licenseClass = licenseClassResult.Value!;

        // Get or Create Driver
        var driverResult = await _driverService.GetByPersonIdAsync(person.PersonId);
        int driverId;
        if (driverResult.IsSuccess)
        {
            driverId = driverResult.Value!.DriverID;
        }
        else
        {
            var createDriverDto = new CreateDriverDto
            {
                PersonID = person.PersonId,
                CreatedByUserID = _currentUserService.UserId
            };
            var addDriverResult = await _driverService.AddAsync(createDriverDto);
            if (addDriverResult.IsFailure)
                return Result<int>.FromFailure(addDriverResult.Error);
            driverId = addDriverResult.Value;
        }

        // Create License
        var now = DateTime.UtcNow;
        var createLicenseDto = new CreateLicenseDto
        {
            ApplicationID = applicationId,
            DriverID = driverId,
            LicenseClassID = licenseClassId,
            IssueDate = now,
            ExpirationDate = now.AddYears(licenseClass.DefaultValidityLength),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PaidFees = licenseClass.LicenseClassFees,
            IsActive = true,
            IssueReason = (byte)IssueReason.FirstTime,
            CreatedByUserID = _currentUserService.UserId
        };
        var licenseResult = await AddAsync(createLicenseDto);
        if (licenseResult.IsFailure)
            return Result<int>.FromFailure(licenseResult.Error);

        // Complete Application
        var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
        if (completeResult.IsFailure)
            return Result<int>.FromFailure(completeResult.Error);

        return Result<int>.Success(licenseResult.Value);
    }

    // RENEW LICENSE
    public async Task<Result<int>> RenewLicenseAsync(int oldLicenseId, string? notes)
    {
        var validation = LicenseValidator.ValidateId(oldLicenseId);
        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        // Get & Validate Old License
        var oldLicense = await _repository.GetLicenseByIdAsync(oldLicenseId);
        if (oldLicense is null)
            return Result<int>.FromFailure("Old license not found.");
        if (!oldLicense.IsActive)
            return Result<int>.FromFailure("Cannot renew an inactive license.");
        if (oldLicense.ExpirationDate > DateTime.UtcNow)
            return Result<int>.FromFailure("Cannot renew before expiration date.");

        // Get Renewal Application Type
        const int renewalApplicationTypeId = 2;
        var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(renewalApplicationTypeId);
        if (applicationTypeResult.IsFailure)
            return Result<int>.FromFailure(applicationTypeResult.Error);
        var applicationType = applicationTypeResult.Value!;

        // Create Application
        var now = DateTime.UtcNow;
        var createApplicationDto = new CreateApplicationDto
        {
            ApplicantPersonID = oldLicense.Driver.PersonID,
            ApplicationDate = now,
            ApplicationTypeID = renewalApplicationTypeId,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = now,
            PaidFees = applicationType.ApplicationTypeFees,
            CreatedByUserID = _currentUserService.UserId
        };
        var applicationResult = await _applicationService.AddNewApplicationAsync(createApplicationDto);
        if (applicationResult.IsFailure)
            return Result<int>.FromFailure(applicationResult.Error);
        var applicationId = applicationResult.Value;

        // Create Renewed License
        var createLicenseDto = new CreateLicenseDto
        {
            ApplicationID = applicationId,
            DriverID = oldLicense.DriverID,
            LicenseClassID = oldLicense.LicenseClass,
            IssueDate = now,
            ExpirationDate = now.AddYears(oldLicense.LicenseClassInfo.DefaultValidityLength),
            PaidFees = oldLicense.LicenseClassInfo.ClassFees,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsActive = true,
            IssueReason = (byte)IssueReason.Renew,
            CreatedByUserID = _currentUserService.UserId
        };
        var licenseResult = await AddAsync(createLicenseDto);
        if (licenseResult.IsFailure)
            return Result<int>.FromFailure(licenseResult.Error);

        // Deactivate Old License
        oldLicense.IsActive = false;
        var deactivateResult = await _repository.UpdateLicenseAsync(oldLicense);
        if (!deactivateResult)
            return Result<int>.FromFailure("Failed to deactivate old license.");

        // Complete Application
        var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
        if (completeResult.IsFailure)
            return Result<int>.FromFailure(completeResult.Error);

        return Result<int>.Success(licenseResult.Value);
    }

    // REPLACE LICENSE
    public async Task<Result<int>> ReplaceLicenseAsync(int oldLicenseId, string replacementReason, int applicationTypeId)
    {
        // Validation
        var licenseValidation = LicenseValidator.ValidateId(oldLicenseId);
        if (licenseValidation.IsFailure)
            return Result<int>.FromFailure(licenseValidation.Error);
        if (string.IsNullOrWhiteSpace(replacementReason))
            return Result<int>.FromFailure("Replacement reason is required.");
        if (applicationTypeId <= 0)
            return Result<int>.FromFailure("Invalid application type ID.");

        // Get & Validate Old License
        var oldLicense = await _repository.GetLicenseByIdAsync(oldLicenseId);
        if (oldLicense is null)
            return Result<int>.FromFailure("Old license not found.");
        if (!oldLicense.IsActive)
            return Result<int>.FromFailure("Cannot replace an inactive license.");

        // Determine Issue Reason
        var normalizedReason = replacementReason.Trim();
        var issueReason = normalizedReason.Equals("Lost License", StringComparison.OrdinalIgnoreCase)
            ? IssueReason.ReplacementForLost
            : IssueReason.ReplacementForDamaged;

        // Get Application Type
        var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(applicationTypeId);
        if (applicationTypeResult.IsFailure)
            return Result<int>.FromFailure(applicationTypeResult.Error);
        var applicationType = applicationTypeResult.Value!;

        // Create Application
        var now = DateTime.UtcNow;
        var createApplicationDto = new CreateApplicationDto
        {
            ApplicantPersonID = oldLicense.Driver.PersonID,
            ApplicationDate = now,
            ApplicationTypeID = applicationTypeId,
            ApplicationStatus = AppStatus.New,
            LastStatusDate = now,
            PaidFees = applicationType.ApplicationTypeFees,
            CreatedByUserID = _currentUserService.UserId
        };
        var applicationResult = await _applicationService.AddNewApplicationAsync(createApplicationDto);
        if (applicationResult.IsFailure)
            return Result<int>.FromFailure(applicationResult.Error);
        var applicationId = applicationResult.Value;

        // Create Replacement License (keeps original expiration)
        var createLicenseDto = new CreateLicenseDto
        {
            ApplicationID = applicationId,
            DriverID = oldLicense.DriverID,
            LicenseClassID = oldLicense.LicenseClass,
            IssueDate = now,
            ExpirationDate = oldLicense.ExpirationDate,
            PaidFees = oldLicense.LicenseClassInfo.ClassFees,
            Notes = normalizedReason,
            IsActive = true,
            IssueReason = (byte)issueReason,
            CreatedByUserID = _currentUserService.UserId
        };
        var licenseResult = await AddAsync(createLicenseDto);
        if (licenseResult.IsFailure)
            return Result<int>.FromFailure(licenseResult.Error);

        // Deactivate Old License
        oldLicense.IsActive = false;
        var deactivateResult = await _repository.UpdateLicenseAsync(oldLicense);
        if (!deactivateResult)
            return Result<int>.FromFailure("Failed to deactivate old license.");

        // Complete Application
        var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
        if (completeResult.IsFailure)
            return Result<int>.FromFailure(completeResult.Error);

        return Result<int>.Success(licenseResult.Value);
    }

    // CHECKS
    public async Task<bool> IsLicenseExistsAsync(int id)
    {
        if (id <= 0) return false;
        return await _repository.IsLicenseExistsAsync(id);
    }

    public async Task<bool> IsDriverHasLicenseAsync(int driverId)
    {
        if (driverId <= 0) return false;
        return await _repository.IsDriverHasLicenseAsync(driverId);
    }

    public async Task<bool> IsApplicationHasLicenseAsync(int applicationId)
    {
        if (applicationId <= 0) return false;
        return await _repository.IsApplicationHasLicenseAsync(applicationId);
    }

    // ADD
    public async Task<Result<int>> AddAsync(CreateLicenseDto dto)
    {
        var validation = LicenseValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        var entity = MapToEntity(dto);
        var id = await _repository.AddLicenseAsync(entity);
        if (id <= 0)
            return Result<int>.FromFailure("Failed to create license.");

        return Result<int>.Success(id);
    }

    // UPDATE
    public async Task<Result> UpdateAsync(UpdateLicenseDto dto)
    {
        var validation = LicenseValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);
        if (!await _repository.IsLicenseExistsAsync(dto.LicenseID))
            return Result.Failure("License not found.");

        var entity = MapToEntity(dto);
        var success = await _repository.UpdateLicenseAsync(entity);
        return success ? Result.Success() : Result.Failure("Failed to update license.");
    }

    // DELETE
    public async Task<Result> DeleteAsync(int id)
    {
        var validation = LicenseValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);
        if (!await _repository.IsLicenseExistsAsync(id))
            return Result.Failure("License not found.");

        var success = await _repository.DeleteLicenseAsync(id);
        return success ? Result.Success() : Result.Failure("Failed to delete license.");
    }

    // ENTITY -> DTO
    private static LicenseDto MapToDto(License license)
    {
        return new LicenseDto
        {
            LicenseID = license.LicenseID,
            ApplicationID = license.ApplicationID,
            ApplicationInfo = license.Application is not null ? $"App #{license.ApplicationID}" : null,
            DriverID = license.DriverID,
            DriverName = license.Driver?.Person?.FullName,
            Driver = license.Driver is null ? null : new DriverDto
            {
                DriverID = license.Driver.DriverID,
                PersonID = license.Driver.PersonID,
                FullName = license.Driver.Person?.FullName ?? string.Empty,
                NationalNo = license.Driver.Person?.NationalNo ?? string.Empty,
                DateOfBirth = license.Driver.Person?.DateOfBirth ?? DateTime.MinValue,
                Gender = license.Driver.Person?.Gender ?? Gender.Male,
                ImagePath = license.Driver.Person?.ImagePath,
                ActiveLicenses = license.Driver.Licenses?.Count(l => l.IsActive) ?? 0,
                CreatedByUserID = license.Driver.CreatedByUserID,
                CreatedByUserName = license.Driver.CreatedByUser?.UserName ?? string.Empty,
                CreatedDate = license.Driver.CreatedDate
            },
            LicenseClassID = license.LicenseClass,
            LicenseClassName = license.LicenseClassInfo?.ClassName,
            IssueDate = license.IssueDate,
            ExpirationDate = license.ExpirationDate,
            Notes = license.Notes,
            PaidFees = license.PaidFees,
            IsActive = license.IsActive,
            IssueReason = license.IssueReason,
            IssueReasonText = ((IssueReason)license.IssueReason).ToString(),
            CreatedByUserID = license.CreatedByUserID,
            CreatedByUserName = license.CreatedByUser?.UserName ?? "Unknown"
        };
    }

    // CREATE DTO -> ENTITY
    private static License MapToEntity(CreateLicenseDto dto)
    {
        return new License
        {
            ApplicationID = dto.ApplicationID,
            DriverID = dto.DriverID,
            LicenseClass = dto.LicenseClassID,
            IssueDate = dto.IssueDate,
            ExpirationDate = dto.ExpirationDate,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            PaidFees = dto.PaidFees,
            IsActive = dto.IsActive,
            IssueReason = dto.IssueReason,
            CreatedByUserID = dto.CreatedByUserID
        };
    }

    // UPDATE DTO -> ENTITY
    private static License MapToEntity(UpdateLicenseDto dto)
    {
        return new License
        {
            LicenseID = dto.LicenseID,
            ApplicationID = dto.ApplicationID,
            DriverID = dto.DriverID,
            LicenseClass = dto.LicenseClassID,
            IssueDate = dto.IssueDate,
            ExpirationDate = dto.ExpirationDate,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            PaidFees = dto.PaidFees,
            IsActive = dto.IsActive,
            IssueReason = dto.IssueReason
        };
    }
}