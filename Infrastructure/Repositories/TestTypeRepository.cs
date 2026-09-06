using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TestTypeRepository(DVLDDbContext context)
    : ITestTypeRepository
{
    private readonly DVLDDbContext _context =
        context ?? throw new ArgumentNullException(nameof(context));

    public Task<List<TestType>> GetAllAsync() =>
        _context.TestTypes
            .AsNoTracking()
            .OrderBy(t => t.TestTypeId)
            .ToListAsync();

    public Task<TestType?> GetByIdAsync(int id) =>
        id <= 0
            ? Task.FromResult<TestType?>(null)
            : _context.TestTypes
                .FirstOrDefaultAsync(t => t.TestTypeId == id);
}