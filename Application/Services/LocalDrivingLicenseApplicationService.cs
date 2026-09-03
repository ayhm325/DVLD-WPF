using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;
using System.Data;

namespace Application.Services;

public class LocalDrivingLicenseApplicationService
    : ILocalDrivingLicenseApplicationService
{
    private const int NewLocalDrivingLicenseApplicationTypeId = 1;

    private readonly ILocalDrivingLicenseApplicationRepository _repository;
    private readonly ILicenseRepository _licenseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationRepository _applicationRepository;
    private readonly ILicenseClassService _licenseClassService;

    public LocalDrivingLicenseApplicationService(
        ILocalDrivingLicenseApplicationRepository repository,
        ILicenseRepository licenseRepository,
        IUnitOfWork unitOfWork,
        IApplicationRepository applicationRepository,
        ILicenseClassService licenseClassService)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _licenseRepository = licenseRepository
            ?? throw new ArgumentNullException(nameof(licenseRepository));

        _unitOfWork = unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _applicationRepository = applicationRepository
            ?? throw new ArgumentNullException(nameof(applicationRepository));

        _licenseClassService = licenseClassService
            ?? throw new ArgumentNullException(nameof(licenseClassService));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetAllLocalDrivingLicenseApplicationsAsync()
    {
        var entities = await _repository.GetAllAsync();

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(await MapListToDtoAsync(entities));
    }

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

        var entity = await _repository.GetByIdAsync(id);

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

        return Result<LocalDrivingLicenseApplicationListDto>.Success(
            LocalDrivingLicenseApplicationMapper.ToDto(
                entity,
                passedTestCount,
                hasLicense));
    }

    public async Task<Result<int>>
        AddLocalDrivingLicenseApplicationAsync(
            CreateLocalDrivingLicenseApplicationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        var application =
            await _applicationRepository.GetApplicationByIdAsync(
                dto.ApplicationID);

        if (application is null)
        {
            return Result<int>.FromNotFound(
                "Main application not found.");
        }

        if (application.ApplicationStatus != AppStatus.New)
        {
            return Result<int>.FromConflict(
                "The main application must have New status.");
        }

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(
                dto.LicenseClassID);

        if (licenseClassResult.IsFailure)
        {
            return Result<int>.FromFailure(
                licenseClassResult.Error);
        }

        var existing =
            await _repository.GetByApplicationIdAsync(
                dto.ApplicationID);

        if (existing.Count > 0)
        {
            return Result<int>.FromConflict(
                "A local driving license application already exists " +
                "for this main application.");
        }

        var entity = new LocalDrivingLicenseApplication
        {
            ApplicationID = dto.ApplicationID,
            LicenseClassID = dto.LicenseClassID
        };

        await _repository
            .CreateLocalDrivingLicenseApplicationAsync(entity);

        var saved = await _unitOfWork.SaveChangesAsync();

        return saved <= 0 ||
               entity.LocalDrivingLicenseApplicationID <= 0
            ? Result<int>.FromFailure(
                "Failed to create local driving license application.")
            : Result<int>.Success(
                entity.LocalDrivingLicenseApplicationID);
    }

    public async Task<Result<int>>
        CreateLocalDrivingLicenseApplicationAsync(
            CreateApplicationDto applicationDto,
            CreateLocalDrivingLicenseApplicationDto localApplicationDto)
    {
        ArgumentNullException.ThrowIfNull(applicationDto);
        ArgumentNullException.ThrowIfNull(localApplicationDto);

        var applicationValidation =
            ApplicationValidator.ValidateCreate(applicationDto);

        if (applicationValidation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                applicationValidation.Error);
        }

        var localValidation =
            LocalDrivingLicenseApplicationValidator.ValidateCreate(
                localApplicationDto);

        if (localValidation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                localValidation.Error);
        }

        if (applicationDto.ApplicationTypeID !=
            NewLocalDrivingLicenseApplicationTypeId)
        {
            return Result<int>.FromValidationFailure(
                "Invalid application type for a local driving license application.");
        }

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(
                localApplicationDto.LicenseClassID);

        if (licenseClassResult.IsFailure)
        {
            return Result<int>.FromFailure(
                licenseClassResult.Error);
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var duplicateApplicationId =
                await _applicationRepository.HasDuplicateApplicationAsync(
                    applicationDto.ApplicantPersonID,
                    localApplicationDto.LicenseClassID);

            if (duplicateApplicationId.HasValue)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromConflict(
                    "A local driving license application already exists " +
                    "for this person and license class. " +
                    $"Application ID: {duplicateApplicationId.Value}");
            }

            var applicationEntity =
                ApplicationMapper.ToEntity(applicationDto);

            await _applicationRepository
                .AddNewApplicationAsync(applicationEntity);

            var applicationSaved =
                await _unitOfWork.SaveChangesAsync();

            if (applicationSaved <= 0 ||
                applicationEntity.ApplicationID <= 0)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to create the main application.");
            }

            localApplicationDto.ApplicationID =
                applicationEntity.ApplicationID;

            var localApplicationEntity =
                new LocalDrivingLicenseApplication
                {
                    ApplicationID =
                        applicationEntity.ApplicationID,

                    LicenseClassID =
                        localApplicationDto.LicenseClassID
                };

            await _repository
                .CreateLocalDrivingLicenseApplicationAsync(
                    localApplicationEntity);

            var localApplicationSaved =
                await _unitOfWork.SaveChangesAsync();

            if (localApplicationSaved <= 0 ||
                localApplicationEntity
                    .LocalDrivingLicenseApplicationID <= 0)
            {
                await transaction.RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to create the local driving license application.");
            }

            await transaction.CommitAsync();

            return Result<int>.Success(
                applicationEntity.ApplicationID);
        }
        catch (Exception)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
            }

            return Result<int>.FromFailure(
                "Failed to create the local driving license application.");
        }
    }

    public async Task<Result>
        UpdateLocalDrivingLicenseApplicationAsync(
            int id,
            UpdateLocalDrivingLicenseApplicationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateUpdate(
                id,
                dto);

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

        if (existing.Application is null)
        {
            return Result.Failure(
                "Main application information is missing.");
        }

        if (existing.Application.ApplicationStatus != AppStatus.New)
        {
            return Result.Conflict(
                "Only a New application can be updated.");
        }

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(
                dto.LicenseClassID);

        if (licenseClassResult.IsFailure)
        {
            return Result.Failure(
                licenseClassResult.Error);
        }

        if (existing.LicenseClassID != dto.LicenseClassID)
        {
            await using var transaction =
                await _unitOfWork.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var duplicateApplicationId =
                    await _applicationRepository
                        .HasDuplicateApplicationAsync(
                            existing.Application.ApplicantPersonID,
                            dto.LicenseClassID);

                if (duplicateApplicationId.HasValue &&
                    duplicateApplicationId.Value !=
                    existing.ApplicationID)
                {
                    await transaction.RollbackAsync();

                    return Result.Conflict(
                        "Another local driving license application " +
                        "already exists for this person and license class.");
                }

                existing.LicenseClassID =
                    dto.LicenseClassID;

                if (!await _repository.UpdateAsync(existing))
                {
                    await transaction.RollbackAsync();

                    return Result.Failure(
                        "Failed to update local driving license application.");
                }

                var saved =
                    await _unitOfWork.SaveChangesAsync();

                if (saved <= 0)
                {
                    await transaction.RollbackAsync();

                    return Result.Failure(
                        "No local driving license application changes were saved.");
                }

                await transaction.CommitAsync();

                return Result.Success();
            }
            catch (Exception)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                }

                return Result.Failure(
                    "Failed to update local driving license application.");
            }
        }

        return Result.Success();
    }

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

        if (existing.Application is null)
        {
            return Result.Failure(
                "Main application information is missing.");
        }

        if (existing.Application.ApplicationStatus != AppStatus.New)
        {
            return Result.Conflict(
                "Only a New application can be deleted.");
        }

        if (!await _repository.DeleteAsync(id))
        {
            return Result.Failure(
                "Failed to delete local driving license application.");
        }

        return await _unitOfWork.SaveChangesAsync() <= 0
            ? Result.Failure(
                "Failed to save local driving license application deletion.")
            : Result.Success();
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
            int applicantPersonId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidatePersonId(
                applicantPersonId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByPersonIdAsync(
                applicantPersonId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(await MapListToDtoAsync(entities));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
            int applicationId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateApplicationId(
                applicationId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByApplicationIdAsync(
                applicationId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(await MapListToDtoAsync(entities));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
            int licenseClassId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateLicenseClassId(
                licenseClassId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);
        }

        var entities =
            await _repository.GetByLicenseClassIdAsync(
                licenseClassId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(await MapListToDtoAsync(entities));
    }

    public async Task<Result<int>>
        GetApplicationIdByLocalIdAsync(int localId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateId(localId);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        var applicationId =
            await _repository.GetApplicationIdByLocalIdAsync(localId);

        return !applicationId.HasValue
            ? Result<int>.FromNotFound(
                "Main application not found for this local application.")
            : Result<int>.Success(applicationId.Value);
    }

    public async Task<bool>
        IsLocalDrivingLicenseApplicationExistsAsync(int id)
        =>
            id > 0 &&
            await _repository.GetByIdAsync(id) is not null;

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
                await _licenseRepository.IsApplicationHasLicenseAsync(
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