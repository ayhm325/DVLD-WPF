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
    private readonly ICurrentUserService _currentUserService;

    public DriverService(
        IDriverRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<DriverDto>> GetByIdAsync(int id)
    {
        var validation = DriverValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<DriverDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);

        if (entity is null)
            return Result<DriverDto>.FromNotFound("Driver not found.");

        return Result<DriverDto>.Success(DriverMapper.ToDto(entity));
    }

    public async Task<Result<List<DriverDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return Result<List<DriverDto>>.Success(DriverMapper.ToDtoList(entities));
    }

    public async Task<Result<DriverDto>> GetByPersonIdAsync(int personId)
    {
        var validation = DriverValidator.ValidatePersonId(personId);
        if (validation.IsFailure)
            return Result<DriverDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByPersonIdAsync(personId);

        if (entity is null)
            return Result<DriverDto>.FromNotFound("Driver not found.");

        return Result<DriverDto>.Success(DriverMapper.ToDto(entity));
    }

    public async Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(int userId)
    {
        var validation = DriverValidator.ValidateCreatedUserId(userId);

        if (validation.IsFailure)
            return Result<List<DriverDto>>.FromFailure(validation.Error);

        var entities = await _repository.GetByCreatedUserIdAsync(userId);

        return Result<List<DriverDto>>.Success(DriverMapper.ToDtoList(entities));
    }

    public async Task<bool> ExistsByIdAsync(int driverId)
    {
        return driverId > 0 &&
               await _repository.ExistsByIdAsync(driverId);
    }

    public async Task<bool> ExistsByPersonIdAsync(int personId)
    {
        return personId > 0 &&
               await _repository.ExistsByPersonIdAsync(personId);
    }

    public async Task<Result<int>> AddAsync(CreateDriverDto dto)
    {
        var validation = DriverValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
            return Result<int>.FromFailure("Authenticated user is required.");

        var alreadyDriver =
            await _repository.ExistsByPersonIdAsync(dto.PersonID);

        if (alreadyDriver)
            return Result<int>.FromConflict(
                "This person is already registered as a driver.");

        var entity = DriverMapper.ToEntity(dto);
        entity.CreatedByUserID = _currentUserService.UserId;

        await _repository.AddAsync(entity);

        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0 || entity.DriverID <= 0)
            return Result<int>.FromFailure("Failed to create driver.");

        return Result<int>.Success(entity.DriverID);
    }

    public async Task<Result> UpdateAsync(UpdateDriverDto dto)
    {
        var validation = DriverValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        var existing = await _repository.GetByIdAsync(dto.DriverID);

        if (existing is null)
            return Result.NotFound("Driver not found.");

        if (existing.PersonID != dto.PersonID)
        {
            var alreadyDriver =
                await _repository.ExistsByPersonIdAsync(dto.PersonID);

            if (alreadyDriver)
                return Result.Conflict(
                    "This person is already registered as another driver.");
        }

        DriverMapper.UpdateEntity(existing, dto);

        var saved = await _unitOfWork.SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure("No driver changes were saved.");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var validation = DriverValidator.ValidateId(id);

        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!await _repository.ExistsByIdAsync(id))
            return Result.NotFound("Driver not found.");

        await _repository.DeleteAsync(id);

        var saved = await _unitOfWork.SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure("Failed to delete driver.");
    }
}