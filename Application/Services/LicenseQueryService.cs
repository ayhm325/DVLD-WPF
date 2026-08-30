using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
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
        _licenseRepository = licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
        _localDrivingLicenseApplicationService = localDrivingLicenseApplicationService ?? throw new ArgumentNullException(nameof(localDrivingLicenseApplicationService));
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
        _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        _detainedLicenseService = detainedLicenseService ?? throw new ArgumentNullException(nameof(detainedLicenseService));
    }

    // =========================================================
    // GET DETAILS BY LOCAL APPLICATION
    // =========================================================

    public async Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(int localAppId)
    {
        // Validate ID
        if (localAppId <= 0)
        {
            return Result<DriverLicenseInfoDto>.FromValidationFailure("Invalid local application ID.");
        }

        // Get Local Application
        var localApplicationResult = await _localDrivingLicenseApplicationService
            .GetLocalDrivingLicenseApplicationByIdAsync(localAppId);

        if (localApplicationResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(localApplicationResult.Error);
        }

        var localApplication = localApplicationResult.Value;

        if (localApplication is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("Local driving license application not found.");
        }

        // Get Main Application ID
        var applicationIdResult = await _localDrivingLicenseApplicationService
            .GetApplicationIdByLocalIdAsync(localAppId);

        if (applicationIdResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(applicationIdResult.Error);
        }

        var applicationId = applicationIdResult.Value;

        if (applicationId <= 0)
        {
            return Result<DriverLicenseInfoDto>.FromFailure("Invalid application ID.");
        }

        // Get Main Application
        var applicationResult = await _applicationService.GetApplicationByIdAsync(applicationId);

        if (applicationResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(applicationResult.Error);
        }

        var application = applicationResult.Value;

        if (application is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("Application not found.");
        }

        // Get Person
        var personResult = await _personService.GetPersonByIdAsync(application.ApplicantPersonID);

        if (personResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(personResult.Error);
        }

        var person = personResult.Value;

        if (person is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("Person information not found.");
        }

        // Get License
        var licenses = await _licenseRepository.GetLicensesByApplicationIdAsync(applicationId);

        var license = licenses.FirstOrDefault(x =>
            x.LicenseClassInfo != null &&
            x.LicenseClassInfo.LicenseClassID == localApplication.LicenseClassID);

        if (license is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("License for the selected license class was not found.");
        }

        // Get Driver
        var driverResult = await _driverService.GetByPersonIdAsync(person.PersonId);

        if (driverResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(driverResult.Error);
        }

        var driver = driverResult.Value;

        if (driver is null || driver.DriverID <= 0)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("Driver information not found.");
        }

        // Check Detention
        var isDetained = await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID);

        // Map
        return Result<DriverLicenseInfoDto>.Success(
            MapLicenseDetails(license, person, driver.DriverID, isDetained));
    }

    // =========================================================
    // GET DETAILS BY LICENSE ID
    // =========================================================

    public async Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(int licenseId)
    {
        // Validate License ID
        var validation = LicenseValidator.ValidateId(licenseId);

        if (validation.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromValidationFailure(validation.Error);
        }

        // Get License
        var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);

        if (license is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("License not found.");
        }

        // Validate Driver
        if (license.DriverID <= 0)
        {
            return Result<DriverLicenseInfoDto>.FromFailure("License has an invalid driver.");
        }

        // Get Person
        var personResult = await _personService.GetPersonByIdAsync(license.Driver!.PersonID);

        if (personResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(personResult.Error);
        }

        var person = personResult.Value;

        if (person is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("Person information not found.");
        }

        // Check Detention
        var isDetained = await _detainedLicenseService.IsLicenseDetainedAsync(license.LicenseID);

        // Map
        return Result<DriverLicenseInfoDto>.Success(
            MapLicenseDetails(license, person, license.DriverID, isDetained));
    }

    // =========================================================
    // MAPPING
    // =========================================================

    private static DriverLicenseInfoDto MapLicenseDetails(
        Domain.Entities.License license,
        PersonDto person,
        int driverId,
        bool isDetained)
    {
        return new DriverLicenseInfoDto
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
        };
    }
}