using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class InternationalService : IInternationalService
{
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
        ICurrentUserService currentUserService)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _licenseService = licenseService
            ?? throw new ArgumentNullException(nameof(licenseService));

        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));

        _applicationTypeService = applicationTypeService
            ?? throw new ArgumentNullException(nameof(applicationTypeService));

        _currentUserService = currentUserService
            ?? throw new ArgumentNullException(nameof(currentUserService));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<InternationalDto>>>
        GetAllAsync()
    {
        var entities = await _repository.GetAllAsync();

        var dtos = entities
            .Select(MapToDto)
            .ToList();

        return Result<List<InternationalDto>>
            .Success(dtos);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<InternationalDto>>
        GetByIdAsync(
            int internationalLicenseId)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateId(internationalLicenseId);

        if (validation.IsFailure)
        {
            return Result<InternationalDto>
                .FromFailure(validation.Error);
        }

        var entity =
            await _repository
                .GetByIdAsync(internationalLicenseId);

        if (entity is null)
        {
            return Result<InternationalDto>
                .FromFailure(
                    "International license not found.");
        }

        return Result<InternationalDto>
            .Success(MapToDto(entity));
    }


    // =========================================================
    // GET BY DRIVER ID
    // =========================================================

    public async Task<Result<List<InternationalDto>>>
        GetByDriverIdAsync(
            int driverId)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateDriverId(driverId);

        if (validation.IsFailure)
        {
            return Result<List<InternationalDto>>
                .FromFailure(validation.Error);
        }

        var entities =
            await _repository
                .GetByDriverIdAsync(driverId);

        var dtos = entities
            .Select(MapToDto)
            .ToList();

        return Result<List<InternationalDto>>
            .Success(dtos);
    }


    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    public async Task<Result<InternationalDto>>
        GetByApplicationIdAsync(
            int applicationId)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateApplicationId(applicationId);

        if (validation.IsFailure)
        {
            return Result<InternationalDto>
                .FromFailure(validation.Error);
        }

        var entity =
            await _repository
                .GetByApplicationIdAsync(applicationId);

        if (entity is null)
        {
            return Result<InternationalDto>
                .FromFailure(
                    "International license not found.");
        }

        return Result<InternationalDto>
            .Success(MapToDto(entity));
    }


    // =========================================================
    // GET BY LOCAL LICENSE ID
    // =========================================================

    public async Task<Result<List<InternationalDto>>>
        GetByLocalLicenseIdAsync(
            int localLicenseId)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateLocalLicenseId(localLicenseId);

        if (validation.IsFailure)
        {
            return Result<List<InternationalDto>>
                .FromFailure(validation.Error);
        }

        var entities =
            await _repository
                .GetByLocalLicenseIdAsync(localLicenseId);

        var dtos = entities
            .Select(MapToDto)
            .ToList();

        return Result<List<InternationalDto>>
            .Success(dtos);
    }


    // =========================================================
    // CHECK ACTIVE
    // =========================================================

    public async Task<bool>
        HasActiveInternationalLicenseAsync(
            int driverId)
    {
        if (driverId <= 0)
            return false;

        return await _repository
            .HasActiveInternationalLicenseAsync(driverId);
    }


    // =========================================================
    // ADD
    // =========================================================

    public async Task<Result>
        AddAsync(
            CreateInternationalLicenseDto dto)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result
                .Failure(validation.Error);
        }

        if (await _repository
            .ExistsByLocalLicenseAsync(
                dto.IssuedUsingLocalLicenseID))
        {
            return Result.Failure(
                "An international license already exists for this local license.");
        }

        var entity = MapToEntity(dto);

        var id =
            await _repository
                .AddAsync(entity);

        if (id <= 0)
        {
            return Result.Failure(
                "Failed to create international license.");
        }

        return Result.Success();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateAsync(
            UpdateInternationalLicenseDto dto)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result
                .Failure(validation.Error);
        }

        var existing =
            await _repository
                .GetByIdAsync(
                    dto.InternationalLicenseID);

        if (existing is null)
        {
            return Result.Failure(
                "International license not found.");
        }

        // Prevent assigning another existing
        // international license to the same local license.
        if (existing.IssuedUsingLocalLicenseID !=
            dto.IssuedUsingLocalLicenseID)
        {
            if (await _repository
                .ExistsByLocalLicenseAsync(
                    dto.IssuedUsingLocalLicenseID))
            {
                return Result.Failure(
                    "An international license already exists for this local license.");
            }
        }

        // Update entity
        existing.ApplicationID =
            dto.ApplicationID;

        existing.DriverID =
            dto.DriverID;

        existing.IssuedUsingLocalLicenseID =
            dto.IssuedUsingLocalLicenseID;

        existing.IssueDate =
            dto.IssueDate;

        existing.ExpirationDate =
            dto.ExpirationDate;

        existing.IsActive =
            dto.IsActive;

        //existing.CreatedByUserID =
        //    dto.CreatedByUserID;

        var updated =
            await _repository
                .UpdateAsync(existing);

        return updated
            ? Result.Success()
            : Result.Failure(
                "Failed to update international license.");
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteAsync(
            int internationalLicenseId)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateId(internationalLicenseId);

        if (validation.IsFailure)
        {
            return Result
                .Failure(validation.Error);
        }

        var existing =
            await _repository
                .GetByIdAsync(internationalLicenseId);

        if (existing is null)
        {
            return Result.Failure(
                "International license not found.");
        }

        var deleted =
            await _repository
                .DeleteAsync(internationalLicenseId);

        return deleted
            ? Result.Success()
            : Result.Failure(
                "Failed to delete international license.");
    }


    // =========================================================
    // ISSUE INTERNATIONAL LICENSE
    // =========================================================

    public async Task<Result<int>>
        IssueInternationalLicenseAsync(
            int localLicenseId)
    {
        // -----------------------------------------------------
        // 1. Validate local license ID
        // -----------------------------------------------------

        var idValidation =
            InternationalLicenseValidator
                .ValidateLocalLicenseId(localLicenseId);

        if (idValidation.IsFailure)
        {
            return Result<int>
                .FromFailure(idValidation.Error);
        }


        // -----------------------------------------------------
        // 2. Get local license
        // -----------------------------------------------------

        var licenseResult =
            await _licenseService
                .GetByIdAsync(localLicenseId);

        if (licenseResult.IsFailure)
        {
            return Result<int>
                .FromFailure(licenseResult.Error);
        }

        if (licenseResult.Value is null)
        {
            return Result<int>
                .FromFailure(
                    "Local license not found.");
        }

        var license =
            licenseResult.Value;


        // -----------------------------------------------------
        // 3. Only class 3
        // -----------------------------------------------------

        if (license.LicenseClassID != 3)
        {
            return Result<int>
                .FromFailure(
                    "Only class 3 licenses can be issued internationally.");
        }


        // -----------------------------------------------------
        // 4. License must be active
        // -----------------------------------------------------

        if (!license.IsActive)
        {
            return Result<int>
                .FromFailure(
                    "License is not active.");
        }


        // -----------------------------------------------------
        // 5. License must not be expired
        // -----------------------------------------------------

        if (license.ExpirationDate <= DateTime.UtcNow)
        {
            return Result<int>
                .FromFailure(
                    "License is expired.");
        }


        // -----------------------------------------------------
        // 6. One international license per local license
        // -----------------------------------------------------

        if (await _repository
            .ExistsByLocalLicenseAsync(localLicenseId))
        {
            return Result<int>
                .FromFailure(
                    "An international license already exists.");
        }


        // -----------------------------------------------------
        // 7. International application type
        // -----------------------------------------------------

        const int internationalApplicationTypeId = 6;

        var applicationTypeResult =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(
                    internationalApplicationTypeId);

        if (applicationTypeResult.IsFailure)
        {
            return Result<int>
                .FromFailure(
                    applicationTypeResult.Error);
        }

        if (applicationTypeResult.Value is null)
        {
            return Result<int>
                .FromFailure(
                    "International application type not found.");
        }

        var applicationType =
            applicationTypeResult.Value;


        // -----------------------------------------------------
        // 8. Driver validation
        // -----------------------------------------------------

        if (license.Driver is null)
        {
            return Result<int>
                .FromFailure(
                    "Driver information is not available.");
        }

        var now = DateTime.UtcNow;


        // -----------------------------------------------------
        // 9. Create application
        // -----------------------------------------------------

        var application =
            new CreateApplicationDto
            {
                ApplicantPersonID =
                    license.Driver.PersonID,

                ApplicationDate =
                    now,

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    now,

                PaidFees =
                    applicationType.ApplicationTypeFees,

                CreatedByUserID =
                    _currentUserService.UserId
            };

        var applicationResult =
            await _applicationService
                .AddNewApplicationAsync(
                    application);

        if (applicationResult.IsFailure)
        {
            return Result<int>
                .FromFailure(
                    applicationResult.Error);
        }

        if (applicationResult.Value <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create international application.");
        }


        // -----------------------------------------------------
        // 10. Create international license entity
        // -----------------------------------------------------

        var internationalLicense =
            new InternationalLicense
            {
                ApplicationID =
                    applicationResult.Value,

                DriverID =
                    license.DriverID,

                IssuedUsingLocalLicenseID =
                    license.LicenseID,

                IssueDate =
                    now,

                ExpirationDate =
                    now.AddYears(1),

                IsActive =
                    true,

                CreatedByUserID =
                    _currentUserService.UserId
            };


        // -----------------------------------------------------
        // 11. Add international license directly
        // -----------------------------------------------------

        var internationalLicenseId =
            await _repository
                .AddAsync(
                    internationalLicense);

        if (internationalLicenseId <= 0)
        {
            return Result<int>
                .FromFailure(
                    "Failed to create international license.");
        }


        // -----------------------------------------------------
        // 12. Complete application
        // -----------------------------------------------------

        var completeResult =
            await _applicationService
                .CompleteApplicationAsync(
                    applicationResult.Value);

        if (completeResult.IsFailure)
        {
            return Result<int>
                .FromFailure(
                    completeResult.Error);
        }


        // -----------------------------------------------------
        // 13. Return created license ID
        // -----------------------------------------------------

        return Result<int>
            .Success(internationalLicenseId);
    }


    // =========================================================
    // GET LOCAL LICENSE INFO
    // =========================================================

    public async Task<Result<DriverLicenseInfoDto>>
        GetLocalLicenseInfoAsync(
            int licenseId)
    {
        var validation =
            InternationalLicenseValidator
                .ValidateLocalLicenseId(licenseId);

        if (validation.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(validation.Error);
        }


        var licenseResult =
            await _licenseService
                .GetByIdAsync(licenseId);

        if (licenseResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    licenseResult.Error);
        }

        if (licenseResult.Value is null)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    "License not found.");
        }

        var license =
            licenseResult.Value;


        // -----------------------------------------------------
        // License class
        // -----------------------------------------------------

        if (license.LicenseClassID != 3)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    "Only class 3 licenses can be converted.");
        }


        // -----------------------------------------------------
        // Driver
        // -----------------------------------------------------

        if (license.Driver is null)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    "Driver information is not available.");
        }

        var driver =
            license.Driver;


        // -----------------------------------------------------
        // Map DTO
        // -----------------------------------------------------

        var dto =
            new DriverLicenseInfoDto
            {
                LicenseId =
                    license.LicenseID,

                DriverId =
                    license.DriverID,

                LicenseClass =
                    license.LicenseClassName
                    ?? "Unknown",

                PersonID =
                    driver.PersonID,

                FullName =
                    driver.FullName,

                NationalNo =
                    driver.NationalNo,

                Gender =
                    driver.Gender == Gender.Male
                        ? "Male"
                        : "Female",

                DateOfBirth =
                    driver.DateOfBirth,

                IssueDate =
                    license.IssueDate,

                ExpirationDate =
                    license.ExpirationDate,

                IsActive =
                    license.IsActive,

                Notes =
                    license.Notes,

                IssueReason =
                    ((IssueReason)license.IssueReason)
                    .ToString(),

                ImagePath =
                    driver.ImagePath
                    ?? string.Empty
            };

        return Result<DriverLicenseInfoDto>
            .Success(dto);
    }


    // =========================================================
    // ENTITY -> DTO
    // =========================================================

    private static InternationalDto MapToDto(
    InternationalLicense entity)
    {
        return new InternationalDto
        {
            InternationalLicenseID =
                entity.InternationalLicenseID,

            ApplicationID =
                entity.ApplicationID,

            DriverID =
                entity.DriverID,

            IssuedUsingLocalLicenseID =
                entity.IssuedUsingLocalLicenseID,

            IssueDate =
                entity.IssueDate,

            ExpirationDate =
                entity.ExpirationDate,

            IsActive =
                entity.IsActive,

            CreatedByUserID =
                entity.CreatedByUserID,

            PersonID =
                entity.Driver?.PersonID ?? 0,

            FullName =
                entity.Driver?.Person?.FullName
                ?? string.Empty,

            DateOfBirth =
                entity.Driver?.Person?.DateOfBirth
                ?? DateTime.MinValue,

            ImagePath =
                entity.Driver?.Person?.ImagePath
                ?? string.Empty,

            NationalNo =
                entity.Driver?.Person?.NationalNo
                ?? string.Empty,

            Gender =
                entity.Driver?.Person?.Gender.ToString()
                ?? string.Empty,

            Fees =
                entity.Application?.PaidFees
                ?? 0m,

            CreatedByUserName =
                entity.CreatedByUser?.UserName
                ?? string.Empty
        };
    }


    // =========================================================
    // CREATE DTO -> ENTITY
    // =========================================================

    private static InternationalLicense
        MapToEntity(
            CreateInternationalLicenseDto dto)
    {
        return new InternationalLicense
        {
            ApplicationID =
                dto.ApplicationID,

            DriverID =
                dto.DriverID,

            IssuedUsingLocalLicenseID =
                dto.IssuedUsingLocalLicenseID,

            IssueDate =
                dto.IssueDate,

            ExpirationDate =
                dto.ExpirationDate,

            IsActive =
                dto.IsActive,

            CreatedByUserID =
                dto.CreatedByUserID
        };
    }
}