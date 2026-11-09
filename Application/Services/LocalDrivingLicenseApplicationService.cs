using Application.Common.Pagination;
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
    IApplicationService applicationService,
    IJordanClock clock,
    ITestAppointmentRepository? testAppointmentRepository = null,
    IPersonRepository? personRepository = null) : ILocalDrivingLicenseApplicationService
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
    private readonly IJordanClock _clock =
        clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly ITestAppointmentRepository? _testAppointmentRepository =
        testAppointmentRepository;
    private readonly IPersonRepository? _personRepository =
        personRepository;

    public Task<Result<PagedResult<LocalDrivingLicenseApplicationListDto>>>
    GetAllLocalDrivingLicenseApplicationsAsync(
        PaginationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is LocalDrivingLicenseApplicationQueryRequest queryRequest)
        {
            return GetAllLocalDrivingLicenseApplicationsAsync(queryRequest, cancellationToken);
        }

        return GetAllInternalAsync(() => _repository.GetAllAsync(request, cancellationToken), cancellationToken);
    }

    public Task<Result<PagedResult<LocalDrivingLicenseApplicationListDto>>>
    GetAllLocalDrivingLicenseApplicationsAsync(
        LocalDrivingLicenseApplicationQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return GetAllInternalAsync(() => _repository.GetAllAsync(request, cancellationToken), cancellationToken);
    }

    private async Task<Result<PagedResult<LocalDrivingLicenseApplicationListDto>>>
    GetAllInternalAsync(Func<Task<PagedResult<LocalDrivingLicenseApplication>>> fetcher, CancellationToken cancellationToken = default)
    {
        var pagedEntities = await fetcher();

        var items = await MapListToDtoAsync(
            pagedEntities.Items, cancellationToken);

        var result = new PagedResult<LocalDrivingLicenseApplicationListDto>
        {
            Items = items,
            PageNumber = pagedEntities.PageNumber,
            PageSize = pagedEntities.PageSize,
            TotalCount = pagedEntities.TotalCount
        };

        return Result<PagedResult<LocalDrivingLicenseApplicationListDto>>
            .Success(result);
    }

    public async Task<Result<LocalDrivingLicenseApplicationListDto>> GetLocalDrivingLicenseApplicationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result<LocalDrivingLicenseApplicationListDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<LocalDrivingLicenseApplicationListDto>.FromNotFound("Local driving license application not found.");

        var passedTestCounts = await _repository.GetPassedTestCountsAsync([entity.LocalDrivingLicenseApplicationID], cancellationToken);
        var applicationIdsWithLicenses = await _licenseRepository.GetApplicationIdsWithLicensesAsync([entity.ApplicationID], cancellationToken);

        return Result<LocalDrivingLicenseApplicationListDto>.Success(
            LocalDrivingLicenseApplicationMapper.ToDto(
                entity,
                passedTestCounts.GetValueOrDefault(entity.LocalDrivingLicenseApplicationID),
                applicationIdsWithLicenses.Contains(entity.ApplicationID)));
    }

    public async Task<Result<decimal>> GetNewLocalDrivingLicenseApplicationFeesAsync(CancellationToken cancellationToken = default)
    {
        var applicationType = await _applicationTypeRepository.GetApplicationTypeByIdAsync(NewLocalDrivingLicenseApplicationTypeId, cancellationToken);
        return applicationType is null
            ? Result<decimal>.FromNotFound("Application type not found.")
            : Result<decimal>.Success(applicationType.ApplicationFees);
    }

    public async Task<Result<int>> CreateLocalDrivingLicenseApplicationAsync(
        int applicantPersonId,
        int licenseClassId,
        CancellationToken cancellationToken = default)
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

        if (!IsAuthenticated())
            return Result<int>.FromFailure(
                "Authenticated user is required.");

        if (_personRepository is null)
            throw new InvalidOperationException("Person repository is required for applicant validation.");

        var person = await _personRepository.GetPersonByIdAsync(applicantPersonId, cancellationToken);
        if (person is null)
            return Result<int>.FromNotFound(
                "Applicant person was not found.");

        var licenseClassResult =
            await _licenseClassService.GetLicenseClassByIdAsync(
                licenseClassId, cancellationToken);

        if (licenseClassResult.IsFailure)
            return Result<int>.FromResult(licenseClassResult);

        if (licenseClassResult.Value is null)
            return Result<int>.FromNotFound(
                "License class was not found.");

        var nowJordan = _clock.Now;

        var ageValidation = LocalDrivingLicenseApplicationValidator.ValidateAge(
            person.DateOfBirth,
            licenseClassResult.Value!.MinAllowedAge,
            nowJordan);

        if (ageValidation.IsFailure)
            return Result<int>.FromValidationFailure(
                ageValidation.Error);

        var applicationType =
            await _applicationTypeRepository
                .GetApplicationTypeByIdAsync(
                    NewLocalDrivingLicenseApplicationTypeId, cancellationToken);

        if (applicationType is null)
            return Result<int>.FromNotFound(
                "Application type not found.");

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var duplicateApplicationId =
                    await _repository.HasDuplicateApplicationAsync(
                        applicantPersonId,
                        licenseClassId,
                        cancellationToken);

                if (duplicateApplicationId.HasValue)
                {
                    return Result<int>.FromConflict(
                        "A local driving license application already exists for this person and license class. " +
                        $"Application ID: {duplicateApplicationId.Value}");
                }

                var applicationEntity =
                    ApplicationMapper.ToEntity(
                        applicationDto,
                        applicationType.ApplicationFees,
                        _currentUserService.UserId,
                        nowJordan);

                await _applicationRepository.AddNewApplicationAsync(
                    applicationEntity, cancellationToken);

                if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0 ||
                    applicationEntity.ApplicationID <= 0)
                {
                    return Result<int>.FromFailure(
                        "Failed to create the main application.");
                }

                var localApplicationEntity =
                    new LocalDrivingLicenseApplication
                    {
                        ApplicationID =
                            applicationEntity.ApplicationID,

                        LicenseClassID = licenseClassId
                    };

                await _repository.AddAsync(
                    localApplicationEntity, cancellationToken);

                if (await _unitOfWork.SaveChangesAsync(cancellationToken) <= 0 ||
                    localApplicationEntity.LocalDrivingLicenseApplicationID <= 0)
                {
                    return Result<int>.FromFailure(
                        "Failed to create the local driving license application.");
                }

                await transaction.CommitAsync(cancellationToken);

                return Result<int>.Success(
                    localApplicationEntity.LocalDrivingLicenseApplicationID);
            },
            IsolationLevel.Serializable,
            cancellationToken);
    }

    public async Task<Result<int>> AddLocalDrivingLicenseApplicationAsync(CreateLocalDrivingLicenseApplicationDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation = LocalDrivingLicenseApplicationValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        var application = await _applicationRepository.GetApplicationByIdAsync(dto.ApplicationID, cancellationToken);
        if (application is null)
            return Result<int>.FromNotFound("Main application not found.");

        if (application.ApplicationTypeID != NewLocalDrivingLicenseApplicationTypeId)
            return Result<int>.FromConflict("The main application must be a New Local Driving License application.");

        if (application.ApplicationStatus != AppStatus.New)
            return Result<int>.FromConflict("The main application must have New status.");

        var licenseClassResult = await _licenseClassService.GetLicenseClassByIdAsync(dto.LicenseClassID, cancellationToken);
        if (licenseClassResult.IsFailure)
            return Result<int>.FromResult(licenseClassResult);

        if (_personRepository != null)
        {
            var person = await _personRepository.GetPersonByIdAsync(application.ApplicantPersonID, cancellationToken);
            if (person != null)
            {
                var ageValidation = LocalDrivingLicenseApplicationValidator.ValidateAge(
                    person.DateOfBirth,
                    licenseClassResult.Value!.MinAllowedAge,
                    _clock.Now);

                if (ageValidation.IsFailure)
                    return Result<int>.FromValidationFailure(ageValidation.Error);
            }
        }

        var existing = await _repository.GetByApplicationIdAsync(dto.ApplicationID, cancellationToken);
        if (existing.Count > 0)
            return Result<int>.FromConflict("A local driving license application already exists for this main application.");

        var entity = new LocalDrivingLicenseApplication
        {
            ApplicationID = dto.ApplicationID,
            LicenseClassID = dto.LicenseClassID
        };
        await _repository.AddAsync(entity, cancellationToken);

        var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        return saved > 0 && entity.LocalDrivingLicenseApplicationID > 0
            ? Result<int>.Success(entity.LocalDrivingLicenseApplicationID)
            : Result<int>.FromFailure("Failed to create local driving license application.");
    }

    public async Task<Result> UpdateLocalDrivingLicenseApplicationAsync(
    int id,
    UpdateLocalDrivingLicenseApplicationDto dto,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var validation = LocalDrivingLicenseApplicationValidator.ValidateUpdate(id, dto);

        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var existing = await _repository.GetForUpdateAsync(id, cancellationToken);

                if (existing is null)
                    return Result.NotFound("Local driving license application not found.");

                if (existing.Application is null)
                    return Result.Failure("Main application information is missing.");

                if (existing.Application.ApplicationStatus != AppStatus.New)
                    return Result.Conflict("Only a New application can be updated.");

                var licenseClassResult =
                    await _licenseClassService.GetLicenseClassByIdAsync(dto.LicenseClassID, cancellationToken);

                if (licenseClassResult.IsFailure)
                    return Result.FromFailure(licenseClassResult);

                if (existing.LicenseClassID == dto.LicenseClassID)
                    return Result.Success();

                if (_personRepository != null)
                {
                    var person = await _personRepository.GetPersonByIdAsync(existing.Application.ApplicantPersonID, cancellationToken);
                    if (person != null)
                    {
                        var ageValidation = LocalDrivingLicenseApplicationValidator.ValidateAge(
                            person.DateOfBirth,
                            licenseClassResult.Value!.MinAllowedAge,
                            _clock.Now);

                        if (ageValidation.IsFailure)
                            return Result.ValidationFailure(ageValidation.Error);
                    }
                }

                var duplicateApplicationId =
                    await _repository.HasDuplicateApplicationAsync(
                        existing.Application.ApplicantPersonID,
                        dto.LicenseClassID,
                        cancellationToken);

                if (duplicateApplicationId.HasValue &&
                    duplicateApplicationId.Value != existing.ApplicationID)
                {
                    return Result.Conflict(
                        "Another local driving license application already exists " +
                        "for this person and license class.");
                }

                existing.LicenseClassID = dto.LicenseClassID;

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result.Success();
            },
            IsolationLevel.Serializable,
            cancellationToken);
    }

    public async Task<Result> DeleteLocalDrivingLicenseApplicationAsync(int id, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(id);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return Result.NotFound("Local driving license application not found.");

        if (existing.Application is null)
            return Result.Failure("Main application information is missing.");

        if (existing.Application.ApplicationStatus != AppStatus.New)
            return Result.Conflict("Only a New application can be deleted.");

        if (!await _repository.DeleteAsync(id, cancellationToken))
            return Result.Failure("Failed to delete local driving license application.");

        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0
            ? Result.Success()
            : Result.Failure("Failed to save local driving license application deletion.");
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicantPersonIdAsync(int applicantPersonId, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidatePersonId(applicantPersonId);
        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByPersonIdAsync(applicantPersonId, cancellationToken);
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(await MapListToDtoAsync(entities, cancellationToken));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByApplicationIdAsync(int applicationId, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByApplicationIdAsync(applicationId, cancellationToken);
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(await MapListToDtoAsync(entities, cancellationToken));
    }

    public async Task<Result<List<LocalDrivingLicenseApplicationListDto>>> GetLocalDrivingLicenseApplicationsByLicenseClassIdAsync(int licenseClassId, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateLicenseClassId(licenseClassId);
        if (validation.IsFailure)
            return Result<List<LocalDrivingLicenseApplicationListDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByLicenseClassIdAsync(licenseClassId, cancellationToken);
        return Result<List<LocalDrivingLicenseApplicationListDto>>.Success(await MapListToDtoAsync(entities, cancellationToken));
    }

    public async Task<Result<ApplicationBasicInfoDto>> GetApplicationBasicInfoAsync(int localApplicationId, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(localApplicationId);
        if (validation.IsFailure)
            return Result<ApplicationBasicInfoDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(localApplicationId, cancellationToken);
        if (entity is null)
            return Result<ApplicationBasicInfoDto>.FromNotFound("Local driving license application not found.");

        if (entity.Application is null)
            return Result<ApplicationBasicInfoDto>.FromNotFound("Main application information not found.");

        return Result<ApplicationBasicInfoDto>.Success(ApplicationMapper.ToBasicInfoDto(entity.Application));
    }

    public async Task<Result<int>> GetApplicationIdByLocalIdAsync(int localId, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(localId);
        if (validation.IsFailure)
            return Result<int>.FromValidationFailure(validation.Error);

        var applicationId = await _repository.GetApplicationIdByLocalIdAsync(localId, cancellationToken);
        return applicationId.HasValue
            ? Result<int>.Success(applicationId.Value)
            : Result<int>.FromNotFound("Main application not found for this local application.");
    }

    public async Task<bool> IsLocalDrivingLicenseApplicationExistsAsync(int id, CancellationToken cancellationToken = default) =>
        id > 0 && await _repository.GetByIdAsync(id, cancellationToken) is not null;

    public async Task<Result> CancelLocalDrivingLicenseApplicationAsync(int localApplicationId, CancellationToken cancellationToken = default)
    {
        var validation = LocalDrivingLicenseApplicationValidator.ValidateId(localApplicationId);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(localApplicationId, cancellationToken);
        if (existing is null)
            return Result.NotFound("Local driving license application not found.");

        if (existing.Application is null)
            return Result.Failure("Main application information is missing.");

        return await _unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                var cancelResult = await _applicationService.CancelApplicationAsync(existing.ApplicationID, cancellationToken);
                if (cancelResult.IsFailure)
                    return cancelResult;

                if (_testAppointmentRepository != null)
                {
                    var activeAppointments =
                        await _testAppointmentRepository.GetActiveScheduledByLocalAppIdForUpdateAsync(localApplicationId, cancellationToken);

                    var nowJordan = _clock.Now;
                    var currentUserId = _currentUserService.IsLoggedIn && _currentUserService.UserId > 0
                        ? _currentUserService.UserId
                        : (int?)null;

                    foreach (var apt in activeAppointments)
                    {
                        apt.Status = AppointmentStatus.Cancelled;
                        apt.CancelledAt = nowJordan;
                        apt.CancelledByUserID = currentUserId;
                        apt.IsLocked = true;
                    }

                    if (activeAppointments.Count > 0)
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                return Result.Success();
            },
            IsolationLevel.ReadCommitted,
            cancellationToken);
    }

    private bool IsAuthenticated() =>
        _currentUserService.IsLoggedIn && _currentUserService.UserId > 0;

    private async Task<List<LocalDrivingLicenseApplicationListDto>> MapListToDtoAsync(
        IReadOnlyList<LocalDrivingLicenseApplication> entities, CancellationToken cancellationToken = default)
    {
        if (entities.Count == 0) return [];

        var localApplicationIds = entities.Select(x => x.LocalDrivingLicenseApplicationID).Where(id => id > 0).Distinct().ToList();
        var applicationIds = entities.Select(x => x.ApplicationID).Where(id => id > 0).Distinct().ToList();

        var passedTestCounts = await _repository.GetPassedTestCountsAsync(localApplicationIds, cancellationToken);
        var applicationIdsWithLicenses = await _licenseRepository.GetApplicationIdsWithLicensesAsync(applicationIds, cancellationToken);

        return entities.Select(entity => LocalDrivingLicenseApplicationMapper.ToDto(
            entity,
            passedTestCounts.GetValueOrDefault(entity.LocalDrivingLicenseApplicationID),
            applicationIdsWithLicenses.Contains(entity.ApplicationID))
        ).ToList();
    }
}
