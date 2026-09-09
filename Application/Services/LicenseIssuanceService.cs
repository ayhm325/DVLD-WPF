using System.Data;
using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class LicenseIssuanceService(
    IUnitOfWork unitOfWork,
    ILicenseRepository licenseRepository,
    ILocalDrivingLicenseApplicationService localApplicationService,
    IApplicationService applicationService,
    IDriverService driverService,
    IPersonService personService,
    ICurrentUserService currentUserService,
    ILicenseClassService licenseClassService,
    ITestWorkflowService testWorkflowService,
    ILogger<LicenseIssuanceService> logger) : ILicenseIssuanceService
{
    private const int NewLocalDrivingLicenseApplicationTypeId = 1;

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILicenseRepository _licenseRepository =
        licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
    private readonly ILocalDrivingLicenseApplicationService _localApplicationService =
        localApplicationService ?? throw new ArgumentNullException(nameof(localApplicationService));
    private readonly IApplicationService _applicationService =
        applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    private readonly IDriverService _driverService =
        driverService ?? throw new ArgumentNullException(nameof(driverService));
    private readonly IPersonService _personService =
        personService ?? throw new ArgumentNullException(nameof(personService));
    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    private readonly ILicenseClassService _licenseClassService =
        licenseClassService ?? throw new ArgumentNullException(nameof(licenseClassService));
    private readonly ITestWorkflowService _testWorkflowService =
        testWorkflowService ?? throw new ArgumentNullException(nameof(testWorkflowService));
    private readonly ILogger<LicenseIssuanceService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<int>> IssueFirstLicenseAsync(int localAppId, string? notes)
    {
        var validation = LicenseValidator.ValidateId(localAppId);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<int>.FromForbidden("Authenticated user is required.");

        var localApplicationResult = await _localApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);
        if (localApplicationResult.IsFailure)
            return PropagateFailure<int>(localApplicationResult);
        if (localApplicationResult.Value is null)
            return Result<int>.FromNotFound("Local driving license application was not found.");

        var localApplication = localApplicationResult.Value;

        var applicationIdResult = await _localApplicationService.GetApplicationIdByLocalIdAsync(localAppId);
        if (applicationIdResult.IsFailure)
            return PropagateFailure<int>(applicationIdResult);

        var applicationId = applicationIdResult.Value;
        var applicationIdValidation = LicenseValidator.ValidateApplicationId(applicationId);
        if (applicationIdValidation.IsFailure)
            return Result<int>.FromValidationFailure(applicationIdValidation.Error);

        var applicationResult = await _applicationService.GetApplicationByIdAsync(applicationId);
        if (applicationResult.IsFailure)
            return PropagateFailure<int>(applicationResult);
        if (applicationResult.Value is null)
            return Result<int>.FromNotFound("Application was not found.");

        var application = applicationResult.Value;
        if (application.ApplicationTypeID != NewLocalDrivingLicenseApplicationTypeId)
            return Result<int>.FromConflict("First-time license issuance is only allowed for a new local driving license application.");

        if (application.ApplicantPersonID <= 0)
            return Result<int>.FromValidationFailure("The application does not have a valid applicant.");

        var personResult = await _personService.GetPersonByIdAsync(application.ApplicantPersonID);
        if (personResult.IsFailure)
            return PropagateFailure<int>(personResult);
        if (personResult.Value is null)
            return Result<int>.FromNotFound("Applicant person was not found.");

        var person = personResult.Value;

        var licenseClassValidation = LicenseValidator.ValidateLicenseClassId(localApplication.LicenseClassID);
        if (licenseClassValidation.IsFailure)
            return Result<int>.FromValidationFailure(licenseClassValidation.Error);

        var licenseClassResult = await _licenseClassService.GetLicenseClassByIdAsync(localApplication.LicenseClassID);
        if (licenseClassResult.IsFailure)
            return PropagateFailure<int>(licenseClassResult);
        if (licenseClassResult.Value is null)
            return Result<int>.FromNotFound("License class was not found.");

        var licenseClass = licenseClassResult.Value;
        if (licenseClass.DefaultValidityLength <= 0)
            return Result<int>.FromValidationFailure("License class has an invalid validity period.");

        if (licenseClass.LicenseClassFees < 0)
            return Result<int>.FromValidationFailure("License class has invalid fees.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var currentApplicationResult = await _applicationService.GetApplicationByIdAsync(applicationId);
            if (currentApplicationResult.IsFailure)
                return await RollbackAsync(transaction, PropagateFailure<int>(currentApplicationResult));
            if (currentApplicationResult.Value is null)
                return await RollbackAsync(transaction, Result<int>.FromNotFound("Application was not found."));

            var currentApplication = currentApplicationResult.Value;
            if (currentApplication.ApplicationStatus != AppStatus.New)
                return await RollbackAsync(transaction, Result<int>.FromConflict("The application is not in a valid state for license issuance."));

            if (!await _testWorkflowService.HasPassedAllTestsAsync(localAppId))
                return await RollbackAsync(transaction, Result<int>.FromConflict("The applicant has not passed all required tests."));

            if (await _licenseRepository.IsApplicationHasLicenseAsync(applicationId))
                return await RollbackAsync(transaction, Result<int>.FromConflict("A license has already been issued for this application."));

            var driverResult = await _driverService.GetByPersonIdAsync(person.PersonId);
            int driverId;

            if (driverResult.IsSuccess)
            {
                var driver = driverResult.Value;
                if (driver is null || driver.DriverID <= 0)
                    return await RollbackAsync(transaction, Result<int>.FromFailure("Driver information was returned incorrectly."));
                driverId = driver.DriverID;
            }
            else
            {
                if (driverResult.ErrorType != ErrorType.NotFound)
                    return await RollbackAsync(transaction, PropagateFailure<int>(driverResult));

                var createDriverResult = await _driverService.AddAsync(new CreateDriverDto { PersonID = person.PersonId });
                if (createDriverResult.IsFailure)
                    return await RollbackAsync(transaction, PropagateFailure<int>(createDriverResult));

                driverId = createDriverResult.Value;
            }

            var driverValidation = LicenseValidator.ValidateDriverId(driverId);
            if (driverValidation.IsFailure)
                return await RollbackAsync(transaction, Result<int>.FromValidationFailure(driverValidation.Error));

            if (await _licenseRepository.IsActiveLicenseExistsAsync(driverId, localApplication.LicenseClassID))
                return await RollbackAsync(transaction, Result<int>.FromConflict("The driver already has an active license for this license class."));

            var issueDate = DateTime.UtcNow;
            var createLicenseDto = new CreateLicenseDto
            {
                ApplicationID = applicationId,
                DriverID = driverId,
                LicenseClassID = localApplication.LicenseClassID,
                IssueDate = issueDate,
                ExpirationDate = issueDate.AddYears(licenseClass.DefaultValidityLength),
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                PaidFees = licenseClass.LicenseClassFees,
                IssueReason = IssueReason.FirstTime
            };

            var licenseValidation = LicenseValidator.ValidateCreate(createLicenseDto);
            if (licenseValidation.IsFailure)
                return await RollbackAsync(transaction, Result<int>.FromValidationFailure(licenseValidation.Error));

            var license = LicenseMapper.ToEntity(createLicenseDto);
            license.CreatedByUserID = _currentUserService.UserId;
            await _licenseRepository.AddLicenseAsync(license);

            var saved = await _unitOfWork.SaveChangesAsync();
            if (saved <= 0 || license.LicenseID <= 0)
                return await RollbackAsync(transaction, Result<int>.FromFailure("Failed to save the driving license."));

            var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
            if (completeResult.IsFailure)
                return await RollbackAsync(transaction, PropagateFailure<int>(completeResult));

            await transaction.CommitAsync();
            return Result<int>.Success(license.LicenseID);
        }
        catch (Exception)
        {
            await RollbackSafelyAsync(transaction, localAppId);
            throw;
        }
    }

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn && _currentUserService.UserId > 0;

    private static async Task<T> RollbackAsync<T>(dynamic transaction, T result)
    {
        await transaction.RollbackAsync();
        return result;
    }

    private async Task RollbackSafelyAsync(dynamic transaction, int localAppId)
    {
        try { await transaction.RollbackAsync(); }
        catch (Exception rollbackException)
        {
            _logger.LogError(rollbackException, "Rollback failed while issuing first license for LocalApplicationId {LocalApplicationId}.", localAppId);
        }
    }

    private static Result<T> PropagateFailure<T>(Result source) => source.ErrorType switch
    {
        ErrorType.Validation => Result<T>.FromValidationFailure(source.Error),
        ErrorType.NotFound => Result<T>.FromNotFound(source.Error),
        ErrorType.Conflict => Result<T>.FromConflict(source.Error),
        ErrorType.Forbidden => Result<T>.FromForbidden(source.Error),
        _ => Result<T>.FromFailure(source.Error)
    };
}