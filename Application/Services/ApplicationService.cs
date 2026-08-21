using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _repository;

    public ApplicationService(
        IApplicationRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<ApplicationDto>>>
        GetAllApplicationsAsync()
    {
        var apps = await _repository
            .GetAllApplicationsAsync();

        var dtos = apps
            .Select(MapToDto)
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
            ApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<ApplicationBasicInfoDto>
                .FromFailure(validation.Error);
        }

        var app = await _repository
            .GetApplicationByIdAsync(id);

        if (app is null)
        {
            return Result<ApplicationBasicInfoDto>
                .FromFailure(
                    "Application not found.");
        }

        var dto = new ApplicationBasicInfoDto
        {
            ApplicationID = app.ApplicationID,

            ApplicantPersonID =
                app.ApplicantPersonID,

            ApplicationStatus =
                (AppStatus)app.ApplicationStatus,

            PaidFees =
                app.PaidFees,

            ApplicationTypeName =
                app.ApplicationType?.ApplicationTypeTitle,

            ApplicantFullName =
                app.Person is not null
                    ? $"{app.Person.FirstName} {app.Person.LastName}"
                    : null,

            ApplicationDate =
                app.ApplicationDate,

            LastStatusDate =
                app.LastStatusDate,

            CreatedByUserName =
                app.CreatedByUser?.UserName
        };

        return Result<ApplicationBasicInfoDto>
            .Success(dto);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<ApplicationDto>>
        GetApplicationByIdAsync(int id)
    {
        var validation =
            ApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<ApplicationDto>
                .FromFailure(validation.Error);
        }

        var app = await _repository
            .GetApplicationByIdAsync(id);

        if (app is null)
        {
            return Result<ApplicationDto>
                .FromFailure(
                    "Application not found.");
        }

        return Result<ApplicationDto>
            .Success(MapToDto(app));
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<Result<int>>
        AddNewApplicationAsync(
            CreateApplicationDto dto)
    {
        var validation =
            ApplicationValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>
                .FromFailure(validation.Error);
        }

        var entity = new ApplicationD
        {
            ApplicantPersonID =
                dto.ApplicantPersonID,

            ApplicationDate =
                dto.ApplicationDate,

            ApplicationTypeID =
                dto.ApplicationTypeID,

            ApplicationStatus =
                (byte)dto.ApplicationStatus,

            LastStatusDate =
                dto.LastStatusDate,

            PaidFees =
                dto.PaidFees,

            CreatedByUserID =
                dto.CreatedByUserID
        };

        var id = await _repository
            .AddNewApplicationAsync(entity);

        if (id <= 0)
        {
            return Result<int>.FromFailure("Failed to create application.");
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
            ApplicationValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result
                .Failure(validation.Error);
        }

        var entity = await _repository
            .GetApplicationByIdAsync(
                dto.ApplicationID);

        if (entity is null)
        {
            return Result
                .Failure(
                    "Application not found.");
        }

        entity.ApplicationStatus =
            (byte)dto.ApplicationStatus;

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

        var updated = await _repository
            .UpdateApplicationAsync(entity);

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
            ApplicationValidator.ValidateId(id);

        if (validation.IsFailure)
            return validation;

        var app = await _repository
            .GetApplicationByIdAsync(id);

        if (app is null)
        {
            return Result.Failure(
                "Application not found.");
        }

        if (app.ApplicationStatus ==
            (int)AppStatus.Completed)
        {
            return Result.Failure(
                "Cannot delete completed application.");
        }

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
            ApplicationValidator.ValidateId(
                applicationId);

        if (validation.IsFailure)
            return validation;

        var app = await _repository
            .GetApplicationByIdAsync(applicationId);

        if (app is null)
        {
            return Result.Failure(
                "Application not found.");
        }

        if (app.ApplicationStatus ==
            (int)AppStatus.Completed)
        {
            return Result.Failure(
                "Cannot cancel completed application.");
        }

        if (app.ApplicationStatus ==
            (int)AppStatus.Cancelled)
        {
            return Result.Failure(
                "Application already cancelled.");
        }

        app.ApplicationStatus =
            (byte)AppStatus.Cancelled;

        app.LastStatusDate =
            DateTime.UtcNow;

        var updated =
            await _repository
                .UpdateApplicationAsync(app);

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
            ApplicationValidator.ValidateId(
                applicationId);

        if (validation.IsFailure)
            return validation;

        var app = await _repository
            .GetApplicationByIdAsync(
                applicationId);

        if (app is null)
        {
            return Result.Failure(
                "Application not found.");
        }

        if (app.ApplicationStatus ==
            (int)AppStatus.Completed)
        {
            return Result.Failure(
                "Application already completed.");
        }

        if (app.ApplicationStatus ==
            (int)AppStatus.Cancelled)
        {
            return Result.Failure(
                "Cannot complete cancelled application.");
        }

        app.ApplicationStatus =
            (byte)AppStatus.Completed;

        app.LastStatusDate =
            DateTime.UtcNow;

        var updated =
            await _repository
                .UpdateApplicationAsync(app);

        return updated
            ? Result.Success()
            : Result.Failure(
                "Complete application failed.");
    }


    // =========================================================
    // MAPPING
    // =========================================================

    private static ApplicationDto MapToDto(
        ApplicationD entity)
    {
        return new ApplicationDto
        {
            ApplicationID =
                entity.ApplicationID,

            ApplicantPersonID =
                entity.ApplicantPersonID,

            ApplicationDate =
                entity.ApplicationDate,

            ApplicationTypeID =
                entity.ApplicationTypeID,

            ApplicationStatus =
                (AppStatus)entity.ApplicationStatus,

            LastStatusDate =
                entity.LastStatusDate,

            PaidFees =
                entity.PaidFees,

            CreatedByUserID =
                entity.CreatedByUserID,

            CreatedByUserName =
                entity.CreatedByUser?.UserName
                ?? string.Empty
        };
    }
}