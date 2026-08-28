using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class LicenseRenewalService : ILicenseRenewalService
{
    private readonly ILicenseRepository _repository;
    private readonly IApplicationService _applicationService;
    private readonly IApplicationTypeService _applicationTypeService;
    private readonly ICurrentUserService _currentUserService;


public LicenseRenewalService(
    ILicenseRepository repository,
    IApplicationService applicationService,
    IApplicationTypeService applicationTypeService,
    ICurrentUserService currentUserService)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));

        _applicationTypeService = applicationTypeService
            ?? throw new ArgumentNullException(nameof(applicationTypeService));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<int>> RenewLicenseAsync(
        int oldLicenseId,
        string? notes)
    {
        // =========================================================
        // VALIDATE LICENSE ID
        // =========================================================

        var validation =
            LicenseValidator.ValidateId(oldLicenseId);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        // =========================================================
        // GET OLD LICENSE
        // =========================================================

        var oldLicense =
            await _repository.GetLicenseByIdAsync(
                oldLicenseId);

        if (oldLicense is null)
        {
            return Result<int>.FromNotFound(
                "Old license not found.");
        }

        // =========================================================
        // VALIDATE LICENSE STATUS
        // =========================================================

        if (!oldLicense.IsActive)
        {
            return Result<int>.FromConflict(
                "Cannot renew an inactive license.");
        }

        if (oldLicense.ExpirationDate > DateTime.UtcNow)
        {
            return Result<int>.FromConflict(
                "Cannot renew before expiration date.");
        }

        // =========================================================
        // RENEWAL APPLICATION TYPE
        // =========================================================

        const int renewalApplicationTypeId = 2;

        var applicationTypeResult =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(
                    renewalApplicationTypeId);

        if (applicationTypeResult.IsFailure)
        {
            return Result<int>.FromFailure(
                applicationTypeResult.Error);
        }

        var applicationType =
            applicationTypeResult.Value!;

        // =========================================================
        // CREATE APPLICATION
        // =========================================================

        var now = DateTime.UtcNow;

        var createApplicationDto =
            new CreateApplicationDto
            {
                ApplicantPersonID =
                    oldLicense.Driver.PersonID,

                ApplicationDate = now,

                ApplicationTypeID =
                    renewalApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate = now,

                PaidFees =
                    applicationType.ApplicationTypeFees,

                CreatedByUserID =
                    _currentUserService.UserId
            };

        var applicationResult =
            await _applicationService
                .AddNewApplicationAsync(
                    createApplicationDto);

        if (applicationResult.IsFailure)
        {
            return Result<int>.FromFailure(
                applicationResult.Error);
        }

        var applicationId =
            applicationResult.Value;

        // =========================================================
        // CREATE NEW LICENSE
        // =========================================================

        var createLicenseDto =
            new CreateLicenseDto
            {
                ApplicationID =
                    applicationId,

                DriverID =
                    oldLicense.DriverID,

                LicenseClassID =
                    oldLicense.LicenseClass,

                IssueDate =
                    now,

                ExpirationDate =
                    now.AddYears(
                        oldLicense
                            .LicenseClassInfo
                            .DefaultValidityLength),

                PaidFees =
                    oldLicense
                        .LicenseClassInfo
                        .ClassFees,

                Notes =
                    string.IsNullOrWhiteSpace(notes)
                        ? null
                        : notes.Trim(),

                IsActive = true,

                IssueReason =
                    (byte)IssueReason.Renew,

                CreatedByUserID =
                    _currentUserService.UserId
            };

        var newLicense =
            LicenseMapper.ToEntity(
                createLicenseDto);

        var newLicenseId =
            await _repository.AddLicenseAsync(
                newLicense);

        if (newLicenseId <= 0)
        {
            return Result<int>.FromFailure(
                "Failed to create renewed license.");
        }

        // =========================================================
        // DEACTIVATE OLD LICENSE
        // =========================================================

        oldLicense.IsActive = false;

        var deactivateResult =
            await _repository
                .UpdateLicenseAsync(oldLicense);

        if (!deactivateResult)
        {
            return Result<int>.FromFailure(
                "Failed to deactivate old license.");
        }

        // =========================================================
        // COMPLETE APPLICATION
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
        // SUCCESS
        // =========================================================

        return Result<int>.Success(
            newLicenseId);
    }
}
