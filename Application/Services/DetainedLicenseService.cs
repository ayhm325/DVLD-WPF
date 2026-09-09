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

    public async Task<Result<List<DetainedLicenseDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return Result<List<DetainedLicenseDto>>.Success(
            entities.Select(DetainedLicenseMapper.ToDto).ToList());
    }

    public async Task<Result<DetainedLicenseDto>> GetByIdAsync(int id)
    {
        var validation = DetainedLicenseValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<DetainedLicenseDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        return entity is null
            ? Result<DetainedLicenseDto>.FromNotFound("Detained license not found.")
            : Result<DetainedLicenseDto>.Success(DetainedLicenseMapper.ToDto(entity));
    }

    public async Task<Result<DetainedLicenseDto>> GetActiveDetainByLicenseIdAsync(int licenseId)
    {
        var validation = DetainedLicenseValidator.ValidateLicenseId(licenseId);
        if (validation.IsFailure)
            return Result<DetainedLicenseDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetActiveDetainByLicenseIdAsync(licenseId);
        return entity is null
            ? Result<DetainedLicenseDto>.FromNotFound("No active detention found for this license.")
            : Result<DetainedLicenseDto>.Success(DetainedLicenseMapper.ToDto(entity));
    }

    public async Task<bool> IsLicenseDetainedAsync(int licenseId) =>
        licenseId > 0 && await _repository.IsLicenseDetainedAsync(licenseId);

    public async Task<Result<DetainedLicenseDto>> AddAsync(CreateDetainedLicenseDto dto)
    {
        var validation = DetainedLicenseValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<DetainedLicenseDto>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<DetainedLicenseDto>.FromForbidden("Authenticated user is required.");

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var license = await _licenseRepository.GetLicenseByIdAsync(dto.LicenseID);
            if (license is null)
                return await RollbackAsync(transaction, Result<DetainedLicenseDto>.FromNotFound("License not found."));

            if (!license.IsActive)
                return await RollbackAsync(transaction,
                    Result<DetainedLicenseDto>.FromConflict("Only an active license can be detained."));

            if (license.ExpirationDate <= DateTime.UtcNow)
                return await RollbackAsync(transaction,
                    Result<DetainedLicenseDto>.FromConflict("An expired license cannot be detained."));

            if (await _repository.IsLicenseDetainedAsync(dto.LicenseID))
                return await RollbackAsync(transaction,
                    Result<DetainedLicenseDto>.FromConflict("License is already detained."));

            var entity = DetainedLicenseMapper.ToEntity(dto, _currentUserService.UserId, DateTime.UtcNow);
            await _repository.AddAsync(entity);

            if (!await _licenseRepository.DeactivateLicenseAsync(license.LicenseID))
                return await RollbackAsync(transaction,
                    Result<DetainedLicenseDto>.FromFailure("Failed to deactivate the license."));

            var saved = await _unitOfWork.SaveChangesAsync();
            if (saved <= 0 || entity.DetainID <= 0)
                return await RollbackAsync(transaction,
                    Result<DetainedLicenseDto>.FromFailure("Failed to save detained license."));

            await transaction.CommitAsync();

            var savedEntity = await _repository.GetByIdAsync(entity.DetainID);
            return savedEntity is null
                ? Result<DetainedLicenseDto>.FromFailure("Unable to retrieve created detained license.")
                : Result<DetainedLicenseDto>.Success(DetainedLicenseMapper.ToDto(savedEntity));
        }
        catch (Exception)
        {
            await RollbackSafelyAsync(transaction, "detaining", dto.LicenseID);
            throw;
        }
    }

    public async Task<Result> ReleaseAsync(ReleaseDetainedLicenseDto dto)
    {
        var validation = DetainedLicenseValidator.ValidateRelease(dto);
        if (validation.IsFailure)
            return validation;

        if (!IsAuthenticated())
            return Result.Forbidden("Authenticated user is required.");

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var detention = await _repository.GetByIdForUpdateAsync(dto.DetainID);
            if (detention is null)
                return await RollbackAsync(transaction, Result.NotFound("Detained license not found."));

            if (detention.IsReleased)
                return await RollbackAsync(transaction, Result.Conflict("License is already released."));

            var license = await _licenseRepository.GetLicenseByIdAsync(detention.LicenseID);
            if (license is null)
                return await RollbackAsync(transaction, Result.NotFound("Associated license not found."));

            var applicationType =
                await _applicationTypeService.GetApplicationTypeByIdAsync(ReleaseDetainedApplicationTypeId);
            if (applicationType.IsFailure)
                return await RollbackAsync(transaction, Result.FromFailure(applicationType));
            if (applicationType.Value is null)
                return await RollbackAsync(transaction, Result.NotFound("Release application type not found."));

            if (license.Driver is null)
                return await RollbackAsync(transaction, Result.NotFound("Driver information is not available."));

            var applicationResult = await _applicationService.AddNewApplicationAsync(
                new CreateApplicationDto
                {
                    ApplicantPersonID = license.Driver.PersonID,
                    ApplicationTypeID = ReleaseDetainedApplicationTypeId
                });

            if (applicationResult.IsFailure)
                return await RollbackAsync(transaction, Result.FromFailure(applicationResult));

            var applicationId = applicationResult.Value;
            if (applicationId <= 0)
                return await RollbackAsync(transaction, Result.Failure("Failed to create release application."));

            detention.IsReleased = true;
            detention.ReleaseDate = DateTime.UtcNow;
            detention.ReleasedByUserID = _currentUserService.UserId;
            detention.ReleaseApplicationID = applicationId;

            var hasAnotherActiveLicense = await _licenseRepository.HasAnotherActiveLicenseAsync(
                license.DriverID, license.LicenseClass, license.LicenseID);

            if (!hasAnotherActiveLicense &&
                license.ExpirationDate > DateTime.UtcNow &&
                !await _licenseRepository.ActivateLicenseAsync(license.LicenseID))
            {
                return await RollbackAsync(transaction, Result.Failure("Failed to restore the license state."));
            }

            var saved = await _unitOfWork.SaveChangesAsync();
            if (saved <= 0)
                return await RollbackAsync(transaction, Result.Failure("Failed to save license release."));

            var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
            if (completeResult.IsFailure)
                return await RollbackAsync(transaction, completeResult);

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception)
        {
            await RollbackSafelyAsync(transaction, "releasing detention", dto.DetainID);
            throw;
        }
    }

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn && _currentUserService.UserId > 0;

    private async Task<T> RollbackAsync<T>(IUnitOfWorkTransaction transaction, T result)
    {
        await transaction.RollbackAsync();
        return result;
    }

    private async Task RollbackSafelyAsync(IUnitOfWorkTransaction transaction, string operation, int id)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(rollbackException, "Rollback failed while {Operation} {Id}.", operation, id);
        }
    }
}