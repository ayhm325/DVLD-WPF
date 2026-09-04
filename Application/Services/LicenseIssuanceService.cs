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
    private const int NewLocalDrivingLicenseApplicationTypeId = 1;

    private readonly ILicenseRepository _repository;
    private readonly ILocalDrivingLicenseApplicationService _localDrivingLicenseApplicationService;
    private readonly IApplicationService _applicationService;
    private readonly IDriverService _driverService;
    private readonly IPersonService _personService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILicenseClassService _licenseClassService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITestWorkflowService _testWorkflowService;

    public LicenseIssuanceService(
        IUnitOfWork unitOfWork,
        ILicenseRepository repository,
        ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
        IApplicationService applicationService,
        IDriverService driverService,
        IPersonService personService,
        ICurrentUserService currentUserService,
        ILicenseClassService licenseClassService,
        ITestWorkflowService testWorkflowService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _localDrivingLicenseApplicationService = localDrivingLicenseApplicationService
            ?? throw new ArgumentNullException(nameof(localDrivingLicenseApplicationService));
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
        _testWorkflowService = testWorkflowService
            ?? throw new ArgumentNullException(nameof(testWorkflowService));
    }

    public async Task<Result<int>> IssueFirstLicenseAsync(int localAppId, string? notes)
    {
        var validation = LicenseValidator.ValidateId(localAppId);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<int>.FromFailure("Authenticated user is required.");

        var localAppResult = await _localDrivingLicenseApplicationService
            .GetLocalDrivingLicenseApplicationByIdAsync(localAppId);

        if (localAppResult.IsFailure)
            return Result<int>.FromFailure(localAppResult.Error);

        if (localAppResult.Value is null)
            return Result<int>.FromNotFound(
                "Local driving license application was not found.");

        var localApp = localAppResult.Value;

        if (localApp.ApplicationStatus != AppStatus.New)
            return Result<int>.FromConflict(
                "License can only be issued for a new application.");

        if (!await _testWorkflowService.HasPassedAllTestsAsync(localAppId))
            return Result<int>.FromConflict(
                "The applicant has not passed all required tests.");

        var applicationIdResult = await _localDrivingLicenseApplicationService
            .GetApplicationIdByLocalIdAsync(localAppId);

        if (applicationIdResult.IsFailure)
            return Result<int>.FromFailure(applicationIdResult.Error);

        var applicationId = applicationIdResult.Value;

        var applicationIdValidation =
            LicenseValidator.ValidateApplicationId(applicationId);

        if (applicationIdValidation.IsFailure)
            return Result<int>.FromValidationFailure(applicationIdValidation.Error);

        var applicationResult =
            await _applicationService.GetApplicationByIdAsync(applicationId);

        if (applicationResult.IsFailure)
            return Result<int>.FromFailure(applicationResult.Error);

        if (applicationResult.Value is null)
            return Result<int>.FromNotFound("Application was not found.");

        var application = applicationResult.Value;

        if (application.ApplicationTypeID != NewLocalDrivingLicenseApplicationTypeId)
            return Result<int>.FromConflict(
                "First-time license issuance is only allowed for a new local driving license application.");

        if (application.ApplicationStatus != AppStatus.New)
            return Result<int>.FromConflict(
                "The application is not in a valid state for license issuance.");

        if (application.ApplicantPersonID <= 0)
            return Result<int>.FromValidationFailure(
                "The application does not have a valid applicant.");

        var personResult =
            await _personService.GetPersonByIdAsync(application.ApplicantPersonID);

        if (personResult.IsFailure)
            return Result<int>.FromFailure(personResult.Error);

        if (personResult.Value is null)
            return Result<int>.FromNotFound("Applicant person was not found.");

        var classValidation =
            LicenseValidator.ValidateLicenseClassId(localApp.LicenseClassID);

        if (classValidation.IsFailure)
            return Result<int>.FromValidationFailure(classValidation.Error);

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(localApp.LicenseClassID);

        if (licenseClassResult.IsFailure)
            return Result<int>.FromFailure(licenseClassResult.Error);

        if (licenseClassResult.Value is null)
            return Result<int>.FromNotFound("License class was not found.");

        var licenseClass = licenseClassResult.Value;

        if (licenseClass.DefaultValidityLength <= 0)
            return Result<int>.FromValidationFailure(
                "License class has an invalid validity period.");

        if (licenseClass.LicenseClassFees < 0)
            return Result<int>.FromValidationFailure(
                "License class has invalid fees.");

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            if (await _repository.IsApplicationHasLicenseAsync(applicationId))
                return Result<int>.FromConflict(
                    "A license has already been issued for this application.");

            var driverResult =
                await _driverService.GetByPersonIdAsync(personResult.Value.PersonId);

            int driverId;

            if (driverResult.IsSuccess)
            {
                if (driverResult.Value is null || driverResult.Value.DriverID <= 0)
                    return Result<int>.FromFailure(
                        "Driver information was returned incorrectly.");

                driverId = driverResult.Value.DriverID;
            }
            else
            {
                if (driverResult.ErrorType != ErrorType.NotFound)
                    return Result<int>.FromFailure(driverResult.Error);

                var createDriverResult =
                    await _driverService.AddAsync(
                        new CreateDriverDto
                        {
                            PersonID = personResult.Value.PersonId
                        });

                if (createDriverResult.IsFailure)
                    return Result<int>.FromFailure(createDriverResult.Error);

                driverId = createDriverResult.Value;
            }

            var driverValidation =
                LicenseValidator.ValidateDriverId(driverId);

            if (driverValidation.IsFailure)
                return Result<int>.FromValidationFailure(driverValidation.Error);

            if (await _repository.IsActiveLicenseExistsAsync(
                    driverId, localApp.LicenseClassID))
                return Result<int>.FromConflict(
                    "The driver already has an active license for this license class.");

            var now = DateTime.UtcNow;

            var createLicenseDto = new CreateLicenseDto
            {
                ApplicationID = applicationId,
                DriverID = driverId,
                LicenseClassID = localApp.LicenseClassID,
                IssueDate = now,
                ExpirationDate =
                    now.AddYears(licenseClass.DefaultValidityLength),
                PaidFees = licenseClass.LicenseClassFees,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                IsActive = true,
                IssueReason = (byte)IssueReason.FirstTime
            };

            var licenseValidation =
                LicenseValidator.ValidateCreate(createLicenseDto);

            if (licenseValidation.IsFailure)
                return Result<int>.FromValidationFailure(
                    licenseValidation.Error);

            var license = LicenseMapper.ToEntity(createLicenseDto);
            license.CreatedByUserID = _currentUserService.UserId;

            await _repository.AddLicenseAsync(license);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                license.LicenseID <= 0)
                return Result<int>.FromFailure(
                    "Failed to save the driving license.");

            var completeResult =
                await _applicationService.CompleteApplicationAsync(applicationId);

            if (completeResult.IsFailure)
                return Result<int>.FromFailure(completeResult.Error);

            await transaction.CommitAsync();

            return Result<int>.Success(license.LicenseID);
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // Preserve the original exception.
            }

            return Result<int>.FromFailure(
                BuildLicenseIssuanceError(ex));
        }
    }

    private static string BuildLicenseIssuanceError(Exception exception)
    {
        var messages = new List<string>();

        for (var current = exception;
             current is not null;
             current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
                messages.Add(current.Message);
        }

        return messages.Count == 0
            ? "An unexpected error occurred while issuing the license."
            : string.Join(Environment.NewLine, messages.Distinct());
    }
}