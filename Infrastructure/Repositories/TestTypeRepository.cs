using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TestTypeRepository : ITestTypeRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public TestTypeRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<List<TestType>> GetAllTestTypeAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestTypes
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<TestType?> GetTestTypeByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.TestTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TestTypeId == id);
        }

        // =========================
        // UPDATE OPERATION
        // =========================

        public async Task<bool> UpdateTestTypeAsync(TestType testtype)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.TestTypes
                .FindAsync(testtype.TestTypeId);

            if (existing is null)
                return false;

            context.Entry(existing)
                .CurrentValues
                .SetValues(testtype);

            return await context.SaveChangesAsync() > 0;
        }
    }
}