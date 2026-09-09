using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Application.Services;

public sealed class InternationalService(
    IInternationalRepository repository,
    ILicenseQueryService licenseQueryService,
    IApplicationService applicationService,
    IApplicationTypeService applicationTypeService,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork,
    ILogger<InternationalService> logger) : IInternationalService
{
    private const int InternationalApplicationTypeId = 6;
    private const int OrdinaryLicenseClassId = 3;
    private const int InternationalLicenseValidityYears = 1;

    private readonly IInternationalRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILicenseQueryService _licenseQueryService =
        licenseQueryService ?? throw new ArgumentNullException(nameof(licenseQueryService));
    private readonly IApplicationService _applicationService =
        applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    private readonly IApplicationTypeService _applicationTypeService =
        applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));
    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<InternationalService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<List<InternationalDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return Result<List<InternationalDto>>.Success(entities.Select(InternationalLicenseMapper.ToDto).ToList());
    }

    public async Task<Result<InternationalDto>> GetByIdAsync(int internationalLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateId(internationalLicenseId);
        if (validation.IsFailure)
            return Result<InternationalDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(internationalLicenseId);
        return entity is null
            ? Result<InternationalDto>.FromNotFound("International license not found.")
            : Result<InternationalDto>.Success(InternationalLicenseMapper.ToDto(entity));
    }

    public async Task<Result<List<InternationalDto>>> GetByDriverIdAsync(int driverId)
    {
        var validation = InternationalLicenseValidator.ValidateDriverId(driverId);
        if (validation.IsFailure)
            return Result<List<InternationalDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByDriverIdAsync(driverId);
        return Result<List<InternationalDto>>.Success(entities.Select(InternationalLicenseMapper.ToDto).ToList());
    }

    public async Task<Result<InternationalDto>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = InternationalLicenseValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<InternationalDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByApplicationIdAsync(applicationId);
        return entity is null
            ? Result<InternationalDto>.FromNotFound("International license not found.")
            : Result<InternationalDto>.Success(InternationalLicenseMapper.ToDto(entity));
    }

    public async Task<Result<List<InternationalDto>>> GetByLocalLicenseIdAsync(int localLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateLocalLicenseId(localLicenseId);
        if (validation.IsFailure)
            return Result<List<InternationalDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByLocalLicenseIdAsync(localLicenseId);
        return Result<List<InternationalDto>>.Success(entities.Select(InternationalLicenseMapper.ToDto).ToList());
    }

    public Task<bool> HasActiveInternationalLicenseAsync(int driverId) =>
        _repository.HasActiveInternationalLicenseAsync(driverId);

    public async Task<Result<int>> IssueInternationalLicenseAsync(int localLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateLocalLicenseId(localLicenseId);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<int>.FromForbidden("Authenticated user is required.");

        var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(InternationalApplicationTypeId);
        if (applicationTypeResult.IsFailure)
            return PropagateFailure<int>(applicationTypeResult);
        if (applicationTypeResult.Value is null)
            return Result<int>.FromNotFound("International application type not found.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var licenseResult = await _licenseQueryService.GetByIdAsync(localLicenseId);
            if (licenseResult.IsFailure)
                return await RollbackAsync(transaction, PropagateFailure<int>(licenseResult));
            if (licenseResult.Value is null)
                return await RollbackAsync(transaction, Result<int>.FromNotFound("Local license not found."));

            var license = licenseResult.Value;
            if (license.LicenseClassID != OrdinaryLicenseClassId)
                return await RollbackAsync(transaction, Result<int>.FromConflict("Only class 3 licenses can be issued internationally."));

            if (!license.IsActive)
                return await RollbackAsync(transaction, Result<int>.FromConflict("The local license is not active."));

            if (license.ExpirationDate <= DateTime.UtcNow)
                return await RollbackAsync(transaction, Result<int>.FromConflict("The local license is expired."));

            if (license.Driver is null)
                return await RollbackAsync(transaction, Result<int>.FromNotFound("Driver information is not available."));

            if (license.Driver.PersonID <= 0)
                return await RollbackAsync(transaction, Result<int>.FromFailure("The license has invalid driver information."));

            if (await _repository.ExistsByLocalLicenseAsync(localLicenseId))
                return await RollbackAsync(transaction, Result<int>.FromConflict("An international license already exists for this local license."));

            if (await _repository.HasActiveInternationalLicenseAsync(license.DriverID))
                return await RollbackAsync(transaction, Result<int>.FromConflict("The driver already has an active international license."));

            var now = DateTime.UtcNow;
            var applicationResult = await _applicationService.AddNewApplicationAsync(new CreateApplicationDto
            {
                ApplicantPersonID = license.Driver.PersonID,
                ApplicationTypeID = InternationalApplicationTypeId
            });

            if (applicationResult.IsFailure)
                return await RollbackAsync(transaction, PropagateFailure<int>(applicationResult));

            var applicationId = applicationResult.Value;
            if (applicationId <= 0)
                return await RollbackAsync(transaction, Result<int>.FromFailure("Failed to create international application."));

            var entity = new InternationalLicense
            {
                ApplicationID = applicationId,
                DriverID = license.DriverID,
                IssuedUsingLocalLicenseID = localLicenseId,
                IssueDate = now,
                ExpirationDate = now.AddYears(InternationalLicenseValidityYears),
                IsActive = true,
                CreatedByUserID = _currentUserService.UserId
            };

            await _repository.AddAsync(entity);
            var saved = await _unitOfWork.SaveChangesAsync();

            if (saved <= 0 || entity.InternationalLicenseID <= 0)
                return await RollbackAsync(transaction, Result<int>.FromFailure("Failed to save international license."));

            var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
            if (completeResult.IsFailure)
                return await RollbackAsync(transaction, PropagateFailure<int>(completeResult));

            await transaction.CommitAsync();
            return Result<int>.Success(entity.InternationalLicenseID);
        }
        catch (Exception)
        {
            await RollbackSafelyAsync(transaction, localLicenseId);
            throw;
        }
    }

    public async Task<Result<DriverLicenseInfoDto>> GetLocalLicenseInfoAsync(int licenseId)
    {
        var validation = InternationalLicenseValidator.ValidateLocalLicenseId(licenseId);
        if (validation.IsFailure)
            return Result<DriverLicenseInfoDto>.FromValidationFailure(validation.Error);

        var licenseResult = await _licenseQueryService.GetByIdAsync(licenseId);
        if (licenseResult.IsFailure)
            return PropagateFailure<DriverLicenseInfoDto>(licenseResult);
        if (licenseResult.Value is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("Local license not found.");

        var license = licenseResult.Value;
        if (license.LicenseClassID != OrdinaryLicenseClassId)
            return Result<DriverLicenseInfoDto>.FromConflict("Only class 3 licenses can be converted to an international license.");

        if (!license.IsActive)
            return Result<DriverLicenseInfoDto>.FromConflict("The local license is not active.");

        if (license.ExpirationDate <= DateTime.UtcNow)
            return Result<DriverLicenseInfoDto>.FromConflict("The local license is expired.");

        if (license.Driver is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("Driver information is not available.");

        if (await _repository.ExistsByLocalLicenseAsync(licenseId))
            return Result<DriverLicenseInfoDto>.FromConflict("An international license already exists for this local license.");

        return Result<DriverLicenseInfoDto>.Success(InternationalLicenseMapper.ToDriverLicenseInfoDto(license));
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
            _logger.LogError(rollbackException, "Rollback failed while issuing international license for local license {LocalLicenseId}.", licenseId);
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