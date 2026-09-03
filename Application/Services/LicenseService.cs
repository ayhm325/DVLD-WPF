using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public LicenseService(
        ILicenseRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<LicenseDto>> GetByIdAsync(int id)
    {
        var validation = LicenseValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<LicenseDto>.FromValidationFailure(validation.Error);
        }

        var license = await _repository.GetLicenseByIdAsync(id);

        if (license is null)
        {
            return Result<LicenseDto>.FromNotFound("License not found.");
        }

        return Result<LicenseDto>.Success(LicenseMapper.ToDto(license));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<LicenseDto>>> GetAllAsync()
    {
        var licenses = await _repository.GetAllLicensesAsync();
        var dtos = licenses.Select(LicenseMapper.ToDto).ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    // =========================================================
    // GET BY DRIVER
    // =========================================================

    public async Task<Result<List<LicenseDto>>> GetByDriverIdAsync(int driverId)
    {
        var validation = LicenseValidator.ValidateDriverId(driverId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);
        }

        var licenses = await _repository.GetLicensesByDriverIdAsync(driverId);

        return Result<List<LicenseDto>>.Success(licenses.Select(LicenseMapper.ToDto).ToList());
    }

    // =========================================================
    // GET BY APPLICATION
    // =========================================================

    public async Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = LicenseValidator.ValidateApplicationId(applicationId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);
        }

        var licenses = await _repository.GetLicensesByApplicationIdAsync(applicationId);

        return Result<List<LicenseDto>>.Success(licenses.Select(LicenseMapper.ToDto).ToList());
    }

    // =========================================================
    // GET BY LICENSE CLASS
    // =========================================================

    public async Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(int licenseClassId)
    {
        var validation = LicenseValidator.ValidateLicenseClassId(licenseClassId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);
        }

        var licenses = await _repository.GetLicensesByLicenseClassIdAsync(licenseClassId);

        return Result<List<LicenseDto>>.Success(licenses.Select(LicenseMapper.ToDto).ToList());
    }

    // =========================================================
    // GET BY PERSON
    // =========================================================

    public async Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(int personId)
    {
        if (personId <= 0)
        {
            return Result<List<LicenseDto>>.FromValidationFailure("Invalid person ID.");
        }

        var licenses = await _repository.GetLicensesByPersonIdAsync(personId);

        return Result<List<LicenseDto>>.Success(licenses.Select(LicenseMapper.ToDto).ToList());
    }

    // =========================================================
    // CHECK LICENSE EXISTS
    // =========================================================

    public async Task<Result<bool>> IsLicenseExistsAsync(int id)
    {
        if (id <= 0)
        {
            return Result<bool>.FromValidationFailure("Invalid license ID.");
        }

        var exists = await _repository.IsLicenseExistsAsync(id);

        return Result<bool>.Success(exists);
    }

    // =========================================================
    // CHECK DRIVER HAS LICENSE
    // =========================================================

    public async Task<Result<bool>> IsDriverHasLicenseAsync(int driverId)
    {
        if (driverId <= 0)
        {
            return Result<bool>.FromValidationFailure("Invalid driver ID.");
        }

        var exists = await _repository.IsDriverHasLicenseAsync(driverId);

        return Result<bool>.Success(exists);
    }

    // =========================================================
    // CHECK APPLICATION HAS LICENSE
    // =========================================================

    public async Task<Result<bool>> IsApplicationHasLicenseAsync(int applicationId)
    {
        if (applicationId <= 0)
        {
            return Result<bool>.FromValidationFailure("Invalid application ID.");
        }

        var exists = await _repository.IsApplicationHasLicenseAsync(applicationId);

        return Result<bool>.Success(exists);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<int>> AddAsync(CreateLicenseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation = LicenseValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
            return Result<int>.FromFailure("Authenticated user is required.");

        var entity = LicenseMapper.ToEntity(dto);
        entity.CreatedByUserID = _currentUserService.UserId;

        await _repository.AddLicenseAsync(entity);

        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0 || entity.LicenseID <= 0)
            return Result<int>.FromFailure("Failed to create license.");

        return Result<int>.Success(entity.LicenseID);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result> UpdateAsync(UpdateLicenseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation = LicenseValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.Error);
        }

        var exists = await _repository.IsLicenseExistsAsync(dto.LicenseID);

        if (!exists)
        {
            return Result.NotFound("License not found.");
        }

        var entity = LicenseMapper.ToEntity(dto);
        var updated = await _repository.UpdateLicenseAsync(entity);

        if (!updated)
        {
            return Result.Failure("Failed to update license.");
        }

        // PERSIST THROUGH UNIT OF WORK
        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0)
        {
            return Result.Failure("No license changes were saved.");
        }

        return Result.Success();
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result> DeleteAsync(int id)
    {
        var validation = LicenseValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.Error);
        }

        var exists = await _repository.IsLicenseExistsAsync(id);

        if (!exists)
        {
            return Result.NotFound("License not found.");
        }

        var deleted = await _repository.DeleteLicenseAsync(id);

        if (!deleted)
        {
            return Result.Failure("Failed to delete license.");
        }

        // PERSIST THROUGH UNIT OF WORK
        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0)
        {
            return Result.Failure("Failed to save license deletion.");
        }

        return Result.Success();
    }
}