using DVLD.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class UserRepository
    {
        private readonly DVLDDbContext _context;

        public UserRepository(DVLDDbContext context)
        {
            _context = context;
        }

        // =========================
        // GET OPERATIONS
        // =========================
        public User? GetUserByUserId(int id)
        {
            return _context.Users
                .Include(u => u.Person)
                .FirstOrDefault(u => u.UserId == id);
        }

        public User? GetUserByPersonId(int id)
        {
            return _context.Users
                .Include(u => u.Person)
                .FirstOrDefault(u => u.PersonId == id);
        }

        public List<User> GetAllUsers()
        {
            return _context.Users
                .Include(u => u.Person)
                .ToList();
        }
        // =========================
        // CHECK OPERATIONS
        // =========================
        public bool IsUserExists(Expression<Func<User, bool>> predicate)
        {
            return _context.Users.Any(predicate);
        }

        public bool CheckUserCredentials(string username, string password)
        {
            return _context.Users.Any(u => u.Username == username && u.Password == password);
        }

        public bool IsUsernameTaken(string username)
        {
            return _context.Users.Any(u => u.Username == username);
        }

        public bool IsUsernameTakenForAnotherUser(string username, int userId)
        {
            return _context.Users.Any(u => u.Username == username && u.UserId != userId);
        }

        public bool IsUserExistsById(int id)
        {
            return _context.Users.Any(u => u.UserId == id);
        }

        public bool IsUserExistsByPersonId(int personId)
        {
            return _context.Users.Any(u => u.PersonId == personId);
        }
        //public bool IsUserExistsById(int id)
        //{
        //    return _context.Users.Any(u => u.UserId == id);
        //}
        //public bool CheckUserExists(string username, string password)
        //{
        //    return _context.Users.Any(u => u.Username == username && u.Password == password);
        //}

        //public bool CheckIfUserExistsByPersonID(int id)
        //{
        //    return _context.Users.Any(u => u.PersonId == id);
        //}

        //public bool CheckIfUserNameExists(string username)
        //{
        //    return _context.Users.Any(u => u.Username == username); 
        //}

        //public bool CheckIfUserNameExistsForAnotherUser(string username,int userId)
        //{
        //    return _context.Users.Any(u => u.Username == username && u.UserId != userId);
        //}

        // =========================
        // CREATE
        // =========================
        public int AddUser(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return user.UserId;
        }

        // =========================
        // UPDATE
        // =========================
        public bool UpdateUser(User user)
        {
            var existing = _context.Users.Find(user.UserId);
            if (existing is null)
                return false;

            _context.Entry(existing).CurrentValues.SetValues(user);
            return _context.SaveChanges() > 0;
        }
        //public bool UpdateUser(User user)
        //{
        //    var existing = _context.Users
        //        .FirstOrDefault(u => u.UserId == user.UserId);

        //    if (existing is null)
        //        return false;

        //    existing.Username = user.Username;
        //    existing.Password = user.Password;
        //    existing.IsActive = user.IsActive;
        //    existing.PersonId = user.PersonId;

        //    return _context.SaveChanges() > 0;
        //}

        // =========================
        // DELETE
        // =========================
        public bool DeleteUser(int id)
        { 
            var user = _context.Users.Find(id);
            if (user is null)
                return false;

            _context.Users.Remove(user);
            return _context.SaveChanges() > 0;
        }

       




    }
}
