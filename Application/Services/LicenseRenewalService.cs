
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
    private readonly IUnitOfWork _unitOfWork;

    public LicenseRenewalService(
        ILicenseRepository repository,
        IApplicationService applicationService,
        IApplicationTypeService applicationTypeService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(nameof(repository));

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
    }


    // =========================================================
    // RENEW LICENSE
    // =========================================================

    public async Task<Result<int>> RenewLicenseAsync(
        int oldLicenseId,
        string? notes)
    {
        // -----------------------------------------------------
        // 1. Validate license ID
        // -----------------------------------------------------

        var validation =
            LicenseValidator.ValidateId(oldLicenseId);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }


        // -----------------------------------------------------
        // 2. Get old license
        // -----------------------------------------------------

        var oldLicense =
            await _repository.GetLicenseByIdAsync(
                oldLicenseId);

        if (oldLicense is null)
        {
            return Result<int>.FromNotFound(
                "Old license not found.");
        }


        // -----------------------------------------------------
        // 3. Validate old license
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // 4. Renewal application type
        // -----------------------------------------------------

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

        if (applicationTypeResult.Value is null)
        {
            return Result<int>.FromNotFound(
                "Renewal application type not found.");
        }

        var applicationType =
            applicationTypeResult.Value;


        // -----------------------------------------------------
        // 5. Current date/user
        // -----------------------------------------------------

        var now =
            DateTime.UtcNow;

        var currentUserId =
            _currentUserService.UserId;


        // -----------------------------------------------------
        // 6. Begin transaction
        // -----------------------------------------------------

        await using var transaction =
            await _unitOfWork
                .BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // 7. Create renewal application
            // -------------------------------------------------

            var createApplicationDto =
                new CreateApplicationDto
                {
                    ApplicantPersonID =
                        oldLicense.Driver.PersonID,

                    ApplicationDate =
                        now,

                    ApplicationTypeID =
                        renewalApplicationTypeId,

                    ApplicationStatus =
                        AppStatus.New,

                    LastStatusDate =
                        now,

                    PaidFees =
                        applicationType.ApplicationTypeFees,

                    CreatedByUserID =
                        currentUserId
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
                    "Failed to create renewal application.");
            }

            var applicationId =
                applicationResult.Value;


            // -------------------------------------------------
            // 8. Create new license
            // -------------------------------------------------

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

                    IsActive =
                        true,

                    IssueReason =
                        (byte)IssueReason.Renew,

                    CreatedByUserID =
                        currentUserId
                };

            var newLicense =
                LicenseMapper.ToEntity(
                    createLicenseDto);


            // -------------------------------------------------
            // 9. Add new license
            // -------------------------------------------------

            await _repository
                .AddLicenseAsync(
                    newLicense);


            // -------------------------------------------------
            // 10. Save new license
            // -------------------------------------------------

            var licenseSaved =
                await _unitOfWork
                    .SaveChangesAsync();

            if (licenseSaved <= 0 ||
                newLicense.LicenseID <= 0)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to save the replacement license.");
            }


            // -------------------------------------------------
            // 11. Deactivate old license
            // -------------------------------------------------

            oldLicense.IsActive = false;

            var deactivateResult =
                await _repository
                    .UpdateLicenseAsync(
                        oldLicense);

            if (!deactivateResult)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to deactivate old license.");
            }


            // -------------------------------------------------
            // 12. Save old license state
            // -------------------------------------------------

            var oldLicenseSaved =
                await _unitOfWork
                    .SaveChangesAsync();

            if (oldLicenseSaved <= 0)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to save old license changes.");
            }


            // -------------------------------------------------
            // 13. Complete application
            // -------------------------------------------------

            var completeResult =
                await _applicationService
                    .CompleteApplicationAsync(
                        applicationId);

            if (completeResult.IsFailure)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    completeResult.Error);
            }


            // -------------------------------------------------
            // 14. Commit transaction
            // -------------------------------------------------

            await transaction
                .CommitAsync();


            // -------------------------------------------------
            // 15. Return new license ID
            // -------------------------------------------------

            return Result<int>.Success(
                newLicense.LicenseID);
        }
        catch (Exception ex)
        {
            await transaction
                .RollbackAsync();

            return Result<int>.FromFailure(
                $"Failed to renew license: {ex.Message}");
        }
    }
}