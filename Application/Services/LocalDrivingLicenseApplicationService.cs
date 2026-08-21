using Application.Common.Results;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class LocalDrivingLicenseApplicationService : ILocalDrivingLicenseApplicationService
{
    private readonly ILocalDrivingLicenseApplicationRepository _repository;
    private readonly ILicenseRepository _licenseRepository;

    public LocalDrivingLicenseApplicationService(
        ILocalDrivingLicenseApplicationRepository repository,
        ILicenseRepository licenseRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _licenseRepository = licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
    }

    // GET ALL
    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetAllLocalDrivingLicenseApplicationsAsync()
    {
        var entities = await _repository.GetAllAsync();
        var dtoList = new List<LocalDrivingLicenseApplicationListDto>();

        foreach (var entity in entities)
        {
            var count = await _repository.GetPassedTestCountAsync(entity.LocalDrivingLicenseApplicationID);
            var hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(entity.ApplicationID);
            dtoList.Add(MapToDto(entity, count, hasLicense));
        }

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // GET BY ID
    public async Task<Result<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationByIdAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<LocalDrivingLicenseApplicationListDto>.FromFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result<LocalDrivingLicenseApplicationListDto>.FromFailure("Local driving license application not found.");

        var count = await _repository.GetPassedTestCountAsync(entity.LocalDrivingLicenseApplicationID);
        var hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(entity.ApplicationID);

        return Result<LocalDrivingLicenseApplicationListDto>.Success(MapToDto(entity, count, hasLicense));
    }

    // ADD
    public async Task<Result<int>> AddLocalDrivingLicenseApplicationAsync(CreateLocalDrivingLicenseApplicationDto dto)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        var entity = new LocalDrivingLicenseApplication
        {
            ApplicationID = dto.ApplicationID,
            LicenseClassID = dto.LicenseClassID
        };

        var id = await _repository.CreateLocalDrivingLicenseApplicationAsync(entity);
        return Result<int>.Success(id);
    }

    // UPDATE
    public async Task<Result> UpdateLocalDrivingLicenseApplicationAsync(int id, UpdateLocalDrivingLicenseApplicationDto dto)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateUpdate(id, dto);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return Result.Failure("Local driving license application not found.");

        existing.LicenseClassID = dto.LicenseClassID;
        var isSuccess = await _repository.UpdateAsync(existing);
        return isSuccess ? Result.Success() : Result.Failure("Failed to update application.");
    }

    // DELETE
    public async Task<Result> DeleteLocalDrivingLicenseApplicationAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            return Result.Failure("Local driving license application not found.");

        var isSuccess = await _repository.DeleteAsync(id);
        return isSuccess ? Result.Success() : Result.Failure("Failed to delete application.");
    }

    // GET BY PERSON ID
    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidatePersonId(applicantPersonId);
        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromFailure(validation.Error);

        var list = await _repository.GetByPersonIdAsync(applicantPersonId);
        var dtoList = await MapListToDtoAsync(list);
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // GET BY APPLICATION ID
    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromFailure(validation.Error);

        var list = await _repository.GetByApplicationIdAsync(applicationId);
        var dtoList = await MapListToDtoAsync(list);
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // GET BY LICENSE CLASS ID
    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateLicenseClassId(licenseClassId);
        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromFailure(validation.Error);

        var list = await _repository.GetByLicenseClassIdAsync(licenseClassId);
        var dtoList = await MapListToDtoAsync(list);
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(dtoList);
    }

    // GET APPLICATION ID BY LOCAL ID
    public async Task<Result<int>> GetApplicationIdByLocalIdAsync(int localId)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(localId);
        if (validation.IsFailure)
            return Result<int>.FromFailure(validation.Error);

        var applicationId = await _repository.GetApplicationIdByLocalIdAsync(localId);
        if (!applicationId.HasValue)
            return Result<int>.FromFailure("Main application not found for this local application.");

        return Result<int>.Success(applicationId.Value);
    }

    // CHECK
    public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id)
    {
        if (id <= 0) return false;
        return await _repository.GetByIdAsync(id) is not null;
    }

    // MAP LIST
    private async Task<List<LocalDrivingLicenseApplicationListDto>> MapListToDtoAsync(List<LocalDrivingLicenseApplication> list)
    {
        var dtoList = new List<LocalDrivingLicenseApplicationListDto>();

        foreach (var entity in list)
        {
            var count = await _repository.GetPassedTestCountAsync(entity.LocalDrivingLicenseApplicationID);
            var hasLicense = await _licenseRepository.IsApplicationHasLicenseAsync(entity.ApplicationID);
            dtoList.Add(MapToDto(entity, count, hasLicense));
        }

        return dtoList;
    }

    // MAP ENTITY -> DTO
    private static LocalDrivingLicenseApplicationListDto MapToDto(
        LocalDrivingLicenseApplication entity,
        int passedTestCount,
        bool hasLicense)
    {
        return new LocalDrivingLicenseApplicationListDto
        {
            LocalDrivingLicenseApplicationID = entity.LocalDrivingLicenseApplicationID,
            LicenseClassID = entity.LicenseClassID,
            LicenseClassName = entity.LicenseClass?.ClassName ?? "N/A",
            NationalNo = entity.Application?.Person?.NationalNo ?? "N/A",
            Fees = entity.LicenseClass?.ClassFees ?? 0,
            FullName = $"{entity.Application?.Person?.FirstName} " +
                        $"{entity.Application?.Person?.SecondName} " +
                        $"{entity.Application?.Person?.ThirdName} " +
                        $"{entity.Application?.Person?.LastName}".Trim(),
            ApplicationDate = entity.Application?.ApplicationDate ?? DateTime.MinValue,
            PassedTest = passedTestCount,
            ApplicationStatus = entity.Application is not null &&
                Enum.IsDefined(typeof(AppStatus), entity.Application.ApplicationStatus)
                    ? (AppStatus)entity.Application.ApplicationStatus
                    : AppStatus.Cancelled,
            HasLicense = hasLicense,
            ApplicantPersonID = entity.Application?.Person?.PersonId ?? 0
        };
    }
}