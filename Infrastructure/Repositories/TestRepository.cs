using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public sealed class TestRepository : ITestRepository
{
    private readonly DVLDDbContext _context;

    public TestRepository(DVLDDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<Test> Query() =>
        _context.Tests
            .AsNoTracking()
            .Include(t => t.TestAppointment)
                .ThenInclude(a => a.TestType)
            .Include(t => t.TestAppointment)
                .ThenInclude(a => a.LocalDrivingLicenseApplication)
            .Include(t => t.User);

    public Task<Test?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return Task.FromResult<Test?>(null);

        return Query().FirstOrDefaultAsync(t => t.TestID == id);
    }

    public Task<Test?> GetForUpdateAsync(int id)
    {
        if (id <= 0)
            return Task.FromResult<Test?>(null);

        return _context.Tests
            .FirstOrDefaultAsync(t => t.TestID == id);
    }

    public Task<List<Test>> GetAllAsync() =>
        Query().ToListAsync();

    public Task<List<Test>> GetByTestAppointmentIdAsync(int appointmentId)
    {
        if (appointmentId <= 0)
            return Task.FromResult<List<Test>>([]);

        return Query()
            .Where(t => t.TestAppointmentID == appointmentId)
            .ToListAsync();
    }

    public Task<List<Test>> GetByUserIdAsync(int userId)
    {
        if (userId <= 0)
            return Task.FromResult<List<Test>>([]);

        return Query()
            .Where(t => t.CreatedByUserID == userId)
            .ToListAsync();
    }

    public async Task<int> GetTrialCountByApplicationIdAsync(
        int localDrivingLicenseApplicationId)
    {
        if (localDrivingLicenseApplicationId <= 0)
            return 0;

        return await _context.Tests
            .AsNoTracking()
            .CountAsync(t =>
                t.TestAppointment.LocalDrivingLicenseApplicationID ==
                localDrivingLicenseApplicationId);
    }

    public Task<bool> IsTestExistsAsync(int id)
    {
        if (id <= 0)
            return Task.FromResult(false);

        return _context.Tests
            .AsNoTracking()
            .AnyAsync(t => t.TestID == id);
    }

    public Task<bool> IsTestAlreadyTakenAsync(int appointmentId)
    {
        if (appointmentId <= 0)
            return Task.FromResult(false);

        return _context.Tests
            .AsNoTracking()
            .AnyAsync(t => t.TestAppointmentID == appointmentId);
    }

    public async Task AddAsync(Test test)
    {
        ArgumentNullException.ThrowIfNull(test);
        await _context.Tests.AddAsync(test);
    }

    public void Delete(Test test)
    {
        ArgumentNullException.ThrowIfNull(test);
        _context.Tests.Remove(test);
    }

    public Task<int> CountAsync(
        Expression<Func<Test, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _context.Tests
            .AsNoTracking()
            .CountAsync(predicate);
    }
}
