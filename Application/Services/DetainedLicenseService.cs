using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public class DetainedLicenseService : IDetainedLicenseService
{
    private readonly IDetainedLicenseRepository _repository;
    private readonly ILicenseRepository _licenseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DetainedLicenseService(
    IDetainedLicenseRepository repository,
    ILicenseRepository licenseRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _licenseRepository = licenseRepository
            ?? throw new ArgumentNullException(nameof(licenseRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<DetainedLicenseDto>>>
        GetAllAsync()
    {
        var entities =
            await _repository.GetAllAsync();

        var dtos =
            entities
                .Select(DetainedLicenseMapper.ToDto)
                .ToList();

        return Result<List<DetainedLicenseDto>>
            .Success(dtos);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<DetainedLicenseDto>>
        GetByIdAsync(int id)
    {
        var validation =
            DetainedLicenseValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<DetainedLicenseDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            await _repository.GetByIdAsync(id);

        if (entity is null)
        {
            return Result<DetainedLicenseDto>
                .FromNotFound(
                    "Detained license not found.");
        }

        return Result<DetainedLicenseDto>
            .Success(
                DetainedLicenseMapper.ToDto(entity));
    }

    // =========================================================
    // GET ACTIVE DETENTION
    // =========================================================

    public async Task<Result<DetainedLicenseDto>>
        GetActiveDetainByLicenseIdAsync(
            int licenseId)
    {
        var validation =
            DetainedLicenseValidator
                .ValidateLicenseId(licenseId);

        if (validation.IsFailure)
        {
            return Result<DetainedLicenseDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            await _repository
                .GetActiveDetainByLicenseIdAsync(
                    licenseId);

        if (entity is null)
        {
            return Result<DetainedLicenseDto>
                .FromNotFound(
                    "No active detention found for this license.");
        }

        return Result<DetainedLicenseDto>
            .Success(
                DetainedLicenseMapper.ToDto(entity));
    }

    // =========================================================
    // CHECK
    // =========================================================

    public async Task<bool>
        IsLicenseDetainedAsync(
            int licenseId)
    {
        if (licenseId <= 0)
        {
            return false;
        }

        return await _repository
            .IsLicenseDetainedAsync(
                licenseId);
    }

    // =========================================================
    // CREATE DETENTION
    // =========================================================

    public async Task<Result<DetainedLicenseDto>>AddAsync(CreateDetainedLicenseDto dto)
    {
        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        var validation =
            DetainedLicenseValidator
                .ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<DetainedLicenseDto>
                .FromValidationFailure(
                    validation.Error);
        }

        // -----------------------------------------------------
        // LOAD LICENSE
        // -----------------------------------------------------

        var license =
            await _licenseRepository
                .GetLicenseByIdAsync(
                    dto.LicenseID);

        if (license is null)
        {
            return Result<DetainedLicenseDto>
                .FromNotFound(
                    "License not found.");
        }

        // -----------------------------------------------------
        // LICENSE MUST BE ACTIVE
        // -----------------------------------------------------

        if (!license.IsActive)
        {
            return Result<DetainedLicenseDto>
                .FromConflict(
                    "Only an active license can be detained.");
        }

        // -----------------------------------------------------
        // PREVENT DUPLICATE ACTIVE DETENTION
        // -----------------------------------------------------

        var alreadyDetained =
            await _repository
                .IsLicenseDetainedAsync(
                    dto.LicenseID);

        if (alreadyDetained)
        {
            return Result<DetainedLicenseDto>
                .FromConflict(
                    "License already detained.");
        }


        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
        {
            return Result<DetainedLicenseDto>
                .FromFailure("Authenticated user is required.");
        }

        // -----------------------------------------------------
        // BEGIN TRANSACTION
        //
        // Both operations must succeed together:
        //
        // 1. Create detention
        // 2. Deactivate license
        //
        // If either fails -> rollback both.
        // -----------------------------------------------------

        await using var transaction =
            await _unitOfWork
                .BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // CREATE DETENTION
            // -------------------------------------------------

            var entity = DetainedLicenseMapper.ToEntity(dto);

            entity.CreatedByUserID = _currentUserService.UserId;

            await _repository.AddAsync(entity);

            // -------------------------------------------------
            // DEACTIVATE LICENSE
            // -------------------------------------------------

            license.IsActive = false;

            var licenseUpdated =
                await _licenseRepository
                    .UpdateLicenseAsync(
                        license);

            if (!licenseUpdated)
            {
                await transaction
                    .RollbackAsync();

                return Result<DetainedLicenseDto>
                    .FromFailure(
                        "Failed to deactivate the license.");
            }

            // -------------------------------------------------
            // SAVE BOTH CHANGES
            // -------------------------------------------------

            var saved =
                await _unitOfWork
                    .SaveChangesAsync();

            if (saved <= 0)
            {
                await transaction
                    .RollbackAsync();

                return Result<DetainedLicenseDto>
                    .FromFailure(
                        "Failed to save detained license.");
            }

            // -------------------------------------------------
            // COMMIT
            // -------------------------------------------------

            await transaction
                .CommitAsync();

            // -------------------------------------------------
            // RELOAD CREATED DETENTION
            // -------------------------------------------------

            var savedEntity =
                await _repository
                    .GetByIdAsync(
                        entity.DetainID);

            if (savedEntity is null)
            {
                return Result<DetainedLicenseDto>
                    .FromNotFound(
                        "Unable to retrieve created detained license.");
            }

            return Result<DetainedLicenseDto>
                .Success(
                    DetainedLicenseMapper
                        .ToDto(savedEntity));
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateAsync(
            UpdateDetainedLicenseDto dto)
    {
        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        var validation =
            DetainedLicenseValidator
                .ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }

        // -----------------------------------------------------
        // LOAD EXISTING DETENTION
        // -----------------------------------------------------

        var entity =
            await _repository
                .GetByIdAsync(
                    dto.DetainID);

        if (entity is null)
        {
            return Result
                .NotFound(
                    "Detained license not found.");
        }

        // -----------------------------------------------------
        // RELEASED DETENTION CANNOT BE REOPENED
        // -----------------------------------------------------

        if (entity.IsReleased &&
            !dto.IsReleased)
        {
            return Result
                .Conflict(
                    "A released license cannot be changed back to active detention.");
        }

        // -----------------------------------------------------
        // UPDATE FINE
        // -----------------------------------------------------

        entity.FineFees =
            dto.FineFees;

        // -----------------------------------------------------
        // UPDATE RELEASE INFORMATION
        // -----------------------------------------------------

        if (dto.IsReleased)
        {
            entity.IsReleased = true;

            entity.ReleaseDate =
                dto.ReleaseDate;

            entity.ReleasedByUserID =
                dto.ReleasedByUserID;

            entity.ReleaseApplicationID =
                dto.ReleaseApplicationID;
        }

        // -----------------------------------------------------
        // UPDATE
        // -----------------------------------------------------

        await _repository
            .UpdateAsync(entity);

        // -----------------------------------------------------
        // SAVE
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result
                .Failure(
                    "Failed to save detained license changes.");
        }

        return Result.Success();
    }

    // =========================================================
    // RELEASE
    // =========================================================

    public async Task<Result>
        ReleaseAsync(
            ReleaseDetainedLicenseDto dto)
    {
        // -----------------------------------------------------
        // VALIDATION
        // -----------------------------------------------------

        var validation =
            DetainedLicenseValidator
                .ValidateRelease(dto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }

        // -----------------------------------------------------
        // LOAD DETENTION
        // -----------------------------------------------------

        var entity =
            await _repository
                .GetByIdAsync(
                    dto.DetainID);

        if (entity is null)
        {
            return Result
                .NotFound(
                    "Detained license not found.");
        }

        // -----------------------------------------------------
        // PREVENT DUPLICATE RELEASE
        // -----------------------------------------------------

        if (entity.IsReleased)
        {
            return Result
                .Conflict(
                    "License already released.");
        }

        // -----------------------------------------------------
        // LOAD LICENSE
        // -----------------------------------------------------

        var license =
            await _licenseRepository
                .GetLicenseByIdAsync(
                    entity.LicenseID);

        if (license is null)
        {
            return Result
                .NotFound(
                    "Associated license not found.");
        }

        // -----------------------------------------------------
        // BEGIN TRANSACTION
        //
        // Both operations must succeed together:
        //
        // 1. Mark detention as released
        // 2. Restore license active state
        //
        // If either fails -> rollback both.
        // -----------------------------------------------------

        await using var transaction =
            await _unitOfWork
                .BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // RELEASE DETENTION
            // -------------------------------------------------

            entity.IsReleased = true;

            entity.ReleaseDate =
                DateTime.Now;

            entity.ReleasedByUserID =
                dto.ReleasedByUserID;

            entity.ReleaseApplicationID =
                dto.ReleaseApplicationID;

            await _repository
                .UpdateAsync(entity);

            // -------------------------------------------------
            // RESTORE LICENSE ACTIVE STATE
            //
            // If the license is still valid -> active.
            // If it expired while detained -> remain inactive.
            // -------------------------------------------------

            license.IsActive =
                license.ExpirationDate >= DateTime.Now;

            var licenseUpdated =
                await _licenseRepository
                    .UpdateLicenseAsync(
                        license);

            if (!licenseUpdated)
            {
                await transaction
                    .RollbackAsync();

                return Result
                    .Failure(
                        "Failed to restore the license active state.");
            }

            // -------------------------------------------------
            // SAVE BOTH CHANGES
            // -------------------------------------------------

            var saved =
                await _unitOfWork
                    .SaveChangesAsync();

            if (saved <= 0)
            {
                await transaction
                    .RollbackAsync();

                return Result
                    .Failure(
                        "Failed to save license release.");
            }

            // -------------------------------------------------
            // COMMIT
            // -------------------------------------------------

            await transaction
                .CommitAsync();

            return Result.Success();
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }
}