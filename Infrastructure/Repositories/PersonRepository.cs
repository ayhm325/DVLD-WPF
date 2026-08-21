using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

    public PersonRepository(IDbContextFactory<DVLDDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    // BASE QUERY
    private static IQueryable<Person> Query(DVLDDbContext context)
    {
        return context.People.AsNoTracking().Include(p => p.Country);
    }

    // GET BY ID
    public async Task<Person?> GetPersonByIdAsync(int id)
    {
        if (id <= 0) return null;
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await Query(context).FirstOrDefaultAsync(p => p.PersonId == id);
    }

    // GET BY NATIONAL NUMBER
    public async Task<Person?> GetPersonByNationalNoAsync(string nationalNo)
    {
        if (string.IsNullOrWhiteSpace(nationalNo)) return null;
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await Query(context).FirstOrDefaultAsync(p => p.NationalNo == nationalNo);
    }

    // GET ALL
    public async Task<List<Person>> GetAllPersonsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await Query(context).ToListAsync();
    }

    // EXISTS
    public async Task<bool> IsPersonExistsByIdAsync(int id)
    {
        if (id <= 0) return false;
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.People.AsNoTracking().AnyAsync(p => p.PersonId == id);
    }

    // NATIONAL NO DUPLICATE
    public async Task<bool> IsNationalNoDuplicatedAsync(string nationalNo, int id)
    {
        if (string.IsNullOrWhiteSpace(nationalNo)) return false;
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.People.AsNoTracking().AnyAsync(p => p.NationalNo == nationalNo && p.PersonId != id);
    }

    // CREATE
    public async Task<int> AddPersonAsync(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.People.AddAsync(person);
        await context.SaveChangesAsync();
        return person.PersonId;
    }

    // UPDATE
    public async Task<bool> UpdatePersonAsync(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);
        if (person.PersonId <= 0) return false;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.People.FirstOrDefaultAsync(p => p.PersonId == person.PersonId);
        if (existing is null) return false;

        context.Entry(existing).CurrentValues.SetValues(person);
        return await context.SaveChangesAsync() > 0;
    }

    // DELETE
    public async Task<bool> DeletePersonAsync(int id)
    {
        if (id <= 0) return false;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var person = await context.People.FirstOrDefaultAsync(p => p.PersonId == id);
        if (person is null) return false;

        context.People.Remove(person);
        return await context.SaveChangesAsync() > 0;
    }
}