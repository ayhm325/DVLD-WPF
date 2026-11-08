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
            .Include(d => d.CreatedByUser)
            .Include(d => d.Licenses
                .Where(l => l.IsActive));

    private IQueryable<Driver> QueryForDelete() =>
        _context.Drivers
            .Include(d => d.Licenses)
            .Include(d => d.InternationalLicenses);

    public async Task<Driver?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        var driver = await QueryWithBasicInfo()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DriverID == id);

        if (driver is not null)
        {
            driver.ActiveLicensesCount = driver.Licenses.Count(l => l.IsActive);
        }

        return driver;
    }

    public Task<Driver?> GetForUpdateAsync(int id) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : QueryWithBasicInfo()
                .FirstOrDefaultAsync(d => d.DriverID == id);

    public Task<Driver?> GetForDeleteAsync(int id) =>
        id <= 0
            ? Task.FromResult<Driver?>(null)
            : QueryForDelete()
                .FirstOrDefaultAsync(
                    d => d.DriverID == id);

    public async Task<List<Driver>> GetAllAsync()
    {
        var drivers = await QueryWithBasicInfo()
            .AsNoTracking()
            .AsSplitQuery()
            .OrderBy(d => d.DriverID)
            .ToListAsync();

        foreach (var driver in drivers)
        {
            driver.ActiveLicensesCount = driver.Licenses.Count(l => l.IsActive);
        }

        return drivers;
    }

    public Task<PagedResult<Driver>> GetAllAsync(PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is DriverQueryRequest queryRequest)
        {
            return GetAllAsync(queryRequest);
        }

        return GetAllAsync(new DriverQueryRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }

    public async Task<PagedResult<Driver>> GetAllAsync(DriverQueryRequest request)
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

        var totalCount = await query.CountAsync();

        var pagedResults = await query
            .OrderBy(d => d.DriverID)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .AsSplitQuery()
            .ToListAsync();

        foreach (var item in pagedResults)
        {
            item.ActiveLicensesCount = item.Licenses.Count(l => l.IsActive);
        }

        return new PagedResult<Driver>
        {
            Items = pagedResults,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<Driver?> GetByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return null;

        var driver = await QueryWithBasicInfo()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.PersonID == personId);

        if (driver is not null)
        {
            driver.ActiveLicensesCount = driver.Licenses.Count(l => l.IsActive);
        }

        return driver;
    }

    public async Task<List<Driver>> GetByCreatedUserIdAsync(int userId)
    {
        if (userId <= 0)
            return [];

        var drivers = await QueryWithBasicInfo()
            .AsNoTracking()
            .AsSplitQuery()
            .Where(d => d.CreatedByUserID == userId)
            .OrderBy(d => d.DriverID)
            .ToListAsync();

        foreach (var driver in drivers)
        {
            driver.ActiveLicensesCount = driver.Licenses.Count(l => l.IsActive);
        }

        return drivers;
    }

    public Task<bool> ExistsByIdAsync(int driverId) =>
        driverId <= 0
            ? Task.FromResult(false)
            : _context.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.DriverID == driverId);

    public Task<bool> ExistsByPersonIdAsync(
        int personId) =>
        personId <= 0
            ? Task.FromResult(false)
            : _context.Drivers
                .AsNoTracking()
                .AnyAsync(
                    d => d.PersonID == personId);

    public async Task AddAsync(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        await _context.Drivers.AddAsync(driver);
    }

    public void Delete(Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        _context.Drivers.Remove(driver);
    }
}
