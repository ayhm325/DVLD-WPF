using Application.Common.Results;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class LocalDrivingLicenseApplicationService
    : ILocalDrivingLicenseApplicationService
{
    private readonly ILocalDrivingLicenseApplicationRepository _repository;
    private readonly ILicenseRepository _licenseRepository;

    public LocalDrivingLicenseApplicationService(
        ILocalDrivingLicenseApplicationRepository repository,
        ILicenseRepository licenseRepository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _licenseRepository = licenseRepository
            ?? throw new ArgumentNullException(nameof(licenseRepository));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetAllLocalDrivingLicenseApplicationsAsync()
    {
        var entities = await _repository.GetAllAsync();

        var dtoList = await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(dtoList);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<LocalDrivingLicenseApplicationListDto>>
        GetLocalDrivingLicenseApplicationByIdAsync(int id)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<LocalDrivingLicenseApplicationListDto>
                .FromValidationFailure(validation.Error);
        }

        var entity =
            await _repository.GetByIdAsync(id);

        if (entity is null)
        {
            return Result<LocalDrivingLicenseApplicationListDto>
                .FromNotFound(
                    "Local driving license application not found.");
        }

        var passedTestCount =
            await _repository.GetPassedTestCountAsync(
                entity.LocalDrivingLicenseApplicationID);

        var hasLicense =
            await _licenseRepository.IsApplicationHasLicenseAsync(
                entity.ApplicationID);

        var dto =
            LocalDrivingLicenseApplicationMapper.ToDto(
                entity,
                passedTestCount,
                hasLicense);

        return Result<LocalDrivingLicenseApplicationListDto>
            .Success(dto);
    }

    // =========================================================
    // ADD
    // =========================================================

    public async Task<Result<int>>
        AddLocalDrivingLicenseApplicationAsync(
            CreateLocalDrivingLicenseApplicationDto dto)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        var entity = new LocalDrivingLicenseApplication
        {
            ApplicationID = dto.ApplicationID,
            LicenseClassID = dto.LicenseClassID
        };

        var id =
            await _repository
                .CreateLocalDrivingLicenseApplicationAsync(entity);

        if (id <= 0)
        {
            return Result<int>.FromFailure(
                "Failed to create local driving license application.");
        }

        return Result<int>.Success(id);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateLocalDrivingLicenseApplicationAsync(
            int id,
            UpdateLocalDrivingLicenseApplicationDto dto)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateUpdate(id, dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var existing =
            await _repository.GetByIdAsync(id);

        if (existing is null)
        {
            return Result.NotFound(
                "Local driving license application not found.");
        }

        existing.LicenseClassID =
            dto.LicenseClassID;

        var isSuccess =
            await _repository.UpdateAsync(existing);

        return isSuccess
            ? Result.Success()
            : Result.Failure(
                "Failed to update application.");
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteLocalDrivingLicenseApplicationAsync(int id)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var existing =
            await _repository.GetByIdAsync(id);

        if (existing is null)
        {
            return Result.NotFound(
                "Local driving license application not found.");
        }

        var isSuccess =
            await _repository.DeleteAsync(id);

        return isSuccess
            ? Result.Success()
            : Result.Failure(
                "Failed to delete application.");
    }

    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
            int applicantPersonId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidatePersonId(applicantPersonId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByPersonIdAsync(
                applicantPersonId);

        var dtoList =
            await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(dtoList);
    }

    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
            int applicationId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateApplicationId(applicationId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByApplicationIdAsync(
                applicationId);

        var dtoList =
            await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(dtoList);
    }

    // =========================================================
    // GET BY LICENSE CLASS ID
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
            int licenseClassId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateLicenseClassId(licenseClassId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByLicenseClassIdAsync(
                licenseClassId);

        var dtoList =
            await MapListToDtoAsync(entities);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(dtoList);
    }

    // =========================================================
    // GET APPLICATION ID BY LOCAL ID
    // =========================================================

    public async Task<Result<int>>
        GetApplicationIdByLocalIdAsync(int localId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateId(localId);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        var applicationId =
            await _repository
                .GetApplicationIdByLocalIdAsync(localId);

        if (!applicationId.HasValue)
        {
            return Result<int>.FromNotFound(
                "Main application not found for this local application.");
        }

        return Result<int>.Success(
            applicationId.Value);
    }

    // =========================================================
    // CHECK
    // =========================================================

    public async Task<bool>
        IsLocalDrivingLicenseApplicationExistsAsync(int id)
    {
        if (id <= 0)
            return false;

        return await _repository.GetByIdAsync(id) is not null;
    }

    // =========================================================
    // MAP LIST
    // =========================================================

    private async Task<List<LocalDrivingLicenseApplicationListDto>>
        MapListToDtoAsync(
            List<LocalDrivingLicenseApplication> entities)
    {
        var dtoList =
            new List<LocalDrivingLicenseApplicationListDto>();

        foreach (var entity in entities)
        {
            var passedTestCount =
                await _repository.GetPassedTestCountAsync(
                    entity.LocalDrivingLicenseApplicationID);

            var hasLicense =
                await _licenseRepository
                    .IsApplicationHasLicenseAsync(
                        entity.ApplicationID);

            dtoList.Add(
                LocalDrivingLicenseApplicationMapper.ToDto(
                    entity,
                    passedTestCount,
                    hasLicense));
        }

        return dtoList;
    }
}