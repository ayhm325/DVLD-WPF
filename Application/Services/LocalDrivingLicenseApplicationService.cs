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

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetAllLocalDrivingLicenseApplicationsAsync()
    {
        var entities = await _repository.GetAllAsync();
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(await MapListToDtoAsync(entities));
    }

    public async Task<Result<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationByIdAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);
        if (validation.IsFailure) return Result<LocalDrivingLicenseApplicationListDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return Result<LocalDrivingLicenseApplicationListDto>.FromNotFound("Local driving license application not found.");

        var passedTestCount = await _repository.GetPassedTestCountAsync(entity.LocalDrivingLicenseApplicationID);
        var hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(entity.ApplicationID);

        return Result<LocalDrivingLicenseApplicationListDto>.Success(LocalDrivingLicenseApplicationMapper.ToDto(entity, passedTestCount, hasLicense));
    }

    public async Task<Result<int>> AddLocalDrivingLicenseApplicationAsync(CreateLocalDrivingLicenseApplicationDto dto)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateCreate(dto);
        if (validation.IsFailure) return Result<int>.FromValidationFailure(validation.Error);

        var entity = new LocalDrivingLicenseApplication
        {
            ApplicationID = dto.ApplicationID,
            LicenseClassID = dto.LicenseClassID
        };

        await _repository.CreateLocalDrivingLicenseApplicationAsync(entity);
        var saved = await _unitOfWork.SaveChangesAsync();

        return saved <= 0 || entity.LocalDrivingLicenseApplicationID <= 0
            ? Result<int>.FromFailure("Failed to create local driving license application.")
            : Result<int>.Success(entity.LocalDrivingLicenseApplicationID);
    }

    public async Task<Result> UpdateLocalDrivingLicenseApplicationAsync(int id, UpdateLocalDrivingLicenseApplicationDto dto)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateUpdate(id, dto);
        if (validation.IsFailure) return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return Result.NotFound("Local driving license application not found.");

        existing.LicenseClassID = dto.LicenseClassID;
        if (!await _repository.UpdateAsync(existing)) return Result.Failure("Failed to update local driving license application.");

        return await _unitOfWork.SaveChangesAsync() <= 0
            ? Result.Failure("No local driving license application changes were saved.")
            : Result.Success();
    }

    public async Task<Result> DeleteLocalDrivingLicenseApplicationAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);
        if (validation.IsFailure) return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null) return Result.NotFound("Local driving license application not found.");

        if (!await _repository.DeleteAsync(id)) return Result.Failure("Failed to delete local driving license application.");

        return await _unitOfWork.SaveChangesAsync() <= 0
            ? Result.Failure("Failed to save local driving license application deletion.")
            : Result.Success();
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidatePersonId(applicantPersonId);
        if (validation.IsFailure) return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(await MapListToDtoAsync(await _repository.GetByPersonIdAsync(applicantPersonId)));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure) return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(await MapListToDtoAsync(await _repository.GetByApplicationIdAsync(applicationId)));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateLicenseClassId(licenseClassId);
        if (validation.IsFailure) return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(await MapListToDtoAsync(await _repository.GetByLicenseClassIdAsync(licenseClassId)));
    }

    public async Task<Result<int>> GetApplicationIdByLocalIdAsync(int localId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(localId);
        if (validation.IsFailure) return Result<int>.FromValidationFailure(validation.Error);

        var applicationId = await _repository.GetApplicationIdByLocalIdAsync(localId);
        return !applicationId.HasValue
            ? Result<int>.FromNotFound("Main application not found for this local application.")
            : Result<int>.Success(applicationId.Value);
    }

    public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id)
        => id > 0 && await _repository.GetByIdAsync(id) is not null;

    private async Task<List<LocalDrivingLicenseApplicationListDto>> MapListToDtoAsync(List<LocalDrivingLicenseApplication> entities)
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