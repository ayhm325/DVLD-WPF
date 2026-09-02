using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
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
    private readonly IApplicationRepository _applicationRepository;

    public LocalDrivingLicenseApplicationService(
        ILocalDrivingLicenseApplicationRepository repository,
        ILicenseRepository licenseRepository,
        IUnitOfWork unitOfWork,
        IApplicationRepository applicationRepository)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(nameof(repository));

        _licenseRepository =
            licenseRepository
            ?? throw new ArgumentNullException(nameof(licenseRepository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));

        _applicationRepository =
            applicationRepository
            ?? throw new ArgumentNullException(nameof(applicationRepository));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetAllLocalDrivingLicenseApplicationsAsync()
    {
        var entities =
            await _repository.GetAllAsync();

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(
                await MapListToDtoAsync(entities));
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<LocalDrivingLicenseApplicationListDto>>
        GetLocalDrivingLicenseApplicationByIdAsync(int id)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<LocalDrivingLicenseApplicationListDto>
                .FromValidationFailure(
                    validation.Error);
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
            await _repository
                .GetPassedTestCountAsync(
                    entity.LocalDrivingLicenseApplicationID);

        var hasLicense =
            await _licenseRepository
                .IsApplicationHasLicenseAsync(
                    entity.ApplicationID);

        return Result<LocalDrivingLicenseApplicationListDto>
            .Success(
                LocalDrivingLicenseApplicationMapper.ToDto(
                    entity,
                    passedTestCount,
                    hasLicense));
    }


    // =========================================================
    // ADD LOCAL APPLICATION
    // =========================================================

    public async Task<Result<int>>
        AddLocalDrivingLicenseApplicationAsync(
            CreateLocalDrivingLicenseApplicationDto dto)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    dto.ApplicationID,

                LicenseClassID =
                    dto.LicenseClassID
            };

        await _repository
            .CreateLocalDrivingLicenseApplicationAsync(
                entity);

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved <= 0 ||
               entity.LocalDrivingLicenseApplicationID <= 0

            ? Result<int>.FromFailure(
                "Failed to create local driving license application.")

            : Result<int>.Success(
                entity.LocalDrivingLicenseApplicationID);
    }


    // =========================================================
    // CREATE LOCAL DRIVING LICENSE APPLICATION
    // =========================================================

    public async Task<Result<int>>
        CreateLocalDrivingLicenseApplicationAsync(
            CreateApplicationDto applicationDto,
            CreateLocalDrivingLicenseApplicationDto localApplicationDto)
    {
        ArgumentNullException.ThrowIfNull(
            applicationDto);

        ArgumentNullException.ThrowIfNull(
            localApplicationDto);


        // -----------------------------------------------------
        // 1. Validate main application
        // -----------------------------------------------------

        var applicationValidation =
            ApplicationValidator
                .ValidateCreate(applicationDto);

        if (applicationValidation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    applicationValidation.Error);
        }


        // -----------------------------------------------------
        // 2. Validate local application
        // -----------------------------------------------------

        var localValidation =
            LocalDrivingLicenseApplicationValidator
                .ValidateCreate(
                    localApplicationDto);

        if (localValidation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    localValidation.Error);
        }


        // -----------------------------------------------------
        // 3. Check duplicate application
        // -----------------------------------------------------

        var duplicateApplicationId =
            await _applicationRepository
                .HasDuplicateApplicationAsync(
                    applicationDto.ApplicantPersonID,
                    localApplicationDto.LicenseClassID);

        if (duplicateApplicationId.HasValue)
        {
            return Result<int>.FromConflict(
                $"A local driving license application already exists " +
                $"for this person and license class. " +
                $"Application ID: {duplicateApplicationId.Value}");
        }


        // -----------------------------------------------------
        // 4. Begin transaction
        // -----------------------------------------------------

        await using var transaction =
            await _unitOfWork
                .BeginTransactionAsync();

        try
        {
            // =================================================
            // 5. CREATE MAIN APPLICATION
            // =================================================

            var applicationEntity =
                ApplicationMapper
                    .ToEntity(applicationDto);

            await _applicationRepository
                .AddNewApplicationAsync(
                    applicationEntity);

            var applicationSaved =
                await _unitOfWork
                    .SaveChangesAsync();

            if (applicationSaved <= 0 ||
                applicationEntity.ApplicationID <= 0)
            {
                await transaction
                    .RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to create the main application.");
            }


            // =================================================
            // 6. CREATE LOCAL APPLICATION
            // =================================================

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
                await _unitOfWork
                    .SaveChangesAsync();

            if (localApplicationSaved <= 0 ||
                localApplicationEntity
                    .LocalDrivingLicenseApplicationID <= 0)
            {
                await transaction
                    .RollbackAsync();

                return Result<int>.FromFailure(
                    "Failed to create the local driving license application.");
            }


            // =================================================
            // 7. COMMIT
            // =================================================

            await transaction
                .CommitAsync();

            return Result<int>
                .Success(
                    applicationEntity.ApplicationID);
        }
        catch (Exception)
        {
            try
            {
                await transaction
                    .RollbackAsync();
            }
            catch
            {
                // Preserve the original failure.
            }

            return Result<int>.FromFailure(
                "Failed to create the local driving license application.");
        }
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
                .ValidateUpdate(
                    id,
                    dto);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }

        var existing =
            await _repository
                .GetByIdAsync(id);

        if (existing is null)
        {
            return Result
                .NotFound(
                    "Local driving license application not found.");
        }

        existing.LicenseClassID =
            dto.LicenseClassID;

        if (!await _repository
                .UpdateAsync(existing))
        {
            return Result
                .Failure(
                    "Failed to update local driving license application.");
        }

        return await _unitOfWork
            .SaveChangesAsync() <= 0

            ? Result.Failure(
                "No local driving license application changes were saved.")

            : Result.Success();
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteLocalDrivingLicenseApplicationAsync(
            int id)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }

        var existing =
            await _repository
                .GetByIdAsync(id);

        if (existing is null)
        {
            return Result
                .NotFound(
                    "Local driving license application not found.");
        }

        if (!await _repository
                .DeleteAsync(id))
        {
            return Result
                .Failure(
                    "Failed to delete local driving license application.");
        }

        return await _unitOfWork
            .SaveChangesAsync() <= 0

            ? Result.Failure(
                "Failed to save local driving license application deletion.")

            : Result.Success();
    }


    // =========================================================
    // GET BY PERSON
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(
            int applicantPersonId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidatePersonId(
                    applicantPersonId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByPersonIdAsync(
                    applicantPersonId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(
                await MapListToDtoAsync(entities));
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
                .ValidateApplicationId(
                    applicationId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByApplicationIdAsync(
                    applicationId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(
                await MapListToDtoAsync(entities));
    }


    // =========================================================
    // GET BY LICENSE CLASS
    // =========================================================

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>>
        GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(
            int licenseClassId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateLicenseClassId(
                    licenseClassId);

        if (validation.IsFailure)
        {
            return Result<List<LocalDrivingLicenseApplicationListDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var entities =
            await _repository
                .GetByLicenseClassIdAsync(
                    licenseClassId);

        return Result<List<LocalDrivingLicenseApplicationListDto>>
            .Success(
                await MapListToDtoAsync(entities));
    }


    // =========================================================
    // GET APPLICATION ID BY LOCAL ID
    // =========================================================

    public async Task<Result<int>>
        GetApplicationIdByLocalIdAsync(
            int localId)
    {
        var validation =
            LocalDrivingLicenseApplicationValidator
                .ValidateId(
                    localId);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    validation.Error);
        }

        var applicationId =
            await _repository
                .GetApplicationIdByLocalIdAsync(
                    localId);

        return !applicationId.HasValue

            ? Result<int>.FromNotFound(
                "Main application not found for this local application.")

            : Result<int>.Success(
                applicationId.Value);
    }


    // =========================================================
    // EXISTS
    // =========================================================

    public async Task<bool>
        IsLocalDrivingLicenseApplicationExistsAsync(
            int id)
        =>
            id > 0 &&
            await _repository
                .GetByIdAsync(id) is not null;


    // =========================================================
    // MAP LIST TO DTO
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
                await _repository
                    .GetPassedTestCountAsync(
                        entity.LocalDrivingLicenseApplicationID);

            var hasLicense =
                await _licenseRepository
                    .IsApplicationHasLicenseAsync(
                        entity.ApplicationID);

            dtoList.Add(
                LocalDrivingLicenseApplicationMapper
                    .ToDto(
                        entity,
                        passedTestCount,
                        hasLicense));
        }

        return dtoList;
    }
}