using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TestTypeRepository(DVLDDbContext context)
    : ITestTypeRepository
{
    private readonly DVLDDbContext _context =
        context
        ?? throw new ArgumentNullException(nameof(context));

    public Task<List<TestType>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _context.TestTypes
            .AsNoTracking()
            .OrderBy(t => t.TestTypeId)
            .ToListAsync(cancellationToken);

    public Task<TestType?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<TestType?>(null)
            : _context.TestTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.TestTypeId == id,
                    cancellationToken);

    public Task<TestType?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0
            ? Task.FromResult<TestType?>(null)
            : _context.TestTypes
                .FirstOrDefaultAsync(
                    t => t.TestTypeId == id,
                    cancellationToken);
}