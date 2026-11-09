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

    public Task<Test?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<Test?>(null)
            : Query()
                .FirstOrDefaultAsync(t => t.TestID == id, cancellationToken);

    public Task<List<Test>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Query()
            .OrderByDescending(t => t.TestID)
            .ToListAsync(cancellationToken);

    public Task<List<Test>> GetByTestAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default) =>
        appointmentId <= 0
            ? Task.FromResult<List<Test>>([])
            : Query()
                .Where(t => t.TestAppointmentID == appointmentId)
                .ToListAsync(cancellationToken);

    public Task<List<Test>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        userId <= 0
            ? Task.FromResult<List<Test>>([])
            : Query()
                .Where(t => t.CreatedByUserID == userId)
                .OrderByDescending(t => t.TestID)
                .ToListAsync(cancellationToken);
   

    public Task<bool> IsTestAlreadyTakenAsync(
        int appointmentId,
        CancellationToken cancellationToken = default) =>
        appointmentId <= 0
            ? Task.FromResult(false)
            : _context.Tests
                .AsNoTracking()
                .AnyAsync(t =>
                    t.TestAppointmentID == appointmentId,
                    cancellationToken);

    public Task AddAsync(Test test, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(test);
        return _context.Tests.AddAsync(test, cancellationToken).AsTask();
    }
}