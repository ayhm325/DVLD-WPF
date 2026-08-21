using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public class ApplicationTypeService : IApplicationTypeService
{
    private readonly IApplicationTypeRepository _applicationTypeRepository;

    public ApplicationTypeService(IApplicationTypeRepository applicationTypeRepository)
    {
        _applicationTypeRepository = applicationTypeRepository ?? throw new ArgumentNullException(nameof(applicationTypeRepository));
    }

    // GET ALL
    public async Task<Result<List<ApplicationTypeDto>>> GetAllApplicationTypesAsync()
    {
        var appTypes = await _applicationTypeRepository.GetAllApplicationTypesAsync();
        return Result<List<ApplicationTypeDto>>.Success([.. appTypes.Select(MapToDto)]);
    }

    // GET BY ID
    public async Task<Result<ApplicationTypeDto>> GetApplicationTypeByIdAsync(int id)
    {
        if (id <= 0)
            return Result<ApplicationTypeDto>.FromFailure("Invalid application type ID.");

        var appType = await _applicationTypeRepository.GetApplicationTypeByIdAsync(id);
        if (appType is null)
            return Result<ApplicationTypeDto>.FromFailure("Application type not found.");

        return Result<ApplicationTypeDto>.Success(MapToDto(appType));
    }

    // UPDATE
    public async Task<Result> UpdateApplicationTypeAsync(int id, ApplicationTypeDto dto)
    {
        var validation = ApplicationTypeValidator.ValidateUpdate(id, dto);
        if (validation.IsFailure)
            return Result.Failure(validation.Error);

        var appType = await _applicationTypeRepository.GetApplicationTypeByIdAsync(id);
        if (appType is null)
            return Result.Failure("Application type not found.");

        appType.ApplicationTypeTitle = dto.ApplicationTypeTitle.Trim();
        appType.ApplicationFees = dto.ApplicationTypeFees;

        var isSuccess = await _applicationTypeRepository.UpdateApplicationTypeAsync(appType);
        return isSuccess ? Result.Success() : Result.Failure("Failed to update application type.");
    }

    // MAPPING
    private static ApplicationTypeDto MapToDto(ApplicationType appType)
    {
        return new ApplicationTypeDto
        {
            ApplicationTypeId = appType.ApplicationTypeId,
            ApplicationTypeTitle = appType.ApplicationTypeTitle,
            ApplicationTypeFees = appType.ApplicationFees
        };
    }
}