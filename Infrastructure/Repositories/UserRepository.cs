using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<DVLDDbContext>
        _contextFactory;


    public UserRepository(
        IDbContextFactory<DVLDDbContext> contextFactory)
    {
        _contextFactory =
            contextFactory
            ?? throw new ArgumentNullException(
                nameof(contextFactory));
    }


    // =========================================================
    // BASE QUERY
    // =========================================================

    private static IQueryable<User> Query(
        DVLDDbContext context)
    {
        return context.Users
            .AsNoTracking()
            .Include(u => u.Person);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<User?> GetUserByUserIdAsync(
        int id)
    {
        if (id <= 0)
            return null;

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await Query(context)
            .FirstOrDefaultAsync(
                u => u.UserId == id);
    }


    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    public async Task<User?> GetUserByPersonIdAsync(
        int personId)
    {
        if (personId <= 0)
            return null;

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await Query(context)
            .FirstOrDefaultAsync(
                u => u.PersonId == personId);
    }


    // =========================================================
    // GET BY USERNAME
    // =========================================================

    public async Task<User?> GetUserByUsernameAsync(
        string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        username = username.Trim();

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await Query(context)
            .FirstOrDefaultAsync(
                u => u.UserName == username);
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<User>>
        GetAllUsersAsync()
    {
        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await Query(context)
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

        username = username.Trim();

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserName == username);
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

        username = username.Trim();

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserName == username &&
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

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.UserId == id);
    }


    // =========================================================
    // CHECK PERSON HAS USER
    // =========================================================

    public async Task<bool>
        IsUserExistsByPersonIdAsync(
            int personId)
    {
        if (personId <= 0)
            return false;

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        return await context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.PersonId == personId);
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<int>
        AddUserAsync(
            User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        await context.Users.AddAsync(user);

        await context.SaveChangesAsync();

        return user.UserId;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<bool>
        UpdateUserAsync(
            User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.UserId <= 0)
            return false;

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        var existingUser =
            await context.Users
                .FirstOrDefaultAsync(
                    u => u.UserId == user.UserId);

        if (existingUser is null)
            return false;

        existingUser.PersonId =
            user.PersonId;

        existingUser.UserName =
            user.UserName;

        existingUser.Password =
            user.Password;

        existingUser.IsActive =
            user.IsActive;

        return await context.SaveChangesAsync() > 0;
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

        await using var context =
            await _contextFactory
                .CreateDbContextAsync();

        var user =
            await context.Users
                .FirstOrDefaultAsync(
                    u => u.UserId == id);

        if (user is null)
            return false;

        context.Users.Remove(user);

        try
        {
            return await context.SaveChangesAsync() > 0;
        }
        catch (DbUpdateException)
        {
            // The user is referenced by another table.
            // The database correctly prevents the delete.
            return false;
        }
    }
}