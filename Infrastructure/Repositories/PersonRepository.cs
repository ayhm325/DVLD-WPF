using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly DVLDDbContext _context;

    public PersonRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }

    // =========================================================
    // BASE QUERY - READ ONLY
    // =========================================================

    private IQueryable<Person> Query()
    {
        return _context.People
            .AsNoTracking()
            .Include(p => p.Country);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<Person?> GetPersonByIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        return await Query()
            .FirstOrDefaultAsync(
                p => p.PersonId == id);
    }

    // =========================================================
    // GET BY NATIONAL NUMBER
    // =========================================================

    public async Task<Person?> GetPersonByNationalNoAsync(
        string nationalNo)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
            return null;

        var normalizedNationalNo =
            nationalNo.Trim();

        return await Query()
            .FirstOrDefaultAsync(
                p =>
                    p.NationalNo ==
                    normalizedNationalNo);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<Person>> GetAllPersonsAsync()
    {
        return await Query()
            .OrderBy(p => p.PersonId)
            .ToListAsync();
    }

    // =========================================================
    // GET FOR UPDATE
    //
    // IMPORTANT:
    // This query MUST be tracked.
    //
    // The returned entity belongs to the same DbContext
    // instance used by UnitOfWork.
    // =========================================================

    public async Task<Person?> GetPersonForUpdateAsync(
        int id)
    {
        if (id <= 0)
            return null;

        return await _context.People
            .FirstOrDefaultAsync(
                p => p.PersonId == id);
    }

    // =========================================================
    // EXISTS BY ID
    // =========================================================

    public async Task<bool> IsPersonExistsByIdAsync(
        int id)
    {
        if (id <= 0)
            return false;

        return await _context.People
            .AsNoTracking()
            .AnyAsync(
                p => p.PersonId == id);
    }

    // =========================================================
    // NATIONAL NUMBER DUPLICATE
    // =========================================================

    public async Task<bool> IsNationalNoDuplicatedAsync(
        string nationalNo,
        int personId)
    {
        if (string.IsNullOrWhiteSpace(nationalNo))
            return false;

        var normalizedNationalNo =
            nationalNo.Trim();

        return await _context.People
            .AsNoTracking()
            .AnyAsync(
                p =>
                    p.NationalNo ==
                    normalizedNationalNo &&
                    p.PersonId != personId);
    }

    // =========================================================
    // HAS APPLICATIONS
    // =========================================================

    public async Task<bool> HasApplicationsAsync(
        int personId)
    {
        if (personId <= 0)
            return false;

        return await _context.Applications
            .AsNoTracking()
            .AnyAsync(
                a =>
                    a.ApplicantPersonID ==
                    personId);
    }

    // =========================================================
    // CREATE
    //
    // Repository only changes the current DbContext.
    // UnitOfWork is responsible for persistence.
    // =========================================================

    public async Task<int> AddPersonAsync(
        Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        await _context.People
            .AddAsync(person);

        // Do NOT call SaveChangesAsync here.
        //
        // PersonId is generated when:
        //
        // IUnitOfWork.SaveChangesAsync()
        //
        // is executed by the Service.

        return person.PersonId;
    }

    // =========================================================
    // UPDATE
    //
    // Kept because it is part of IPersonRepository.
    //
    // Service currently uses GetPersonForUpdateAsync()
    // and modifies the tracked entity directly.
    // =========================================================

    public async Task<bool> UpdatePersonAsync(
        Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        if (person.PersonId <= 0)
            return false;

        var existing =
            await _context.People
                .FirstOrDefaultAsync(
                    p =>
                        p.PersonId ==
                        person.PersonId);

        if (existing is null)
            return false;

        existing.NationalNo =
            person.NationalNo;

        existing.FirstName =
            person.FirstName;

        existing.SecondName =
            person.SecondName;

        existing.ThirdName =
            person.ThirdName;

        existing.LastName =
            person.LastName;

        existing.DateOfBirth =
            person.DateOfBirth;

        existing.Gender =
            person.Gender;

        existing.Address =
            person.Address;

        existing.Phone =
            person.Phone;

        existing.Email =
            person.Email;

        existing.NationalityCountryID =
            person.NationalityCountryID;

        existing.ImagePath =
            person.ImagePath;

        // No SaveChangesAsync.
        //
        // UnitOfWork owns persistence.

        return true;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool> DeletePersonAsync(
        int id)
    {
        if (id <= 0)
            return false;

        var person =
            await _context.People
                .FirstOrDefaultAsync(
                    p =>
                        p.PersonId ==
                        id);

        if (person is null)
            return false;

        _context.People.Remove(person);

        // No SaveChangesAsync.
        //
        // UnitOfWork owns persistence.

        return true;
    }
}