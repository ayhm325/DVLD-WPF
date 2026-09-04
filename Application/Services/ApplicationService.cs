using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public sealed class ApplicationService
    : IApplicationService
{
    private readonly IApplicationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ApplicationService(
        IApplicationRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(
                nameof(repository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));

        _currentUserService =
            currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<ApplicationDto>>>
        GetAllApplicationsAsync()
    {
        var entities =
            await _repository
                .GetAllApplicationsAsync();

        return Result<List<ApplicationDto>>
            .Success(
                entities
                    .Select(ApplicationMapper.ToDto)
                    .ToList());
    }


    // =========================================================
    // GET BASIC INFO
    // =========================================================

    public async Task<Result<ApplicationBasicInfoDto>>
        GetBasicInfoAsync(int id)
    {
        var validation =
            ApplicationValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<ApplicationBasicInfoDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            await _repository
                .GetApplicationByIdAsync(id);

        if (entity is null)
        {
            return Result<ApplicationBasicInfoDto>
                .FromNotFound(
                    "Application not found.");
        }

        return Result<ApplicationBasicInfoDto>
            .Success(
                ApplicationMapper
                    .ToBasicInfoDto(entity));
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<ApplicationDto>>
        GetApplicationByIdAsync(int id)
    {
        var validation =
            ApplicationValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<ApplicationDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            await _repository
                .GetApplicationByIdAsync(id);

        if (entity is null)
        {
            return Result<ApplicationDto>
                .FromNotFound(
                    "Application not found.");
        }

        return Result<ApplicationDto>
            .Success(
                ApplicationMapper.ToDto(entity));
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<int>>
        AddNewApplicationAsync(
            CreateApplicationDto dto)
    {
        var validation =
            ApplicationValidator
                .ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromValidationFailure(
                    validation.Error);
        }

        var entity =
            ApplicationMapper
                .ToEntity(dto);

        entity.CreatedByUserID =
            _currentUserService.UserId;

        if (entity.CreatedByUserID <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Authenticated user is required.");
        }

        await _repository
            .AddNewApplicationAsync(entity);

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        if (saved <= 0 ||
            entity.ApplicationID <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create application.");
        }

        return Result<int>
            .Success(
                entity.ApplicationID);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateApplicationAsync(
            UpdateApplicationDto dto)
    {
        var validation =
            ApplicationValidator
                .ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var entity =
            await _repository
                .GetApplicationForUpdateAsync(
                    dto.ApplicationID);

        if (entity is null)
        {
            return Result.NotFound(
                "Application not found.");
        }

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result.Conflict(
                "Completed applications cannot be modified.");
        }

        if (entity.ApplicationStatus ==
                AppStatus.Cancelled &&
            dto.ApplicationStatus !=
                AppStatus.Cancelled)
        {
            return Result.Conflict(
                "Cancelled applications cannot be reactivated.");
        }

        entity.ApplicationStatus =
            dto.ApplicationStatus;

        entity.PaidFees =
            dto.PaidFees;

        entity.LastStatusDate =
            dto.LastStatusDate;

        entity.ApplicationTypeID =
            dto.ApplicationTypeID;

        entity.ApplicantPersonID =
            dto.ApplicantPersonID;

        entity.ApplicationDate =
            dto.ApplicationDate;

        // Entity is already tracked.
        // No repository Update call is required.

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Application update failed.");
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteApplicationAsync(
            int id)
    {
        var validation =
            ApplicationValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var entity =
            await _repository
                .GetApplicationForUpdateAsync(id);

        if (entity is null)
        {
            return Result.NotFound(
                "Application not found.");
        }

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result.Conflict(
                "Cannot delete completed application.");
        }

        _repository
            .DeleteApplication(entity);

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Delete application failed.");
    }


    // =========================================================
    // CHECK DUPLICATE APPLICATION
    // =========================================================

    public async Task<int?>
        HasDuplicateApplicationAsync(
            int personId,
            int licenseClassId)
    {
        if (personId <= 0 ||
            licenseClassId <= 0)
        {
            return null;
        }

        return await _repository
            .HasDuplicateApplicationAsync(
                personId,
                licenseClassId);
    }


    // =========================================================
    // CANCEL
    // =========================================================

    public async Task<Result>
        CancelApplicationAsync(
            int applicationId)
    {
        var validation =
            ApplicationValidator
                .ValidateId(applicationId);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var entity =
            await _repository
                .GetApplicationForUpdateAsync(
                    applicationId);

        if (entity is null)
        {
            return Result.NotFound(
                "Application not found.");
        }

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result.Conflict(
                "Cannot cancel completed application.");
        }

        if (entity.ApplicationStatus ==
            AppStatus.Cancelled)
        {
            return Result.Conflict(
                "Application already cancelled.");
        }

        entity.ApplicationStatus =
            AppStatus.Cancelled;

        entity.LastStatusDate =
            DateTime.UtcNow;

        // Entity is already tracked.
        // No repository Update call is required.

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Cancel application failed.");
    }


    // =========================================================
    // COMPLETE
    // =========================================================

    public async Task<Result>
        CompleteApplicationAsync(
            int applicationId)
    {
        var validation =
            ApplicationValidator
                .ValidateId(applicationId);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var entity =
            await _repository
                .GetApplicationForUpdateAsync(
                    applicationId);

        if (entity is null)
        {
            return Result.NotFound(
                "Application not found.");
        }

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result.Conflict(
                "Application already completed.");
        }

        if (entity.ApplicationStatus ==
            AppStatus.Cancelled)
        {
            return Result.Conflict(
                "Cannot complete cancelled application.");
        }

        entity.ApplicationStatus =
            AppStatus.Completed;

        entity.LastStatusDate =
            DateTime.UtcNow;

        // Entity is already tracked.
        // No repository Update call is required.

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Complete application failed.");
    }
}
