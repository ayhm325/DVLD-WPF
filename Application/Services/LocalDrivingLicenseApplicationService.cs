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

public sealed class LocalDrivingLicenseApplicationService(
    ILocalDrivingLicenseApplicationRepository repository,
    ILicenseRepository licenseRepository,
    IUnitOfWork unitOfWork,
    IApplicationRepository applicationRepository,
    IApplicationTypeRepository applicationTypeRepository,
    ILicenseClassService licenseClassService,
    ICurrentUserService currentUserService,
    IApplicationService applicationService)
    : ILocalDrivingLicenseApplicationService
{
    private const int NewLocalDrivingLicenseApplicationTypeId = 1;

    private readonly ILocalDrivingLicenseApplicationRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    private readonly ILicenseRepository _licenseRepository =
        licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    private readonly IApplicationRepository _applicationRepository =
        applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));

    private readonly IApplicationTypeRepository _applicationTypeRepository =
        applicationTypeRepository ?? throw new ArgumentNullException(nameof(applicationTypeRepository));

    private readonly ILicenseClassService _licenseClassService =
        licenseClassService ?? throw new ArgumentNullException(nameof(licenseClassService));

    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    private readonly IApplicationService _applicationService =
        applicationService ?? throw new ArgumentNullException(nameof(applicationService));

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetAllLocalDrivingLicenseApplicationsAsync()
    {
        var entities = await _repository.GetAllAsync();
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(
            await MapListToDtoAsync(entities));
    }

    public async Task<Result<LocalDrivingLicenseApplicationListDto>>
        GetLocalDrivingLicenseApplicationByIdAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<LocalDrivingLicenseApplicationListDto>
                .FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id);
        if (entity is null)
            return Result<LocalDrivingLicenseApplicationListDto>
                .FromNotFound("Local driving license application not found.");

        var passedTestCounts = await _repository.GetPassedTestCountsAsync(
            [entity.LocalDrivingLicenseApplicationID]);

        var applicationIdsWithLicenses =
            await _licenseRepository.GetApplicationIdsWithLicensesAsync(
                [entity.ApplicationID]);

        return Result<LocalDrivingLicenseApplicationListDto>.Success(
            LocalDrivingLicenseApplicationMapper.ToDto(
                entity,
                passedTestCounts.GetValueOrDefault(
                    entity.LocalDrivingLicenseApplicationID),
                applicationIdsWithLicenses.Contains(entity.ApplicationID)));
    }

    public async Task<Result<decimal>>
        GetNewLocalDrivingLicenseApplicationFeesAsync()
    {
        var applicationType =
            await _applicationTypeRepository.GetApplicationTypeByIdAsync(
                NewLocalDrivingLicenseApplicationTypeId);

        return applicationType is null
            ? Result<decimal>.FromNotFound("Application type not found.")
            : Result<decimal>.Success(applicationType.ApplicationFees);
    }

    public async Task<Result<int>>
        CreateLocalDrivingLicenseApplicationAsync(
            int applicantPersonId,
            int licenseClassId)
    {
        var applicationDto = new CreateApplicationDto
        {
            ApplicantPersonID = applicantPersonId,
            ApplicationTypeID = NewLocalDrivingLicenseApplicationTypeId
        };

        var applicationValidation =
            ApplicationValidator.ValidateCreate(applicationDto);

        if (applicationValidation.IsFailure)
            return Result<int>.FromValidationFailure(
                applicationValidation.Error);

        var localValidation =
            LocalDrivingLicenseApplicationValidator.ValidateCreate(
                new CreateLocalDrivingLicenseApplicationDto
                {
                    ApplicationID = 0,
                    LicenseClassID = licenseClassId
                });

        if (localValidation.IsFailure)
            return Result<int>.FromValidationFailure(
                localValidation.Error);

        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<int>.FromFailure(
                "Authenticated user is required.");

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(licenseClassId);

        if (licenseClassResult.IsFailure)
            return Result<int>.FromFailure(licenseClassResult.Error);

        var applicationType =
            await _applicationTypeRepository.GetApplicationTypeByIdAsync(
                NewLocalDrivingLicenseApplicationTypeId);

        if (applicationType is null)
            return Result<int>.FromNotFound("Application type not found.");

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var duplicateApplicationId =
                await _repository.HasDuplicateApplicationAsync(
                    applicantPersonId, licenseClassId);

            if (duplicateApplicationId.HasValue)
                return Result<int>.FromConflict(
                    "A local driving license application already exists " +
                    "for this person and license class. " +
                    $"Application ID: {duplicateApplicationId.Value}");

            var applicationEntity = ApplicationMapper.ToEntity(
                applicationDto,
                applicationType.ApplicationFees,
                _currentUserService.UserId);

            await _applicationRepository.AddNewApplicationAsync(applicationEntity);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                applicationEntity.ApplicationID <= 0)
            {
                return Result<int>.FromFailure(
                    "Failed to create the main application.");
            }

            var localApplicationEntity = new LocalDrivingLicenseApplication
            {
                ApplicationID = applicationEntity.ApplicationID,
                LicenseClassID = licenseClassId
            };

            await _repository.AddAsync(localApplicationEntity);

            if (await _unitOfWork.SaveChangesAsync() <= 0 ||
                localApplicationEntity.LocalDrivingLicenseApplicationID <= 0)
            {
                return Result<int>.FromFailure(
                    "Failed to create the local driving license application.");
            }

            await transaction.CommitAsync();

            return Result<int>.Success(
                localApplicationEntity.LocalDrivingLicenseApplicationID);
        }
        catch
        {
            try { await transaction.RollbackAsync(); }
            catch { /* Preserve the original operation failure. */ }

            return Result<int>.FromFailure(
                "Failed to create the local driving license application.");
        }
    }

    public async Task<Result<int>>
        AddLocalDrivingLicenseApplicationAsync(
            CreateLocalDrivingLicenseApplicationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateCreate(dto);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        var application =
            await _applicationRepository.GetApplicationByIdAsync(dto.ApplicationID);

        if (application is null)
            return Result<int>.FromNotFound("Main application not found.");

        if (application.ApplicationTypeID != NewLocalDrivingLicenseApplicationTypeId)
            return Result<int>.FromConflict(
                "The main application must be a New Local Driving License application.");

        if (application.ApplicationStatus != AppStatus.New)
            return Result<int>.FromConflict(
                "The main application must have New status.");

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(dto.LicenseClassID);

        if (licenseClassResult.IsFailure)
            return Result<int>.FromFailure(licenseClassResult.Error);

        var existing =
            await _repository.GetByApplicationIdAsync(dto.ApplicationID);

        if (existing.Count > 0)
            return Result<int>.FromConflict(
                "A local driving license application already exists for this main application.");

        var entity = new LocalDrivingLicenseApplication
        {
            ApplicationID = dto.ApplicationID,
            LicenseClassID = dto.LicenseClassID
        };

        await _repository.AddAsync(entity);

        var saved = await _unitOfWork.SaveChangesAsync();

        return saved > 0 && entity.LocalDrivingLicenseApplicationID > 0
            ? Result<int>.Success(entity.LocalDrivingLicenseApplicationID)
            : Result<int>.FromFailure(
                "Failed to create local driving license application.");
    }

    public async Task<Result>
        UpdateLocalDrivingLicenseApplicationAsync(
            int id,
            UpdateLocalDrivingLicenseApplicationDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateUpdate(id, dto);

        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetForUpdateAsync(id);

        if (existing is null)
            return Result.NotFound(
                "Local driving license application not found.");

        if (existing.Application is null)
            return Result.Failure(
                "Main application information is missing.");

        if (existing.Application.ApplicationStatus != AppStatus.New)
            return Result.Conflict(
                "Only a New application can be updated.");

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(dto.LicenseClassID);

        if (licenseClassResult.IsFailure)
            return Result.Failure(licenseClassResult.Error);

        if (existing.LicenseClassID == dto.LicenseClassID)
            return Result.Success();

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var duplicateApplicationId =
                await _repository.HasDuplicateApplicationAsync(
                    existing.Application.ApplicantPersonID,
                    dto.LicenseClassID);

            if (duplicateApplicationId.HasValue &&
                duplicateApplicationId.Value != existing.ApplicationID)
            {
                return Result.Conflict(
                    "Another local driving license application already exists " +
                    "for this person and license class.");
            }

            existing.LicenseClassID = dto.LicenseClassID;

            if (await _unitOfWork.SaveChangesAsync() <= 0)
                return Result.Failure(
                    "No local driving license application changes were saved.");

            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            try { await transaction.RollbackAsync(); }
            catch { /* Preserve the original operation failure. */ }

            return Result.Failure(
                "Failed to update local driving license application.");
        }
    }

    public async Task<Result>
        DeleteLocalDrivingLicenseApplicationAsync(int id)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(id);

        if (existing is null)
            return Result.NotFound(
                "Local driving license application not found.");

        if (existing.Application is null)
            return Result.Failure(
                "Main application information is missing.");

        if (existing.Application.ApplicationStatus != AppStatus.New)
            return Result.Conflict(
                "Only a New application can be deleted.");

        if (!await _repository.DeleteAsync(id))
            return Result.Failure(
                "Failed to delete local driving license application.");

        return await _unitOfWork.SaveChangesAsync() > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to save local driving license application deletion.");
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
            int applicantPersonId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidatePersonId(
                applicantPersonId);

        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);

        var entities = await _repository.GetByPersonIdAsync(applicantPersonId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(
            await MapListToDtoAsync(entities));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicationIdAsync(
            int applicationId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateApplicationId(
                applicationId);

        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);

        var entities = await _repository.GetByApplicationIdAsync(applicationId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(
            await MapListToDtoAsync(entities));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
            int licenseClassId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateLicenseClassId(
                licenseClassId);

        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(validation.Error);

        var entities = await _repository.GetByLicenseClassIdAsync(licenseClassId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(
            await MapListToDtoAsync(entities));
    }

    public async Task<Result<ApplicationBasicInfoDto>>
    GetApplicationBasicInfoAsync(int localApplicationId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateId(
                localApplicationId);

        if (validation.IsFailure)
        {
            return Result<ApplicationBasicInfoDto>
                .FromValidationFailure(validation.Error);
        }

        var entity =
            await _repository.GetByIdAsync(localApplicationId);

        if (entity is null)
        {
            return Result<ApplicationBasicInfoDto>
                .FromNotFound(
                    "Local driving license application not found.");
        }

        if (entity.Application is null)
        {
            return Result<ApplicationBasicInfoDto>
                .FromNotFound(
                    "Main application information not found.");
        }

        return Result<ApplicationBasicInfoDto>.Success(
            ApplicationMapper.ToBasicInfoDto(entity.Application));
    }

    public async Task<Result<int>>
        GetApplicationIdByLocalIdAsync(int localId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateId(localId);

        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        var applicationId =
            await _repository.GetApplicationIdByLocalIdAsync(localId);

        return applicationId.HasValue
            ? Result<int>.Success(applicationId.Value)
            : Result<int>.FromNotFound(
                "Main application not found for this local application.");
    }

    public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id)
    => id > 0 && await _repository.GetByIdAsync(id) is not null;

    public async Task<Result>
        CancelLocalDrivingLicenseApplicationAsync(int localApplicationId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator.ValidateId(localApplicationId);

        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(localApplicationId);

        if (existing is null)
            return Result.NotFound(
                "Local driving license application not found.");

        if (existing.Application is null)
            return Result.Failure(
                "Main application information is missing.");

        return await _applicationService.CancelApplicationAsync(
            existing.ApplicationID);
    }

    private async Task<List<LocalDrivingLicenseApplicationListDto>>
        MapListToDtoAsync(List<LocalDrivingLicenseApplication> entities)
    {
        if (entities.Count == 0)
            return [];

        var localApplicationIds = entities
            .Select(x => x.LocalDrivingLicenseApplicationID)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var applicationIds = entities
            .Select(x => x.ApplicationID)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var passedTestCounts =
            await _repository.GetPassedTestCountsAsync(localApplicationIds);

        var applicationIdsWithLicenses =
            await _licenseRepository.GetApplicationIdsWithLicensesAsync(
                applicationIds);

        return entities.Select(entity =>
            LocalDrivingLicenseApplicationMapper.ToDto(
                entity,
                passedTestCounts.GetValueOrDefault(
                    entity.LocalDrivingLicenseApplicationID),
                applicationIdsWithLicenses.Contains(entity.ApplicationID))
        ).ToList();
    }
}