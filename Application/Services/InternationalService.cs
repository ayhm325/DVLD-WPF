using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class InternationalService : IInternationalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInternationalRepository _repository;
    private readonly ILicenseQueryService _licenseQueryService;
    private readonly IApplicationService _applicationService;
    private readonly IApplicationTypeService _applicationTypeService;
    private readonly ICurrentUserService _currentUserService;

    public InternationalService(
        IInternationalRepository repository,
        ILicenseQueryService licenseQueryService,
        IApplicationService applicationService,
        IApplicationTypeService applicationTypeService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _licenseQueryService = licenseQueryService ?? throw new ArgumentNullException(nameof(licenseQueryService));
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _applicationTypeService = applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // ============ Queries ============

    public async Task<Result<List<InternationalDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();

        var dtos = entities.Select(InternationalLicenseMapper.ToDto).ToList();

        return Result<List<InternationalDto>>.Success(dtos);
    }

    public async Task<Result<InternationalDto>> GetByIdAsync(int internationalLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateId(internationalLicenseId);
        if (validation.IsFailure)
            return Result<InternationalDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByIdAsync(internationalLicenseId);
        if (entity is null)
            return Result<InternationalDto>.FromNotFound("International license not found.");

        return Result<InternationalDto>.Success(InternationalLicenseMapper.ToDto(entity));
    }

    public async Task<Result<List<InternationalDto>>> GetByDriverIdAsync(int driverId)
    {
        var validation = InternationalLicenseValidator.ValidateDriverId(driverId);
        if (validation.IsFailure)
            return Result<List<InternationalDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByDriverIdAsync(driverId);
        var dtos = entities.Select(InternationalLicenseMapper.ToDto).ToList();

        return Result<List<InternationalDto>>.Success(dtos);
    }

    public async Task<Result<InternationalDto>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = InternationalLicenseValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<InternationalDto>.FromValidationFailure(validation.Error);

        var entity = await _repository.GetByApplicationIdAsync(applicationId);
        if (entity is null)
            return Result<InternationalDto>.FromNotFound("International license not found.");

        return Result<InternationalDto>.Success(InternationalLicenseMapper.ToDto(entity));
    }

    public async Task<Result<List<InternationalDto>>> GetByLocalLicenseIdAsync(int localLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateLocalLicenseId(localLicenseId);
        if (validation.IsFailure)
            return Result<List<InternationalDto>>.FromValidationFailure(validation.Error);

        var entities = await _repository.GetByLocalLicenseIdAsync(localLicenseId);
        var dtos = entities.Select(InternationalLicenseMapper.ToDto).ToList();

        return Result<List<InternationalDto>>.Success(dtos);
    }

    public async Task<bool> HasActiveInternationalLicenseAsync(int driverId)
    {
        if (driverId <= 0)
            return false;

        return await _repository.HasActiveInternationalLicenseAsync(driverId);
    }

    // ============ Mutations ============

    public async Task<Result> AddAsync(CreateInternationalLicenseDto dto)
    {
        var validation = InternationalLicenseValidator.ValidateCreate(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        // Authenticated user required
        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result.Failure("Authenticated user is required.");

        // Duplicate local license check
        if (await _repository.ExistsByLocalLicenseAsync(dto.IssuedUsingLocalLicenseID))
            return Result.Conflict("An international license already exists for this local license.");

        var entity = InternationalLicenseMapper.ToEntity(dto);
        entity.CreatedByUserID = _currentUserService.UserId;

        await _repository.AddAsync(entity);

        var saved = await _unitOfWork.SaveChangesAsync();
        if (saved <= 0 || entity.InternationalLicenseID <= 0)
            return Result.Failure("Failed to save international license.");

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(UpdateInternationalLicenseDto dto)
    {
        var validation = InternationalLicenseValidator.ValidateUpdate(dto);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(dto.InternationalLicenseID);
        if (existing is null)
            return Result.NotFound("International license not found.");

        // Prevent duplicate local license binding
        if (existing.IssuedUsingLocalLicenseID != dto.IssuedUsingLocalLicenseID)
        {
            if (await _repository.ExistsByLocalLicenseAsync(dto.IssuedUsingLocalLicenseID))
                return Result.Conflict("An international license already exists for this local license.");
        }

        existing.ApplicationID = dto.ApplicationID;
        existing.DriverID = dto.DriverID;
        existing.IssuedUsingLocalLicenseID = dto.IssuedUsingLocalLicenseID;
        existing.IssueDate = dto.IssueDate;
        existing.ExpirationDate = dto.ExpirationDate;
        existing.IsActive = dto.IsActive;

        var updated = await _repository.UpdateAsync(existing);
        if (!updated)
            return Result.Failure("Failed to update international license.");

        var saved = await _unitOfWork.SaveChangesAsync();
        return saved > 0
            ? Result.Success()
            : Result.Failure("Failed to save international license changes.");
    }

    public async Task<Result> DeleteAsync(int internationalLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateId(internationalLicenseId);
        if (validation.IsFailure)
            return Result.ValidationFailure(validation.Error);

        var existing = await _repository.GetByIdAsync(internationalLicenseId);
        if (existing is null)
            return Result.NotFound("International license not found.");

        var deleted = await _repository.DeleteAsync(internationalLicenseId);
        if (!deleted)
            return Result.Failure("Failed to delete international license.");

        var saved = await _unitOfWork.SaveChangesAsync();
        return saved > 0
            ? Result.Success()
            : Result.Failure("Failed to save international license deletion.");
    }

    // ============ Issue International License ============

    public async Task<Result<int>> IssueInternationalLicenseAsync(int localLicenseId)
    {
        // Validate input
        var idValidation = InternationalLicenseValidator.ValidateLocalLicenseId(localLicenseId);
        if (idValidation.IsFailure)
            return Result<int>.FromValidationFailure(idValidation.Error);

        // Authenticated user required
        if (!_currentUserService.IsLoggedIn || _currentUserService.UserId <= 0)
            return Result<int>.FromFailure("Authenticated user is required.");

        var currentUserId = _currentUserService.UserId;

        // Load local license
        var licenseResult = await _licenseQueryService.GetByIdAsync(localLicenseId);
        if (licenseResult.IsFailure)
            return Result<int>.FromFailure(licenseResult.Error);
        if (licenseResult.Value is null)
            return Result<int>.FromNotFound("Local license not found.");

        var license = licenseResult.Value;

        // Business rules: must be class 3, active and not expired
        if (license.LicenseClassID != 3)
            return Result<int>.FromValidationFailure("Only class 3 licenses can be issued internationally.");

        if (!license.IsActive)
            return Result<int>.FromConflict("License is not active.");

        if (license.ExpirationDate <= DateTime.UtcNow)
            return Result<int>.FromConflict("License is expired.");

        if (await _repository.ExistsByLocalLicenseAsync(localLicenseId))
            return Result<int>.FromConflict("An international license already exists.");

        // Load international application type (Id = 6)
        const int internationalApplicationTypeId = 6;
        var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(internationalApplicationTypeId);
        if (applicationTypeResult.IsFailure)
            return Result<int>.FromFailure(applicationTypeResult.Error);
        if (applicationTypeResult.Value is null)
            return Result<int>.FromNotFound("International application type not found.");

        if (license.Driver is null)
            return Result<int>.FromNotFound("Driver information is not available.");

        var now = DateTime.UtcNow;

        // Transactional creation of application + license
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            // 1) Create the base application
            var application = new CreateApplicationDto
            {
                ApplicantPersonID = license.Driver.PersonID,
                ApplicationDate = now,
                ApplicationTypeID = applicationTypeResult.Value.ApplicationTypeId,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = now,
                PaidFees = applicationTypeResult.Value.ApplicationTypeFees
            };

            var applicationResult = await _applicationService.AddNewApplicationAsync(application);
            if (applicationResult.IsFailure)
                return Result<int>.FromFailure(applicationResult.Error);

            var applicationId = applicationResult.Value;
            if (applicationId <= 0)
                return Result<int>.FromFailure("Failed to create international application.");

            // 2) Create the international license
            var internationalLicense = new InternationalLicense
            {
                ApplicationID = applicationId,
                DriverID = license.DriverID,
                IssuedUsingLocalLicenseID = license.LicenseID,
                IssueDate = now,
                ExpirationDate = now.AddYears(1),
                IsActive = true,
                CreatedByUserID = currentUserId
            };

            await _repository.AddAsync(internationalLicense);

            if (await _unitOfWork.SaveChangesAsync() <= 0 || internationalLicense.InternationalLicenseID <= 0)
                return Result<int>.FromFailure("Failed to create international license.");

            // 3) Complete the application
            var completeResult = await _applicationService.CompleteApplicationAsync(applicationId);
            if (completeResult.IsFailure)
                return Result<int>.FromFailure(completeResult.Error);

            await transaction.CommitAsync();

            return Result<int>.Success(internationalLicense.InternationalLicenseID);
        }
        catch (Exception ex)
        {
            try { await transaction.RollbackAsync(); } catch { }

            return Result<int>.FromFailure($"Failed to issue international license: {ex.Message}");
        }
    }

    // ============ Get Local License Info ============

    public async Task<Result<DriverLicenseInfoDto>> GetLocalLicenseInfoAsync(int licenseId)
    {
        var validation = InternationalLicenseValidator.ValidateLocalLicenseId(licenseId);
        if (validation.IsFailure)
            return Result<DriverLicenseInfoDto>.FromValidationFailure(validation.Error);

        var licenseResult = await _licenseQueryService.GetByIdAsync(licenseId);
        if (licenseResult.IsFailure)
            return Result<DriverLicenseInfoDto>.FromFailure(licenseResult.Error);
        if (licenseResult.Value is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("License not found.");

        var license = licenseResult.Value;

        // Must be class 3 to be convertible
        if (license.LicenseClassID != 3)
            return Result<DriverLicenseInfoDto>.FromValidationFailure("Only class 3 licenses can be converted.");

        // Driver info required
        if (license.Driver is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("Driver information is not available.");

        var dto = InternationalLicenseMapper.ToDriverLicenseInfoDto(license);

        return Result<DriverLicenseInfoDto>.Success(dto);
    }
}
