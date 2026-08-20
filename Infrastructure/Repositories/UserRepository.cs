using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public UserRepository(
            IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory
                ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // BASE QUERY
        // =========================

        private IQueryable<User> Query(DVLDDbContext context)
        {
            return context.Users
                .AsNoTracking()
                .Include(u => u.Person);
        }

        // =========================
        // GET OPERATIONS
        // =========================

        public async Task<User?> GetUserByUserIdAsync(int id)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetUserByPersonIdAsync(int personId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(u => u.PersonId == personId);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await Query(context)
                .ToListAsync();
        }

        // =========================
        // CHECK OPERATIONS
        // =========================

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserName == username);
        }

        public async Task<bool> IsUsernameTakenForAnotherUserAsync(
            string username,
            int userId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UserName == username &&
                    u.UserId != userId);
        }

        public async Task<bool> IsUserExistsByIdAsync(int id)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserId == id);
        }

        public async Task<bool> IsUserExistsByPersonIdAsync(int personId)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.PersonId == personId);
        }

        // =========================
        // CREATE
        // =========================

        public async Task<int> AddUserAsync(User user)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            await context.Users.AddAsync(user);

            await context.SaveChangesAsync();

            return user.UserId;
        }

        // =========================
        // UPDATE
        // =========================

        public async Task<bool> UpdateUserAsync(User user)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            var existingUser = await context.Users
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);

            if (existingUser is null)
                return false;

            context.Entry(existingUser)
                .CurrentValues
                .SetValues(user);

            return await context.SaveChangesAsync() > 0;
        }

        // =========================
        // DELETE
        // =========================

        public async Task<bool> DeleteUserAsync(int id)
        {
            using var context =
                await _contextFactory.CreateDbContextAsync();

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user is null)
                return false;

            context.Users.Remove(user);

            return await context.SaveChangesAsync() > 0;
        }
    }
}