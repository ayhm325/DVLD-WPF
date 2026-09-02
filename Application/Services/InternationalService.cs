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
    private readonly ILicenseService _licenseService;
    private readonly IApplicationService _applicationService;
    private readonly IApplicationTypeService _applicationTypeService;
    private readonly ICurrentUserService _currentUserService;

    public InternationalService(
        IInternationalRepository repository,
        ILicenseService licenseService,
        IApplicationService applicationService,
        IApplicationTypeService applicationTypeService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _applicationTypeService = applicationTypeService ?? throw new ArgumentNullException(nameof(applicationTypeService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<InternationalDto>>> GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();

        var dtos = entities
            .Select(InternationalLicenseMapper.ToDto)
            .ToList();

        return Result<List<InternationalDto>>.Success(dtos);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<InternationalDto>> GetByIdAsync(int internationalLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateId(internationalLicenseId);

        if (validation.IsFailure)
        {
            return Result<InternationalDto>.FromValidationFailure(validation.Error);
        }

        var entity = await _repository.GetByIdAsync(internationalLicenseId);

        if (entity is null)
        {
            return Result<InternationalDto>.FromNotFound("International license not found.");
        }

        return Result<InternationalDto>.Success(InternationalLicenseMapper.ToDto(entity));
    }


    // =========================================================
    // GET BY DRIVER ID
    // =========================================================

    public async Task<Result<List<InternationalDto>>> GetByDriverIdAsync(int driverId)
    {
        var validation = InternationalLicenseValidator.ValidateDriverId(driverId);

        if (validation.IsFailure)
        {
            return Result<List<InternationalDto>>.FromValidationFailure(validation.Error);
        }

        var entities = await _repository.GetByDriverIdAsync(driverId);

        var dtos = entities
            .Select(InternationalLicenseMapper.ToDto)
            .ToList();

        return Result<List<InternationalDto>>.Success(dtos);
    }


    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    public async Task<Result<InternationalDto>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = InternationalLicenseValidator.ValidateApplicationId(applicationId);

        if (validation.IsFailure)
        {
            return Result<InternationalDto>.FromValidationFailure(validation.Error);
        }

        var entity = await _repository.GetByApplicationIdAsync(applicationId);

        if (entity is null)
        {
            return Result<InternationalDto>.FromNotFound("International license not found.");
        }

        return Result<InternationalDto>.Success(InternationalLicenseMapper.ToDto(entity));
    }


    // =========================================================
    // GET BY LOCAL LICENSE ID
    // =========================================================

    public async Task<Result<List<InternationalDto>>> GetByLocalLicenseIdAsync(int localLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateLocalLicenseId(localLicenseId);

        if (validation.IsFailure)
        {
            return Result<List<InternationalDto>>.FromValidationFailure(validation.Error);
        }

        var entities = await _repository.GetByLocalLicenseIdAsync(localLicenseId);

        var dtos = entities
            .Select(InternationalLicenseMapper.ToDto)
            .ToList();

        return Result<List<InternationalDto>>.Success(dtos);
    }


    // =========================================================
    // CHECK ACTIVE
    // =========================================================

    public async Task<bool> HasActiveInternationalLicenseAsync(int driverId)
    {
        if (driverId <= 0)
        {
            return false;
        }

        return await _repository.HasActiveInternationalLicenseAsync(driverId);
    }


    // =========================================================
    // ADD
    // =========================================================

    public async Task<Result> AddAsync(CreateInternationalLicenseDto dto)
    {
        var validation = InternationalLicenseValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.Error);
        }

        if (await _repository.ExistsByLocalLicenseAsync(dto.IssuedUsingLocalLicenseID))
        {
            return Result.Conflict("An international license already exists for this local license.");
        }

        var entity = InternationalLicenseMapper.ToEntity(dto);

        await _repository.AddAsync(entity);

        var saved = await _unitOfWork.SaveChangesAsync();

        if (saved <= 0 || entity.InternationalLicenseID <= 0)
        {
            return Result.Failure("Failed to save international license.");
        }

        return Result.Success();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result> UpdateAsync(UpdateInternationalLicenseDto dto)
    {
        var validation = InternationalLicenseValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.Error);
        }

        var existing = await _repository.GetByIdAsync(dto.InternationalLicenseID);

        if (existing is null)
        {
            return Result.NotFound("International license not found.");
        }

        // Prevent assigning another international
        // license to the same local license.
        if (existing.IssuedUsingLocalLicenseID != dto.IssuedUsingLocalLicenseID)
        {
            var exists = await _repository.ExistsByLocalLicenseAsync(dto.IssuedUsingLocalLicenseID);

            if (exists)
            {
                return Result.Conflict("An international license already exists for this local license.");
            }
        }

        existing.ApplicationID = dto.ApplicationID;
        existing.DriverID = dto.DriverID;
        existing.IssuedUsingLocalLicenseID = dto.IssuedUsingLocalLicenseID;
        existing.IssueDate = dto.IssueDate;
        existing.ExpirationDate = dto.ExpirationDate;
        existing.IsActive = dto.IsActive;

        var updated = await _repository.UpdateAsync(existing);

        if (!updated)
        {
            return Result.Failure("Failed to update international license.");
        }

        var saved = await _unitOfWork.SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure("Failed to save international license changes.");
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result> DeleteAsync(int internationalLicenseId)
    {
        var validation = InternationalLicenseValidator.ValidateId(internationalLicenseId);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.Error);
        }

        var existing = await _repository.GetByIdAsync(internationalLicenseId);

        if (existing is null)
        {
            return Result.NotFound("International license not found.");
        }

        var deleted = await _repository.DeleteAsync(internationalLicenseId);

        if (!deleted)
        {
            return Result.Failure("Failed to delete international license.");
        }

        var saved = await _unitOfWork.SaveChangesAsync();

        return saved > 0
            ? Result.Success()
            : Result.Failure("Failed to save international license deletion.");
    }


    // =========================================================
    // ISSUE INTERNATIONAL LICENSE
    // =========================================================

    public async Task<Result<int>> IssueInternationalLicenseAsync(int localLicenseId)
    {
        // -----------------------------------------------------
        // 1. Validate local license ID
        // -----------------------------------------------------

        var idValidation = InternationalLicenseValidator.ValidateLocalLicenseId(localLicenseId);

        if (idValidation.IsFailure)
        {
            return Result<int>.FromValidationFailure(idValidation.Error);
        }


        // -----------------------------------------------------
        // 2. Get local license
        // -----------------------------------------------------

        var licenseResult = await _licenseService.GetByIdAsync(localLicenseId);

        if (licenseResult.IsFailure)
        {
            return Result<int>.FromFailure(licenseResult.Error);
        }

        if (licenseResult.Value is null)
        {
            return Result<int>.FromNotFound("Local license not found.");
        }

        var license = licenseResult.Value;


        // -----------------------------------------------------
        // 3. Only class 3
        // -----------------------------------------------------

        if (license.LicenseClassID != 3)
        {
            return Result<int>.FromValidationFailure("Only class 3 licenses can be issued internationally.");
        }


        // -----------------------------------------------------
        // 4. License must be active
        // -----------------------------------------------------

        if (!license.IsActive)
        {
            return Result<int>.FromConflict("License is not active.");
        }


        // -----------------------------------------------------
        // 5. License must not be expired
        // -----------------------------------------------------

        if (license.ExpirationDate <= DateTime.UtcNow)
        {
            return Result<int>.FromConflict("License is expired.");
        }


        // -----------------------------------------------------
        // 6. One international license per local license
        // -----------------------------------------------------

        if (await _repository.ExistsByLocalLicenseAsync(localLicenseId))
        {
            return Result<int>.FromConflict("An international license already exists.");
        }


        // -----------------------------------------------------
        // 7. International application type
        // -----------------------------------------------------

        const int internationalApplicationTypeId = 6;

        var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(internationalApplicationTypeId);

        if (applicationTypeResult.IsFailure)
        {
            return Result<int>.FromFailure(applicationTypeResult.Error);
        }

        if (applicationTypeResult.Value is null)
        {
            return Result<int>.FromNotFound("International application type not found.");
        }

        var applicationType = applicationTypeResult.Value;


        // -----------------------------------------------------
        // 8. Driver validation
        // -----------------------------------------------------

        if (license.Driver is null)
        {
            return Result<int>.FromNotFound("Driver information is not available.");
        }

        var now = DateTime.UtcNow;


        // -----------------------------------------------------
        // 9. Atomic operation
        // -----------------------------------------------------

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            // -------------------------------------------------
            // Create application
            // -------------------------------------------------

            var application = new CreateApplicationDto
            {
                ApplicantPersonID = license.Driver.PersonID,
                ApplicationDate = now,
                ApplicationTypeID = applicationType.ApplicationTypeId,
                ApplicationStatus = AppStatus.New,
                LastStatusDate = now,
                PaidFees = applicationType.ApplicationTypeFees,
                CreatedByUserID = _currentUserService.UserId
            };

            var applicationResult = await _applicationService.AddNewApplicationAsync(application);

            if (applicationResult.IsFailure)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure(applicationResult.Error);
            }

            if (applicationResult.Value <= 0)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure("Failed to create international application.");
            }


            // -------------------------------------------------
            // Create international license
            // -------------------------------------------------

            var internationalLicense = new InternationalLicense
            {
                ApplicationID = applicationResult.Value,
                DriverID = license.DriverID,
                IssuedUsingLocalLicenseID = license.LicenseID,
                IssueDate = now,
                ExpirationDate = now.AddYears(1),
                IsActive = true,
                CreatedByUserID = _currentUserService.UserId
            };

            await _repository.AddAsync(internationalLicense);


            // -------------------------------------------------
            // Save international license
            // -------------------------------------------------

            var licenseSaved = await _unitOfWork.SaveChangesAsync();

            if (licenseSaved <= 0 || internationalLicense.InternationalLicenseID <= 0)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure("Failed to create international license.");
            }


            // -------------------------------------------------
            // Complete application
            // -------------------------------------------------

            var completeResult = await _applicationService.CompleteApplicationAsync(applicationResult.Value);

            if (completeResult.IsFailure)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure(completeResult.Error);
            }


            // -------------------------------------------------
            // Persist final application state
            // -------------------------------------------------

            var completedSaved = await _unitOfWork.SaveChangesAsync();

            if (completedSaved <= 0)
            {
                await transaction.RollbackAsync();
                return Result<int>.FromFailure("Failed to complete international application.");
            }


            // -------------------------------------------------
            // Commit transaction
            // -------------------------------------------------

            await transaction.CommitAsync();


            // -------------------------------------------------
            // Return generated ID
            // -------------------------------------------------

            return Result<int>.Success(internationalLicense.InternationalLicenseID);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result<int>.FromFailure($"Failed to issue international license: {ex.Message}");
        }
    }


    // =========================================================
    // GET LOCAL LICENSE INFO
    // =========================================================

    public async Task<Result<DriverLicenseInfoDto>> GetLocalLicenseInfoAsync(int licenseId)
    {
        var validation = InternationalLicenseValidator.ValidateLocalLicenseId(licenseId);

        if (validation.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromValidationFailure(validation.Error);
        }

        var licenseResult = await _licenseService.GetByIdAsync(licenseId);

        if (licenseResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>.FromFailure(licenseResult.Error);
        }

        if (licenseResult.Value is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("License not found.");
        }

        var license = licenseResult.Value;


        // -----------------------------------------------------
        // License class
        // -----------------------------------------------------

        if (license.LicenseClassID != 3)
        {
            return Result<DriverLicenseInfoDto>.FromValidationFailure("Only class 3 licenses can be converted.");
        }


        // -----------------------------------------------------
        // Driver
        // -----------------------------------------------------

        if (license.Driver is null)
        {
            return Result<DriverLicenseInfoDto>.FromNotFound("Driver information is not available.");
        }


        // -----------------------------------------------------
        // Map using Mapper
        // -----------------------------------------------------

        var dto = InternationalLicenseMapper.ToDriverLicenseInfoDto(license);

        return Result<DriverLicenseInfoDto>.Success(dto);
    }
}