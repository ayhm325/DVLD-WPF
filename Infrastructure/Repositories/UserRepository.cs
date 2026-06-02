using DVLD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class UserRepository
    {
        private readonly IDbContextFactory<DVLDDbContext> _contextFactory;

        public UserRepository(IDbContextFactory<DVLDDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public async Task<User?> GetUserByUserIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.Person)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<User?> GetUserByPersonIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.Person)
                .FirstOrDefaultAsync(u => u.PersonId == id);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.Person)
                .ToListAsync();
        }
        // =========================
        // CHECK OPERATIONS
        // =========================
        public async Task<bool> IsUserExistsAsync(Expression<Func<User, bool>> predicate)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(predicate);
        }

        public async Task<bool> CheckUserCredentialsAsync(string username, string password)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(u => u.Username == username && u.Password == password);
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsUsernameTakenForAnotherUserAsync(string username, int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(u => u.Username == username && u.UserId != userId);
        }

        public async Task<bool> IsUserExistsByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(u => u.UserId == id);
        }

        public async Task<bool> IsUserExistsByPersonIdAsync(int personId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(u => u.PersonId == personId);
        }

        public async Task<bool> CheckUserExistsAsync(string username, string password)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(u => u.Username == username && u.Password == password);
        }

        //// =========================
        //// CREATE
        //// =========================
        public async Task<int> AddUserAsync(User user)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.UserId;
        }

        //// =========================
        //// UPDATE
        //// =========================
        public async Task<bool> UpdateUserAsync(User user)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Users.FindAsync(user.UserId);
            if (existing is null) return false;

            context.Entry(existing).CurrentValues.SetValues(user);
            return await context.SaveChangesAsync() > 0;
        }


        //// =========================
        //// DELETE
        //// =========================
        public async Task<bool> DeleteUserAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(id);

            if (user == null) return false;

            context.Users.Remove(user);
            return await context.SaveChangesAsync() > 0;
        }






    }
}
