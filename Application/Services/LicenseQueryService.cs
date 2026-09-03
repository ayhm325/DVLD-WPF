using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class LicenseQueryService : ILicenseQueryService
{
    private readonly ILicenseRepository _licenseRepository;
    private readonly ILocalDrivingLicenseApplicationService _localDrivingLicenseApplicationService;
    private readonly IApplicationService _applicationService;
    private readonly IDriverService _driverService;
    private readonly IPersonService _personService;
    private readonly IDetainedLicenseService _detainedLicenseService;


    public LicenseQueryService(
    ILicenseRepository licenseRepository,
    ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
    IApplicationService applicationService,
    IDriverService driverService,
    IPersonService personService,
    IDetainedLicenseService detainedLicenseService)
    {
        _licenseRepository = licenseRepository
            ?? throw new ArgumentNullException(nameof(licenseRepository));

        _localDrivingLicenseApplicationService =
            localDrivingLicenseApplicationService
            ?? throw new ArgumentNullException(
                nameof(localDrivingLicenseApplicationService));

        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));

        _driverService = driverService
            ?? throw new ArgumentNullException(nameof(driverService));

        _personService = personService
            ?? throw new ArgumentNullException(nameof(personService));

        _detainedLicenseService = detainedLicenseService
            ?? throw new ArgumentNullException(nameof(detainedLicenseService));
    }

    public async Task<Result<LicenseDto>> GetByIdAsync(
        int licenseId)
    {
        var validation = LicenseValidator.ValidateId(licenseId);

        if (validation.IsFailure)
        {
            return Result<LicenseDto>.FromValidationFailure(
                validation.Error);
        }

        var license =
            await _licenseRepository.GetLicenseByIdAsync(licenseId);

        if (license is null)
        {
            return Result<LicenseDto>.FromNotFound(
                "License not found.");
        }

        return Result<LicenseDto>.Success(
            LicenseMapper.ToDto(license));
    }

    public async Task<Result<List<LicenseDto>>> GetAllAsync()
    {
        var licenses =
            await _licenseRepository.GetAllLicensesAsync();

        var dtos = licenses
            .Select(LicenseMapper.ToDto)
            .ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByDriverIdAsync(
        int driverId)
    {
        var validation =
            LicenseValidator.ValidateDriverId(driverId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>.FromValidationFailure(
                validation.Error);
        }

        var licenses =
            await _licenseRepository
                .GetLicensesByDriverIdAsync(driverId);

        var dtos = licenses
            .Select(LicenseMapper.ToDto)
            .ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(
        int applicationId)
    {
        var validation =
            LicenseValidator.ValidateApplicationId(applicationId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>.FromValidationFailure(
                validation.Error);
        }

        var licenses =
            await _licenseRepository
                .GetLicensesByApplicationIdAsync(applicationId);

        var dtos = licenses
            .Select(LicenseMapper.ToDto)
            .ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(
        int licenseClassId)
    {
        var validation =
            LicenseValidator.ValidateLicenseClassId(
                licenseClassId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>.FromValidationFailure(
                validation.Error);
        }

        var licenses =
            await _licenseRepository
                .GetLicensesByLicenseClassIdAsync(
                    licenseClassId);

        var dtos = licenses
            .Select(LicenseMapper.ToDto)
            .ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>>
        GetLicensesByPersonIdAsync(int personId)
    {
        if (personId <= 0)
        {
            return Result<List<LicenseDto>>
                .FromValidationFailure(
                    "Invalid person ID.");
        }

        var licenses =
            await _licenseRepository
                .GetLicensesByPersonIdAsync(personId);

        var dtos = licenses
            .Select(LicenseMapper.ToDto)
            .ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<bool>> IsLicenseExistsAsync(
        int licenseId)
    {
        if (licenseId <= 0)
        {
            return Result<bool>.FromValidationFailure(
                "Invalid license ID.");
        }

        var exists =
            await _licenseRepository
                .IsLicenseExistsAsync(licenseId);

        return Result<bool>.Success(exists);
    }

    public async Task<Result<bool>> IsDriverHasLicenseAsync(
        int driverId)
    {
        if (driverId <= 0)
        {
            return Result<bool>.FromValidationFailure(
                "Invalid driver ID.");
        }

        var exists =
            await _licenseRepository
                .IsDriverHasLicenseAsync(driverId);

        return Result<bool>.Success(exists);
    }

    public async Task<Result<bool>> IsApplicationHasLicenseAsync(
        int applicationId)
    {
        if (applicationId <= 0)
        {
            return Result<bool>.FromValidationFailure(
                "Invalid application ID.");
        }

        var exists =
            await _licenseRepository
                .IsApplicationHasLicenseAsync(applicationId);

        return Result<bool>.Success(exists);
    }

    public async Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(
        int localAppId)
    {
        if (localAppId <= 0)
        {
            return Result<DriverLicenseInfoDto>
                .FromValidationFailure(
                    "Invalid local application ID.");
        }

        var localApplicationResult =
            await _localDrivingLicenseApplicationService
                .GetLocalDrivingLicenseApplicationByIdAsync(
                    localAppId);

        if (localApplicationResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                localApplicationResult.Error);
        }

        var localApplication =
            localApplicationResult.Value;

        if (localApplication is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound(
                "Local driving license application not found.");
        }

        var applicationIdResult =
            await _localDrivingLicenseApplicationService
                .GetApplicationIdByLocalIdAsync(localAppId);

        if (applicationIdResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                applicationIdResult.Error);
        }

        var applicationId =
            applicationIdResult.Value;

        if (applicationId <= 0)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                "Invalid application ID.");
        }

        var applicationResult =
            await _applicationService
                .GetApplicationByIdAsync(applicationId);

        if (applicationResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                applicationResult.Error);
        }

        var application =
            applicationResult.Value;

        if (application is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound(
                "Application not found.");
        }

        var personResult =
            await _personService
                .GetPersonByIdAsync(
                    application.ApplicantPersonID);

        if (personResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                personResult.Error);
        }

        var person = personResult.Value;

        if (person is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound(
                "Person information not found.");
        }

        var licenses =
            await _licenseRepository
                .GetLicensesByApplicationIdAsync(
                    applicationId);

        var license = licenses.FirstOrDefault(x =>
            x.LicenseClassInfo != null &&
            x.LicenseClassInfo.LicenseClassID ==
            localApplication.LicenseClassID);

        if (license is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound(
                "License for the selected license class was not found.");
        }

        var driverResult =
            await _driverService
                .GetByPersonIdAsync(person.PersonId);

        if (driverResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                driverResult.Error);
        }

        var driver = driverResult.Value;

        if (driver is null || driver.DriverID <= 0)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound(
                "Driver information not found.");
        }

        var isDetained =
            await _detainedLicenseService
                .IsLicenseDetainedAsync(
                    license.LicenseID);

        return Result<DriverLicenseInfoDto>.Success(
            MapLicenseDetails(
                license,
                person,
                driver.DriverID,
                isDetained));
    }

    public async Task<Result<DriverLicenseInfoDto>>
        GetLicenseDetailsByIdAsync(int licenseId)
    {
        var validation =
            LicenseValidator.ValidateId(licenseId);

        if (validation.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var license =
            await _licenseRepository
                .GetLicenseByIdAsync(licenseId);

        if (license is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound(
                "License not found.");
        }

        if (license.DriverID <= 0)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                "License has an invalid driver.");
        }

        if (license.Driver is null)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                "License driver information was not loaded.");
        }

        var personResult =
            await _personService
                .GetPersonByIdAsync(
                    license.Driver.PersonID);

        if (personResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(
                personResult.Error);
        }

        var person = personResult.Value;

        if (person is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound(
                "Person information not found.");
        }

        var isDetained =
            await _detainedLicenseService
                .IsLicenseDetainedAsync(
                    license.LicenseID);

        return Result<DriverLicenseInfoDto>.Success(
            MapLicenseDetails(
                license,
                person,
                license.DriverID,
                isDetained));
    }

    private static DriverLicenseInfoDto MapLicenseDetails(
        Domain.Entities.License license,
        PersonDto person,
        int driverId,
        bool isDetained)
    {
        return new DriverLicenseInfoDto
        {
            LicenseId = license.LicenseID,
            LicenseClass =
                license.LicenseClassInfo?.ClassName
                ?? "Unknown",
            IssueDate = license.IssueDate,
            ExpirationDate = license.ExpirationDate,
            IsActive = license.IsActive,
            IsDetained = isDetained,
            IssueReason =
                ((IssueReason)license.IssueReason).ToString(),
            Notes = license.Notes,
            LicenseClassFees =
                license.LicenseClassInfo?.ClassFees ?? 0,
            DriverId = driverId,
            PersonID = person.PersonId,
            FullName = person.FullName,
            NationalNo = person.NationalNo,
            DateOfBirth = person.DateOfBirth,
            Gender = person.Gender.ToString(),
            ImagePath = person.ImagePath
        };
    }

}
