using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class LicenseReplacementService : ILicenseReplacementService
{
    private readonly ILicenseRepository _licenseRepository;
    private readonly IApplicationService _applicationService;
    private readonly IApplicationTypeService _applicationTypeService;
    private readonly ICurrentUserService _currentUserService;

    public LicenseReplacementService(
        ILicenseRepository licenseRepository,
        IApplicationService applicationService,
        IApplicationTypeService applicationTypeService,
        ICurrentUserService currentUserService)
    {
        _licenseRepository =
            licenseRepository
            ?? throw new ArgumentNullException(nameof(licenseRepository));

        _applicationService =
            applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));

        _applicationTypeService =
            applicationTypeService
            ?? throw new ArgumentNullException(nameof(applicationTypeService));

        _currentUserService =
            currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<int>> ReplaceLicenseAsync(
        int oldLicenseId,
        string replacementReason,
        int applicationTypeId)
    {
        // =========================================================
        // 1. VALIDATE LICENSE ID
        // =========================================================

        var licenseValidation =
            LicenseValidator.ValidateId(oldLicenseId);

        if (licenseValidation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                licenseValidation.Error);
        }

        // =========================================================
        // 2. VALIDATE REPLACEMENT REASON
        // =========================================================

        if (string.IsNullOrWhiteSpace(replacementReason))
        {
            return Result<int>.FromValidationFailure(
                "Replacement reason is required.");
        }

        var normalizedReason =
            replacementReason.Trim();

        // =========================================================
        // 3. VALIDATE APPLICATION TYPE ID
        // =========================================================

        if (applicationTypeId <= 0)
        {
            return Result<int>.FromValidationFailure(
                "Invalid application type ID.");
        }

        // =========================================================
        // 4. GET OLD LICENSE
        // =========================================================

        var oldLicense =
            await _licenseRepository
                .GetLicenseByIdAsync(oldLicenseId);

        if (oldLicense is null)
        {
            return Result<int>.FromNotFound(
                "License not found.");
        }

        // =========================================================
        // 5. VALIDATE LICENSE STATUS
        // =========================================================

        if (!oldLicense.IsActive)
        {
            return Result<int>.FromConflict(
                "Cannot replace an inactive license.");
        }

        // =========================================================
        // 6. VALIDATE REQUIRED RELATIONSHIPS
        // =========================================================

        if (oldLicense.Driver is null)
        {
            return Result<int>.FromFailure(
                "The license is not associated with a valid driver.");
        }

        if (oldLicense.Driver.Person is null)
        {
            return Result<int>.FromFailure(
                "The driver is not associated with a valid person.");
        }

        if (oldLicense.LicenseClassInfo is null)
        {
            return Result<int>.FromFailure(
                "The license is not associated with a valid license class.");
        }

        // =========================================================
        // 7. VALIDATE REPLACEMENT REASON
        // =========================================================

        IssueReason issueReason;

        if (normalizedReason.Equals(
                "Lost License",
                StringComparison.OrdinalIgnoreCase))
        {
            issueReason =
                IssueReason.ReplacementForLost;
        }
        else if (normalizedReason.Equals(
                     "Damaged License",
                     StringComparison.OrdinalIgnoreCase))
        {
            issueReason =
                IssueReason.ReplacementForDamaged;
        }
        else
        {
            return Result<int>.FromValidationFailure(
                "Invalid replacement reason. " +
                "Allowed reasons are Lost License or Damaged License.");
        }

        // =========================================================
        // 8. GET APPLICATION TYPE
        // =========================================================

        var applicationTypeResult =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(
                    applicationTypeId);

        if (applicationTypeResult.IsFailure)
        {
            return Result<int>.FromFailure(
                applicationTypeResult.Error);
        }

        var applicationType =
            applicationTypeResult.Value;

        if (applicationType is null)
        {
            return Result<int>.FromNotFound(
                "Application type not found.");
        }

        // =========================================================
        // 9. CURRENT DATE/TIME
        // =========================================================

        var now = DateTime.UtcNow;

        // =========================================================
        // 10. CREATE REPLACEMENT APPLICATION
        // =========================================================

        var createApplicationDto =
            new CreateApplicationDto
            {
                ApplicantPersonID =
                    oldLicense.Driver.PersonID,

                ApplicationDate =
                    now,

                ApplicationTypeID =
                    applicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    now,

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

        if (applicationId <= 0)
        {
            return Result<int>.FromFailure(
                "Failed to create replacement application.");
        }

        // =========================================================
        // 11. CREATE NEW LICENSE
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

                // Replacement does NOT extend validity.
                // It keeps the original expiration date.
                ExpirationDate =
                    oldLicense.ExpirationDate,

                PaidFees =
                    oldLicense
                        .LicenseClassInfo
                        .ClassFees,

                Notes =
                    normalizedReason,

                IsActive =
                    true,

                IssueReason =
                    (byte)issueReason,

                CreatedByUserID =
                    _currentUserService.UserId
            };

        var newLicense =
            LicenseMapper.ToEntity(
                createLicenseDto);

        var newLicenseId =
            await _licenseRepository
                .AddLicenseAsync(
                    newLicense);

        if (newLicenseId <= 0)
        {
            return Result<int>.FromFailure(
                "Failed to create replacement license.");
        }

        // =========================================================
        // 12. DEACTIVATE OLD LICENSE
        // =========================================================

        oldLicense.IsActive = false;

        var deactivateResult =
            await _licenseRepository
                .UpdateLicenseAsync(
                    oldLicense);

        if (!deactivateResult)
        {
            return Result<int>.FromFailure(
                "Failed to deactivate the old license.");
        }

        // =========================================================
        // 13. COMPLETE APPLICATION
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
        // 14. SUCCESS
        // =========================================================

        return Result<int>.Success(
            newLicenseId);
    }
}