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
    private const int LostReplacementApplicationTypeId = 3;
    private const int DamagedReplacementApplicationTypeId = 4;

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
        _licenseRepository = licenseRepository
            ?? throw new ArgumentNullException(nameof(licenseRepository));

        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));

        _applicationTypeService = applicationTypeService
            ?? throw new ArgumentNullException(nameof(applicationTypeService));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<int>> ReplaceLicenseAsync(
        int oldLicenseId,
        string replacementReason)
    {
        var validation = LicenseValidator.ValidateId(oldLicenseId);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (string.IsNullOrWhiteSpace(replacementReason))
        {
            return Result<int>.FromValidationFailure(
                "Replacement reason is required.");
        }

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            return Result<int>.FromFailure(
                "Authenticated user is required.");
        }

        var currentUserId = _currentUserService.UserId;
        var normalizedReason = replacementReason.Trim();

        var replacementInfo =
            GetReplacementInfo(normalizedReason);

        if (replacementInfo is null)
        {
            return Result<int>.FromValidationFailure(
                "Invalid replacement reason. " +
                "Allowed reasons are Lost License or Damaged License.");
        }

        var oldLicense =
            await _licenseRepository.GetLicenseByIdAsync(oldLicenseId);

        if (oldLicense is null)
            return Result<int>.FromNotFound("License not found.");

        if (!oldLicense.IsActive)
        {
            return Result<int>.FromConflict(
                "Cannot replace an inactive license.");
        }

        if (oldLicense.Driver is null)
        {
            return Result<int>.FromFailure(
                "The license is not associated with a valid driver.");
        }

        if (oldLicense.LicenseClassInfo is null)
        {
            return Result<int>.FromFailure(
                "The license is not associated with a valid license class.");
        }

        var applicationTypeResult =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(
                    replacementInfo.Value.ApplicationTypeId);

        if (applicationTypeResult.IsFailure)
        {
            return Result<int>.FromFailure(
                applicationTypeResult.Error);
        }

        if (applicationTypeResult.Value is null)
        {
            return Result<int>.FromNotFound(
                "Replacement application type not found.");
        }

        var applicationType = applicationTypeResult.Value;
        var now = DateTime.UtcNow;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            var createApplicationDto =
                new CreateApplicationDto
                {
                    ApplicantPersonID =
                        oldLicense.Driver.PersonID,

                    ApplicationDate =
                        now,

                    ApplicationTypeID =
                        replacementInfo.Value.ApplicationTypeId,

                    ApplicationStatus =
                        AppStatus.New,

                    LastStatusDate =
                        now,

                    PaidFees =
                        applicationType.ApplicationTypeFees
                };

            var applicationResult =
                await _applicationService
                    .AddNewApplicationAsync(
                        createApplicationDto);

            if (applicationResult.IsFailure)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    applicationResult.Error);
            }

            if (applicationResult.Value <= 0)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to create replacement application.");
            }

            oldLicense.IsActive = false;

            if (!await _licenseRepository
                    .UpdateLicenseAsync(oldLicense))
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to deactivate the old license.");
            }

            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to save the old license status.");
            }

            var createLicenseDto =
                new CreateLicenseDto
                {
                    ApplicationID =
                        applicationResult.Value,

                    DriverID =
                        oldLicense.DriverID,

                    LicenseClassID =
                        oldLicense.LicenseClass,

                    IssueDate =
                        now,

                    ExpirationDate =
                        oldLicense.ExpirationDate,

                    PaidFees =
                        oldLicense.LicenseClassInfo.ClassFees,

                    Notes =
                        normalizedReason,

                    IsActive =
                        true,

                    IssueReason =
                        (byte)replacementInfo.Value.IssueReason
                };

            var licenseValidation =
                LicenseValidator.ValidateCreate(
                    createLicenseDto);

            if (licenseValidation.IsFailure)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromValidationFailure(
                    licenseValidation.Error);
            }

            var newLicense =
                LicenseMapper.ToEntity(
                    createLicenseDto);

            newLicense.CreatedByUserID =
                currentUserId;

            await _licenseRepository
                .AddLicenseAsync(newLicense);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                newLicense.LicenseID <= 0)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to save the replacement license.");
            }

            var completeResult =
                await _applicationService
                    .CompleteApplicationAsync(
                        applicationResult.Value);

            if (completeResult.IsFailure)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    completeResult.Error);
            }

            await transaction.CommitAsync();

            return Result<int>.Success(
                newLicense.LicenseID);
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

    private static (
        int ApplicationTypeId,
        IssueReason IssueReason)?
        GetReplacementInfo(string replacementReason)
    {
        if (replacementReason.Equals(
                "Lost License",
                StringComparison.OrdinalIgnoreCase))
        {
            return (
                LostReplacementApplicationTypeId,
                IssueReason.ReplacementForLost);
        }

        if (replacementReason.Equals(
                "Damaged License",
                StringComparison.OrdinalIgnoreCase))
        {
            return (
                DamagedReplacementApplicationTypeId,
                IssueReason.ReplacementForDamaged);
        }

        return null;
    }
}