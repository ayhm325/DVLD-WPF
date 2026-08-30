using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DriverService(
        IDriverRepository repository,
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
    // GET BY ID
    // =========================================================

    public async Task<Result<DriverDto>>
        GetByIdAsync(int id)
    {
        var validation =
            DriverValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<DriverDto>
                .FromFailure(
                    validation.Error);
        }

        var entity =
            await _repository
                .GetByIdAsync(id);

        if (entity is null)
        {
            return Result<DriverDto>
                .FromFailure(
                    "Driver not found.");
        }

        return Result<DriverDto>
            .Success(
                DriverMapper.ToDto(entity));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<DriverDto>>>
        GetAllAsync()
    {
        var entities =
            await _repository
                .GetAllAsync();

        return Result<List<DriverDto>>
            .Success(
                DriverMapper.ToDtoList(
                    entities));
    }

    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    public async Task<Result<DriverDto>>
        GetByPersonIdAsync(
            int personId)
    {
        var validation =
            DriverValidator
                .ValidatePersonId(
                    personId);

        if (validation.IsFailure)
        {
            return Result<DriverDto>
                .FromFailure(
                    validation.Error);
        }

        var entity =
            await _repository
                .GetByPersonIdAsync(
                    personId);

        if (entity is null)
        {
            return Result<DriverDto>
                .FromFailure(
                    "Driver not found.");
        }

        return Result<DriverDto>
            .Success(
                DriverMapper.ToDto(entity));
    }

    // =========================================================
    // GET BY CREATED USER ID
    // =========================================================

    public async Task<Result<List<DriverDto>>>
        GetByCreatedUserIdAsync(
            int userId)
    {
        var validation =
            DriverValidator
                .ValidateCreatedUserId(
                    userId);

        if (validation.IsFailure)
        {
            return Result<List<DriverDto>>
                .FromFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByCreatedUserIdAsync(
                    userId);

        return Result<List<DriverDto>>
            .Success(
                DriverMapper.ToDtoList(
                    entities));
    }

    // =========================================================
    // EXISTS BY ID
    // =========================================================

    public async Task<bool>
        ExistsByIdAsync(
            int driverId)
    {
        if (driverId <= 0)
            return false;

        return await _repository
            .ExistsByIdAsync(
                driverId);
    }

    // =========================================================
    // EXISTS BY PERSON ID
    // =========================================================

    public async Task<bool>
        ExistsByPersonIdAsync(
            int personId)
    {
        if (personId <= 0)
            return false;

        return await _repository
            .ExistsByPersonIdAsync(
                personId);
    }

    // =========================================================
    // ADD
    // =========================================================

    public async Task<Result<int>>
        AddAsync(
            CreateDriverDto dto)
    {
        var validation =
            DriverValidator
                .ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromFailure(
                    validation.Error);
        }

        // -----------------------------------------------------
        // Business rule:
        // One person can have only one Driver record.
        // -----------------------------------------------------

        if (await _repository
                .ExistsByPersonIdAsync(
                    dto.PersonID))
        {
            return Result<int>
                .FromFailure(
                    "This person is already registered as a driver.");
        }

        // -----------------------------------------------------
        // Map
        // -----------------------------------------------------

        var entity =
            DriverMapper.ToEntity(dto);

        // -----------------------------------------------------
        // Add to current DbContext
        // -----------------------------------------------------

        await _repository
            .AddAsync(entity);

        // -----------------------------------------------------
        // Persist through UnitOfWork
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0 ||
            entity.DriverID <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create driver.");
        }

        return Result<int>
            .Success(
                entity.DriverID);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateAsync(
            UpdateDriverDto dto)
    {
        var validation =
            DriverValidator
                .ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result.Failure(
                validation.Error);
        }

        // -----------------------------------------------------
        // Load tracked entity
        // -----------------------------------------------------

        var existing =
            await _repository
                .GetByIdAsync(
                    dto.DriverID);

        if (existing is null)
        {
            return Result.Failure(
                "Driver not found.");
        }

        // -----------------------------------------------------
        // Business rule:
        // Person cannot belong to another driver.
        // -----------------------------------------------------

        if (existing.PersonID != dto.PersonID &&
            await _repository
                .ExistsByPersonIdAsync(
                    dto.PersonID))
        {
            return Result.Failure(
                "This person is already registered as another driver.");
        }

        // -----------------------------------------------------
        // Apply update
        // -----------------------------------------------------

        DriverMapper.UpdateEntity(
            existing,
            dto);

        // -----------------------------------------------------
        // Persist through UnitOfWork
        // -----------------------------------------------------

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "No driver changes were saved.");
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteAsync(int id)
    {
        var validation =
            DriverValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.Failure(
                validation.Error);
        }

        if (!await _repository
                .ExistsByIdAsync(id))
        {
            return Result.Failure(
                "Driver not found.");
        }

        await _repository
            .DeleteAsync(id);

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to delete driver.");
    }
}