using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Validators;
using Domain.Enums;

namespace Application.Services;

public class LicenseQueryService : ILicenseQueryService
{
    private readonly ILicenseRepository _licenseRepository;
    private readonly ILocalDrivingLicenseApplicationService
        _localDrivingLicenseApplicationService;
    private readonly IApplicationService _applicationService;
    private readonly IDriverService _driverService;
    private readonly IPersonService _personService;
    private readonly IDetainedLicenseService _detainedLicenseService;

    public LicenseQueryService(
        ILicenseRepository licenseRepository,
        ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
        IApplicationService applicationService,
        IDriverService driverService,
        IPersonService personService,
        IDetainedLicenseService detainedLicenseService)
    {
        _licenseRepository =
            licenseRepository
            ?? throw new ArgumentNullException(
                nameof(licenseRepository));

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

    public async Task<Result<DriverLicenseInfoDto>>
        GetDetailsAsync(int localAppId)
    {
        if (localAppId <= 0)
        {
            return Result<DriverLicenseInfoDto>
                .FromValidationFailure(
                    "Invalid local application ID.");
        }

        var localAppResult =
            await _localDrivingLicenseApplicationService
                .GetLocalDrivingLicenseApplicationByIdAsync(
                    localAppId);

        if (localAppResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(localAppResult.Error);
        }

        var localApplication =
            localAppResult.Value!;

        var applicationIdResult =
            await _localDrivingLicenseApplicationService
                .GetApplicationIdByLocalIdAsync(
                    localAppId);

        if (applicationIdResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(applicationIdResult.Error);
        }

        var applicationId =
            applicationIdResult.Value;

        var applicationResult =
            await _applicationService
                .GetApplicationByIdAsync(applicationId);

        if (applicationResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(applicationResult.Error);
        }

        var application =
            applicationResult.Value!;

        var personResult =
            await _personService
                .GetPersonByIdAsync(
                    application.ApplicantPersonID);

        if (personResult.IsFailure)
        {
            return Result<DriverLicenseInfoDto>
                .FromFailure(personResult.Error);
        }

        var person =
            personResult.Value!;

        var licenses =
            await _licenseRepository
                .GetLicensesByApplicationIdAsync(
                    applicationId);

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

        var driverResult =
            await _driverService
                .GetByPersonIdAsync(
                    person.PersonId);

        var driverId =
            driverResult.IsSuccess
                ? driverResult.Value!.DriverID
                : 0;

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
            await _licenseRepository
                .GetLicenseByIdAsync(licenseId);

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
}