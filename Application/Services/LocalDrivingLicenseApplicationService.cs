using Application.Common.Results;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class LocalDrivingLicenseApplicationService : ILocalDrivingLicenseApplicationService
{
    private readonly ILocalDrivingLicenseApplicationRepository _repository;
    private readonly ILicenseRepository _licenseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LocalDrivingLicenseApplicationService(
        ILocalDrivingLicenseApplicationRepository repository,
        ILicenseRepository licenseRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _licenseRepository = licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetAllLocalDrivingLicenseApplicationsAsync()
    {
        var entities = await _repository.GetAllAsync();
        var dtoList = await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationByIdAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<LocalDrivingLicenseApplicationListDto>.FromValidationFailure(validation.Error);
        }

        var entity = await _repository.GetByIdAsync(id);

        if (entity is null)
        {
            return Result<LocalDrivingLicenseApplicationListDto>.FromNotFound("Local driving license application not found.");
        }

        var passedTestCount = await _repository.GetPassedTestCountAsync(entity.LocalDrivingLicenseApplicationID);
        var hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(entity.ApplicationID);

        var dto = LocalDrivingLicenseApplicationMapper.ToDto(entity, passedTestCount, hasLicense);

        return Result<LocalDrivingLicenseApplicationListDto>.Success(dto);
    }

    // =========================================================
    // ADD
    // =========================================================

    public async Task<Result<int>> AddLocalDrivingLicenseApplicationAsync(CreateLocalDrivingLicenseApplicationDto dto)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(validation.Error);
        }

        // Create Entity
        var entity = new LocalDrivingLicenseApplication
        {
            ApplicationID = dto.ApplicationID,
            LicenseClassID = dto.LicenseClassID
        };

        // Add to DbContext
        await _repository.CreateLocalDrivingLicenseApplicationAsync(entity);

        // Persist through UnitOfWork — database-generated ID is guaranteed after SaveChangesAsync()
        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0 || entity.LocalDrivingLicenseApplicationID <= 0)
        {
            return Result<int>.FromFailure("Failed to create local driving license application.");
        }

        return Result<int>.Success(entity.LocalDrivingLicenseApplicationID);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result> UpdateLocalDrivingLicenseApplicationAsync(int id, UpdateLocalDrivingLicenseApplicationDto dto)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateUpdate(id, dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.Error);
        }

        // Load existing entity
        var existing = await _repository.GetByIdAsync(id);

        if (existing is null)
        {
            return Result.NotFound("Local driving license application not found.");
        }

        // Apply changes
        existing.LicenseClassID = dto.LicenseClassID;

        // Stage update
        var updated = await _repository.UpdateAsync(existing);

        if (!updated)
        {
            return Result.Failure("Failed to update local driving license application.");
        }

        // Persist through UnitOfWork
        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0)
        {
            return Result.Failure("No local driving license application changes were saved.");
        }

        return Result.Success();
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result> DeleteLocalDrivingLicenseApplicationAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.Error);
        }

        // Check exists
        var existing = await _repository.GetByIdAsync(id);

        if (existing is null)
        {
            return Result.NotFound("Local driving license application not found.");
        }

        // Delete
        var deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            return Result.Failure("Failed to delete local driving license application.");
        }

        // Persist through UnitOfWork
        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0)
        {
            return Result.Failure("Failed to save local driving license application deletion.");
        }

        return Result.Success();
    }

    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidatePersonId(applicantPersonId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);
        }

        var entities = await _repository.GetByPersonIdAsync(applicantPersonId);
        var dtoList = await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateApplicationId(applicationId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);
        }

        var entities = await _repository.GetByApplicationIdAsync(applicationId);
        var dtoList = await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // =========================================================
    // GET BY LICENSE CLASS ID
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateLicenseClassId(licenseClassId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);
        }

        var entities = await _repository.GetByLicenseClassIdAsync(licenseClassId);
        var dtoList = await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // =========================================================
    // GET APPLICATION ID BY LOCAL ID
    // =========================================================

    public async Task<Result<int>> GetApplicationIdByLocalIdAsync(int localId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(localId);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(validation.Error);
        }

        var applicationId = await _repository.GetApplicationIdByLocalIdAsync(localId);

        if (!applicationId.HasValue)
        {
            return Result<int>.FromNotFound("Main application not found for this local application.");
        }

        return Result<int>.Success(applicationId.Value);
    }

    // =========================================================
    // CHECK EXISTS
    // =========================================================

    public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id)
    {
        if (id <= 0)
            return false;

        return await _repository.GetByIdAsync(id) is not null;
    }

    // =========================================================
    // MAP LIST
    // =========================================================

    private async Task<List<LocalDrivingLicenseApplicationListDto>> MapListToDtoAsync(
        List<LocalDrivingLicenseApplication> entities)
    {
        var dtoList = new List<LocalDrivingLicenseApplicationListDto>();

        foreach (var entity in entities)
        {
            var passedTestCount = await _repository.GetPassedTestCountAsync(entity.LocalDrivingLicenseApplicationID);
            var hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(entity.ApplicationID);

            dtoList.Add(LocalDrivingLicenseApplicationMapper.ToDto(entity, passedTestCount, hasLicense));
        }

        return dtoList;
    }
}