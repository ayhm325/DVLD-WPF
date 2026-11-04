using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Mappers;
using Application.Mappings;
using Application.Validators;
using Domain.Entities;

namespace Application.Services;

public sealed class LicenseQueryService(
    ILicenseRepository licenseRepository,
    ILocalDrivingLicenseApplicationService localApplicationService,
    IDetainedLicenseRepository detainedLicenseRepository,
    IJordanClock clock) : ILicenseQueryService
{
    private readonly ILicenseRepository _licenseRepository =
        licenseRepository ?? throw new ArgumentNullException(nameof(licenseRepository));
    private readonly ILocalDrivingLicenseApplicationService _localApplicationService =
        localApplicationService ?? throw new ArgumentNullException(nameof(localApplicationService));
    private readonly IDetainedLicenseRepository _detainedLicenseRepository =
        detainedLicenseRepository ?? throw new ArgumentNullException(nameof(detainedLicenseRepository));
    private readonly IJordanClock _clock =
        clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<Result<LicenseDto>> GetByIdAsync(int licenseId)
    {
        var validation = LicenseValidator.ValidateId(licenseId);
        if (validation.IsFailure)
            return Result<LicenseDto>.FromValidationFailure(validation.Error);

        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);
        if (license is null)
            return Result<LicenseDto>.FromNotFound("License not found.");

        var (isExpired, isEligible, reason) = await EvaluateRenewalEligibilityAsync(license);
        return Result<LicenseDto>.Success(LicenseMapper.ToDto(license, isExpired, isEligible, reason));
    }

    public async Task<Result<List<LicenseDto>>> GetAllAsync()
    {
        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var licenses = await _licenseRepository.GetAllLicensesAsync();
        var allDetained = await _detainedLicenseRepository.GetAllAsync() ?? Enumerable.Empty<DetainedLicense>();
        var activeDetainedIds = allDetained.Where(d => !d.IsReleased).Select(d => d.LicenseID).ToHashSet();
        var today = _clock.Now.Date;

        var dtos = licenses.Select(l =>
        {
            var isExpired = today > l.ExpirationDate.Date;
            bool isEligible;
            string? reason;

            if (!isExpired)
            {
                isEligible = false;
                reason = "License is not expired yet. Renewal is only permitted starting the calendar day after expiration.";
            }
            else if (activeDetainedIds.Contains(l.LicenseID))
            {
                isEligible = false;
                reason = "Cannot renew a currently detained license. The license must be released first.";
            }
            else
            {
                var newer = licenses.FirstOrDefault(other =>
                    other.DriverID == l.DriverID &&
                    other.LicenseClass == l.LicenseClass &&
                    other.LicenseID > l.LicenseID);

                if (newer != null)
                {
                    isEligible = false;
                    var action = newer.IssueReason == Domain.Enums.IssueReason.Renew ? "renewed" : "superseded";
                    reason = $"This license was already {action} (License #{newer.LicenseID}).";
                }
                else
                {
                    isEligible = true;
                    reason = null;
                }
            }

            return LicenseMapper.ToDto(l, isExpired, isEligible, reason);
        }).ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByDriverIdAsync(int driverId)
    {
        var validation = LicenseValidator.ValidateDriverId(driverId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);

        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var licenses = await _licenseRepository.GetLicensesByDriverIdAsync(driverId);
        var allDetained = await _detainedLicenseRepository.GetAllAsync() ?? Enumerable.Empty<DetainedLicense>();
        var activeDetainedIds = allDetained.Where(d => !d.IsReleased).Select(d => d.LicenseID).ToHashSet();
        var today = _clock.Now.Date;

        var dtos = licenses.Select(l =>
        {
            var isExpired = today > l.ExpirationDate.Date;
            bool isEligible;
            string? reason;

            if (!isExpired)
            {
                isEligible = false;
                reason = "License is not expired yet. Renewal is only permitted starting the calendar day after expiration.";
            }
            else if (activeDetainedIds.Contains(l.LicenseID))
            {
                isEligible = false;
                reason = "Cannot renew a currently detained license. The license must be released first.";
            }
            else
            {
                var newer = licenses.FirstOrDefault(other =>
                    other.DriverID == l.DriverID &&
                    other.LicenseClass == l.LicenseClass &&
                    other.LicenseID > l.LicenseID);

                if (newer != null)
                {
                    isEligible = false;
                    var action = newer.IssueReason == Domain.Enums.IssueReason.Renew ? "renewed" : "superseded";
                    reason = $"This license was already {action} (License #{newer.LicenseID}).";
                }
                else
                {
                    isEligible = true;
                    reason = null;
                }
            }

            return LicenseMapper.ToDto(l, isExpired, isEligible, reason);
        }).ToList();

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = LicenseValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);

        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var licenses = await _licenseRepository.GetLicensesByApplicationIdAsync(applicationId);
        var dtos = new List<LicenseDto>();
        foreach (var l in licenses)
        {
            var (isExpired, isEligible, reason) = await EvaluateRenewalEligibilityAsync(l);
            dtos.Add(LicenseMapper.ToDto(l, isExpired, isEligible, reason));
        }

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(int licenseClassId)
    {
        var validation = LicenseValidator.ValidateLicenseClassId(licenseClassId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);

        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var licenses = await _licenseRepository.GetLicensesByLicenseClassIdAsync(licenseClassId);
        var dtos = new List<LicenseDto>();
        foreach (var l in licenses)
        {
            var (isExpired, isEligible, reason) = await EvaluateRenewalEligibilityAsync(l);
            dtos.Add(LicenseMapper.ToDto(l, isExpired, isEligible, reason));
        }

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return Result<List<LicenseDto>>.FromValidationFailure("Invalid person ID.");

        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var licenses = await _licenseRepository.GetLicensesByPersonIdAsync(personId);
        var dtos = new List<LicenseDto>();
        foreach (var l in licenses)
        {
            var (isExpired, isEligible, reason) = await EvaluateRenewalEligibilityAsync(l);
            dtos.Add(LicenseMapper.ToDto(l, isExpired, isEligible, reason));
        }

        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<bool>> IsLicenseExistsAsync(int licenseId) =>
        licenseId <= 0
            ? Result<bool>.FromValidationFailure("Invalid license ID.")
            : Result<bool>.Success(await _licenseRepository.IsLicenseExistsAsync(licenseId));

    public async Task<Result<bool>> IsDriverHasLicenseAsync(int driverId) =>
        driverId <= 0
            ? Result<bool>.FromValidationFailure("Invalid driver ID.")
            : Result<bool>.Success(await _licenseRepository.IsDriverHasLicenseAsync(driverId));

    public async Task<Result<bool>> IsApplicationHasLicenseAsync(int applicationId) =>
        applicationId <= 0
            ? Result<bool>.FromValidationFailure("Invalid application ID.")
            : Result<bool>.Success(await _licenseRepository.IsApplicationHasLicenseAsync(applicationId));

    public async Task<Result<DriverLicenseInfoDto>> GetDetailsAsync(int localAppId)
    {
        if (localAppId <= 0)
            return Result<DriverLicenseInfoDto>.FromValidationFailure("Invalid local application ID.");

        var localResult = await _localApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);
        if (localResult.IsFailure)
            return Result<DriverLicenseInfoDto>.FromResult(localResult);
        if (localResult.Value is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("Local driving license application not found.");

        var localApplication = localResult.Value;
        var applicationIdResult = await _localApplicationService.GetApplicationIdByLocalIdAsync(localAppId);
        if (applicationIdResult.IsFailure)
            return Result<DriverLicenseInfoDto>.FromResult(applicationIdResult);

        var applicationId = applicationIdResult.Value;
        if (applicationId <= 0)
            return Result<DriverLicenseInfoDto>.FromValidationFailure("Invalid application ID.");

        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var licenses = await _licenseRepository.GetLicensesByApplicationIdAsync(applicationId);
        var license = licenses.FirstOrDefault(x => x.LicenseClass == localApplication.LicenseClassID);
        if (license is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("License for the selected license class was not found.");

        return await GetLicenseDetailsResultAsync(license);
    }

    public async Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(int licenseId)
    {
        var validation = LicenseValidator.ValidateId(licenseId);
        if (validation.IsFailure)
            return Result<DriverLicenseInfoDto>.FromValidationFailure(validation.Error);

        await _licenseRepository.DeactivateExpiredLicensesAsync(_clock.Now.Date);
        var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);
        if (license is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("License not found.");

        return await GetLicenseDetailsResultAsync(license);
    }

    private async Task<(bool isExpired, bool isEligible, string? reason)> EvaluateRenewalEligibilityAsync(
        License license,
        bool? precomputedIsDetained = null)
    {
        var today = _clock.Now.Date;
        var isExpired = today > license.ExpirationDate.Date;

        if (!isExpired)
        {
            return (false, false, "License is not expired yet. Renewal is only permitted starting the calendar day after expiration.");
        }

        var isDetained = precomputedIsDetained ?? await _detainedLicenseRepository.IsLicenseDetainedAsync(license.LicenseID);
        if (isDetained)
        {
            return (true, false, "Cannot renew a currently detained license. The license must be released first.");
        }

        var newer = await _licenseRepository.GetNewerLicenseForDriverAndClassAsync(
            license.DriverID,
            license.LicenseClass,
            license.LicenseID);

        if (newer != null)
        {
            var action = newer.IssueReason == Domain.Enums.IssueReason.Renew ? "renewed" : "superseded";
            return (true, false, $"This license was already {action} (License #{newer.LicenseID}).");
        }

        return (true, true, null);
    }

    private async Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsResultAsync(Domain.Entities.License license)
    {
        if (license.DriverID <= 0)
            return Result<DriverLicenseInfoDto>.FromFailure("License has an invalid driver.");

        if (license.Driver is null)
            return Result<DriverLicenseInfoDto>.FromFailure("License driver information was not loaded.");

        if (license.Driver.Person is null)
            return Result<DriverLicenseInfoDto>.FromFailure("License person information was not loaded.");

        var person = PersonMapper.ToDto(license.Driver.Person);
        var isDetained = await _detainedLicenseRepository.IsLicenseDetainedAsync(license.LicenseID);
        var (isExpired, isEligible, reason) = await EvaluateRenewalEligibilityAsync(license, isDetained);

        return Result<DriverLicenseInfoDto>.Success(MapLicenseDetails(license, person, license.DriverID, isDetained, isExpired, isEligible, reason));
    }

    private static DriverLicenseInfoDto MapLicenseDetails(
        Domain.Entities.License license,
        Application.DTOs.PersonDTO.PersonDto person,
        int driverId,
        bool isDetained,
        bool isExpired,
        bool isEligible,
        string? reason) => new()
        {
            LicenseId = license.LicenseID,
            LicenseClass = license.LicenseClassInfo?.ClassName ?? "Unknown",
            IssueDate = license.IssueDate,
            ExpirationDate = license.ExpirationDate,
            IsActive = license.IsActive,
            IsDetained = isDetained,
            IsExpired = isExpired,
            IsEligibleForRenewal = isEligible,
            IneligibilityReason = reason,
            IssueReason = Enum.IsDefined(license.IssueReason) ? license.IssueReason.ToString() : "Unknown",
            Notes = license.Notes,
            LicenseClassFees = license.LicenseClassInfo?.ClassFees ?? 0,
            DriverId = driverId,
            PersonID = person.PersonId,
            FullName = person.FullName,
            NationalNo = person.NationalNo,
            DateOfBirth = person.DateOfBirth,
            Gender = person.Gender.ToString(),
            ImagePath = person.ImagePath
        };
}
