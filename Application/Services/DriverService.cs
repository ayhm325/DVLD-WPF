using Application.Common.Pagination;
using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.DriverDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public sealed class DriverService(
    IDriverRepository repository,
    IPersonRepository personRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IJordanClock clock) : IDriverService
{
    private readonly IDriverRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly IPersonRepository _personRepository =
        personRepository ?? throw new ArgumentNullException(nameof(personRepository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    private readonly IJordanClock _clock =
        clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<DriverDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var validation = DriverValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<DriverDto>.FromValidationFailure(
                validation.Error);
        }

        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        return entity is null
            ? Result<DriverDto>.FromNotFound("Driver not found.")
            : Result<DriverDto>.Success(
                DriverMapper.ToDto(entity));
    }

    public async Task<Result<List<DriverDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetAllAsync(cancellationToken);

        return Result<List<DriverDto>>.Success(
            DriverMapper.ToDtoList(entities));
    }

    public Task<Result<PagedResult<DriverDto>>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is DriverQueryRequest queryRequest)
        {
            return GetAllAsync(queryRequest, cancellationToken);
        }

        return GetAllInternalAsync(() => _repository.GetAllAsync(request, cancellationToken));
    }

    public Task<Result<PagedResult<DriverDto>>> GetAllAsync(DriverQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return GetAllInternalAsync(() => _repository.GetAllAsync(request, cancellationToken));
    }

    private async Task<Result<PagedResult<DriverDto>>> GetAllInternalAsync(
        Func<Task<PagedResult<Domain.Entities.Driver>>> fetcher)
    {
        var pagedEntities = await fetcher();

        var items = pagedEntities.Items
            .Select(DriverMapper.ToDto)
            .ToList();

        return Result<PagedResult<DriverDto>>.Success(new PagedResult<DriverDto>
        {
            Items = items,
            PageNumber = pagedEntities.PageNumber,
            PageSize = pagedEntities.PageSize,
            TotalCount = pagedEntities.TotalCount
        });
    }

    public async Task<Result<DriverDto>> GetByPersonIdAsync(int personId, CancellationToken cancellationToken = default)
    {
        var validation =
            DriverValidator.ValidatePersonId(personId);

        if (validation.IsFailure)
        {
            return Result<DriverDto>.FromValidationFailure(
                validation.Error);
        }

        var entity =
            await _repository.GetByPersonIdAsync(personId, cancellationToken);

        return entity is null
            ? Result<DriverDto>.FromNotFound("Driver not found.")
            : Result<DriverDto>.Success(
                DriverMapper.ToDto(entity));
    }

    public async Task<Result<List<DriverDto>>> GetByCreatedUserIdAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        var validation =
            DriverValidator.ValidateCreatedUserId(userId);

        if (validation.IsFailure)
        {
            return Result<List<DriverDto>>.FromValidationFailure(
                validation.Error);
        }

        var entities =
            await _repository.GetByCreatedUserIdAsync(userId, cancellationToken);

        return Result<List<DriverDto>>.Success(
            DriverMapper.ToDtoList(entities));
    }

    public Task<bool> ExistsByIdAsync(int driverId, CancellationToken cancellationToken = default) =>
        driverId > 0
            ? _repository.ExistsByIdAsync(driverId, cancellationToken)
            : Task.FromResult(false);

    public Task<bool> ExistsByPersonIdAsync(int personId, CancellationToken cancellationToken = default) =>
        personId > 0
            ? _repository.ExistsByPersonIdAsync(personId, cancellationToken)
            : Task.FromResult(false);

    public async Task<Result<int>> AddAsync(CreateDriverDto dto, CancellationToken cancellationToken = default)
    {
        var validation =
            DriverValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        if (!IsAuthenticated())
        {
            return Result<int>.FromForbidden(
                "Authenticated user is required.");
        }

        if (!await _personRepository.IsPersonExistsByIdAsync(dto.PersonID, cancellationToken))
        {
            return Result<int>.FromNotFound(
                "Person not found.");
        }

        if (await _repository.ExistsByPersonIdAsync(dto.PersonID, cancellationToken))
        {
            return Result<int>.FromConflict(
                "This person is already registered as a driver.");
        }

        var entity = DriverMapper.ToEntity(dto, _clock.Now);

        entity.CreatedByUserID =
            _currentUserService.UserId;

        await _repository.AddAsync(entity, cancellationToken);

        var saved =
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saved > 0 && entity.DriverID > 0
            ? Result<int>.Success(entity.DriverID)
            : Result<int>.FromFailure(
                "Failed to create driver.");
    }

    public async Task<Result> UpdateAsync(UpdateDriverDto dto, CancellationToken cancellationToken = default)
    {
        var validation =
            DriverValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        if (!IsAuthenticated())
        {
            return Result.Forbidden(
                "Authenticated user is required.");
        }

        var existing =
            await _repository.GetForUpdateAsync(dto.DriverID, cancellationToken);

        if (existing is null)
        {
            return Result.NotFound(
                "Driver not found.");
        }

        if (!await _personRepository.IsPersonExistsByIdAsync(dto.PersonID, cancellationToken))
        {
            return Result.NotFound(
                "Person not found.");
        }

        if (existing.PersonID != dto.PersonID &&
            await _repository.ExistsByPersonIdAsync(dto.PersonID, cancellationToken))
        {
            return Result.Conflict(
                "This person is already registered as another driver.");
        }

        bool hasChanges = existing.PersonID != dto.PersonID;
        if (!hasChanges)
        {
            return Result.Success();
        }

        DriverMapper.UpdateEntity(existing, dto);

        var saved =
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "No driver changes were saved.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var validation =
            DriverValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        if (!IsAuthenticated())
        {
            return Result.Forbidden(
                "Authenticated user is required.");
        }

        var driver =
            await _repository.GetForDeleteAsync(id, cancellationToken);

        if (driver is null)
        {
            return Result.NotFound(
                "Driver not found.");
        }

        if (driver.Licenses.Count > 0)
        {
            return Result.Conflict(
                "Cannot delete a driver with existing licenses.");
        }

        if (driver.InternationalLicenses.Count > 0)
        {
            return Result.Conflict(
                "Cannot delete a driver with an existing international license.");
        }

        _repository.Delete(driver);

        var saved =
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to delete driver.");
    }

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn &&
        _currentUserService.UserId > 0;
}
