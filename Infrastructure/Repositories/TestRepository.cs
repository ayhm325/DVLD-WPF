using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TestRepository(DVLDDbContext context)
    : ITestRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    private IQueryable<Test> Query() =>
        _context.Tests
            .AsNoTracking()
            .Include(t => t.TestAppointment)
                .ThenInclude(a => a.TestType)
            .Include(t => t.User);

    public Task<Test?> GetByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<Test?>(null)
            : Query()
                .FirstOrDefaultAsync(t => t.TestID == id);

    public Task<List<Test>> GetAllAsync() =>
        Query()
            .OrderByDescending(t => t.TestID)
            .ToListAsync();

    public Task<List<Test>> GetByTestAppointmentIdAsync(
        int appointmentId) =>
        appointmentId <= 0
            ? Task.FromResult<List<Test>>([])
            : Query()
                .Where(t => t.TestAppointmentID == appointmentId)
                .ToListAsync();

    public Task<List<Test>> GetByUserIdAsync(int userId) =>
        userId <= 0
            ? Task.FromResult<List<Test>>([])
            : Query()
                .Where(t => t.CreatedByUserID == userId)
                .OrderByDescending(t => t.TestID)
                .ToListAsync();

    public Task<int> GetTrialCountByApplicationIdAsync(
        int localDrivingLicenseApplicationId) =>
        localDrivingLicenseApplicationId <= 0
            ? Task.FromResult(0)
            : _context.Tests
                .AsNoTracking()
                .CountAsync(t =>
                    t.TestAppointment
                        .LocalDrivingLicenseApplicationID ==
                    localDrivingLicenseApplicationId);

    public Task<bool> IsTestAlreadyTakenAsync(
        int appointmentId) =>
        appointmentId <= 0
            ? Task.FromResult(false)
            : _context.Tests
                .AsNoTracking()
                .AnyAsync(t =>
                    t.TestAppointmentID == appointmentId);

    public Task AddAsync(Test test)
    {
        ArgumentNullException.ThrowIfNull(test);
        return _context.Tests.AddAsync(test).AsTask();
    }
}
