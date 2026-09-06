using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Services;

public sealed class LicenseReplacementService
: ILicenseReplacementService
{
    private const int LostReplacementApplicationTypeId = 3;
    private const int DamagedReplacementApplicationTypeId = 4;

    private readonly ILicenseRepository _licenseRepository;
    private readonly IApplicationService _applicationService;
    private readonly IApplicationTypeService _applicationTypeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LicenseReplacementService> _logger;

    public LicenseReplacementService(
        ILicenseRepository licenseRepository,
        IApplicationService applicationService,
        IApplicationTypeService applicationTypeService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<LicenseReplacementService> logger)
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

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _logger =
            logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<int>> ReplaceLicenseAsync(
        int oldLicenseId,
        string replacementReason)
    {
        var validation =
            LicenseValidator.ValidateId(oldLicenseId);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        if (string.IsNullOrWhiteSpace(replacementReason))
        {
            return Result<int>.FromValidationFailure(
                "Replacement reason is required.");
        }

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            return Result<int>.FromForbidden(
                "Authenticated user is required.");
        }

        var replacementInfo =
            GetReplacementInfo(replacementReason.Trim());

        if (replacementInfo is null)
        {
            return Result<int>.FromValidationFailure(
                "Invalid replacement reason. " +
                "Allowed reasons are Lost License or Damaged License.");
        }

        var applicationTypeResult =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(
                    replacementInfo.Value.ApplicationTypeId);

        if (applicationTypeResult.IsFailure)
            return PropagateFailure<int>(
                applicationTypeResult);

        if (applicationTypeResult.Value is null)
        {
            return Result<int>.FromNotFound(
                "Replacement application type not found.");
        }

        var currentUserId =
            _currentUserService.UserId;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var oldLicense =
                await _licenseRepository
                    .GetLicenseByIdAsync(oldLicenseId);

            if (oldLicense is null)
            {
                return Result<int>.FromNotFound(
                    "License not found.");
            }

            if (!oldLicense.IsActive)
            {
                return Result<int>.FromConflict(
                    "Cannot replace an inactive license.");
            }

            if (oldLicense.Driver is null)
            {
                return Result<int>.FromNotFound(
                    "Driver information is not available.");
            }

            if (oldLicense.LicenseClassInfo is null)
            {
                return Result<int>.FromNotFound(
                    "License class information is not available.");
            }

            var now = DateTime.UtcNow;

            if (oldLicense.ExpirationDate <= now)
            {
                return Result<int>.FromConflict(
                    "Cannot replace an expired license.");
            }

            var createApplicationDto =
                new CreateApplicationDto
                {
                    ApplicantPersonID =
                        oldLicense.Driver.PersonID,

                    ApplicationTypeID =
                        replacementInfo.Value.ApplicationTypeId
                };

            var applicationResult =
                await _applicationService
                    .AddNewApplicationAsync(
                        createApplicationDto);

            if (applicationResult.IsFailure)
                return PropagateFailure<int>(
                    applicationResult);

            var applicationId =
                applicationResult.Value;

            if (applicationId <= 0)
            {
                return Result<int>.FromFailure(
                    "Failed to create replacement application.");
            }

            var createLicenseDto =
                new CreateLicenseDto
                {
                    ApplicationID = applicationId,
                    DriverID = oldLicense.DriverID,
                    LicenseClassID = oldLicense.LicenseClass,
                    IssueDate = now,
                    ExpirationDate =
                        oldLicense.ExpirationDate,

                    PaidFees =
                        oldLicense.LicenseClassInfo.ClassFees,

                    Notes =
                        replacementReason.Trim(),


                    IssueReason = replacementInfo.Value.IssueReason
                };

            var licenseValidation =
                LicenseValidator.ValidateCreate(
                    createLicenseDto);

            if (licenseValidation.IsFailure)
            {
                return Result<int>.FromValidationFailure(
                    licenseValidation.Error);
            }

            if (!await _licenseRepository.DeactivateLicenseAsync(oldLicenseId))
            {
                return Result<int>.FromFailure(
                    "Failed to deactivate the old license.");
            }

            var newLicense =
                LicenseMapper.ToEntity(
                    createLicenseDto);

            newLicense.CreatedByUserID =
                currentUserId;

            await _licenseRepository
                .AddLicenseAsync(newLicense);

            var saved =
                await _unitOfWork.SaveChangesAsync();

            if (saved <= 0 ||
                newLicense.LicenseID <= 0)
            {
                return Result<int>.FromFailure(
                    "Failed to save the replacement license.");
            }

           

            var completeResult =
                await _applicationService
                    .CompleteApplicationAsync(
                        applicationId);

            if (completeResult.IsFailure)
                return PropagateFailure<int>(
                    completeResult);

            await transaction.CommitAsync();

            return Result<int>.Success(
                newLicense.LicenseID);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while replacing license {LicenseId}.",
                oldLicenseId);

            try
            {
                await transaction.RollbackAsync();
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(
                    rollbackException,
                    "Rollback failed while replacing license {LicenseId}.",
                    oldLicenseId);
            }

            return Result<int>.FromFailure(
                "An unexpected error occurred while replacing the license.");
        }
    }

    private static (
        int ApplicationTypeId,
        IssueReason IssueReason)?
        GetReplacementInfo(
            string replacementReason)
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

    private static Result<T> PropagateFailure<T>(
        Result source)
    {
        return source.ErrorType switch
        {
            ErrorType.Validation =>
                Result<T>.FromValidationFailure(
                    source.Error),

            ErrorType.NotFound =>
                Result<T>.FromNotFound(
                    source.Error),

            ErrorType.Conflict =>
                Result<T>.FromConflict(
                    source.Error),

            ErrorType.Forbidden =>
                Result<T>.FromForbidden(
                    source.Error),

            _ =>
                Result<T>.FromFailure(
                    source.Error)
        };
    }
}
