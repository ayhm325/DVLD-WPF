using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class ApplicationService
    : IApplicationService
{
    private readonly IApplicationRepository _repository;


    public ApplicationService(
        IApplicationRepository repository)
    {
        _repository =
            repository
            ?? throw new ArgumentNullException(
                nameof(repository));
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

        var dtos =
            entities
                .Select(ApplicationMapper.ToDto)
                .ToList();

        return Result<List<ApplicationDto>>
            .Success(dtos);
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
                ApplicationMapper
                    .ToDto(entity));
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


        var id =
            await _repository
                .AddNewApplicationAsync(
                    entity);


        if (id <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create application.");
        }


        return Result<int>
            .Success(id);
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
            return Result
                .ValidationFailure(
                    validation.Error);
        }


        var entity =
            await _repository
                .GetApplicationByIdAsync(
                    dto.ApplicationID);

        if (entity is null)
        {
            return Result
                .NotFound(
                    "Application not found.");
        }


        // =====================================================
        // BUSINESS RULES
        // =====================================================

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result
                .Conflict(
                    "Completed applications cannot be modified.");
        }


        if (entity.ApplicationStatus ==
            AppStatus.Cancelled &&
            dto.ApplicationStatus !=
            AppStatus.Cancelled)
        {
            return Result
                .Conflict(
                    "Cancelled applications cannot be reactivated.");
        }


        // =====================================================
        // UPDATE
        // =====================================================

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

        entity.CreatedByUserID =
            dto.CreatedByUserID;


        var updated =
            await _repository
                .UpdateApplicationAsync(
                    entity);


        return updated
            ? Result.Success()
            : Result.Failure(
                "Application update failed.");
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteApplicationAsync(int id)
    {
        var validation =
            ApplicationValidator
                .ValidateId(id);

        if (validation.IsFailure)
        {
            return Result
                .ValidationFailure(
                    validation.Error);
        }


        var entity =
            await _repository
                .GetApplicationByIdAsync(id);

        if (entity is null)
        {
            return Result
                .NotFound(
                    "Application not found.");
        }


        // =====================================================
        // BUSINESS RULE
        // =====================================================

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result
                .Conflict(
                    "Cannot delete completed application.");
        }


        // =====================================================
        // DELETE
        // =====================================================

        var deleted =
            await _repository
                .DeleteApplicationAsync(id);


        return deleted
            ? Result.Success()
            : Result.Failure(
                "Delete application failed.");
    }


    // =========================================================
    // CHECK DUPLICATE
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
            return Result
                .ValidationFailure(
                    validation.Error);
        }


        var entity =
            await _repository
                .GetApplicationByIdAsync(
                    applicationId);

        if (entity is null)
        {
            return Result
                .NotFound(
                    "Application not found.");
        }


        // =====================================================
        // BUSINESS RULE
        // =====================================================

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result
                .Conflict(
                    "Cannot cancel completed application.");
        }


        if (entity.ApplicationStatus ==
            AppStatus.Cancelled)
        {
            return Result
                .Conflict(
                    "Application already cancelled.");
        }


        // =====================================================
        // CANCEL
        // =====================================================

        entity.ApplicationStatus =
            AppStatus.Cancelled;

        entity.LastStatusDate =
            DateTime.UtcNow;


        var updated =
            await _repository
                .UpdateApplicationAsync(
                    entity);


        return updated
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
            return Result
                .ValidationFailure(
                    validation.Error);
        }


        var entity =
            await _repository
                .GetApplicationByIdAsync(
                    applicationId);

        if (entity is null)
        {
            return Result
                .NotFound(
                    "Application not found.");
        }


        // =====================================================
        // BUSINESS RULES
        // =====================================================

        if (entity.ApplicationStatus ==
            AppStatus.Completed)
        {
            return Result
                .Conflict(
                    "Application already completed.");
        }


        if (entity.ApplicationStatus ==
            AppStatus.Cancelled)
        {
            return Result
                .Conflict(
                    "Cannot complete cancelled application.");
        }


        // =====================================================
        // COMPLETE
        // =====================================================

        entity.ApplicationStatus =
            AppStatus.Completed;

        entity.LastStatusDate =
            DateTime.UtcNow;


        var updated =
            await _repository
                .UpdateApplicationAsync(
                    entity);


        return updated
            ? Result.Success()
            : Result.Failure(
                "Complete application failed.");
    }
}