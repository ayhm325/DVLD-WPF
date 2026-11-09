using Application.Common.Pagination;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class LocalDrivingLicenseApplicationRepository : ILocalDrivingLicenseApplicationRepository
{
    private readonly DVLDDbContext _context;

    public LocalDrivingLicenseApplicationRepository(DVLDDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<LocalDrivingLicenseApplication> Query() =>
        _context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .Include(x => x.Application)
                .ThenInclude(x => x.Person)
            .Include(x => x.LicenseClass);

    public Task<PagedResult<LocalDrivingLicenseApplication>> GetAllAsync(
        PaginationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is LocalDrivingLicenseApplicationQueryRequest queryRequest)
        {
            return GetAllAsync(queryRequest, cancellationToken);
        }

        return GetAllAsync(new LocalDrivingLicenseApplicationQueryRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        }, cancellationToken);
    }

    public async Task<PagedResult<LocalDrivingLicenseApplication>> GetAllAsync(
        LocalDrivingLicenseApplicationQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = Query();

        if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
            !string.Equals(request.StatusFilter, "All", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<AppStatus>(request.StatusFilter, true, out var status))
        {
            query = query.Where(x => x.Application.ApplicationStatus == status);
        }

        if (request.LicenseClassId.HasValue && request.LicenseClassId > 0)
        {
            query = query.Where(x => x.LicenseClassID == request.LicenseClassId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var term = request.SearchQuery.Trim();
            var searchBy = request.SearchBy?.Trim().ToLowerInvariant() ?? "all";

            switch (searchBy)
            {
                case "nationalno":
                    query = query.Where(x => x.Application.Person.NationalNo.Contains(term));
                    break;

                case "name":
                case "fullname":
                    query = query.Where(x =>
                        x.Application.Person.FirstName.Contains(term) ||
                        x.Application.Person.SecondName.Contains(term) ||
                        (x.Application.Person.ThirdName != null && x.Application.Person.ThirdName.Contains(term)) ||
                        x.Application.Person.LastName.Contains(term) ||
                        (x.Application.Person.FirstName + " " + x.Application.Person.LastName).Contains(term) ||
                        (x.Application.Person.FirstName + " " + x.Application.Person.SecondName + " " + x.Application.Person.LastName).Contains(term));
                    break;

                case "appid":
                case "localdrivinglicenseapplicationid":
                    if (int.TryParse(term, out var appId))
                    {
                        query = query.Where(x => x.LocalDrivingLicenseApplicationID == appId);
                    }
                    else
                    {
                        query = query.Where(x => false);
                    }
                    break;

                case "class":
                    query = query.Where(x => x.LicenseClass.ClassName.Contains(term));
                    break;

                case "all":
                default:
                    var isNum = int.TryParse(term, out var id);
                    query = query.Where(x =>
                        x.Application.Person.NationalNo.Contains(term) ||
                        x.Application.Person.FirstName.Contains(term) ||
                        x.Application.Person.SecondName.Contains(term) ||
                        (x.Application.Person.ThirdName != null && x.Application.Person.ThirdName.Contains(term)) ||
                        x.Application.Person.LastName.Contains(term) ||
                        (x.Application.Person.FirstName + " " + x.Application.Person.LastName).Contains(term) ||
                        (x.Application.Person.FirstName + " " + x.Application.Person.SecondName + " " + x.Application.Person.LastName).Contains(term) ||
                        x.LicenseClass.ClassName.Contains(term) ||
                        (isNum && x.LocalDrivingLicenseApplicationID == id));
                    break;
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.LocalDrivingLicenseApplicationID)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LocalDrivingLicenseApplication>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public Task<LocalDrivingLicenseApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<LocalDrivingLicenseApplication?>(null)
            : Query()
                .FirstOrDefaultAsync(
                    x => x.LocalDrivingLicenseApplicationID == id, cancellationToken);

    public Task<LocalDrivingLicenseApplication?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<LocalDrivingLicenseApplication?>(null)
            : _context.LocalDrivingLicenseApplications
                .Include(x => x.Application)
                .FirstOrDefaultAsync(
                    x => x.LocalDrivingLicenseApplicationID == id, cancellationToken);

    public Task<List<LocalDrivingLicenseApplication>> GetByPersonIdAsync(
        int personId, CancellationToken cancellationToken = default)
    {
        if (personId <= 0)
            return Task.FromResult<List<LocalDrivingLicenseApplication>>([]);

        return Query()
            .Where(x => x.Application.ApplicantPersonID == personId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<LocalDrivingLicenseApplication>> GetByApplicationIdAsync(
        int applicationId, CancellationToken cancellationToken = default)
    {
        if (applicationId <= 0)
            return Task.FromResult<List<LocalDrivingLicenseApplication>>([]);

        return Query()
            .Where(x => x.ApplicationID == applicationId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<LocalDrivingLicenseApplication>> GetByLicenseClassIdAsync(
        int licenseClassId, CancellationToken cancellationToken = default)
    {
        if (licenseClassId <= 0)
            return Task.FromResult<List<LocalDrivingLicenseApplication>>([]);

        return Query()
            .Where(x => x.LicenseClassID == licenseClassId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetPassedTestCountsAsync(
        IEnumerable<int> localApplicationIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localApplicationIds);

        var ids = localApplicationIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        return await _context.Tests
            .AsNoTracking()
            .Where(x =>
                x.TestAppointment != null &&
                ids.Contains(
                    x.TestAppointment.LocalDrivingLicenseApplicationID) &&
                x.TestResult == true)
            .GroupBy(x =>
                x.TestAppointment!.LocalDrivingLicenseApplicationID)
            .Select(g => new
            {
                LocalApplicationId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(
                x => x.LocalApplicationId,
                x => x.Count,
                cancellationToken);
    }

    public async Task<int?> GetApplicationIdByLocalIdAsync(int localId, CancellationToken cancellationToken = default)
    {
        if (localId <= 0)
            return null;

        return await _context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .Where(x =>
                x.LocalDrivingLicenseApplicationID == localId)
            .Select(x => (int?)x.ApplicationID)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> HasDuplicateApplicationAsync(
        int applicantPersonId,
        int licenseClassId, CancellationToken cancellationToken = default)
    {
        if (applicantPersonId <= 0 || licenseClassId <= 0)
            return null;

        return await _context.LocalDrivingLicenseApplications
            .AsNoTracking()
            .Where(x =>
                x.LicenseClassID == licenseClassId &&
                x.Application.ApplicantPersonID == applicantPersonId &&
                (x.Application.ApplicationStatus == AppStatus.New ||
                 x.Application.ApplicationStatus == AppStatus.Completed))
            .Select(x => (int?)x.ApplicationID)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(LocalDrivingLicenseApplication entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return _context.LocalDrivingLicenseApplications
            .AddAsync(entity, cancellationToken)
            .AsTask();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return false;

        var existing =
            await _context.LocalDrivingLicenseApplications
                .FirstOrDefaultAsync(
                    x => x.LocalDrivingLicenseApplicationID == id, cancellationToken);

        if (existing is null)
            return false;

        _context.LocalDrivingLicenseApplications.Remove(existing);

        return true;
    }
}
