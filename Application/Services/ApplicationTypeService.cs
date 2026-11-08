using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public sealed class ApplicationTypeService(
    IApplicationTypeRepository applicationTypeRepository,
    IUnitOfWork unitOfWork) : IApplicationTypeService
{
    private readonly IApplicationTypeRepository _applicationTypeRepository =
        applicationTypeRepository
        ?? throw new ArgumentNullException(nameof(applicationTypeRepository));

    private readonly IUnitOfWork _unitOfWork =
        unitOfWork
        ?? throw new ArgumentNullException(nameof(unitOfWork));

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<ApplicationTypeDto>>>
        GetAllApplicationTypesAsync(
            CancellationToken cancellationToken = default)
    {
        var appTypes =
            await _applicationTypeRepository
                .GetAllApplicationTypesAsync(cancellationToken);

        return Result<List<ApplicationTypeDto>>.Success(
            appTypes.Select(MapToDto).ToList());
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<ApplicationTypeDto>>
        GetApplicationTypeByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return Result<ApplicationTypeDto>.FromValidationFailure(
                "Invalid application type ID.");
        }

        var appType =
            await _applicationTypeRepository
                .GetApplicationTypeByIdAsync(id, cancellationToken);

        if (appType is null)
        {
            return Result<ApplicationTypeDto>.FromNotFound(
                "Application type not found.");
        }

        return Result<ApplicationTypeDto>.Success(
            MapToDto(appType));
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result> UpdateApplicationTypeAsync(
        int id,
        ApplicationTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        var validation =
            ApplicationTypeValidator.ValidateUpdate(
                id,
                dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var appType =
            await _applicationTypeRepository
                .GetApplicationTypeByIdAsync(id, cancellationToken);

        if (appType is null)
        {
            return Result.NotFound(
                "Application type not found.");
        }

        appType.ApplicationTypeTitle =
            dto.ApplicationTypeTitle.Trim();

        appType.ApplicationFees =
            dto.ApplicationTypeFees;

        var updated =
            await _applicationTypeRepository
                .UpdateApplicationTypeAsync(appType, cancellationToken);

        if (!updated)
        {
            return Result.Failure(
                "Failed to update application type.");
        }

        var saved =
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saved > 0
            ? Result.Success()
            : Result.Failure(
                "Failed to save application type changes.");
    }

    // =========================================================
    // MAPPING
    // =========================================================

    private static ApplicationTypeDto MapToDto(
        ApplicationType appType)
    {
        return new ApplicationTypeDto
        {
            ApplicationTypeId = appType.ApplicationTypeId,
            ApplicationTypeTitle = appType.ApplicationTypeTitle,
            ApplicationTypeFees = appType.ApplicationFees
        };
    }
}