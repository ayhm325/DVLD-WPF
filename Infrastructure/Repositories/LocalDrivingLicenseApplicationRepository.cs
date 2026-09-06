using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class LocalDrivingLicenseApplicationRepository
    : ILocalDrivingLicenseApplicationRepository
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

    public Task<List<LocalDrivingLicenseApplication>> GetAllAsync() =>
        Query().ToListAsync();

    public async Task<LocalDrivingLicenseApplication?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        return await _context.LocalDrivingLicenseApplications
            .Include(x => x.Application)
                .ThenInclude(x => x.Person)
            .Include(x => x.LicenseClass)
            .FirstOrDefaultAsync(x =>
                x.LocalDrivingLicenseApplicationID == id);
    }

    public Task<List<LocalDrivingLicenseApplication>> GetByPersonIdAsync(
        int personId)
    {
        if (personId <= 0)
            return Task.FromResult<List<LocalDrivingLicenseApplication>>([]);

        return Query()
            .Where(x => x.Application.ApplicantPersonID == personId)
            .ToListAsync();
    }

    public Task<List<LocalDrivingLicenseApplication>> GetByApplicationIdAsync(
        int applicationId)
    {
        if (applicationId <= 0)
            return Task.FromResult<List<LocalDrivingLicenseApplication>>([]);

        return Query()
            .Where(x => x.ApplicationID == applicationId)
            .ToListAsync();
    }

    public Task<List<LocalDrivingLicenseApplication>> GetByLicenseClassIdAsync(
        int licenseClassId)
    {
        if (licenseClassId <= 0)
            return Task.FromResult<List<LocalDrivingLicenseApplication>>([]);

        return Query()
            .Where(x => x.LicenseClassID == licenseClassId)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetPassedTestCountsAsync(
    IEnumerable<int> localApplicationIds)
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
                x => x.Count);
    }

    public async Task<int?> GetApplicationIdByLocalIdAsync(int localId)
    {
        if (localId <= 0)
            return null;

        return await _context.LocalDrivingLicenseApplications
            .Where(x => x.LocalDrivingLicenseApplicationID == localId)
            .Select(x => (int?)x.ApplicationID)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> HasDuplicateApplicationAsync(
        int applicantPersonId,
        int licenseClassId)
    {
        if (applicantPersonId <= 0 || licenseClassId <= 0)
            return null;

        return await _context.LocalDrivingLicenseApplications
            .Where(x =>
                x.LicenseClassID == licenseClassId &&
                x.Application.ApplicantPersonID == applicantPersonId &&
                (x.Application.ApplicationStatus == AppStatus.New ||
                 x.Application.ApplicationStatus == AppStatus.Completed))
            .Select(x => (int?)x.ApplicationID)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateLocalDrivingLicenseApplicationAsync(
        LocalDrivingLicenseApplication entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _context.LocalDrivingLicenseApplications.AddAsync(entity);

        return entity.LocalDrivingLicenseApplicationID;
    }

    public async Task<bool> UpdateAsync(
        LocalDrivingLicenseApplication entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.LocalDrivingLicenseApplicationID <= 0)
            return false;

        var existing = await _context.LocalDrivingLicenseApplications
            .FirstOrDefaultAsync(x =>
                x.LocalDrivingLicenseApplicationID ==
                entity.LocalDrivingLicenseApplicationID);

        if (existing is null)
            return false;

        _context.Entry(existing).CurrentValues.SetValues(entity);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
            return false;

        var existing = await _context.LocalDrivingLicenseApplications
            .FirstOrDefaultAsync(x =>
                x.LocalDrivingLicenseApplicationID == id);

        if (existing is null)
            return false;

        _context.LocalDrivingLicenseApplications.Remove(existing);

        return true;
    }
}