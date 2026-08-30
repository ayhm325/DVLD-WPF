using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;

namespace Application.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _repository;

    public LicenseService(ILicenseRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
        {
            return Result<int>.FromValidationFailure(validation.Error);
        }

        var entity = LicenseMapper.ToEntity(dto);
        var id = await _repository.AddLicenseAsync(entity);

        if (id <= 0)
        {
            return Result<int>.FromFailure("Failed to create license.");
        }

        return Result<int>.Success(id);
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

        return updated
            ? Result.Success()
            : Result.Failure("Failed to update license.");
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

        return deleted
            ? Result.Success()
            : Result.Failure("Failed to delete license.");
    }
}