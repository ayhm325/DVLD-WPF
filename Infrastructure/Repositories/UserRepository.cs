using Application.DTOs.AuthDTO;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly DVLDDbContext _context;

    public UserRepository(DVLDDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<User> Query()
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Person);
    }

    public Task<User?> GetUserByUserIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return Query()
            .FirstOrDefaultAsync(
                u => u.UserId == id,
                cancellationToken);
    }

    public Task<User?> GetUserProfileAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .ThenInclude(p => p.Country)
            .FirstOrDefaultAsync(
                u => u.UserId == userId,
                cancellationToken);
    }

    public async Task<UserDetailsDto?> GetUserDetailsByIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId && u.Person != null)
            .Select(u => new UserDetailsDto
            {
                UserId = u.UserId,
                PersonId = u.PersonId,
                UserName = u.UserName,
                IsActive = u.IsActive,

                Role = (int)u.Role,

                NationalNo = u.Person.NationalNo,
                FirstName = u.Person.FirstName,
                SecondName = u.Person.SecondName,
                ThirdName = u.Person.ThirdName,
                LastName = u.Person.LastName,
                DateOfBirth = u.Person.DateOfBirth,

                Gender = u.Person.Gender.ToString(),

                Address = u.Person.Address,
                Phone = u.Person.Phone,
                Email = u.Person.Email,

                NationalityCountryID = u.Person.NationalityCountryID,
                CountryName = u.Person.Country != null
                    ? u.Person.Country.CountryName
                    : null,

                ImagePath = u.Person.ImagePath
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<User?> GetUserByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return Query()
            .FirstOrDefaultAsync(
                u => u.PersonId == personId,
                cancellationToken);
    }

    public Task<User?> GetUserByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return Query()
            .FirstOrDefaultAsync(
                u => u.UserName == username,
                cancellationToken);
    }

    public Task<List<User>> GetAllUsersAsync(
        CancellationToken cancellationToken = default)
    {
        return Query()
            .OrderBy(u => u.UserId)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetUserForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .FirstOrDefaultAsync(
                u => u.UserId == id,
                cancellationToken);
    }

    public Task<bool> IsUsernameTakenAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserName == username,
                cancellationToken);
    }

    public Task<bool> IsUsernameTakenForAnotherUserAsync(
        string username,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserName == username &&
                    u.UserId != userId,
                cancellationToken);
    }

    public Task<bool> IsUserExistsByPersonIdAsync(
        int personId,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.PersonId == personId,
                cancellationToken);
    }

    public Task<bool> IsUserActiveAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return Task.FromResult(false);

        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserId == userId && u.IsActive,
                cancellationToken);
    }

    public Task<UserSecurityState?> GetSecurityStateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return Task.FromResult<UserSecurityState?>(null);

        return _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new UserSecurityState
            {
                IsActive = u.IsActive,
                SecurityStamp = u.SecurityStamp
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> GetActiveAdminCountAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .CountAsync(
                u => u.Role == UserRole.Admin && u.IsActive,
                cancellationToken);
    }

    public async Task AddUserAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void DeleteUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _context.Users.Remove(user);
    }
}
