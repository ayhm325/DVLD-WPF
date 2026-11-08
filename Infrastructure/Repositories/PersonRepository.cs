using Application.Common.Pagination;
using Application.DTOs.PersonDTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
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

    public Task<PagedResult<Person>> GetAllAsync(
        PaginationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request is PersonQueryRequest queryRequest)
        {
            return GetAllAsync(queryRequest);
        }

        return GetAllAsync(new PersonQueryRequest
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        });
    }

    public async Task<PagedResult<Person>> GetAllAsync(
        PersonQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = ApplyPersonFilters(Query(), request);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.PersonId)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<Person>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<Person>> GetPeopleAvailableForUserAsync(
        PersonQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = Query()
            .Where(p => !_context.Users.Any(u => u.PersonId == p.PersonId));

        query = ApplyPersonFilters(query, request);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.PersonId)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<Person>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    private static IQueryable<Person> ApplyPersonFilters(
        IQueryable<Person> query,
        PersonQueryRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.GenderFilter) &&
            !string.Equals(request.GenderFilter, "all", StringComparison.OrdinalIgnoreCase))
        {
            var filter = request.GenderFilter.Trim();
            if (filter == "0" || string.Equals(filter, "male", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Gender == Gender.Male);
            }
            else if (filter == "1" || string.Equals(filter, "female", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => p.Gender == Gender.Female);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            var term = request.SearchQuery.Trim();
            var searchBy = request.SearchBy?.Trim().ToLowerInvariant() ?? "all";

            switch (searchBy)
            {
                case "nationalno":
                    query = query.Where(p => p.NationalNo.Contains(term));
                    break;

                case "name":
                case "fullname":
                    query = query.Where(p =>
                        p.FirstName.Contains(term) ||
                        p.SecondName.Contains(term) ||
                        (p.ThirdName != null && p.ThirdName.Contains(term)) ||
                        p.LastName.Contains(term) ||
                        (p.FirstName + " " + p.LastName).Contains(term) ||
                        (p.FirstName + " " + p.SecondName + " " + p.LastName).Contains(term));
                    break;

                case "id":
                case "personid":
                    var cleanId = term.TrimStart('#');
                    if (int.TryParse(cleanId, out var id))
                    {
                        query = query.Where(p => p.PersonId == id);
                    }
                    else
                    {
                        query = query.Where(p => false);
                    }
                    break;

                case "phone":
                    query = query.Where(p => p.Phone.Contains(term));
                    break;

                case "all":
                default:
                    var cleanTermId = term.TrimStart('#');
                    var isNum = int.TryParse(cleanTermId, out var parsedId);
                    query = query.Where(p =>
                        p.NationalNo.Contains(term) ||
                        p.FirstName.Contains(term) ||
                        p.SecondName.Contains(term) ||
                        (p.ThirdName != null && p.ThirdName.Contains(term)) ||
                        p.LastName.Contains(term) ||
                        (p.FirstName + " " + p.LastName).Contains(term) ||
                        (p.FirstName + " " + p.SecondName + " " + p.LastName).Contains(term) ||
                        p.Phone.Contains(term) ||
                        (isNum && p.PersonId == parsedId));
                    break;
            }
        }

        return query;
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

    public async Task<PersonStatisticsDto> GetStatisticsAsync()
    {
        var genderCounts = await _context.People
            .AsNoTracking()
            .GroupBy(p => p.Gender)
            .Select(g => new { Gender = g.Key, Count = g.Count() })
            .ToListAsync();

        var maleCount = genderCounts.FirstOrDefault(x => x.Gender == Gender.Male)?.Count ?? 0;
        var femaleCount = genderCounts.FirstOrDefault(x => x.Gender == Gender.Female)?.Count ?? 0;
        var totalCount = genderCounts.Sum(x => x.Count);

        return new PersonStatisticsDto
        {
            TotalCount = totalCount,
            MaleCount = maleCount,
            FemaleCount = femaleCount
        };
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
