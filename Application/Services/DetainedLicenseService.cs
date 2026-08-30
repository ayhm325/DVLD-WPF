using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public class DetainedLicenseService : IDetainedLicenseService
{
    private readonly IDetainedLicenseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DetainedLicenseService(
        IDetainedLicenseRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(
                nameof(repository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
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
    // GET ACTIVE DETAIN
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
            .IsLicenseDetainedAsync(licenseId);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<DetainedLicenseDto>>
        AddAsync(
            CreateDetainedLicenseDto dto)
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

        // -----------------------------------------------------
        // MAP DTO -> ENTITY
        // -----------------------------------------------------

        var entity =
            DetainedLicenseMapper
                .ToEntity(dto);

        // -----------------------------------------------------
        // ADD
        //
        // Repository only tracks the entity.
        // It does NOT save changes.
        // -----------------------------------------------------

        await _repository
            .AddAsync(entity);

        // -----------------------------------------------------
        // SAVE
        //
        // Database-generated DetainID is available
        // after SaveChangesAsync().
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0)
        {
            return Result<DetainedLicenseDto>
                .FromFailure(
                    "Failed to save detained license.");
        }

        // -----------------------------------------------------
        // RELOAD
        //
        // Get the complete entity including:
        // License -> Driver -> Person
        // CreatedByUser
        // ReleasedByUser
        // ReleaseApplication
        // -----------------------------------------------------

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
        // LOAD EXISTING ENTITY
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
        // BUSINESS RULE
        //
        // A released detention cannot be reopened.
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
            entity.IsReleased =
                true;

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
        // RELEASE
        // -----------------------------------------------------

        entity.IsReleased =
            true;

        entity.ReleaseDate =
            DateTime.UtcNow;

        entity.ReleasedByUserID =
            dto.ReleasedByUserID;

        entity.ReleaseApplicationID =
            dto.ReleaseApplicationID;

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
                    "Failed to save license release.");
        }

        return Result.Success();
    }
}