using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories;

public class TestRepository
    : ITestRepository
{
    private readonly DVLDDbContext _context;

    public TestRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    // =========================================================
    // BASE QUERY
    // =========================================================

    private IQueryable<Test> Query()
    {
        return _context.Tests
            .AsNoTracking()

            .Include(t =>
                t.TestAppointment)
                .ThenInclude(a =>
                    a.TestType)

            .Include(t =>
                t.TestAppointment)
                .ThenInclude(a =>
                    a.LocalDrivingLicenseApplication)

            .Include(t =>
                t.User);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Test?>
        GetByIdAsync(
            int id)
    {
        if (id <= 0)
            return null;

        return await Query()
            .FirstOrDefaultAsync(
                t =>
                    t.TestID == id);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<Test>>
        GetAllAsync()
    {
        return await Query()
            .ToListAsync();
    }

    // =========================================================
    // GET BY TEST APPOINTMENT ID
    // =========================================================

    public async Task<List<Test>>
        GetByTestAppointmentIdAsync(
            int appointmentId)
    {
        if (appointmentId <= 0)
            return [];

        return await Query()
            .Where(
                t =>
                    t.TestAppointmentID ==
                    appointmentId)
            .ToListAsync();
    }

    // =========================================================
    // GET BY USER ID
    // =========================================================

    public async Task<List<Test>>
        GetByUserIdAsync(
            int userId)
    {
        if (userId <= 0)
            return [];

        return await Query()
            .Where(
                t =>
                    t.CreatedByUserID ==
                    userId)
            .ToListAsync();
    }

    // =========================================================
    // GET TRIAL COUNT BY APPLICATION ID
    // =========================================================

    public async Task<int>
        GetTrialCountByApplicationIdAsync(
            int localDrivingLicenseApplicationId)
    {
        if (localDrivingLicenseApplicationId <= 0)
            return 0;

        return await _context.Tests
            .AsNoTracking()
            .CountAsync(
                t =>
                    t.TestAppointment
                        .LocalDrivingLicenseApplicationID ==
                    localDrivingLicenseApplicationId);
    }

    // =========================================================
    // CHECK TEST EXISTS
    // =========================================================

    public async Task<bool>
        IsTestExistsAsync(
            int id)
    {
        if (id <= 0)
            return false;

        return await _context.Tests
            .AsNoTracking()
            .AnyAsync(
                t =>
                    t.TestID == id);
    }

    // =========================================================
    // CHECK TEST ALREADY TAKEN
    // =========================================================

    public async Task<bool> IsTestAlreadyTakenAsync(int appointmentId)
    {
        if (appointmentId <= 0)
            return false;

        return await _context.Tests
            .AsNoTracking()
            .AnyAsync(
                t =>
                    t.TestAppointmentID ==
                    appointmentId);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task AddAsync(Test test)
    {
        ArgumentNullException.ThrowIfNull(test);

        await _context.Tests.AddAsync(test);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateAsync(
            Test test)
    {
        ArgumentNullException.ThrowIfNull(
            test);

        if (test.TestID <= 0)
            return false;

        var existing =
            await _context.Tests
                .FirstOrDefaultAsync(
                    t =>
                        t.TestID ==
                        test.TestID);

        if (existing is null)
            return false;

        _context.Entry(existing)
            .CurrentValues
            .SetValues(test);

        // -----------------------------------------------------
        // No SaveChangesAsync().
        // UnitOfWork owns persistence.
        // -----------------------------------------------------

        return true;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool>
        DeleteAsync(
            int id)
    {
        if (id <= 0)
            return false;

        var entity =
            await _context.Tests
                .FirstOrDefaultAsync(
                    t =>
                        t.TestID == id);

        if (entity is null)
            return false;

        _context.Tests
            .Remove(entity);

        // -----------------------------------------------------
        // No SaveChangesAsync().
        // UnitOfWork owns persistence.
        // -----------------------------------------------------

        return true;
    }

    // =========================================================
    // COUNT
    // =========================================================

    public async Task<int>
        CountAsync(
            Expression<Func<Test, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(
            predicate);

        return await _context.Tests
            .AsNoTracking()
            .CountAsync(predicate);
    }
}