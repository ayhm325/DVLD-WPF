using Application.Interfaces;
using Domain.Entities;
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

    public Task<User?> GetUserByUserIdAsync(int id)
    {
        return Query()
            .FirstOrDefaultAsync(
                u => u.UserId == id);
    }

    public Task<User?> GetUserByPersonIdAsync(int personId)
    {
        return Query()
            .FirstOrDefaultAsync(
                u => u.PersonId == personId);
    }

    public Task<User?> GetUserByUsernameAsync(
        string username)
    {
        return Query()
            .FirstOrDefaultAsync(
                u => u.UserName == username);
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return Query()
            .OrderBy(u => u.UserId)
            .ToListAsync();
    }

    public Task<User?> GetUserForUpdateAsync(int id)
    {
        return _context.Users
            .FirstOrDefaultAsync(
                u => u.UserId == id);
    }

    public Task<bool> IsUsernameTakenAsync(
        string username)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserName == username);
    }

    public Task<bool> IsUsernameTakenForAnotherUserAsync(
        string username,
        int userId)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserName == username &&
                    u.UserId != userId);
    }

    public Task<bool> IsUserExistsByPersonIdAsync(
        int personId)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.PersonId == personId);
    }

    public async Task AddUserAsync(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _context.Users.AddAsync(user);
    }

    public void DeleteUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        _context.Users.Remove(user);
    }
}
