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

public sealed class LicenseReplacementService(
    ILicenseRepository licenseRepository,
    IApplicationService applicationService,
    IApplicationTypeService applicationTypeService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<LicenseReplacementService> logger) : ILicenseReplacementService
{
    private const int LostReplacementApplicationTypeId = 3;
    private const int DamagedReplacementApplicationTypeId = 4;

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

    private readonly ILogger<LicenseReplacementService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<int>> ReplaceLicenseAsync(
        int oldLicenseId,
        string replacementReason)
    {
        var validation = LicenseValidator.ValidateId(oldLicenseId);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (string.IsNullOrWhiteSpace(replacementReason))
            return Result<int>.FromValidationFailure("Replacement reason is required.");

        if (!IsAuthenticated())
            return Result<int>.FromForbidden("Authenticated user is required.");

        var reason = replacementReason.Trim();
        var replacementInfo = GetReplacementInfo(reason);

        if (replacementInfo is null)
            return Result<int>.FromValidationFailure(
                "Invalid replacement reason. Allowed reasons are Lost License or Damaged License.");

        var applicationTypeResult =
            await _applicationTypeService.GetApplicationTypeByIdAsync(
                replacementInfo.Value.ApplicationTypeId);

        if (applicationTypeResult.IsFailure)
            return PropagateFailure<int>(applicationTypeResult);

        if (applicationTypeResult.Value is null)
            return Result<int>.FromNotFound("Replacement application type not found.");

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var oldLicense = await _licenseRepository.GetLicenseByIdAsync(oldLicenseId);

            if (oldLicense is null)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromNotFound("License not found."));

            if (!oldLicense.IsActive)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromConflict("Cannot replace an inactive license."));

            if (oldLicense.Driver is null)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromNotFound("Driver information is not available."));

            if (oldLicense.LicenseClassInfo is null)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromNotFound("License class information is not available."));

            var now = DateTime.UtcNow;

            if (oldLicense.ExpirationDate <= now)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromConflict("Cannot replace an expired license."));

            var applicationResult = await _applicationService.AddNewApplicationAsync(
                new CreateApplicationDto
                {
                    ApplicantPersonID = oldLicense.Driver.PersonID,
                    ApplicationTypeID = replacementInfo.Value.ApplicationTypeId
                });

            if (applicationResult.IsFailure)
                return await RollbackAsync(
                    transaction,
                    PropagateFailure<int>(applicationResult));

            var applicationId = applicationResult.Value;

            if (applicationId <= 0)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromFailure(
                        "Failed to create replacement application."));

            var createLicenseDto = new CreateLicenseDto
            {
                ApplicationID = applicationId,
                DriverID = oldLicense.DriverID,
                LicenseClassID = oldLicense.LicenseClass,
                IssueDate = now,
                ExpirationDate = oldLicense.ExpirationDate,
                PaidFees = oldLicense.LicenseClassInfo.ClassFees,
                Notes = reason,
                IssueReason = replacementInfo.Value.IssueReason
            };

            var licenseValidation = LicenseValidator.ValidateCreate(createLicenseDto);

            if (licenseValidation.IsFailure)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromValidationFailure(licenseValidation.Error));

            if (!await _licenseRepository.DeactivateLicenseAsync(oldLicenseId))
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromFailure(
                        "Failed to deactivate the old license."));

            var newLicense = LicenseMapper.ToEntity(createLicenseDto);
            newLicense.CreatedByUserID = _currentUserService.UserId;

            await _licenseRepository.AddLicenseAsync(newLicense);

            var saved = await _unitOfWork.SaveChangesAsync();

            if (saved <= 0 || newLicense.LicenseID <= 0)
                return await RollbackAsync(
                    transaction,
                    Result<int>.FromFailure(
                        "Failed to save the replacement license."));

            var completeResult =
                await _applicationService.CompleteApplicationAsync(applicationId);

            if (completeResult.IsFailure)
                return await RollbackAsync(
                    transaction,
                    PropagateFailure<int>(completeResult));

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

    private static (int ApplicationTypeId, IssueReason IssueReason)? GetReplacementInfo(
        string reason) =>
        reason.Equals("Lost License", StringComparison.OrdinalIgnoreCase)
            ? (LostReplacementApplicationTypeId, IssueReason.ReplacementForLost)
            : reason.Equals("Damaged License", StringComparison.OrdinalIgnoreCase)
                ? (DamagedReplacementApplicationTypeId, IssueReason.ReplacementForDamaged)
                : null;

    private static async Task<T> RollbackAsync<T>(
        IUnitOfWorkTransaction transaction,
        T result)
    {
        await transaction.RollbackAsync();
        return result;
    }

    private async Task RollbackSafelyAsync(
        IUnitOfWorkTransaction transaction,
        int licenseId)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Rollback failed while replacing license {LicenseId}.",
                licenseId);
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