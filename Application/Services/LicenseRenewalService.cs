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
    private const int RenewalApplicationTypeId = 2;

    private readonly ILicenseRepository _repository;
    private readonly IApplicationService _applicationService;
    private readonly IApplicationTypeService _applicationTypeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public LicenseRenewalService(
        ILicenseRepository repository,
        IApplicationService applicationService,
        IApplicationTypeService applicationTypeService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _applicationTypeService = applicationTypeService
            ?? throw new ArgumentNullException(nameof(applicationTypeService));
        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<int>> RenewLicenseAsync(
        int oldLicenseId,
        string? notes)
    {
        var validation = LicenseValidator.ValidateId(oldLicenseId);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<int>.FromFailure(
                "Authenticated user is required.");

        var oldLicense = await _repository.GetLicenseByIdAsync(oldLicenseId);

        if (oldLicense is null)
            return Result<int>.FromNotFound("Old license not found.");

        if (!oldLicense.IsActive)
            return Result<int>.FromConflict(
                "Cannot renew an inactive license.");

        var now = DateTime.UtcNow;

        if (oldLicense.ExpirationDate > now)
            return Result<int>.FromConflict(
                "Cannot renew before expiration date.");

        if (oldLicense.Driver is null)
            return Result<int>.FromNotFound(
                "Driver information is not available.");

        if (oldLicense.LicenseClassInfo is null)
            return Result<int>.FromNotFound(
                "License class information is not available.");

        var applicationTypeResult =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(RenewalApplicationTypeId);

        if (applicationTypeResult.IsFailure)
            return Result<int>.FromFailure(
                applicationTypeResult.Error);

        if (applicationTypeResult.Value is null)
            return Result<int>.FromNotFound(
                "Renewal application type not found.");

        var applicationType = applicationTypeResult.Value;

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync();

        try
        {
            var createApplicationDto = new CreateApplicationDto
            {
                ApplicantPersonID = oldLicense.Driver.PersonID,
                ApplicationDate = now,
                ApplicationTypeID = RenewalApplicationTypeId,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = now,
                PaidFees = applicationType.ApplicationTypeFees
            };

            var applicationResult =
                await _applicationService
                    .AddNewApplicationAsync(createApplicationDto);

            if (applicationResult.IsFailure)
                return Result<int>.FromFailure(
                    applicationResult.Error);

            var applicationId = applicationResult.Value;

            if (applicationId <= 0)
                return Result<int>.FromFailure(
                    "Failed to create renewal application.");

            var createLicenseDto = new CreateLicenseDto
            {
                ApplicationID = applicationId,
                DriverID = oldLicense.DriverID,
                LicenseClassID = oldLicense.LicenseClass,
                IssueDate = now,
                ExpirationDate = now.AddYears(
                    oldLicense.LicenseClassInfo.DefaultValidityLength),
                PaidFees = oldLicense.LicenseClassInfo.ClassFees,
                Notes = string.IsNullOrWhiteSpace(notes)
                    ? null
                    : notes.Trim(),
                IsActive = true,
                IssueReason = (byte)IssueReason.Renew
            };

            var licenseValidation =
                LicenseValidator.ValidateCreate(createLicenseDto);

            if (licenseValidation.IsFailure)
                return Result<int>.FromValidationFailure(
                    licenseValidation.Error);

            oldLicense.IsActive = false;

            if (!await _repository.UpdateLicenseAsync(oldLicense))
                return Result<int>.FromFailure(
                    "Failed to deactivate old license.");

            if (await _unitOfWork.SaveChangesAsync() <= 0)
                return Result<int>.FromFailure(
                    "Failed to save old license changes.");

            var newLicense = LicenseMapper.ToEntity(createLicenseDto);
            newLicense.CreatedByUserID = _currentUserService.UserId;

            await _repository.AddLicenseAsync(newLicense);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                newLicense.LicenseID <= 0)
            {
                return Result<int>.FromFailure(
                    "Failed to save the renewed license.");
            }

            var completeResult =
                await _applicationService
                    .CompleteApplicationAsync(applicationId);

            if (completeResult.IsFailure)
                return Result<int>.FromFailure(
                    completeResult.Error);

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
                $"Failed to renew license: {ex.Message}");
        }
    }
}
