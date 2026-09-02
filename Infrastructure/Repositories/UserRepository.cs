using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DVLDDbContext _context;

    public UserRepository(DVLDDbContext context)
    {
        _context =context ?? throw new ArgumentNullException(nameof(context));
    }

    // =========================================================
    // BASE QUERY
    //
    // Read-only queries use NoTracking.
    // =========================================================

    private IQueryable<User> Query()
    {
        return _context.Users
            .AsNoTracking()
            .Include(u => u.Person);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<User?>
        GetUserByUserIdAsync(
            int id)
    {
        if (id <= 0)
            return null;

        return await Query()
            .FirstOrDefaultAsync(
                u =>
                    u.UserId == id);
    }

    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    public async Task<User?>
        GetUserByPersonIdAsync(
            int personId)
    {
        if (personId <= 0)
            return null;

        return await Query()
            .FirstOrDefaultAsync(
                u =>
                    u.PersonId == personId);
    }

    // =========================================================
    // GET BY USERNAME
    // =========================================================

    public async Task<User?>
        GetUserByUsernameAsync(
            string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        var normalizedUsername =
            username.Trim();

        return await Query()
            .FirstOrDefaultAsync(
                u =>
                    u.UserName ==
                    normalizedUsername);
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<User>>
        GetAllUsersAsync()
    {
        return await Query()
            .OrderBy(
                u => u.UserId)
            .ToListAsync();
    }

    // =========================================================
    // CHECK USERNAME
    // =========================================================

    public async Task<bool>
        IsUsernameTakenAsync(
            string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        var normalizedUsername =
            username.Trim();

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserName ==
                    normalizedUsername);
    }

    // =========================================================
    // CHECK USERNAME FOR ANOTHER USER
    // =========================================================

    public async Task<bool>
        IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            userId <= 0)
        {
            return false;
        }

        var normalizedUsername =
            username.Trim();

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserName ==
                    normalizedUsername &&
                    u.UserId != userId);
    }

    // =========================================================
    // CHECK USER EXISTS
    // =========================================================

    public async Task<bool>
        IsUserExistsByIdAsync(
            int id)
    {
        if (id <= 0)
            return false;

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserId == id);
    }

    // =========================================================
    // CHECK PERSON HAS USER
    // =========================================================

    public async Task<bool> IsUserExistsByPersonIdAsync(int personId)
    {
        if (personId <= 0)
            return false;

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.PersonId == personId);
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<int>
        AddUserAsync(
            User user)
    {
        ArgumentNullException.ThrowIfNull(
            user);

        await _context.Users
            .AddAsync(user);

        // -----------------------------------------------------
        // IMPORTANT
        //
        // No SaveChangesAsync here.
        //
        // UnitOfWork owns persistence.
        // -----------------------------------------------------

        return user.UserId;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateUserAsync(
            User user)
    {
        ArgumentNullException.ThrowIfNull(
            user);

        if (user.UserId <= 0)
            return false;

        // -----------------------------------------------------
        // LOAD TRACKED ENTITY
        // -----------------------------------------------------

        var existingUser =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.UserId ==
                        user.UserId);

        if (existingUser is null)
            return false;

        // -----------------------------------------------------
        // APPLY CHANGES
        // -----------------------------------------------------

        existingUser.PersonId = user.PersonId;

        existingUser.UserName = user.UserName;

        existingUser.Password = user.Password;

        existingUser.IsActive = user.IsActive;

        // -----------------------------------------------------
        // IMPORTANT
        //
        // No SaveChangesAsync here.
        //
        // UnitOfWork owns persistence.
        // -----------------------------------------------------

        return true;
    }

    // =========================================================
    // DELETE
    // =========================================================

    public async Task<bool>
        DeleteUserAsync(
            int id)
    {
        if (id <= 0)
            return false;

        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.UserId == id);

        if (user is null)
            return false;

        _context.Users
            .Remove(user);

        // -----------------------------------------------------
        // IMPORTANT
        //
        // No SaveChangesAsync here.
        //
        // Foreign-key violations, if any, will surface when
        // UnitOfWork.SaveChangesAsync() is executed.
        // -----------------------------------------------------

        return true;
    }
}