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
    private readonly IUnitOfWork _unitOfWork;

    public LicenseReplacementService(
        ILicenseRepository licenseRepository,
        IApplicationService applicationService,
        IApplicationTypeService applicationTypeService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _licenseRepository = licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _applicationTypeService = applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<int>> ReplaceLicenseAsync(
        int oldLicenseId,
        string replacementReason,
        int applicationTypeId)
    {
        var validation = LicenseValidator.ValidateId(oldLicenseId);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (string.IsNullOrWhiteSpace(replacementReason))
            return Result<int>.FromValidationFailure("Replacement reason is required.");

        if (applicationTypeId <= 0)
            return Result<int>.FromValidationFailure("Invalid application type ID.");

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<int>.FromFailure("Authenticated user is required.");

        var currentUserId = _currentUserService.UserId;
        var normalizedReason = replacementReason.Trim();

        var oldLicense = await _licenseRepository.GetLicenseByIdAsync(oldLicenseId);

        if (oldLicense is null)
            return Result<int>.FromNotFound("License not found.");

        if (!oldLicense.IsActive)
            return Result<int>.FromConflict("Cannot replace an inactive license.");

        if (oldLicense.Driver is null)
            return Result<int>.FromFailure(
                "The license is not associated with a valid driver.");

        if (oldLicense.LicenseClassInfo is null)
            return Result<int>.FromFailure(
                "The license is not associated with a valid license class.");

        var issueReason =
            normalizedReason.Equals(
                "Lost License",
                StringComparison.OrdinalIgnoreCase)
                ? IssueReason.ReplacementForLost
                : normalizedReason.Equals(
                    "Damaged License",
                    StringComparison.OrdinalIgnoreCase)
                    ? IssueReason.ReplacementForDamaged
                    : (IssueReason?)null;

        if (issueReason is null)
        {
            return Result<int>.FromValidationFailure(
                "Invalid replacement reason. Allowed reasons are Lost License or Damaged License.");
        }

        var applicationTypeResult =
            await _applicationTypeService.GetApplicationTypeByIdAsync(
                applicationTypeId);

        if (applicationTypeResult.IsFailure)
            return Result<int>.FromFailure(applicationTypeResult.Error);

        if (applicationTypeResult.Value is null)
            return Result<int>.FromNotFound("Application type not found.");

        var applicationType = applicationTypeResult.Value;
        var now = DateTime.UtcNow;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            var createApplicationDto = new CreateApplicationDto
            {
                ApplicantPersonID = oldLicense.Driver.PersonID,
                ApplicationDate = now,
                ApplicationTypeID = applicationTypeId,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = now,
                PaidFees = applicationType.ApplicationTypeFees
            };

            var applicationResult =
                await _applicationService.AddNewApplicationAsync(
                    createApplicationDto);

            if (applicationResult.IsFailure)
                return Result<int>.FromFailure(applicationResult.Error);

            if (applicationResult.Value <= 0)
                return Result<int>.FromFailure(
                    "Failed to create replacement application.");

            // Deactivate the old license first.
            // The database allows only one active license
            // per Driver + LicenseClass.
            oldLicense.IsActive = false;

            if (!await _licenseRepository.UpdateLicenseAsync(oldLicense))
            {
                return Result<int>.FromFailure(
                    "Failed to deactivate the old license.");
            }

            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                return Result<int>.FromFailure(
                    "Failed to save the old license status.");
            }

            var createLicenseDto = new CreateLicenseDto
            {
                ApplicationID = applicationResult.Value,
                DriverID = oldLicense.DriverID,
                LicenseClassID = oldLicense.LicenseClass,
                IssueDate = now,
                ExpirationDate = oldLicense.ExpirationDate,
                PaidFees = oldLicense.LicenseClassInfo.ClassFees,
                Notes = normalizedReason,
                IsActive = true,
                IssueReason = (byte)issueReason.Value
            };

            var licenseValidation =
                LicenseValidator.ValidateCreate(createLicenseDto);

            if (licenseValidation.IsFailure)
            {
                return Result<int>.FromValidationFailure(
                    licenseValidation.Error);
            }

            var newLicense = LicenseMapper.ToEntity(createLicenseDto);
            newLicense.CreatedByUserID = currentUserId;

            await _licenseRepository.AddLicenseAsync(newLicense);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                newLicense.LicenseID <= 0)
            {
                return Result<int>.FromFailure(
                    "Failed to save the replacement license.");
            }

            var completeResult =
                await _applicationService.CompleteApplicationAsync(
                    applicationResult.Value);

            if (completeResult.IsFailure)
                return Result<int>.FromFailure(completeResult.Error);

            await transaction.CommitAsync();

            return Result<int>.Success(newLicense.LicenseID);
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
            }

            return Result<int>.FromFailure(
                $"Failed to replace license: {ex.Message}");
        }
    }
}