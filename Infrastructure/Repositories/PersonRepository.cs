using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PersonRepository : IPersonRepository
{
    private readonly DVLDDbContext _context;

    public PersonRepository(DVLDDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<Person> Query()
    {
        return _context.People
            .AsNoTracking()
            .Include(p => p.Country);
    }

    public Task<Person?> GetPersonByIdAsync(int id)
    {
        return Query()
            .FirstOrDefaultAsync(
                p => p.PersonId == id);
    }

    public Task<Person?> GetPersonByNationalNoAsync(
        string nationalNo)
    {
        return Query()
            .FirstOrDefaultAsync(
                p => p.NationalNo == nationalNo);
    }

    public Task<List<Person>> GetAllPersonsAsync()
    {
        return Query()
            .OrderBy(p => p.PersonId)
            .ToListAsync();
    }

    public Task<Person?> GetPersonForUpdateAsync(int id)
    {
        return _context.People
            .FirstOrDefaultAsync(
                p => p.PersonId == id);
    }

    public Task<bool> IsPersonExistsByIdAsync(int id)
    {
        return _context.People
            .AsNoTracking()
            .AnyAsync(
                p => p.PersonId == id);
    }

    public Task<bool> IsNationalNoDuplicatedAsync(
        string nationalNo,
        int personId)
    {
        return _context.People
            .AsNoTracking()
            .AnyAsync(
                p =>
                    p.NationalNo == nationalNo &&
                    p.PersonId != personId);
    }

    public Task<bool> HasApplicationsAsync(int personId)
    {
        return _context.Applications
            .AsNoTracking()
            .AnyAsync(
                a =>
                    a.ApplicantPersonID == personId);
    }

    public async Task AddPersonAsync(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        await _context.People.AddAsync(person);
    }

    public async Task<bool> DeletePersonAsync(int id)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(
                p => p.PersonId == id);

        if (person is null)
            return false;

        _context.People.Remove(person);

        return true;
    }
}