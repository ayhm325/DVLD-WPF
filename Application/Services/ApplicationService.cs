using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class ApplicationService(
    IApplicationRepository repository,
    IApplicationTypeRepository applicationTypeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService) : IApplicationService
{
    private readonly IApplicationRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly IApplicationTypeRepository _applicationTypeRepository =
        applicationTypeRepository ?? throw new ArgumentNullException(nameof(applicationTypeRepository));
    private readonly IUnitOfWork _unitOfWork =
        unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ICurrentUserService _currentUserService =
        currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));

    public async Task<Result<List<ApplicationDto>>> GetAllApplicationsAsync()
    {
        var entities = await _repository.GetAllApplicationsAsync();
        return Result<List<ApplicationDto>>.Success(entities.Select(ApplicationMapper.ToDto).ToList());
    }

    public async Task<Result<ApplicationBasicInfoDto>> GetBasicInfoAsync(int id)
    {
        var validation = ApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<ApplicationBasicInfoDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetApplicationByIdAsync(id);
        return entity is null
            ? Result<ApplicationBasicInfoDto>.FromNotFound("Application not found.")
            : Result<ApplicationBasicInfoDto>.Success(ApplicationMapper.ToBasicInfoDto(entity));
    }

    public async Task<Result<ApplicationDto>> GetApplicationByIdAsync(int id)
    {
        var validation = ApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<ApplicationDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetApplicationByIdAsync(id);
        return entity is null
            ? Result<ApplicationDto>.FromNotFound("Application not found.")
            : Result<ApplicationDto>.Success(ApplicationMapper.ToDto(entity));
    }

    public async Task<Result<int>> AddNewApplicationAsync(CreateApplicationDto dto)
    {
        var validation = ApplicationValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        if (!IsAuthenticated())
            return Result<int>.FromForbidden("Authenticated user is required.");

        var applicationType = await _applicationTypeRepository.GetApplicationTypeByIdAsync(dto.ApplicationTypeID);
        if (applicationType is null)
            return Result<int>.FromNotFound("Application type not found.");

        var entity = ApplicationMapper.ToEntity(dto, applicationType.ApplicationFees, _currentUserService.UserId);
        await _repository.AddNewApplicationAsync(entity);

        var saved = await _unitOfWork.SaveChangesAsync();
        return saved > 0 && entity.ApplicationID > 0
            ? Result<int>.Success(entity.ApplicationID)
            : Result<int>.FromFailure("Failed to create application.");
    }

    public async Task<Result> UpdateApplicationAsync(UpdateApplicationDto dto)
    {
        var validation = ApplicationValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var entity = await _repository.GetApplicationForUpdateAsync(dto.ApplicationID);
        if (entity is null)
            return Result.NotFound("Application not found.");

        if (entity.ApplicationStatus == AppStatus.Completed)
            return Result.Conflict("Completed applications cannot be modified.");
        if (entity.ApplicationStatus == AppStatus.Cancelled)
            return Result.Conflict("Cancelled applications cannot be modified.");

        var applicationType = await _applicationTypeRepository.GetApplicationTypeByIdAsync(dto.ApplicationTypeID);
        if (applicationType is null)
            return Result.NotFound("Application type not found.");

        entity.ApplicationTypeID = dto.ApplicationTypeID;
        entity.PaidFees = applicationType.ApplicationFees;

        return await SaveAsync("Application update failed.");
    }

    public async Task<Result> DeleteApplicationAsync(int id)
    {
        var validation = ApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var entity = await _repository.GetApplicationForUpdateAsync(id);
        if (entity is null)
            return Result.NotFound("Application not found.");
        if (entity.ApplicationStatus == AppStatus.Completed)
            return Result.Conflict("Cannot delete completed application.");

        _repository.DeleteApplication(entity);
        return await SaveAsync("Delete application failed.");
    }

    public async Task<Result> CancelApplicationAsync(int applicationId)
    {
        var entityResult = await GetTrackedApplicationAsync(applicationId);
        if (entityResult.IsFailure)
            return Result.FromFailure(entityResult);

        var entity = entityResult.Value!;
        if (entity.ApplicationStatus == AppStatus.Completed)
            return Result.Conflict("Cannot cancel completed application.");
        if (entity.ApplicationStatus == AppStatus.Cancelled)
            return Result.Conflict("Application already cancelled.");

        entity.ApplicationStatus = AppStatus.Cancelled;
        entity.LastStatusDate = DateTime.UtcNow;
        return await SaveAsync("Cancel application failed.");
    }

    public async Task<Result> CompleteApplicationAsync(int applicationId)
    {
        var entityResult = await GetTrackedApplicationAsync(applicationId);
        if (entityResult.IsFailure)
            return Result.FromFailure(entityResult);

        var entity = entityResult.Value!;
        if (entity.ApplicationStatus == AppStatus.Completed)
            return Result.Conflict("Application already completed.");
        if (entity.ApplicationStatus == AppStatus.Cancelled)
            return Result.Conflict("Cannot complete cancelled application.");

        entity.ApplicationStatus = AppStatus.Completed;
        entity.LastStatusDate = DateTime.UtcNow;
        return await SaveAsync("Complete application failed.");
    }

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn && _currentUserService.UserId > 0;

    private async Task<Result<ApplicationD>> GetTrackedApplicationAsync(int id)
    {
        var validation = ApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<ApplicationD>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetApplicationForUpdateAsync(id);
        return entity is null
            ? Result<ApplicationD>.FromNotFound("Application not found.")
            : Result<ApplicationD>.Success(entity);
    }

    private async Task<Result> SaveAsync(string errorMessage) =>
        await _unitOfWork.SaveChangesAsync() > 0 ? Result.Success() : Result.Failure(errorMessage);
}