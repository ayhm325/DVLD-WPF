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
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _licenseRepository = licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<List<DetainedLicenseDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();
        return Result<List<DetainedLicenseDto>>.Success(
            entities.Select(DetainedLicenseMapper.ToDto).ToList());
    }

    public async Task<Result<DetainedLicenseDto>> GetByIdAsync(int id)
    {
        var validation = DetainedLicenseValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<DetainedLicenseDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        return entity is null
            ? Result<DetainedLicenseDto>.FromNotFound("Detained license not found.")
            : Result<DetainedLicenseDto>.Success(DetainedLicenseMapper.ToDto(entity));
    }

    public async Task<Result<DetainedLicenseDto>> GetActiveDetainByLicenseIdAsync(int licenseId)
    {
        var validation = DetainedLicenseValidator.ValidateLicenseId(licenseId);
        if (validation.IsFailure)
            return Result<DetainedLicenseDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetActiveDetainByLicenseIdAsync(licenseId);
        return entity is null
            ? Result<DetainedLicenseDto>.FromNotFound("No active detention found for this license.")
            : Result<DetainedLicenseDto>.Success(DetainedLicenseMapper.ToDto(entity));
    }

    public async Task<bool> IsLicenseDetainedAsync(int licenseId) =>
        licenseId > 0 && await _repository.IsLicenseDetainedAsync(licenseId);

    public async Task<Result<DetainedLicenseDto>> AddAsync(CreateDetainedLicenseDto dto)
    {
        var validation = DetainedLicenseValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<DetainedLicenseDto>.FromValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<DetainedLicenseDto>.FromFailure("Authenticated user is required.");

        var license = await _licenseRepository.GetLicenseByIdAsync(dto.LicenseID);
        if (license is null)
            return Result<DetainedLicenseDto>.FromNotFound("License not found.");

        if (!license.IsActive)
            return Result<DetainedLicenseDto>.FromConflict("Only an active license can be detained.");

        if (await _repository.IsLicenseDetainedAsync(dto.LicenseID))
            return Result<DetainedLicenseDto>.FromConflict("License already detained.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var entity = DetainedLicenseMapper.ToEntity(dto);
            entity.CreatedByUserID = _currentUserService.UserId;

            await _repository.AddAsync(entity);

            license.IsActive = false;

            if (!await _licenseRepository.UpdateLicenseAsync(license))
            {
                await transaction.RollbackAsync();
                return Result<DetainedLicenseDto>.FromFailure("Failed to deactivate the license.");
            }

            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                await transaction.RollbackAsync();
                return Result<DetainedLicenseDto>.FromFailure("Failed to save detained license.");
            }

            await transaction.CommitAsync();

            var savedEntity = await _repository.GetByIdAsync(entity.DetainID);
            return savedEntity is null
                ? Result<DetainedLicenseDto>.FromNotFound("Unable to retrieve created detained license.")
                : Result<DetainedLicenseDto>.Success(DetainedLicenseMapper.ToDto(savedEntity));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> UpdateAsync(UpdateDetainedLicenseDto dto)
    {
        var validation = DetainedLicenseValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(dto.DetainID);
        if (entity is null)
            return Result.NotFound("Detained license not found.");

        if (entity.IsReleased && !dto.IsReleased)
            return Result.Conflict(
                "A released license cannot be changed back to active detention.");

        entity.FineFees = dto.FineFees;

        if (dto.IsReleased)
        {
            entity.IsReleased = true;
            entity.ReleaseDate = dto.ReleaseDate;
            entity.ReleaseApplicationID = dto.ReleaseApplicationID;
        }

        await _repository.UpdateAsync(entity);

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure("Failed to save detained license changes.");
    }

    public async Task<Result> ReleaseAsync(ReleaseDetainedLicenseDto dto)
    {
        var validation = DetainedLicenseValidator.ValidateRelease(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.Failure("Authenticated user is required.");

        var entity = await _repository.GetByIdAsync(dto.DetainID);
        if (entity is null)
            return Result.NotFound("Detained license not found.");

        if (entity.IsReleased)
            return Result.Conflict("License already released.");

        var license = await _licenseRepository.GetLicenseByIdAsync(entity.LicenseID);
        if (license is null)
            return Result.NotFound("Associated license not found.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            entity.IsReleased = true;
            entity.ReleaseDate = DateTime.UtcNow;
            entity.ReleasedByUserID = _currentUserService.UserId;
            entity.ReleaseApplicationID = dto.ReleaseApplicationID;

            await _repository.UpdateAsync(entity);

            license.IsActive = license.ExpirationDate >= DateTime.UtcNow;

            if (!await _licenseRepository.UpdateLicenseAsync(license))
            {
                await transaction.RollbackAsync();
                return Result.Failure("Failed to restore the license active state.");
            }

            if (await _unitOfWork.SaveChangesAsync() <= 0)
            {
                await transaction.RollbackAsync();
                return Result.Failure("Failed to save license release.");
            }

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}