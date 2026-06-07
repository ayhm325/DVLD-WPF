using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Infrastructure.Repositories
{
    public class PersonRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public PersonRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public async Task<Person?> GetPersonByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.People
                .Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.PersonId == id);
        }

        public async Task<Person?> GetPersonByNationalNoAsync(string nationalNo)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.People
                .Include(p => p.Country)
                .FirstOrDefaultAsync(p => p.NationalNo == nationalNo);
        }

        public async Task<List<Person>> GetAllPersonsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.People
                .Include(p => p.Country)
                .ToListAsync();
        }

        // =========================
        // CHECK OPERATIONS
        // =========================
        public async Task<bool> IsPersonExistsByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.People.AnyAsync(p => p.PersonId == id);
        }

        public async Task<bool> IsNationalNoDuplicatedAsync(string nationalNo, int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.People.AnyAsync(p => p.NationalNo == nationalNo && p.PersonId != id);
        }

        //// =========================
        //// CREATE
        //// =========================
        public async Task<int> AddPersonAsync(Person person)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.People.AddAsync(person);
            await context.SaveChangesAsync();
            return person.PersonId;
        }

        //// =========================
        //// UPDATE
        //// =========================
        public async Task<bool> UpdatePersonAsync(Person person)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.People.FindAsync(person.PersonId);
            if (existing is null) return false;

            // تحديث الخصائص
            context.Entry(existing).CurrentValues.SetValues(person);

            return await context.SaveChangesAsync() > 0;
        }       

        //// =========================
        //// DELETE
        //// =========================
        public async Task<bool> DeletePersonAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var person = await context.People.FindAsync(id);
            if (person is null) return false;

            context.People.Remove(person);
            return await context.SaveChangesAsync() > 0;
        }
    }
}
