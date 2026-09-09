using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Data;

namespace Application.Services;

public sealed class LicenseRenewalService(
    ILicenseRepository licenseRepository,
    IApplicationService applicationService,
    IApplicationTypeService applicationTypeService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<LicenseRenewalService> logger) : ILicenseRenewalService
{
    private const int RenewalApplicationTypeId = 2;

    private readonly ILicenseRepository _licenseRepository =
        licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
    private readonly IApplicationService _applicationService =
        applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    private readonly IApplicationTypeService _applicationTypeService =
        applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));
    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<LicenseRenewalService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<int>> RenewLicenseAsync(int oldLicenseId, string? notes)
    {
        var validation = LicenseValidator.ValidateId(oldLicenseId);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<int>.FromForbidden("Authenticated user is required.");

        var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(RenewalApplicationTypeId);
        if (applicationTypeResult.IsFailure)
            return PropagateFailure<int>(applicationTypeResult);
        if (applicationTypeResult.Value is null)
            return Result<int>.FromNotFound("Renewal application type not found.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var oldLicense = await _licenseRepository.GetLicenseByIdAsync(oldLicenseId);
            if (oldLicense is null)
                return await RollbackAsync(transaction, Result<int>.FromNotFound("Old license not found."));

            if (!oldLicense.IsActive)
                return await RollbackAsync(transaction, Result<int>.FromConflict("Cannot renew an inactive license."));

            var now = DateTime.UtcNow;
            if (oldLicense.ExpirationDate > now)
                return await RollbackAsync(transaction, Result<int>.FromConflict("Cannot renew before expiration date."));

            if (oldLicense.Driver is null)
                return await RollbackAsync(transaction, Result<int>.FromNotFound("Driver information is not available."));

            if (oldLicense.LicenseClassInfo is null)
                return await RollbackAsync(transaction, Result<int>.FromNotFound("License class information is not available."));

            var licenseClass = oldLicense.LicenseClassInfo;
            if (licenseClass.DefaultValidityLength <= 0)
                return await RollbackAsync(transaction, Result<int>.FromValidationFailure("License class has an invalid validity period."));

            if (licenseClass.ClassFees < 0)
                return await RollbackAsync(transaction, Result<int>.FromValidationFailure("License class has invalid fees."));

            var applicationResult = await _applicationService.AddNewApplicationAsync(new CreateApplicationDto
            {
                ApplicantPersonID = oldLicense.Driver.PersonID,
                ApplicationTypeID = RenewalApplicationTypeId
            });

            if (applicationResult.IsFailure)
                return await RollbackAsync(transaction, PropagateFailure<int>(applicationResult));

            var applicationId = applicationResult.Value;
            if (applicationId <= 0)
                return await RollbackAsync(transaction, Result<int>.FromFailure("Failed to create renewal application."));

            var createLicenseDto = new CreateLicenseDto
            {
                ApplicationID = applicationId,
                DriverID = oldLicense.DriverID,
                LicenseClassID = oldLicense.LicenseClass,
                IssueDate = now,
                ExpirationDate = now.AddYears(licenseClass.DefaultValidityLength),
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                PaidFees = licenseClass.ClassFees,
                IssueReason = IssueReason.Renew
            };

            var licenseValidation = LicenseValidator.ValidateCreate(createLicenseDto);
            if (licenseValidation.IsFailure)
                return await RollbackAsync(transaction, Result<int>.FromValidationFailure(licenseValidation.Error));

            if (!await _licenseRepository.DeactivateLicenseAsync(oldLicenseId))
                return await RollbackAsync(transaction, Result<int>.FromFailure("Failed to deactivate old license."));

            var newLicense = LicenseMapper.ToEntity(createLicenseDto);
            newLicense.CreatedByUserID = _currentUserService.UserId;
            await _licenseRepository.AddLicenseAsync(newLicense);

            var saved = await _unitOfWork.SaveChangesAsync();
            if (saved <= 0 || newLicense.LicenseID <= 0)
                return await RollbackAsync(transaction, Result<int>.FromFailure("Failed to save the renewed license."));

            var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
            if (completeResult.IsFailure)
                return await RollbackAsync(transaction, PropagateFailure<int>(completeResult));

            await transaction.CommitAsync();
            return Result<int>.Success(newLicense.LicenseID);
        }
        catch (Exception)
        {
            await RollbackSafelyAsync(transaction, oldLicenseId);
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

    private async Task RollbackSafelyAsync(dynamic transaction, int licenseId)
    {
        try { await transaction.RollbackAsync(); }
        catch (Exception rollbackException)
        {
            _logger.LogError(rollbackException, "Rollback failed while renewing license {LicenseId}.", licenseId);
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