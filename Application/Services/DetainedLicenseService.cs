using Application.Common.Results;
using Application.DTOs.DetainedLicenseDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

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
                .Select(MapToDto)
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
            return Result<DetainedLicenseDto>
                .FromFailure(validation.Error);

        var entity =
            await _repository.GetByIdAsync(id);

        if (entity is null)
        {
            return Result<DetainedLicenseDto>
                .FromFailure(
                    "Detained license not found.");
        }

        return Result<DetainedLicenseDto>
            .Success(MapToDto(entity));
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
                .FromFailure(validation.Error);
        }

        var entity =
            await _repository
                .GetActiveDetainByLicenseIdAsync(
                    licenseId);

        if (entity is null)
        {
            return Result<DetainedLicenseDto>
                .FromFailure(
                    "No active detention found for this license.");
        }

        return Result<DetainedLicenseDto>
            .Success(MapToDto(entity));
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
                .FromFailure(validation.Error);
        }


        // Prevent multiple active detentions
        var alreadyDetained =
            await _repository
                .IsLicenseDetainedAsync(
                    dto.LicenseID);

        if (alreadyDetained)
        {
            return Result<DetainedLicenseDto>
                .FromFailure(
                    "License already detained.");
        }


        // DTO -> Entity
        var entity = new DetainedLicense
        {
            LicenseID = dto.LicenseID,
            DetainDate = dto.DetainDate,
            FineFees = dto.FineFees,
            CreatedByUserID = dto.CreatedByUserID,

            IsReleased = false,
            ReleaseDate = null,
            ReleasedByUserID = null,
            ReleaseApplicationID = null
        };


        // Repository works with Entity
        var created =
            await _repository.AddAsync(entity);


        // Reload entity with navigation properties
        var savedEntity =
            await _repository
                .GetByIdAsync(created.DetainID);

        if (savedEntity is null)
        {
            return Result<DetainedLicenseDto>
                .FromFailure(
                    "Unable to retrieve created detained license.");
        }


        // Entity -> DTO
        return Result<DetainedLicenseDto>
            .Success(MapToDto(savedEntity));
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
            return Result.Failure(validation.Error);


        var entity =
            await _repository
                .GetByIdAsync(dto.DetainID);

        if (entity is null)
        {
            return Result.Failure(
                "Detained license not found.");
        }


        // Released detention cannot become active again
        if (entity.IsReleased &&
            !dto.IsReleased)
        {
            return Result.Failure(
                "A released license cannot be changed back to active detention.");
        }


        // Update basic data
        entity.FineFees =
            dto.FineFees;


        // Update release information
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
            return Result.Failure(
                validation.Error);


        var entity =
            await _repository
                .GetByIdAsync(dto.DetainID);

        if (entity is null)
        {
            return Result.Failure(
                "Detained license not found.");
        }


        if (entity.IsReleased)
        {
            return Result.Failure(
                "License already released.");
        }


        entity.IsReleased = true;

        entity.ReleaseDate =
            DateTime.Now;

        entity.ReleasedByUserID =
            dto.ReleasedByUserID;

        entity.ReleaseApplicationID =
            dto.ReleaseApplicationID;


        await _repository
            .UpdateAsync(entity);

        return Result.Success();
    }


    // =========================================================
    // MAPPING
    // =========================================================

    private static DetainedLicenseDto
        MapToDto(
            DetainedLicense entity)
    {
        var person =
            entity.License?
                .Driver?
                .Person;

        return new DetainedLicenseDto
        {
            DetainID =
                entity.DetainID,

            LicenseID =
                entity.LicenseID,

            PersonID =
                person?.PersonId ?? 0,

            ApplicantPersonID =
                person?.PersonId ?? 0,

            DetainDate =
                entity.DetainDate,

            FineFees =
                entity.FineFees,

            CreatedByUserID =
                entity.CreatedByUserID,

            CreatedByUserName =
                entity.CreatedByUser?.UserName
                ?? string.Empty,

            IsReleased =
                entity.IsReleased,

            ReleaseDate =
                entity.ReleaseDate,

            ReleasedByUserID =
                entity.ReleasedByUserID,

            ReleaseApplicationID =
                entity.ReleaseApplicationID,

            NationalNo =
                person?.NationalNo
                ?? string.Empty,

            FullName =
                person?.FullName
                ?? string.Empty
        };
    }
}