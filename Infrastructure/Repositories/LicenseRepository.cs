using System.Linq.Expressions;
using Application.Common.Pagination;
using Application.Common.Time;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class LicenseRepository(DVLDDbContext context)
    : ILicenseRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<License> Query() =>
        _context.Licenses
            .Include(l => l.Driver)
                .ThenInclude(d => d.Person)
            .Include(l => l.LicenseClassInfo)
            .Include(l => l.CreatedByUser);

    public Task<License?> GetLicenseByIdAsync(int id)
    {
        if (id <= 0)
            return Task.FromResult<License?>(null);

        return _context.Licenses
            .FromSqlInterpolated($"""
            SELECT *
            FROM Licenses WITH (UPDLOCK, HOLDLOCK)
            WHERE LicenseID = {id}
            """)
            .Include(l => l.Driver)
                .ThenInclude(d => d.Person)
            .Include(l => l.LicenseClassInfo)
            .Include(l => l.CreatedByUser)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }

    public Task<License?> GetByDriverIdAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult<License?>(null)
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.DriverID == driverId)
                .OrderByDescending(l => l.IssueDate)
                .FirstOrDefaultAsync();

    public Task<List<License>> GetAllLicensesAsync() =>
        GetAllLicensesAsync((bool?)null, null);

    public Task<List<License>> GetAllLicensesAsync(bool? isExpired, DateOnly? jordanToday = null)
    {
        var query = Query().AsNoTracking().AsSplitQuery();

        if (isExpired.HasValue && jordanToday.HasValue)
        {
            var cutoff = jordanToday.Value.ToDateTime(TimeOnly.MinValue);
            query = isExpired.Value
                ? query.Where(l => l.ExpirationDate < cutoff)
                : query.Where(l => l.ExpirationDate >= cutoff);
        }

        return query
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync();
    }

    public Task<PagedResult<License>> GetAllLicensesAsync(PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is LicenseQueryRequest queryRequest)
        {
            return GetAllLicensesAsync(queryRequest, null);
        }

        return GetAllLicensesAsync(new LicenseQueryRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        }, null);
    }

    public async Task<PagedResult<License>> GetAllLicensesAsync(
        LicenseQueryRequest request,
        DateOnly? jordanToday = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = Query().AsNoTracking().AsSplitQuery();

        if (request.IsExpired.HasValue && jordanToday.HasValue)
        {
            var cutoff = jordanToday.Value.ToDateTime(TimeOnly.MinValue);
            query = request.IsExpired.Value
                ? query.Where(l => l.ExpirationDate < cutoff)
                : query.Where(l => l.ExpirationDate >= cutoff);
        }

        if (!string.IsNullOrWhiteSpace(request.StatusFilter))
        {
            var status = request.StatusFilter.Trim().ToLowerInvariant();
            if (status == "active")
            {
                query = query.Where(l => l.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(l => !l.IsActive);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var term = request.SearchQuery.Trim();
            var searchBy = request.SearchBy?.Trim().ToLowerInvariant() ?? "all";

            switch (searchBy)
            {
                case "licenseid":
                case "id":
                    var cleanLicenseId = term.TrimStart('#');
                    if (int.TryParse(cleanLicenseId, out var licenseId))
                    {
                        query = query.Where(l => l.LicenseID == licenseId);
                    }
                    else
                    {
                        query = query.Where(l => false);
                    }
                    break;

                case "appid":
                case "applicationid":
                    var cleanAppId = term.TrimStart('#');
                    if (int.TryParse(cleanAppId, out var appId))
                    {
                        query = query.Where(l => l.ApplicationID == appId);
                    }
                    else
                    {
                        query = query.Where(l => false);
                    }
                    break;

                case "driver":
                case "drivername":
                case "name":
                    query = query.Where(l =>
                        l.Driver.Person.FirstName.Contains(term) ||
                        l.Driver.Person.SecondName.Contains(term) ||
                        (l.Driver.Person.ThirdName != null && l.Driver.Person.ThirdName.Contains(term)) ||
                        l.Driver.Person.LastName.Contains(term) ||
                        (l.Driver.Person.FirstName + " " + l.Driver.Person.LastName).Contains(term) ||
                        (l.Driver.Person.FirstName + " " + l.Driver.Person.SecondName + " " + l.Driver.Person.LastName).Contains(term));
                    break;

                case "class":
                case "classname":
                case "licenseclass":
                    query = query.Where(l => l.LicenseClassInfo.ClassName.Contains(term));
                    break;

                case "all":
                default:
                    var cleanTerm = term.TrimStart('#');
                    var isNum = int.TryParse(cleanTerm, out var parsedNum);
                    query = query.Where(l =>
                        (isNum && (l.LicenseID == parsedNum || l.ApplicationID == parsedNum)) ||
                        l.Driver.Person.FirstName.Contains(term) ||
                        l.Driver.Person.SecondName.Contains(term) ||
                        (l.Driver.Person.ThirdName != null && l.Driver.Person.ThirdName.Contains(term)) ||
                        l.Driver.Person.LastName.Contains(term) ||
                        (l.Driver.Person.FirstName + " " + l.Driver.Person.LastName).Contains(term) ||
                        (l.Driver.Person.FirstName + " " + l.Driver.Person.SecondName + " " + l.Driver.Person.LastName).Contains(term) ||
                        l.LicenseClassInfo.ClassName.Contains(term));
                    break;
            }
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.IssueDate)
            .ThenByDescending(l => l.LicenseID)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<License>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<List<License>> GetLicensesByDriverIdAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.DriverID == driverId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<List<License>> GetLicensesByApplicationIdAsync(int applicationId) =>
        applicationId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.ApplicationID == applicationId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<List<License>> GetLicensesByLicenseClassIdAsync(int licenseClassId) =>
        licenseClassId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.LicenseClass == licenseClassId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<List<License>> GetLicensesByPersonIdAsync(int personId) =>
        personId <= 0
            ? Task.FromResult<List<License>>([])
            : Query()
                .AsNoTracking()
                .AsSplitQuery()
                .Where(l => l.Driver.PersonID == personId)
                .OrderByDescending(l => l.IssueDate)
                .ToListAsync();

    public Task<bool> IsLicenseExistsAsync(int id) =>
        id <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l => l.LicenseID == id);

    public Task<bool> IsDriverHasLicenseAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l => l.DriverID == driverId);

    public Task<bool> IsApplicationHasLicenseAsync(int applicationId) =>
        applicationId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l => l.ApplicationID == applicationId);

    public Task<bool> IsActiveLicenseExistsAsync(
        int driverId,
        int licenseClassId) =>
        driverId <= 0 || licenseClassId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l =>
                    l.DriverID == driverId &&
                    l.LicenseClass == licenseClassId &&
                    l.IsActive);

    public async Task<HashSet<int>> GetApplicationIdsWithLicensesAsync(
        IEnumerable<int> applicationIds)
    {
        ArgumentNullException.ThrowIfNull(applicationIds);

        var ids = applicationIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        var result = await _context.Licenses
            .AsNoTracking()
            .Where(l => ids.Contains(l.ApplicationID))
            .Select(l => l.ApplicationID)
            .Distinct()
            .ToListAsync();

        return result.ToHashSet();
    }

    public Task AddLicenseAsync(License license)
    {
        ArgumentNullException.ThrowIfNull(license);

        return _context.Licenses
            .AddAsync(license)
            .AsTask();
    }

    public async Task<bool> DeactivateLicenseAsync(int licenseId)
    {
        if (licenseId <= 0)
            return false;

        var affectedRows = await _context.Licenses
            .Where(l =>
                l.LicenseID == licenseId &&
                l.IsActive)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    l => l.IsActive,
                    false));

        if (affectedRows <= 0)
            return false;

        SyncTrackedLicenseActiveState(licenseId, false);
        return true;
    }

    public Task<bool> HasAnotherActiveLicenseAsync(
        int driverId,
        int licenseClassId,
        int excludedLicenseId) =>
        driverId <= 0 ||
        licenseClassId <= 0 ||
        excludedLicenseId <= 0
            ? Task.FromResult(false)
            : _context.Licenses
                .AsNoTracking()
                .AnyAsync(l =>
                    l.DriverID == driverId &&
                    l.LicenseClass == licenseClassId &&
                    l.LicenseID != excludedLicenseId &&
                    l.IsActive);

    public async Task<bool> ActivateLicenseAsync(int licenseId, DateOnly jordanToday)
    {
        if (licenseId <= 0)
            return false;

        var jordanTodayStart = jordanToday.ToDateTime(TimeOnly.MinValue);

        var affectedRows = await _context.Licenses
            .Where(l =>
                l.LicenseID == licenseId &&
                !l.IsActive &&
                l.ExpirationDate >= jordanTodayStart)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(
                    l => l.IsActive,
                    true));

        if (affectedRows <= 0)
            return false;

        SyncTrackedLicenseActiveState(licenseId, true);
        return true;
    }

    public async Task<int> DeactivateExpiredLicensesAsync(DateOnly jordanToday)
    {
        var cutoff = jordanToday.ToDateTime(TimeOnly.MinValue);
        var affected = await _context.Licenses
            .Where(l => l.IsActive && l.ExpirationDate < cutoff)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(l => l.IsActive, false));

        if (affected > 0)
        {
            foreach (var entry in _context.ChangeTracker.Entries<License>())
            {
                if (LicenseExpirationPolicy.IsExpired(entry.Entity.ExpirationDate, jordanToday))
                {
                    entry.Entity.IsActive = false;
                    entry.Property(l => l.IsActive).OriginalValue = false;
                    entry.Property(l => l.IsActive).IsModified = false;
                }
            }
        }

        return affected;
    }

    public Task<License?> GetNewerLicenseForDriverAndClassAsync(
        int driverId,
        int licenseClassId,
        int licenseId) =>
        driverId <= 0 || licenseClassId <= 0 || licenseId <= 0
            ? Task.FromResult<License?>(null)
            : _context.Licenses
                .AsNoTracking()
                .Where(l => l.DriverID == driverId &&
                            l.LicenseClass == licenseClassId &&
                            l.LicenseID > licenseId)
                .OrderByDescending(l => l.LicenseID)
                .FirstOrDefaultAsync();

    public async Task<Dictionary<int, License>> GetNewerLicensesBatchAsync(
        IEnumerable<License> licenses,
        CancellationToken cancellationToken = default)
    {
        if (licenses is null)
            return [];

        var candidateList = licenses
            .Where(l => l is not null && l.DriverID > 0 && l.LicenseClass > 0 && l.LicenseID > 0)
            .ToList();

        if (candidateList.Count == 0)
            return [];

        var candidatePairs = candidateList
            .GroupBy(c => (c.DriverID, c.LicenseClass))
            .Select(g => (
                DriverId: g.Key.DriverID,
                LicenseClassId: g.Key.LicenseClass,
                MinLicenseId: g.Min(c => c.LicenseID)))
            .ToList();

        var predicate = BuildCandidatePairsPredicate(candidatePairs);

        var potentialNewer = await _context.Licenses
            .AsNoTracking()
            .Where(predicate)
            .Select(l => new
            {
                l.LicenseID,
                l.DriverID,
                l.LicenseClass,
                l.IssueReason
            })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<int, License>();

        foreach (var candidate in candidateList)
        {
            var newer = potentialNewer
                .Where(p => p.DriverID == candidate.DriverID &&
                            p.LicenseClass == candidate.LicenseClass &&
                            p.LicenseID > candidate.LicenseID)
                .OrderByDescending(p => p.LicenseID)
                .FirstOrDefault();

            if (newer is not null)
            {
                result[candidate.LicenseID] = new License
                {
                    LicenseID = newer.LicenseID,
                    DriverID = newer.DriverID,
                    LicenseClass = newer.LicenseClass,
                    IssueReason = newer.IssueReason
                };
            }
        }

        return result;
    }

    private static Expression<Func<License, bool>> BuildCandidatePairsPredicate(
        IReadOnlyList<(int DriverId, int LicenseClassId, int MinLicenseId)> pairs)
    {
        var parameter = Expression.Parameter(typeof(License), "l");
        Expression? combined = null;

        var driverIdProp = Expression.Property(parameter, nameof(License.DriverID));
        var classIdProp = Expression.Property(parameter, nameof(License.LicenseClass));
        var licenseIdProp = Expression.Property(parameter, nameof(License.LicenseID));

        foreach (var pair in pairs)
        {
            var driverEqual = Expression.Equal(
                driverIdProp,
                Expression.Constant(pair.DriverId, typeof(int)));

            var classEqual = Expression.Equal(
                classIdProp,
                Expression.Constant(pair.LicenseClassId, typeof(int)));

            var idGreater = Expression.GreaterThan(
                licenseIdProp,
                Expression.Constant(pair.MinLicenseId, typeof(int)));

            var pairCondition = Expression.AndAlso(
                Expression.AndAlso(driverEqual, classEqual),
                idGreater);

            combined = combined == null
                ? pairCondition
                : Expression.OrElse(combined, pairCondition);
        }

        return Expression.Lambda<Func<License, bool>>(
            combined ?? Expression.Constant(false),
            parameter);
    }

    public async Task<RenewalSummaryDto> GetRenewalSummaryAsync(
        DateOnly jordanToday,
        CancellationToken cancellationToken = default)
    {
        var cutoff = jordanToday.ToDateTime(TimeOnly.MinValue);

        var expiredQuery = _context.Licenses
            .AsNoTracking()
            .Where(l => l.ExpirationDate < cutoff);

        var totalExpired = await expiredQuery.CountAsync(cancellationToken);

        if (totalExpired == 0)
        {
            return new RenewalSummaryDto
            {
                TotalExpired = 0,
                EligibleForRenewal = 0,
                IneligibleForRenewal = 0
            };
        }

        var eligibleCount = await expiredQuery
            .Where(l => !_context.Licenses.Any(newer =>
                newer.DriverID == l.DriverID &&
                newer.LicenseClass == l.LicenseClass &&
                newer.LicenseID > l.LicenseID))
            .Where(l => !_context.Licenses.Any(other =>
                other.DriverID == l.DriverID &&
                other.LicenseClass == l.LicenseClass &&
                other.LicenseID != l.LicenseID &&
                other.IsActive))
            .Where(l => !_context.DetainedLicenses.Any(d =>
                d.LicenseID == l.LicenseID &&
                !d.IsReleased))
            .CountAsync(cancellationToken);

        return new RenewalSummaryDto
        {
            TotalExpired = totalExpired,
            EligibleForRenewal = eligibleCount,
            IneligibleForRenewal = totalExpired - eligibleCount
        };
    }

    private void SyncTrackedLicenseActiveState(
        int licenseId,
        bool isActive)
    {
        var trackedEntry =
            _context.ChangeTracker
                .Entries<License>()
                .FirstOrDefault(e => e.Entity.LicenseID == licenseId);

        if (trackedEntry is not null)
        {
            trackedEntry.Entity.IsActive = isActive;
            trackedEntry.Property(l => l.IsActive).OriginalValue = isActive;
            trackedEntry.Property(l => l.IsActive).IsModified = false;
        }
    }
}
