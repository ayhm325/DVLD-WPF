using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DVLDDbContext _context;


    public UserRepository(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }


    // =========================================================
    // BASE QUERY
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

        username = username.Trim();

        return await Query()
            .FirstOrDefaultAsync(
                u =>
                    u.UserName == username);
    }


    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<List<User>>
        GetAllUsersAsync()
    {
        return await Query()
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

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserName == username);
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

        return await _context.Users
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

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.UserId == id);
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

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(
                u =>
                    u.PersonId == personId);
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

        // =====================================================
        // IMPORTANT
        // =====================================================
        // لا يوجد SaveChangesAsync هنا.
        //
        // الـ UnitOfWork هو المسؤول عن الحفظ.
        //
        // بعد SaveChangesAsync من الـ UnitOfWork
        // سيتم توليد UserId من قاعدة البيانات.
        //
        // إذا كان الـ Service يحتاج الـ ID مباشرة،
        // يجب تنفيذ SaveChangesAsync في تلك العملية
        // قبل استخدام الـ ID في Entity أخرى.
        // =====================================================

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


        var existingUser =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.UserId == user.UserId);

        if (existingUser is null)
            return false;


        // =====================================================
        // UPDATE VALUES
        // =====================================================

        existingUser.PersonId =
            user.PersonId;

        existingUser.UserName =
            user.UserName;

        existingUser.Password =
            user.Password;

        existingUser.IsActive =
            user.IsActive;


        // =====================================================
        // IMPORTANT
        // =====================================================
        // لا يوجد SaveChangesAsync هنا.
        //
        // الـ UnitOfWork سيقوم بالحفظ.
        // =====================================================

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


        // =====================================================
        // IMPORTANT
        // =====================================================
        // لا يوجد SaveChangesAsync هنا.
        //
        // لا نحاول التقاط DbUpdateException هنا أيضًا،
        // لأن الخطأ سيحدث فعليًا عند:
        //
        //     UnitOfWork.SaveChangesAsync()
        //
        // وبالتالي الـ Service / Transaction هو المكان
        // الصحيح لمعالجة فشل عملية الحفظ.
        // =====================================================

        return true;
    }
}