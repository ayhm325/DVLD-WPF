
using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class LicenseIssuanceService : ILicenseIssuanceService
{
    private readonly ILicenseRepository _repository;
    private readonly ILocalDrivingLicenseApplicationService _localDrivingLicenseApplicationService;
    private readonly IApplicationService _applicationService;
    private readonly IDriverService _driverService;
    private readonly IPersonService _personService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILicenseClassService _licenseClassService;

    public LicenseIssuanceService(
        ILicenseRepository repository,
        ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
        IApplicationService applicationService,
        IDriverService driverService,
        IPersonService personService,
        ICurrentUserService currentUserService,
        ILicenseClassService licenseClassService)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

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

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _licenseClassService = licenseClassService
            ?? throw new ArgumentNullException(nameof(licenseClassService));
    }

    // =========================================================
    // ISSUE FIRST LICENSE
    // =========================================================

    public async Task<Result<int>> IssueFirstLicenseAsync(
        int localAppId,
        string? notes)
    {
        // =========================================================
        // 1. VALIDATE LOCAL APPLICATION ID
        // =========================================================

        var idValidation =
            LicenseValidator.ValidateId(localAppId);

        if (idValidation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                idValidation.Error);
        }

        // =========================================================
        // 2. GET LOCAL APPLICATION
        // =========================================================

        var localAppResult =
            await _localDrivingLicenseApplicationService
                .GetLocalDrivingLicenseApplicationByIdAsync(
                    localAppId);

        if (localAppResult.IsFailure)
        {
            return Result<int>.FromFailure(
                localAppResult.Error);
        }

        var localApp = localAppResult.Value!;

        // =========================================================
        // 3. VALIDATE APPLICATION STATUS
        // =========================================================

        if (localApp.ApplicationStatus != AppStatus.New)
        {
            return Result<int>.FromConflict(
                "License can only be issued for a new application.");
        }

        // =========================================================
        // 4. VALIDATE TESTS
        // =========================================================

        if (localApp.PassedTest < 3)
        {
            return Result<int>.FromConflict(
                "All required tests must be passed before issuing the license.");
        }

        // =========================================================
        // 5. PREVENT DUPLICATE LICENSE
        // =========================================================

        if (localApp.HasLicense)
        {
            return Result<int>.FromConflict(
                "A license has already been issued for this application.");
        }

        // =========================================================
        // 6. GET APPLICATION ID
        // =========================================================

        var applicationIdResult =
            await _localDrivingLicenseApplicationService
                .GetApplicationIdByLocalIdAsync(
                    localAppId);

        if (applicationIdResult.IsFailure)
        {
            return Result<int>.FromFailure(
                applicationIdResult.Error);
        }

        var applicationId =
            applicationIdResult.Value;

        // =========================================================
        // 7. GET MAIN APPLICATION
        // =========================================================

        var applicationResult =
            await _applicationService
                .GetApplicationByIdAsync(
                    applicationId);

        if (applicationResult.IsFailure)
        {
            return Result<int>.FromFailure(
                applicationResult.Error);
        }

        var application =
            applicationResult.Value!;

        // =========================================================
        // 8. VALIDATE APPLICANT
        // =========================================================

        if (application.ApplicantPersonID <= 0)
        {
            return Result<int>.FromValidationFailure(
                "The application does not have a valid applicant.");
        }

        // =========================================================
        // 9. GET PERSON
        // =========================================================

        var personResult =
            await _personService
                .GetPersonByIdAsync(
                    application.ApplicantPersonID);

        if (personResult.IsFailure)
        {
            return Result<int>.FromFailure(
                personResult.Error);
        }

        var person =
            personResult.Value!;

        // =========================================================
        // 10. VALIDATE LICENSE CLASS ID
        // =========================================================

        var licenseClassId =
            localApp.LicenseClassID;

        var licenseClassValidation =
            LicenseValidator.ValidateLicenseClassId(
                licenseClassId);

        if (licenseClassValidation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                licenseClassValidation.Error);
        }

        // =========================================================
        // 11. GET LICENSE CLASS
        // =========================================================

        var licenseClassResult =
            await _licenseClassService
                .GetLicenseClassByIdAsync(
                    licenseClassId);

        if (licenseClassResult.IsFailure)
        {
            return Result<int>.FromFailure(
                licenseClassResult.Error);
        }

        var licenseClass =
            licenseClassResult.Value!;

        // =========================================================
        // 12. VALIDATE LICENSE CLASS
        // =========================================================

        if (licenseClass.DefaultValidityLength <= 0)
        {
            return Result<int>.FromValidationFailure(
                "License class has an invalid validity period.");
        }

        if (licenseClass.LicenseClassFees < 0)
        {
            return Result<int>.FromValidationFailure(
                "License class has invalid fees.");
        }

        // =========================================================
        // 13. GET OR CREATE DRIVER
        // =========================================================

        var driverResult =
            await _driverService
                .GetByPersonIdAsync(
                    person.PersonId);

        int driverId;

        if (driverResult.IsSuccess)
        {
            var driver =
                driverResult.Value;

            if (driver is null)
            {
                return Result<int>.FromFailure(
                    "Driver information was returned incorrectly.");
            }

            driverId =
                driver.DriverID;
        }
        else
        {
            var createDriverDto =
                new CreateDriverDto
                {
                    PersonID =
                        person.PersonId,

                    CreatedByUserID =
                        _currentUserService.UserId
                };

            var createDriverResult =
                await _driverService
                    .AddAsync(createDriverDto);

            if (createDriverResult.IsFailure)
            {
                return Result<int>.FromFailure(
                    createDriverResult.Error);
            }

            driverId =
                createDriverResult.Value;
        }

        // =========================================================
        // 14. VALIDATE DRIVER ID
        // =========================================================

        if (driverId <= 0)
        {
            return Result<int>.FromFailure(
                "Failed to obtain a valid driver.");
        }

        // =========================================================
        // 15. PREPARE LICENSE DATA
        // =========================================================

        var now =
            DateTime.UtcNow;

        var expirationDate =
            now.AddYears(
                licenseClass.DefaultValidityLength);

        var normalizedNotes =
            string.IsNullOrWhiteSpace(notes)
                ? null
                : notes.Trim();

        // =========================================================
        // 16. CREATE LICENSE DTO
        // =========================================================

        var createLicenseDto =
            new CreateLicenseDto
            {
                ApplicationID =
                    applicationId,

                DriverID =
                    driverId,

                LicenseClassID =
                    licenseClassId,

                IssueDate =
                    now,

                ExpirationDate =
                    expirationDate,

                PaidFees =
                    licenseClass.LicenseClassFees,

                Notes =
                    normalizedNotes,

                IsActive =
                    true,

                IssueReason =
                    (byte)IssueReason.FirstTime,

                CreatedByUserID =
                    _currentUserService.UserId
            };

        // =========================================================
        // 17. MAP DTO TO ENTITY
        // =========================================================

        var license =
            LicenseMapper.ToEntity(
                createLicenseDto);

        // =========================================================
        // 18. CREATE LICENSE
        // =========================================================

        var newLicenseId =
            await _repository
                .AddLicenseAsync(
                    license);

        if (newLicenseId <= 0)
        {
            return Result<int>.FromFailure(
                "Failed to create the driving license.");
        }

        // =========================================================
        // 19. COMPLETE APPLICATION
        // =========================================================

        var completeResult =
            await _applicationService
                .CompleteApplicationAsync(
                    applicationId);

        if (completeResult.IsFailure)
        {
            return Result<int>.FromFailure(
                completeResult.Error);
        }

        // =========================================================
        // 20. SUCCESS
        // =========================================================

        return Result<int>.Success(
            newLicenseId);
    }
}

//using Application.Common.Results;
//using Application.DTOs;
//using Application.DTOs.DriverDTO;
//using Application.DTOs.LicenseDTO;
//using Application.Interfaces;
//using Application.Mappers;
//using Application.Validators;
//using Domain.Enums;

//namespace Application.Services;

//public class LicenseIssuanceService : ILicenseIssuanceService
//{
//    private readonly ILicenseRepository _repository;
//    private readonly ILocalDrivingLicenseApplicationService
//    _localDrivingLicenseApplicationService;


//private readonly IApplicationService _applicationService;
//    private readonly IDriverService _driverService;
//    private readonly IPersonService _personService;
//    private readonly ICurrentUserService _currentUserService;
//    private readonly ILicenseClassService _licenseClassService;

//    public LicenseIssuanceService(
//        ILicenseRepository repository,
//        ILocalDrivingLicenseApplicationService
//            localDrivingLicenseApplicationService,
//        IApplicationService applicationService,
//        IDriverService driverService,
//        IPersonService personService,
//        ICurrentUserService currentUserService,
//        ILicenseClassService licenseClassService)
//    {
//        _repository = repository
//            ?? throw new ArgumentNullException(nameof(repository));

//        _localDrivingLicenseApplicationService =
//            localDrivingLicenseApplicationService
//            ?? throw new ArgumentNullException(
//                nameof(localDrivingLicenseApplicationService));

//        _applicationService = applicationService
//            ?? throw new ArgumentNullException(nameof(applicationService));

//        _driverService = driverService
//            ?? throw new ArgumentNullException(nameof(driverService));

//        _personService = personService
//            ?? throw new ArgumentNullException(nameof(personService));

//        _currentUserService = currentUserService
//            ?? throw new ArgumentNullException(nameof(currentUserService));

//        _licenseClassService = licenseClassService
//            ?? throw new ArgumentNullException(nameof(licenseClassService));
//    }

//    public async Task<Result<int>> IssueFirstLicenseAsync(
//        int localAppId,
//        string? notes)
//    {
//        // =========================================================
//        // VALIDATE LOCAL APPLICATION ID
//        // =========================================================

//        if (localAppId <= 0)
//        {
//            return Result<int>.FromValidationFailure(
//                "Invalid local application ID.");
//        }

//        // =========================================================
//        // GET LOCAL DRIVING LICENSE APPLICATION
//        // =========================================================

//        var localAppResult =
//            await _localDrivingLicenseApplicationService
//                .GetLocalDrivingLicenseApplicationByIdAsync(localAppId);

//        if (localAppResult.IsFailure)
//        {
//            return Result<int>.FromFailure(
//                localAppResult.Error);
//        }

//        var localApp = localAppResult.Value!;

//        // =========================================================
//        // GET APPLICATION ID
//        // =========================================================

//        var applicationIdResult =
//            await _localDrivingLicenseApplicationService
//                .GetApplicationIdByLocalIdAsync(localAppId);

//        if (applicationIdResult.IsFailure)
//        {
//            return Result<int>.FromFailure(
//                applicationIdResult.Error);
//        }

//        var applicationId = applicationIdResult.Value;

//        // =========================================================
//        // GET APPLICATION
//        // =========================================================

//        var applicationResult =
//            await _applicationService
//                .GetApplicationByIdAsync(applicationId);

//        if (applicationResult.IsFailure)
//        {
//            return Result<int>.FromFailure(
//                applicationResult.Error);
//        }

//        var application = applicationResult.Value!;

//        // =========================================================
//        // GET PERSON
//        // =========================================================

//        var personResult =
//            await _personService
//                .GetPersonByIdAsync(
//                    application.ApplicantPersonID);

//        if (personResult.IsFailure)
//        {
//            return Result<int>.FromFailure(
//                personResult.Error);
//        }

//        var person = personResult.Value!;

//        // =========================================================
//        // VALIDATE LICENSE CLASS
//        // =========================================================

//        var licenseClassId =
//            localApp.LicenseClassID;

//        var licenseClassValidation =
//            LicenseValidator.ValidateLicenseClassId(
//                licenseClassId);

//        if (licenseClassValidation.IsFailure)
//        {
//            return Result<int>.FromValidationFailure(
//                licenseClassValidation.Error);
//        }

//        // =========================================================
//        // GET LICENSE CLASS
//        // =========================================================

//        var licenseClassResult =
//            await _licenseClassService
//                .GetLicenseClassByIdAsync(
//                    licenseClassId);

//        if (licenseClassResult.IsFailure)
//        {
//            return Result<int>.FromFailure(
//                licenseClassResult.Error);
//        }

//        var licenseClass = licenseClassResult.Value!;

//        // =========================================================
//        // GET OR CREATE DRIVER
//        // =========================================================

//        var driverResult =
//            await _driverService
//                .GetByPersonIdAsync(person.PersonId);

//        int driverId;

//        if (driverResult.IsSuccess)
//        {
//            driverId =
//                driverResult.Value!.DriverID;
//        }
//        else
//        {
//            var createDriverDto =
//                new CreateDriverDto
//                {
//                    PersonID = person.PersonId,
//                    CreatedByUserID =
//                        _currentUserService.UserId
//                };

//            var addDriverResult =
//                await _driverService
//                    .AddAsync(createDriverDto);

//            if (addDriverResult.IsFailure)
//            {
//                return Result<int>.FromFailure(
//                    addDriverResult.Error);
//            }

//            driverId = addDriverResult.Value;
//        }

//        // =========================================================
//        // CREATE LICENSE
//        // =========================================================

//        var now = DateTime.UtcNow;

//        var createLicenseDto =
//            new CreateLicenseDto
//            {
//                ApplicationID = applicationId,

//                DriverID = driverId,

//                LicenseClassID = licenseClassId,

//                IssueDate = now,

//                ExpirationDate =
//                    now.AddYears(
//                        licenseClass.DefaultValidityLength),

//                Notes =
//                    string.IsNullOrWhiteSpace(notes)
//                        ? null
//                        : notes.Trim(),

//                PaidFees =
//                    licenseClass.LicenseClassFees,

//                IsActive = true,

//                IssueReason =
//                    (byte)IssueReason.FirstTime,

//                CreatedByUserID =
//                    _currentUserService.UserId
//            };

//        // =========================================================
//        // ADD LICENSE
//        // =========================================================

//        var licenseResult =
//            await _repository.AddLicenseAsync(
//                LicenseMapper.ToEntity(createLicenseDto));

//        if (licenseResult <= 0)
//        {
//            return Result<int>.FromFailure(
//                "Failed to create license.");
//        }

//        // =========================================================
//        // COMPLETE APPLICATION
//        // =========================================================

//        var completeResult =
//            await _applicationService
//                .CompleteApplicationAsync(applicationId);

//        if (completeResult.IsFailure)
//        {
//            return Result<int>.FromFailure(
//                completeResult.Error);
//        }

//        // =========================================================
//        // SUCCESS
//        // =========================================================

//        return Result<int>.Success(
//            licenseResult);
//    }

//}
