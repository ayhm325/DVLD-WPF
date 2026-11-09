using Application.Common.Pagination;
using Application.DTOs.DriverDTO;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class DriverRepository(DVLDDbContext context)
    : IDriverRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<Driver> QueryWithBasicInfo() =>
        _context.Drivers
            .Include(d => d.Person)
            .Include(d => d.CreatedByUser);

    private IQueryable<Driver> QueryForDelete() =>
        _context.Drivers
            .Include(d => d.Licenses)
            .Include(d => d.InternationalLicenses);

    public async Task<Driver?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return null;

        var result = await QueryWithBasicInfo()
            .AsNoTracking()
            .Where(d => d.DriverID == id)
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return null;

        result.Driver.ActiveLicensesCount = result.ActiveCount;
        return result.Driver;
    }

    public Task<Driver?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : _context.Drivers
                .FirstOrDefaultAsync(d => d.DriverID == id, cancellationToken);

    public Task<Driver?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : QueryForDelete()
                .FirstOrDefaultAsync(
                    d => d.DriverID == id, cancellationToken);

    public async Task<List<Driver>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await QueryWithBasicInfo()
            .AsNoTracking()
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .OrderBy(x => x.Driver.DriverID)
            .ToListAsync(cancellationToken);

        foreach (var item in results)
        {
            item.Driver.ActiveLicensesCount = item.ActiveCount;
        }

        return results.Select(x => x.Driver).ToList();
    }

    public Task<PagedResult<Driver>> GetAllAsync(PaginationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is DriverQueryRequest queryRequest)
        {
            return GetAllAsync(queryRequest, cancellationToken);
        }

        return GetAllAsync(new DriverQueryRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        }, cancellationToken);
    }

    public async Task<PagedResult<Driver>> GetAllAsync(DriverQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = QueryWithBasicInfo().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.LicenseFilter))
        {
            var licenseFilter = request.LicenseFilter.Trim().ToLowerInvariant();
            if (licenseFilter == "active")
            {
                query = query.Where(d => d.Licenses.Any(l => l.IsActive));
            }
            else if (licenseFilter == "none")
            {
                query = query.Where(d => !d.Licenses.Any(l => l.IsActive));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var term = request.SearchQuery.Trim();
            var searchBy = request.SearchBy?.Trim().ToLowerInvariant() ?? "all";

            switch (searchBy)
            {
                case "nationalno":
                    query = query.Where(d => d.Person.NationalNo.Contains(term));
                    break;

                case "name":
                case "fullname":
                    query = query.Where(d =>
                        d.Person.FirstName.Contains(term) ||
                        d.Person.SecondName.Contains(term) ||
                        (d.Person.ThirdName != null && d.Person.ThirdName.Contains(term)) ||
                        d.Person.LastName.Contains(term) ||
                        (d.Person.FirstName + " " + d.Person.LastName).Contains(term) ||
                        (d.Person.FirstName + " " + d.Person.SecondName + " " + d.Person.LastName).Contains(term));
                    break;

                case "driverid":
                case "id":
                    var cleanDriverId = term.TrimStart('#');
                    if (int.TryParse(cleanDriverId, out var driverId))
                    {
                        query = query.Where(d => d.DriverID == driverId);
                    }
                    else
                    {
                        query = query.Where(d => false);
                    }
                    break;

                case "personid":
                    var cleanPersonId = term.TrimStart('#');
                    if (int.TryParse(cleanPersonId, out var personId))
                    {
                        query = query.Where(d => d.PersonID == personId);
                    }
                    else
                    {
                        query = query.Where(d => false);
                    }
                    break;

                case "all":
                default:
                    var cleanTerm = term.TrimStart('#');
                    var isNum = int.TryParse(cleanTerm, out var parsedNum);
                    query = query.Where(d =>
                        d.Person.NationalNo.Contains(term) ||
                        d.Person.FirstName.Contains(term) ||
                        d.Person.SecondName.Contains(term) ||
                        (d.Person.ThirdName != null && d.Person.ThirdName.Contains(term)) ||
                        d.Person.LastName.Contains(term) ||
                        (d.Person.FirstName + " " + d.Person.LastName).Contains(term) ||
                        (d.Person.FirstName + " " + d.Person.SecondName + " " + d.Person.LastName).Contains(term) ||
                        (isNum && (d.DriverID == parsedNum || d.PersonID == parsedNum)));
                    break;
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResults = await query
            .OrderBy(d => d.DriverID)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .ToListAsync(cancellationToken);

        foreach (var item in pagedResults)
        {
            item.Driver.ActiveLicensesCount = item.ActiveCount;
        }

        return new PagedResult<Driver>
        {
            Items = pagedResults.Select(x => x.Driver).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Driver?> GetByPersonIdAsync(int personId, CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
            return null;

        var result = await QueryWithBasicInfo()
            .AsNoTracking()
            .Where(d => d.PersonID == personId)
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return null;

        result.Driver.ActiveLicensesCount = result.ActiveCount;
        return result.Driver;
    }

    public async Task<List<Driver>> GetByCreatedUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return [];

        var results = await QueryWithBasicInfo()
            .AsNoTracking()
            .Where(d => d.CreatedByUserID == userId)
            .Select(d => new
            {
                Driver = d,
                ActiveCount = d.Licenses.Count(l => l.IsActive)
            })
            .OrderBy(x => x.Driver.DriverID)
            .ToListAsync(cancellationToken);

        foreach (var item in results)
        {
            item.Driver.ActiveLicensesCount = item.ActiveCount;
        }

        return results.Select(x => x.Driver).ToList();
    }

    public Task<bool> ExistsByIdAsync(int driverId, CancellationToken cancellationToken = default) =>
        driverId <= 0
            ? Task.FromResult(false)
            : _context.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.DriverID == driverId, cancellationToken);

    public Task<bool> ExistsByPersonIdAsync(
        int personId, CancellationToken cancellationToken = default) =>
        personId <= 0
            ? Task.FromResult(false)
            : _context.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.PersonID == personId, cancellationToken);

    public async Task AddAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(driver);

        await _context.Drivers.AddAsync(driver, cancellationToken);
    }

    public void Delete(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        _context.Drivers.Remove(driver);
    }
}
