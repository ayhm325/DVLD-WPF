using Application.Common.Results;
using Application.DTOs.DriverDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class LicenseService : ILicenseService
{
    private readonly ILicenseRepository _repository;
    private readonly ILocalDrivingLicenseApplicationService
        _localDrivingLicenseApplicationService;
    private readonly IApplicationService _applicationService;
    private readonly IDriverService _driverService;
    private readonly IPersonService _personService;
    private readonly IDetainedLicenseService _detainedLicenseService;

    public LicenseService(
        ILicenseRepository repository,
        ILocalDrivingLicenseApplicationService
            localDrivingLicenseApplicationService,
        IApplicationService applicationService,
        IDriverService driverService,
        IPersonService personService,
        IDetainedLicenseService detainedLicenseService)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));

        _localDrivingLicenseApplicationService =
            localDrivingLicenseApplicationService
            ?? throw new ArgumentNullException(
                nameof(localDrivingLicenseApplicationService));

        _applicationService =
            applicationService
            ?? throw new ArgumentNullException(
                nameof(applicationService));

        _driverService =
            driverService
            ?? throw new ArgumentNullException(
                nameof(driverService));

        _personService =
            personService
            ?? throw new ArgumentNullException(
                nameof(personService));

        _detainedLicenseService =
            detainedLicenseService
            ?? throw new ArgumentNullException(
                nameof(detainedLicenseService));
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Result<LicenseDto>> GetByIdAsync(int id)
    {
        var validation =
            LicenseValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result<LicenseDto>.FromValidationFailure(
                validation.Error);
        }

        var license =
            await _repository.GetLicenseByIdAsync(id);

        if (license is null)
        {
            return Result<LicenseDto>.FromNotFound(
                "License not found.");
        }

        return Result<LicenseDto>.Success(
            LicenseMapper.ToDto(license));
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<Result<List<LicenseDto>>> GetAllAsync()
    {
        var licenses =
            await _repository.GetAllLicensesAsync();

        return Result<List<LicenseDto>>.Success(
            licenses
                .Select(LicenseMapper.ToDto)
                .ToList());
    }


    // =========================================================
    // GET BY DRIVER ID
    // =========================================================

    public async Task<Result<List<LicenseDto>>>
        GetByDriverIdAsync(int driverId)
    {
        var validation =
            LicenseValidator.ValidateDriverId(driverId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var licenses =
            await _repository
                .GetLicensesByDriverIdAsync(driverId);

        return Result<List<LicenseDto>>.Success(
            licenses
                .Select(LicenseMapper.ToDto)
                .ToList());
    }


    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    public async Task<Result<List<LicenseDto>>>
        GetByApplicationIdAsync(int applicationId)
    {
        var validation =
            LicenseValidator.ValidateApplicationId(
                applicationId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var licenses =
            await _repository
                .GetLicensesByApplicationIdAsync(
                    applicationId);

        return Result<List<LicenseDto>>.Success(
            licenses
                .Select(LicenseMapper.ToDto)
                .ToList());
    }


    // =========================================================
    // GET BY LICENSE CLASS ID
    // =========================================================

    public async Task<Result<List<LicenseDto>>>
        GetByLicenseClassIdAsync(int licenseClassId)
    {
        var validation =
            LicenseValidator.ValidateLicenseClassId(
                licenseClassId);

        if (validation.IsFailure)
        {
            return Result<List<LicenseDto>>
                .FromValidationFailure(
                    validation.Error);
        }

        var licenses =
            await _repository
                .GetLicensesByLicenseClassIdAsync(
                    licenseClassId);

        return Result<List<LicenseDto>>.Success(
            licenses
                .Select(LicenseMapper.ToDto)
                .ToList());
    }


    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    public async Task<Result<List<LicenseDto>>>
        GetLicensesByPersonIdAsync(int personId)
    {
        if (personId <= 0)
        {
            return Result<List<LicenseDto>>
                .FromValidationFailure(
                    "Invalid person ID.");
        }

        var licenses =
            await _repository
                .GetLicensesByPersonIdAsync(personId);

        return Result<List<LicenseDto>>.Success(
            licenses
                .Select(LicenseMapper.ToDto)
                .ToList());
    }


    // =========================================================
    // GET DETAILS BY LOCAL APPLICATION ID
    // =========================================================

    public async Task<Result<DriverLicenseInfoDto>>
     GetDetailsAsync(int localAppId)
    {
        if (localAppId <= 0)
        {
            return Result<DriverLicenseInfoDto>
                .FromValidationFailure(
                    "Invalid local application ID.");
        }

        // =========================================================
        // GET LOCAL DRIVING LICENSE APPLICATION
        // =========================================================

        var localAppResult =
            await _localDrivingLicenseApplicationService
                .GetLocalDrivingLicenseApplicationByIdAsync(
                    localAppId);

        if (localAppResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    localAppResult.Error);
        }

        var localApplication =
            localAppResult.Value!;

        // =========================================================
        // GET APPLICATION ID
        // =========================================================

        var applicationIdResult =
            await _localDrivingLicenseApplicationService
                .GetApplicationIdByLocalIdAsync(
                    localAppId);

        if (applicationIdResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    applicationIdResult.Error);
        }

        var applicationId =
            applicationIdResult.Value;

        // =========================================================
        // GET APPLICATION
        // =========================================================

        var applicationResult =
            await _applicationService
                .GetApplicationByIdAsync(
                    applicationId);

        if (applicationResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    applicationResult.Error);
        }

        var application =
            applicationResult.Value!;

        // =========================================================
        // GET PERSON
        // =========================================================

        var personResult =
            await _personService
                .GetPersonByIdAsync(
                    application.ApplicantPersonID);

        if (personResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(
                    personResult.Error);
        }

        var person =
            personResult.Value!;

        // =========================================================
        // GET LICENSES FOR THIS APPLICATION
        // =========================================================

        var licenses =
            await _repository
                .GetLicensesByApplicationIdAsync(
                    applicationId);

        // =========================================================
        // GET LICENSE FOR THE SELECTED LICENSE CLASS
        // =========================================================

        var license =
            licenses.FirstOrDefault(x =>
                x.LicenseClassInfo != null &&
                x.LicenseClassInfo.LicenseClassID ==
                localApplication.LicenseClassID);

        if (license is null)
        {
            return Result<DriverLicenseInfoDto>
                .FromNotFound(
                    "License for the selected license class was not found.");
        }

        // =========================================================
        // GET DRIVER
        // =========================================================

        var driverResult =
            await _driverService
                .GetByPersonIdAsync(
                    person.PersonId);

        var driverId =
            driverResult.IsSuccess
                ? driverResult.Value!.DriverID
                : 0;

        // =========================================================
        // CHECK DETAINED
        // =========================================================

        var isDetained =
            await _detainedLicenseService
                .IsLicenseDetainedAsync(
                    license.LicenseID);

        // =========================================================
        // RETURN
        // =========================================================

        return Result<DriverLicenseInfoDto>.Success(
            new DriverLicenseInfoDto
            {
                LicenseId =
                    license.LicenseID,

                LicenseClass =
                    license.LicenseClassInfo?.ClassName
                    ?? "Unknown",

                IssueDate =
                    license.IssueDate,

                ExpirationDate =
                    license.ExpirationDate,

                IsActive =
                    license.IsActive,

                IsDetained =
                    isDetained,

                IssueReason =
                    ((IssueReason)license.IssueReason)
                        .ToString(),

                Notes =
                    license.Notes,

                LicenseClassFees =
                    license.LicenseClassInfo?.ClassFees
                    ?? 0,

                DriverId =
                    driverId,

                PersonID =
                    person.PersonId,

                FullName =
                    person.FullName,

                NationalNo =
                    person.NationalNo,

                DateOfBirth =
                    person.DateOfBirth,

                Gender =
                    person.Gender.ToString(),

                ImagePath =
                    person.ImagePath
            });
    }

    // =========================================================
    // GET LICENSE DETAILS BY LICENSE ID
    // =========================================================

    public async Task<Result<DriverLicenseInfoDto>>
        GetLicenseDetailsByIdAsync(int licenseId)
    {
        var validation =
            LicenseValidator.ValidateId(licenseId);

        if (validation.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromValidationFailure(
                    validation.Error);
        }

        var license =
            await _repository
                .GetLicenseByIdAsync(
                    licenseId);

        if (license is null)
        {
            return Result<DriverLicenseInfoDto>
                .FromNotFound(
                    "License not found.");
        }

        var person =
            license.Driver?.Person;

        if (person is null)
        {
            return Result<DriverLicenseInfoDto>
                .FromNotFound(
                    "Person information not found.");
        }

        var isDetained =
            await _detainedLicenseService
                .IsLicenseDetainedAsync(
                    license.LicenseID);

        return Result<DriverLicenseInfoDto>.Success(
            new DriverLicenseInfoDto
            {
                LicenseId =
                    license.LicenseID,

                LicenseClass =
                    license.LicenseClassInfo?.ClassName
                    ?? "Unknown",

                IssueDate =
                    license.IssueDate,

                ExpirationDate =
                    license.ExpirationDate,

                IsActive =
                    license.IsActive,

                IsDetained =
                    isDetained,

                IssueReason =
                    ((IssueReason)license.IssueReason)
                    .ToString(),

                Notes =
                    license.Notes,

                LicenseClassFees =
                    license.LicenseClassInfo?.ClassFees
                    ?? 0,

                DriverId =
                    license.DriverID,

                PersonID =
                    person.PersonId,

                FullName =
                    person.FullName,

                NationalNo =
                    person.NationalNo,

                DateOfBirth =
                    person.DateOfBirth,

                Gender =
                    person.Gender.ToString(),

                ImagePath =
                    person.ImagePath
            });
    }


    // =========================================================
    // CHECKS
    // =========================================================

    public async Task<Result<bool>>
        IsLicenseExistsAsync(int id)
    {
        if (id <= 0)
        {
            return Result<bool>.FromValidationFailure(
                "Invalid license ID.");
        }

        var exists =
            await _repository
                .IsLicenseExistsAsync(id);

        return Result<bool>.Success(exists);
    }


    public async Task<Result<bool>>
        IsDriverHasLicenseAsync(int driverId)
    {
        if (driverId <= 0)
        {
            return Result<bool>.FromValidationFailure(
                "Invalid driver ID.");
        }

        var exists =
            await _repository
                .IsDriverHasLicenseAsync(
                    driverId);

        return Result<bool>.Success(exists);
    }


    public async Task<Result<bool>>
        IsApplicationHasLicenseAsync(
            int applicationId)
    {
        if (applicationId <= 0)
        {
            return Result<bool>.FromValidationFailure(
                "Invalid application ID.");
        }

        var exists =
            await _repository
                .IsApplicationHasLicenseAsync(
                    applicationId);

        return Result<bool>.Success(exists);
    }


    // =========================================================
    // ADD
    // =========================================================

    public async Task<Result<int>>
        AddAsync(CreateLicenseDto dto)
    {
        var validation =
            LicenseValidator.ValidateCreate(dto);

        if (validation.IsFailure)
        {
            return Result<int>.FromValidationFailure(
                validation.Error);
        }

        var entity =
            LicenseMapper.ToEntity(dto);

        var id =
            await _repository
                .AddLicenseAsync(entity);

        if (id <= 0)
        {
            return Result<int>.FromFailure(
                "Failed to create license.");
        }

        return Result<int>.Success(id);
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<Result>
        UpdateAsync(UpdateLicenseDto dto)
    {
        var validation =
            LicenseValidator.ValidateUpdate(dto);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var exists =
            await _repository
                .IsLicenseExistsAsync(
                    dto.LicenseID);

        if (!exists)
        {
            return Result.NotFound(
                "License not found.");
        }

        var entity =
            LicenseMapper.ToEntity(dto);

        var success =
            await _repository
                .UpdateLicenseAsync(entity);

        return success
            ? Result.Success()
            : Result.Failure(
                "Failed to update license.");
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task<Result>
        DeleteAsync(int id)
    {
        var validation =
            LicenseValidator.ValidateId(id);

        if (validation.IsFailure)
        {
            return Result.ValidationFailure(
                validation.Error);
        }

        var exists =
            await _repository
                .IsLicenseExistsAsync(id);

        if (!exists)
        {
            return Result.NotFound(
                "License not found.");
        }

        var success =
            await _repository
                .DeleteLicenseAsync(id);

        return success
            ? Result.Success()
            : Result.Failure(
                "Failed to delete license.");
    }
}