using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PersonRepository : IPersonRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public PersonRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // BASE QUERY
        // =========================
        private IQueryable<Person> Query(DVLDDbContext context)
        {
            return context.People
                .AsNoTracking()
                .Include(p => p.Country);
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public async Task<Person?> GetPersonByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(p => p.PersonId == id);
        }

        public async Task<Person?> GetPersonByNationalNoAsync(string nationalNo)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(p => p.NationalNo == nationalNo);
        }

        public async Task<List<Person>> GetAllPersonsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        // =========================
        // CHECK OPERATIONS
        // =========================
        public async Task<bool> IsPersonExistsByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.People
                .AsNoTracking()
                .AnyAsync(p => p.PersonId == id);
        }

        public async Task<bool> IsNationalNoDuplicatedAsync(string nationalNo, int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.People
                .AsNoTracking()
                .AnyAsync(p =>
                    p.NationalNo == nationalNo &&
                    p.PersonId != id);
        }

        // =========================
        // CREATE
        // =========================
        public async Task<int> AddPersonAsync(Person person)
        {
            using var  context = await _contextFactory.CreateDbContextAsync();

            await context.People.AddAsync(person);

            await context.SaveChangesAsync();

            return person.PersonId;
        }

        // =========================
        // UPDATE
        // =========================
        public async Task<bool> UpdatePersonAsync(Person person)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.People
                .FirstOrDefaultAsync(p => p.PersonId == person.PersonId);

            if (existing is null)
                return false;

            context.Entry(existing)
                   .CurrentValues
                   .SetValues(person);

            return await context.SaveChangesAsync() > 0;
        }

        // =========================
        // DELETE
        // =========================
        public async Task<bool> DeletePersonAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var person = await context.People.FindAsync(id);

            if (person is null)
                return false;

            context.People.Remove(person);

            return await context.SaveChangesAsync() > 0;
        }
    }
}