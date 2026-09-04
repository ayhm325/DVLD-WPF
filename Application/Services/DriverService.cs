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
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DriverService(
        IDriverRepository repository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<DriverDto>> GetByIdAsync(int id)
    {
        var validation = DriverValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<DriverDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);

        return entity is null
            ? Result<DriverDto>.FromNotFound("Driver not found.")
            : Result<DriverDto>.Success(DriverMapper.ToDto(entity));
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

        return entity is null
            ? Result<DriverDto>.FromNotFound("Driver not found.")
            : Result<DriverDto>.Success(DriverMapper.ToDto(entity));
    }

    public async Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(int userId)
    {
        var validation = DriverValidator.ValidateCreatedUserId(userId);
        if (validation.IsFailure)
            return Result<List<DriverDto>>.FromFailure(validation.Error);

        var entities = await _repository.GetByCreatedUserIdAsync(userId);
        return Result<List<DriverDto>>.Success(DriverMapper.ToDtoList(entities));
    }

    public async Task<bool> ExistsByIdAsync(int driverId) =>
    driverId > 0 && await _repository.ExistsByIdAsync(driverId);

    public async Task<bool> ExistsByPersonIdAsync(int personId) =>
        personId > 0 && await _repository.ExistsByPersonIdAsync(personId);

    public async Task<Result<int>> AddAsync(CreateDriverDto dto)
    {
        var validation = DriverValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<int>.FromFailure("Authenticated user is required.");

        if (!await _personRepository.IsPersonExistsByIdAsync(dto.PersonID))
            return Result<int>.FromNotFound("Person not found.");

        if (await _repository.ExistsByPersonIdAsync(dto.PersonID))
            return Result<int>.FromConflict(
                "This person is already registered as a driver.");

        var entity = DriverMapper.ToEntity(dto);
        entity.CreatedByUserID = _currentUserService.UserId;

        await _repository.AddAsync(entity);

        if (await _unitOfWork.SaveChangesAsync() <= 0 || entity.DriverID <= 0)
            return Result<int>.FromFailure("Failed to create driver.");

        return Result<int>.Success(entity.DriverID);
    }

    public async Task<Result> UpdateAsync(UpdateDriverDto dto)
    {
        var validation = DriverValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.ValidationFailure("You must be logged in first.");

        var existing = await _repository.GetByIdAsync(dto.DriverID);
        if (existing is null)
            return Result.NotFound("Driver not found.");

        if (!await _personRepository.IsPersonExistsByIdAsync(dto.PersonID))
            return Result.NotFound("Person not found.");

        if (existing.PersonID != dto.PersonID &&
            await _repository.ExistsByPersonIdAsync(dto.PersonID))
            return Result.Conflict(
                "This person is already registered as another driver.");

        DriverMapper.UpdateEntity(existing, dto);

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure("No driver changes were saved.");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var validation = DriverValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.ValidationFailure("You must be logged in first.");

        var driver = await _repository.GetByIdAsync(id);
        if (driver is null)
            return Result.NotFound("Driver not found.");

        if (driver.Licenses.Any())
            return Result.Conflict("Cannot delete a driver with existing licenses.");

        if (driver.InternationalLicenses.Any())
            return Result.Conflict(
                "Cannot delete a driver with an existing international license.");

        await _repository.DeleteAsync(id);

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure("Failed to delete driver.");
    }
}