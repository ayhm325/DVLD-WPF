using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Services;

public sealed class DetainedLicenseService(
    IDetainedLicenseRepository repository,
    ILicenseRepository licenseRepository,
    IApplicationService applicationService,
    IApplicationTypeService applicationTypeService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<DetainedLicenseService> logger) : IDetainedLicenseService
{
    private const int ReleaseDetainedApplicationTypeId = 5;

    private readonly IDetainedLicenseRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly ILicenseRepository _licenseRepository =
        licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));

    private readonly IApplicationService _applicationService =
        applicationService ?? throw new ArgumentNullException(nameof(applicationService));

    private readonly IApplicationTypeService _applicationTypeService =
        applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    private readonly ILogger<DetainedLicenseService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<DetainedLicenseDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();

        return Result<List<DetainedLicenseDto>>.Success(
            entities.Select(DetainedLicenseMapper.ToDto).ToList());
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<DetainedLicenseDto>> GetByIdAsync(int id)
    {
        var validation = DetainedLicenseValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<DetainedLicenseDto>.FromValidationFailure(
                validation.Error);
        }

        var entity = await _repository.GetByIdAsync(id);

        return entity is null
            ? Result<DetainedLicenseDto>.FromNotFound(
                "Detained license not found.")
            : Result<DetainedLicenseDto>.Success(
                DetainedLicenseMapper.ToDto(entity));
    }

    // =========================================================
    // GET ACTIVE DETENTION
    // =========================================================

    public async Task<Result<DetainedLicenseDto>>
        GetActiveDetainByLicenseIdAsync(int licenseId)
    {
        var validation =
            DetainedLicenseValidator.ValidateLicenseId(licenseId);

        if (validation.IsFailure)
        {
            return Result<DetainedLicenseDto>.FromValidationFailure(
                validation.Error);
        }

        var entity =
            await _repository.GetActiveDetainByLicenseIdAsync(licenseId);

        return entity is null
            ? Result<DetainedLicenseDto>.FromNotFound(
                "No active detention found for this license.")
            : Result<DetainedLicenseDto>.Success(
                DetainedLicenseMapper.ToDto(entity));
    }

    // =========================================================
    // CHECK DETENTION
    // =========================================================

    public async Task<bool> IsLicenseDetainedAsync(int licenseId) =>
        licenseId > 0 &&
        await _repository.IsLicenseDetainedAsync(licenseId);

    // =========================================================
    // ADD DETENTION
    // =========================================================

    public async Task<Result<DetainedLicenseDto>> AddAsync(
        CreateDetainedLicenseDto dto)
    {
        var validation =
            DetainedLicenseValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<DetainedLicenseDto>.FromValidationFailure(
                validation.Error);
        }

        if (!IsAuthenticated())
        {
            return Result<DetainedLicenseDto>.FromForbidden(
                "Authenticated user is required.");
        }

        var license =
            await _licenseRepository.GetLicenseByIdAsync(dto.LicenseID);

        if (license is null)
        {
            return Result<DetainedLicenseDto>.FromNotFound(
                "License not found.");
        }

        if (!license.IsActive)
        {
            return Result<DetainedLicenseDto>.FromConflict(
                "Only an active license can be detained.");
        }

        if (license.ExpirationDate <= DateTime.UtcNow)
        {
            return Result<DetainedLicenseDto>.FromConflict(
                "An expired license cannot be detained.");
        }

        if (await _repository.IsLicenseDetainedAsync(dto.LicenseID))
        {
            return Result<DetainedLicenseDto>.FromConflict(
                "License is already detained.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var now = DateTime.UtcNow;

            var entity = DetainedLicenseMapper.ToEntity(
                dto,
                _currentUserService.UserId,
                now);

            await _repository.AddAsync(entity);

            license.IsActive = false;

            if (!await _licenseRepository.UpdateLicenseAsync(license))
            {
                return Result<DetainedLicenseDto>.FromFailure(
                    "Failed to deactivate the license.");
            }

            var saved = await _unitOfWork.SaveChangesAsync();

            if (saved <= 0 || entity.DetainID <= 0)
            {
                return Result<DetainedLicenseDto>.FromFailure(
                    "Failed to save detained license.");
            }

            await transaction.CommitAsync();

            var savedEntity =
                await _repository.GetByIdAsync(entity.DetainID);

            return savedEntity is null
                ? Result<DetainedLicenseDto>.FromFailure(
                    "Unable to retrieve created detained license.")
                : Result<DetainedLicenseDto>.Success(
                    DetainedLicenseMapper.ToDto(savedEntity));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error detaining license {LicenseId}.",
                dto.LicenseID);

            await RollbackSafelyAsync(
                transaction,
                "detaining",
                dto.LicenseID);

            return Result<DetainedLicenseDto>.FromFailure(
                "An unexpected error occurred while detaining the license.");
        }
    }

    // =========================================================
    // RELEASE
    // =========================================================

    public async Task<Result> ReleaseAsync(
        ReleaseDetainedLicenseDto dto)
    {
        var validation =
            DetainedLicenseValidator.ValidateRelease(dto);

        if (validation.IsFailure)
        {
            return validation;
        }

        if (!IsAuthenticated())
        {
            return Result.Forbidden(
                "Authenticated user is required.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var detention =
                await _repository.GetByIdForUpdateAsync(
                    dto.DetainID);

            if (detention is null)
            {
                return Result.NotFound(
                    "Detained license not found.");
            }

            if (detention.IsReleased)
            {
                return Result.Conflict(
                    "License is already released.");
            }

            var license =
                await _licenseRepository.GetLicenseByIdAsync(
                    detention.LicenseID);

            if (license is null)
            {
                return Result.NotFound(
                    "Associated license not found.");
            }

            var applicationType =
                await _applicationTypeService
                    .GetApplicationTypeByIdAsync(
                        ReleaseDetainedApplicationTypeId);

            if (applicationType.IsFailure)
            {
                return Result.FromFailure(applicationType);
            }

            var applicationResult =
                await _applicationService.AddNewApplicationAsync(
                    new CreateApplicationDto
                    {
                        ApplicantPersonID =
                            license.Driver.PersonID,

                        ApplicationTypeID =
                            ReleaseDetainedApplicationTypeId
                    });

            if (applicationResult.IsFailure)
            {
                return Result.FromFailure(applicationResult);
            }

            var applicationId = applicationResult.Value;

            if (applicationId <= 0)
            {
                return Result.Failure(
                    "Failed to create release application.");
            }

            detention.IsReleased = true;
            detention.ReleaseDate = DateTime.UtcNow;
            detention.ReleasedByUserID =
                _currentUserService.UserId;
            detention.ReleaseApplicationID =
                applicationId;

            var hasAnotherActiveLicense =
                await _licenseRepository.HasAnotherActiveLicenseAsync(
                    license.DriverID,
                    license.LicenseClass,
                    license.LicenseID);

            if (!hasAnotherActiveLicense)
            {
                license.IsActive =
                    license.ExpirationDate > DateTime.UtcNow;

                if (!await _licenseRepository.UpdateLicenseAsync(license))
                {
                    return Result.Failure(
                        "Failed to restore the license state.");
                }
            }

            var saved = await _unitOfWork.SaveChangesAsync();

            if (saved <= 0)
            {
                return Result.Failure(
                    "Failed to save license release.");
            }

            var completeResult =
                await _applicationService
                    .CompleteApplicationAsync(applicationId);

            if (completeResult.IsFailure)
            {
                return completeResult;
            }

            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error releasing detained license {DetainId}.",
                dto.DetainID);

            await RollbackSafelyAsync(
                transaction,
                "releasing detention",
                dto.DetainID);

            return Result.Failure(
                "An unexpected error occurred while releasing the license.");
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn &&
        _currentUserService.UserId > 0;

    private async Task RollbackSafelyAsync(
        IUnitOfWorkTransaction transaction,
        string operation,
        int id)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(
                rollbackException,
                "Rollback failed while {Operation} {Id}.",
                operation,
                id);
        }
    }
}