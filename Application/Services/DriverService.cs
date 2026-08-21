using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class DriverService : IDriverService
{
    private readonly IDriverRepository _repository;

    public DriverService(IDriverRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    // GET BY ID
    public async Task<Result<DriverDto>> GetByIdAsync(int id)
    {
        var validation = DriverValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<DriverDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result<DriverDto>.FromFailure("Driver not found.");

        return Result<DriverDto>.Success(MapToDto(entity));
    }

    // GET ALL
    public async Task<Result<List<DriverDto>>> GetAllAsync()
    {
        var drivers = await _repository.GetAllAsync();
        return Result<List<DriverDto>>.Success(drivers.Select(MapToDto).ToList());
    }

    // GET BY PERSON ID
    public async Task<Result<DriverDto>> GetByPersonIdAsync(int personId)
    {
        var validation = DriverValidator.ValidatePersonId(personId);
        if (validation.IsFailure)
            return Result<DriverDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByPersonIdAsync(personId);
        if (entity is null)
            return Result<DriverDto>.FromFailure("Driver not found.");

        return Result<DriverDto>.Success(MapToDto(entity));
    }

    // GET BY CREATED USER ID
    public async Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(int userId)
    {
        var validation = DriverValidator.ValidateCreatedUserId(userId);
        if (validation.IsFailure)
            return Result<List<DriverDto>>.FromFailure(validation.Error);

        var drivers = await _repository.GetByCreatedUserIdAsync(userId);
        return Result<List<DriverDto>>.Success(drivers.Select(MapToDto).ToList());
    }

    // EXISTS
    public async Task<bool> ExistsByIdAsync(int driverId)
    {
        if (driverId <= 0) return false;
        return await _repository.ExistsByIdAsync(driverId);
    }

    public async Task<bool> ExistsByPersonIdAsync(int personId)
    {
        if (personId <= 0) return false;
        return await _repository.ExistsByPersonIdAsync(personId);
    }

    // ADD
    public async Task<Result<int>> AddAsync(CreateDriverDto dto)
    {
        var validation = DriverValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        if (await _repository.ExistsByPersonIdAsync(dto.PersonID))
            return Result<int>.FromFailure(
                "This person is already registered as a driver.");

        var entity = new Driver
        {
            PersonID = dto.PersonID,
            CreatedByUserID = dto.CreatedByUserID,
            CreatedDate = DateTime.UtcNow
        };

        await _repository.AddAsync(entity);

        if (entity.DriverID <= 0)
            return Result<int>.FromFailure(
                "Failed to create driver.");

        return Result<int>.Success(entity.DriverID);
    }

    // UPDATE
    public async Task<Result> UpdateAsync(UpdateDriverDto dto)
    {
        var validation = DriverValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        var existing = await _repository.GetByIdAsync(dto.DriverID);
        if (existing is null)
            return Result.Failure("Driver not found.");

        // One person = one driver
        if (existing.PersonID != dto.PersonID &&
            await _repository.ExistsByPersonIdAsync(dto.PersonID))
            return Result.Failure("This person is already registered as another driver.");

        existing.PersonID = dto.PersonID;

        try
        {
            await _repository.UpdateAsync(existing);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    // DELETE
    public async Task<Result> DeleteAsync(int id)
    {
        var validation = DriverValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        if (!await _repository.ExistsByIdAsync(id))
            return Result.Failure("Driver not found.");

        try
        {
            await _repository.DeleteAsync(id);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("Failed to delete driver.");
        }
    }

    // MAPPING
    private static DriverDto MapToDto(Driver entity)
    {
        return new DriverDto
        {
            DriverID = entity.DriverID,
            PersonID = entity.PersonID,
            FullName = entity.Person?.FullName ?? string.Empty,
            NationalNo = entity.Person?.NationalNo ?? string.Empty,
            DateOfBirth = entity.Person?.DateOfBirth ?? DateTime.MinValue,
            Gender = entity.Person?.Gender ?? default,
            ImagePath = entity.Person?.ImagePath,
            CreatedByUserID = entity.CreatedByUserID,
            CreatedByUserName = entity.CreatedByUser?.UserName ?? string.Empty,
            CreatedDate = entity.CreatedDate,
            ActiveLicenses = entity.Licenses?.Count(l => l.IsActive) ?? 0
        };
    }
}