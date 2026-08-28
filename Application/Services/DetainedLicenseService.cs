using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public class DetainedLicenseService : IDetainedLicenseService
{
    private readonly IDetainedLicenseRepository _repository;

    public DetainedLicenseService(
        IDetainedLicenseRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
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
            DetainedLicenseValidator.ValidateId(id);

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
            return false;

        return await _repository
            .IsLicenseDetainedAsync(licenseId);
    }


    // =========================================================
    // ADD
    // =========================================================

    public async Task<Result<DetainedLicenseDto>>
        AddAsync(
            CreateDetainedLicenseDto dto)
    {
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
        // Prevent duplicate active detention
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
        // DTO -> Entity
        // -----------------------------------------------------

        var entity =
            DetainedLicenseMapper
                .ToEntity(dto);


        // -----------------------------------------------------
        // CREATE
        // -----------------------------------------------------

        var created =
            await _repository
                .AddAsync(entity);

        if (created is null ||
            created.DetainID <= 0)
        {
            return Result<DetainedLicenseDto>
                .FromFailure(
                    "Failed to create detained license.");
        }


        // -----------------------------------------------------
        // Reload entity with navigation properties
        // -----------------------------------------------------

        var savedEntity =
            await _repository
                .GetByIdAsync(
                    created.DetainID);

        if (savedEntity is null)
        {
            return Result<DetainedLicenseDto>
                .FromNotFound(
                    "Unable to retrieve created detained license.");
        }


        // -----------------------------------------------------
        // Entity -> DTO
        // -----------------------------------------------------

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
        var validation =
            DetainedLicenseValidator
                .ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }


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
        // Released detention cannot become active again
        // -----------------------------------------------------

        if (entity.IsReleased &&
            !dto.IsReleased)
        {
            return Result
                .Conflict(
                    "A released license cannot be changed back to active detention.");
        }


        // -----------------------------------------------------
        // Update basic data
        // -----------------------------------------------------

        entity.FineFees =
            dto.FineFees;


        // -----------------------------------------------------
        // Update release information
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

        return Result.Success();
    }


    // =========================================================
    // RELEASE
    // =========================================================

    public async Task<Result>
        ReleaseAsync(
            ReleaseDetainedLicenseDto dto)
    {
        var validation =
            DetainedLicenseValidator
                .ValidateRelease(dto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }


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


        if (entity.IsReleased)
        {
            return Result
                .Conflict(
                    "License already released.");
        }


        // -----------------------------------------------------
        // Release
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

        return Result.Success();
    }
}