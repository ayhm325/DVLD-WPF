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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITestWorkflowService _testWorkflowService;

    public LicenseIssuanceService(IUnitOfWork unitOfWork, ILicenseRepository repository,
        ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
        IApplicationService applicationService, IDriverService driverService, IPersonService personService,
        ICurrentUserService currentUserService, ILicenseClassService licenseClassService,
        ITestWorkflowService testWorkflowService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _localDrivingLicenseApplicationService = localDrivingLicenseApplicationService ?? throw new ArgumentNullException(nameof(localDrivingLicenseApplicationService));
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
        _personService = personService ?? throw new ArgumentNullException(nameof(personService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _licenseClassService = licenseClassService ?? throw new ArgumentNullException(nameof(licenseClassService));
        _testWorkflowService = testWorkflowService ?? throw new ArgumentNullException(nameof(testWorkflowService));
    }

    public async Task<Result<int>> IssueFirstLicenseAsync(int localAppId, string? notes)
    {
        // 1. Basic Validation & Load Local App
        var idValidation = LicenseValidator.ValidateId(localAppId);
        if (idValidation.IsFailure) return Result<int>.FromValidationFailure(idValidation.Error);

        var localAppResult = await _localDrivingLicenseApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);
        if (localAppResult.IsFailure) return Result<int>.FromFailure(localAppResult.Error);
        if (localAppResult.Value is null) return Result<int>.FromFailure("Local driving license application was not found.");

        var localApp = localAppResult.Value;

        // 2. Status & Tests Validation
        if (localApp.ApplicationStatus != AppStatus.New) return Result<int>.FromConflict("License can only be issued for a new application.");
        if (!await _testWorkflowService.HasPassedAllTestsAsync(localAppId)) return Result<int>.FromConflict("The applicant has not passed all required tests.");
        if (localApp.HasLicense) return Result<int>.FromConflict("A license has already been issued for this application.");

        // 3. User Validation
        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<int>.FromFailure("No valid logged-in user was found.");
        var currentUserId = _currentUserService.UserId;

        // 4. Load Application & Person
        var applicationIdResult = await _localDrivingLicenseApplicationService.GetApplicationIdByLocalIdAsync(localAppId);
        if (applicationIdResult.IsFailure) return Result<int>.FromFailure(applicationIdResult.Error);

        var appValidation = LicenseValidator.ValidateApplicationId(applicationIdResult.Value);
        if (appValidation.IsFailure) return Result<int>.FromValidationFailure(appValidation.Error);

        var applicationResult = await _applicationService.GetApplicationByIdAsync(applicationIdResult.Value);
        if (applicationResult.IsFailure) return Result<int>.FromFailure(applicationResult.Error);
        if (applicationResult.Value is null) return Result<int>.FromFailure("Application was not found.");

        var application = applicationResult.Value;
        if (application.ApplicantPersonID <= 0) return Result<int>.FromValidationFailure("The application does not have a valid applicant.");

        var personResult = await _personService.GetPersonByIdAsync(application.ApplicantPersonID);
        if (personResult.IsFailure) return Result<int>.FromFailure(personResult.Error);
        if (personResult.Value is null) return Result<int>.FromFailure("Applicant person was not found.");

        // 5. License Class Validation
        var classValidation = LicenseValidator.ValidateLicenseClassId(localApp.LicenseClassID);
        if (classValidation.IsFailure) return Result<int>.FromValidationFailure(classValidation.Error);

        var licenseClassResult = await _licenseClassService.GetLicenseClassByIdAsync(localApp.LicenseClassID);
        if (licenseClassResult.IsFailure) return Result<int>.FromFailure(licenseClassResult.Error);
        if (licenseClassResult.Value is null) return Result<int>.FromFailure("License class was not found.");

        var licenseClass = licenseClassResult.Value;
        if (licenseClass.DefaultValidityLength <= 0) return Result<int>.FromValidationFailure("License class has an invalid validity period.");
        if (licenseClass.LicenseClassFees < 0) return Result<int>.FromValidationFailure("License class has invalid fees.");

        // 6. Get or Create Driver
        int driverId;
        var driverResult = await _driverService.GetByPersonIdAsync(personResult.Value.PersonId);

        if (driverResult.IsSuccess)
        {
            if (driverResult.Value is null || driverResult.Value.DriverID <= 0)
                return Result<int>.FromFailure("Driver information was returned incorrectly.");
            driverId = driverResult.Value.DriverID;
        }
        else
        {
            if (!string.Equals(driverResult.Error, "Driver not found.", StringComparison.Ordinal))
                return Result<int>.FromFailure(driverResult.Error);

            var createDriverResult = await _driverService.AddAsync(new CreateDriverDto
            {
                PersonID = personResult.Value.PersonId,
                CreatedByUserID = currentUserId
            });

            if (createDriverResult.IsFailure) return Result<int>.FromFailure(createDriverResult.Error);
            driverId = createDriverResult.Value;
        }

        var driverValidation = LicenseValidator.ValidateDriverId(driverId);
        if (driverValidation.IsFailure) return Result<int>.FromValidationFailure(driverValidation.Error);

        // 7. Prepare & Validate License DTO
        var applicationId = applicationIdResult.Value;
        var createLicenseDto = new CreateLicenseDto
        {
            ApplicationID = applicationId,
            DriverID = driverId,
            LicenseClassID = localApp.LicenseClassID,
            IssueDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(licenseClass.DefaultValidityLength),
            PaidFees = licenseClass.LicenseClassFees,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsActive = true,
            IssueReason = (byte)IssueReason.FirstTime,
            CreatedByUserID = currentUserId
        };

        var licenseValidation = LicenseValidator.ValidateCreate(createLicenseDto);
        if (licenseValidation.IsFailure) return Result<int>.FromValidationFailure(licenseValidation.Error);

        // 8. Transaction: Create License & Complete Application
        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var license = LicenseMapper.ToEntity(createLicenseDto);
            await _repository.AddLicenseAsync(license);

            if (await _unitOfWork.SaveChangesAsync() <= 0 || license.LicenseID <= 0)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure("Failed to save the driving license.");
            }

            var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
            if (completeResult.IsFailure)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure(completeResult.Error);
            }

            await transaction.CommitAsync();
            return Result<int>.Success(license.LicenseID);
        }
        catch (Exception ex)
        {
            try { await transaction.RollbackAsync(); } catch { /* Preserve original exception */ }
            return Result<int>.FromFailure(BuildLicenseIssuanceError(ex));
        }
    }

    private static string BuildLicenseIssuanceError(Exception exception)
    {
        var messages = new List<string>();
        var current = exception;

        while (current is not null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message)) messages.Add(current.Message);
            current = current.InnerException;
        }

        return messages.Count == 0
            ? "An unexpected error occurred while issuing the license."
            : string.Join(Environment.NewLine, messages.Distinct());
    }
}