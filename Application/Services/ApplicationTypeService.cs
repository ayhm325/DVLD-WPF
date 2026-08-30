using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class ApplicationTypeService
    : IApplicationTypeService
{
    private readonly IApplicationTypeRepository
        _applicationTypeRepository;

    private readonly IUnitOfWork
        _unitOfWork;

    public ApplicationTypeService(
        IApplicationTypeRepository applicationTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _applicationTypeRepository =
            applicationTypeRepository
            ?? throw new ArgumentNullException(
                nameof(applicationTypeRepository));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(
                nameof(unitOfWork));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<
        Result<List<ApplicationTypeDto>>>
        GetAllApplicationTypesAsync()
    {
        var appTypes =
            await _applicationTypeRepository
                .GetAllApplicationTypesAsync();

        return Result<List<ApplicationTypeDto>>
            .Success(
                [
                    .. appTypes.Select(MapToDto)
                ]);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<
        Result<ApplicationTypeDto>>
        GetApplicationTypeByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Result<ApplicationTypeDto>
                .FromFailure(
                    "Invalid application type ID.");
        }

        var appType =
            await _applicationTypeRepository
                .GetApplicationTypeByIdAsync(id);

        if (appType is null)
        {
            return Result<ApplicationTypeDto>
                .FromFailure(
                    "Application type not found.");
        }

        return Result<ApplicationTypeDto>
            .Success(
                MapToDto(appType));
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateApplicationTypeAsync(
            int id,
            ApplicationTypeDto dto)
    {
        var validation =
            ApplicationTypeValidator
                .ValidateUpdate(
                    id,
                    dto);

        if (validation.IsFailure)
        {
            return Result
                .Failure(validation.Error);
        }

        var appType =
            await _applicationTypeRepository
                .GetApplicationTypeByIdAsync(id);

        if (appType is null)
        {
            return Result
                .Failure(
                    "Application type not found.");
        }

        appType.ApplicationTypeTitle =
            dto.ApplicationTypeTitle.Trim();

        appType.ApplicationFees =
            dto.ApplicationTypeFees;

        var updated =
            await _applicationTypeRepository
                .UpdateApplicationTypeAsync(
                    appType);

        if (!updated)
        {
            return Result
                .Failure(
                    "Failed to update application type.");
        }

        var saved =
            await _unitOfWork
                .SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to save application type changes.");
    }

    // =========================================================
    // MAPPING
    // =========================================================

    private static ApplicationTypeDto
        MapToDto(
            ApplicationType appType)
    {
        return new ApplicationTypeDto
        {
            ApplicationTypeId =
                appType.ApplicationTypeId,

            ApplicationTypeTitle =
                appType.ApplicationTypeTitle,

            ApplicationTypeFees =
                appType.ApplicationFees
        };
    }
}