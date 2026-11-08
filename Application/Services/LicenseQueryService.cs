using Application.Common.Pagination;
using Application.Common.Results;
using Application.Common.Time;
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

        var today = _clock.Today;
        var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);
        if (license is null)
            return Result<LicenseDto>.FromNotFound("License not found.");

        var (isExpired, isEligible, reason) = await EvaluateRenewalEligibilityAsync(license, today);
        return Result<LicenseDto>.Success(LicenseMapper.ToDto(license, isExpired, isEligible, reason));
    }

    public Task<Result<List<LicenseDto>>> GetAllAsync() =>
        GetAllAsync((bool?)null);

    public async Task<Result<List<LicenseDto>>> GetAllAsync(bool? isExpired)
    {
        var today = _clock.Today;
        var licenses = isExpired.HasValue
            ? await _licenseRepository.GetAllLicensesAsync(isExpired.Value, today)
            : await _licenseRepository.GetAllLicensesAsync();
        var dtos = await MapToDtosBatchAsync(licenses ?? [], today, checkExternalNewerIfMissing: false);
        return Result<List<LicenseDto>>.Success(dtos);
    }

    public Task<Result<PagedResult<LicenseDto>>> GetAllAsync(PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is LicenseQueryRequest queryRequest)
        {
            return GetAllAsync(queryRequest);
        }

        return GetAllAsync(new LicenseQueryRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }

    public async Task<Result<PagedResult<LicenseDto>>> GetAllAsync(LicenseQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var today = _clock.Today;
        var pagedLicenses = await _licenseRepository.GetAllLicensesAsync(request, today);
        var dtos = await MapToDtosBatchAsync(pagedLicenses.Items, today, checkExternalNewerIfMissing: true);

        return Result<PagedResult<LicenseDto>>.Success(new PagedResult<LicenseDto>
        {
            Items = dtos,
            PageNumber = pagedLicenses.PageNumber,
            PageSize = pagedLicenses.PageSize,
            TotalCount = pagedLicenses.TotalCount
        });
    }

    public async Task<Result<RenewalSummaryDto>> GetRenewalSummaryAsync()
    {
        var today = _clock.Today;
        var summary = await _licenseRepository.GetRenewalSummaryAsync(today);
        return Result<RenewalSummaryDto>.Success(summary);
    }

    public async Task<Result<List<LicenseDto>>> GetByDriverIdAsync(int driverId)
    {
        var validation = LicenseValidator.ValidateDriverId(driverId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);

        var today = _clock.Today;
        var licenses = await _licenseRepository.GetLicensesByDriverIdAsync(driverId);
        var dtos = await MapToDtosBatchAsync(licenses, today, checkExternalNewerIfMissing: false);
        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByApplicationIdAsync(int applicationId)
    {
        var validation = LicenseValidator.ValidateApplicationId(applicationId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);

        var today = _clock.Today;
        var licenses = await _licenseRepository.GetLicensesByApplicationIdAsync(applicationId);
        var dtos = await MapToDtosBatchAsync(licenses, today, checkExternalNewerIfMissing: true);
        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetByLicenseClassIdAsync(int licenseClassId)
    {
        var validation = LicenseValidator.ValidateLicenseClassId(licenseClassId);
        if (validation.IsFailure)
            return Result<List<LicenseDto>>.FromValidationFailure(validation.Error);

        var today = _clock.Today;
        var licenses = await _licenseRepository.GetLicensesByLicenseClassIdAsync(licenseClassId);
        var dtos = await MapToDtosBatchAsync(licenses, today, checkExternalNewerIfMissing: false);
        return Result<List<LicenseDto>>.Success(dtos);
    }

    public async Task<Result<List<LicenseDto>>> GetLicensesByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return Result<List<LicenseDto>>.FromValidationFailure("Invalid person ID.");

        var today = _clock.Today;
        var licenses = await _licenseRepository.GetLicensesByPersonIdAsync(personId);
        var dtos = await MapToDtosBatchAsync(licenses, today, checkExternalNewerIfMissing: false);
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

        var today = _clock.Today;
        var licenses = await _licenseRepository.GetLicensesByApplicationIdAsync(applicationId);
        var license = licenses.FirstOrDefault(x => x.LicenseClass == localApplication.LicenseClassID);
        if (license is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("License for the selected license class was not found.");

        return await GetLicenseDetailsResultAsync(license, today);
    }

    public async Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsByIdAsync(int licenseId)
    {
        var validation = LicenseValidator.ValidateId(licenseId);
        if (validation.IsFailure)
            return Result<DriverLicenseInfoDto>.FromValidationFailure(validation.Error);

        var today = _clock.Today;
        var license = await _licenseRepository.GetLicenseByIdAsync(licenseId);
        if (license is null)
            return Result<DriverLicenseInfoDto>.FromNotFound("License not found.");

        return await GetLicenseDetailsResultAsync(license, today);
    }

    private async Task<List<LicenseDto>> MapToDtosBatchAsync(
        IReadOnlyList<License>? licenses,
        DateOnly today,
        bool checkExternalNewerIfMissing = false)
    {
        if (licenses == null || licenses.Count == 0)
            return [];

        var licenseIds = licenses
            .Select(l => l.LicenseID)
            .Where(id => id > 0)
            .Distinct();

        var activeDetainedIds = await _detainedLicenseRepository
            .GetActiveDetainedLicenseIdsAsync(licenseIds);

        Dictionary<int, License>? externalNewerMap = null;

        if (checkExternalNewerIfMissing)
        {
            var candidateLicenses = licenses
                .Where(l =>
                    LicenseExpirationPolicy.IsExpired(l.ExpirationDate, today) &&
                    !activeDetainedIds.Contains(l.LicenseID) &&
                    !licenses.Any(other =>
                        other.DriverID == l.DriverID &&
                        other.LicenseClass == l.LicenseClass &&
                        other.LicenseID > l.LicenseID))
                .ToList();

            if (candidateLicenses.Count > 0)
            {
                externalNewerMap = await _licenseRepository
                    .GetNewerLicensesBatchAsync(candidateLicenses);
            }
        }

        var dtos = new List<LicenseDto>(licenses.Count);

        foreach (var l in licenses)
        {
            var isExpired = LicenseExpirationPolicy.IsExpired(l.ExpirationDate, today);
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
                var newer = licenses
                    .Where(other =>
                        other.DriverID == l.DriverID &&
                        other.LicenseClass == l.LicenseClass &&
                        other.LicenseID > l.LicenseID)
                    .OrderByDescending(other => other.LicenseID)
                    .FirstOrDefault();

                if (newer is null && externalNewerMap is not null)
                {
                    externalNewerMap.TryGetValue(l.LicenseID, out newer);
                }

                if (newer is not null)
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

            dtos.Add(LicenseMapper.ToDto(l, isExpired, isEligible, reason));
        }

        return dtos;
    }

    private async Task<(bool isExpired, bool isEligible, string? reason)> EvaluateRenewalEligibilityAsync(
        License license,
        DateOnly today,
        bool? precomputedIsDetained = null)
    {
        var isExpired = LicenseExpirationPolicy.IsExpired(license.ExpirationDate, today);

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

    private async Task<Result<DriverLicenseInfoDto>> GetLicenseDetailsResultAsync(
        Domain.Entities.License license,
        DateOnly today)
    {
        if (license.DriverID <= 0)
            return Result<DriverLicenseInfoDto>.FromFailure("License has an invalid driver.");

        if (license.Driver is null)
            return Result<DriverLicenseInfoDto>.FromFailure("License driver information was not loaded.");

        if (license.Driver.Person is null)
            return Result<DriverLicenseInfoDto>.FromFailure("License person information was not loaded.");

        var person = PersonMapper.ToDto(license.Driver.Person);
        var isDetained = await _detainedLicenseRepository.IsLicenseDetainedAsync(license.LicenseID);
        var (isExpired, isEligible, reason) = await EvaluateRenewalEligibilityAsync(license, today, isDetained);

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
            IsActive = license.IsActive && !isExpired,
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
