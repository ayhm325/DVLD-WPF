using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.IntegrationTests.Auth;

/// <summary>
/// Real SQL Server integration tests verifying transactional atomicity and rollback
/// for security-sensitive user updates, deactivations, and user deletions against an isolated test database.
/// </summary>
public sealed class UserSecurityUpdateAtomicSqlTests : IClassFixture<SqlServerTestDatabase>
{
    private static readonly IRefreshTokenService TokenService = new RefreshTokenService();
    private readonly SqlServerTestDatabase _database;
    private readonly IJordanClock _clock = new JordanClock();

    public UserSecurityUpdateAtomicSqlTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// Test 1: Successful security update (Staff promotion to Admin)
    /// Commits both user role change, rotated security stamp, and session revocation atomically.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenStaffPromotedToAdmin_CommitsUserChangesAndSessionRevocationAtomically()
    {
        var (staffUserId, personId, oldStamp) = await SeedUserAsync("staff_promote_target", UserRole.Staff, isActive: true);
        var (otherUserId, _, otherOldStamp) = await SeedUserAsync("staff_promote_other", UserRole.Staff, isActive: true);

        var session1 = NewSession(staffUserId, TokenService.CreateToken());
        var session2 = NewSession(staffUserId, TokenService.CreateToken());
        var otherSession = NewSession(otherUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(session1, session2, otherSession);
            await seedContext.SaveChangesAsync();
        }

        await using (var serviceContext = _database.CreateContext())
        {
            var userRepo = new UserRepository(serviceContext);
            var sessionRepo = new UserSessionRepository(serviceContext);
            var uow = new UnitOfWork(serviceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            var result = await sut.UpdateUserAsync(
                staffUserId,
                new UpdateUserDto
                {
                    UserName = "staff_promote_target",
                    PersonId = personId,
                    IsActive = true,
                    Role = UserRole.Admin
                });

            Assert.True(result.IsSuccess, $"{result.ErrorType}: {result.Error}");
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var updatedUser = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == staffUserId);

            Assert.Equal(UserRole.Admin, updatedUser.Role);
            Assert.NotEqual(oldStamp, updatedUser.SecurityStamp);
            Assert.False(string.IsNullOrWhiteSpace(updatedUser.SecurityStamp));

            var targetSessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == staffUserId)
                .ToListAsync();

            Assert.Equal(2, targetSessions.Count);
            Assert.All(targetSessions, s =>
            {
                Assert.NotNull(s.RevokedAtUtc);
                Assert.Equal(SessionRevocationReasons.SecurityStateChanged, s.RevocationReason);
            });

            var otherUser = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == otherUserId);

            Assert.Equal(otherOldStamp, otherUser.SecurityStamp);

            var otherSessionReloaded = await verifyContext.UserSessions
                .AsNoTracking()
                .SingleAsync(s => s.Id == otherSession.Id);

            Assert.Null(otherSessionReloaded.RevokedAtUtc);
        }
    }

    /// <summary>
    /// Test 2: Real SQL failure during user SaveChangesAsync rolls back session revocation.
    /// Injects a database-level CHECK constraint that prevents deactivating the user.
    /// Verifies that when SaveChangesAsync fails, the transaction rolls back:
    /// the user remains active, the security stamp is unchanged, and sessions remain active.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenSqlFailureDuringSave_RollsBackSessionRevocationAndUserChanges()
    {
        var (staffUserId, personId, oldStamp) = await SeedUserAsync("staff_deact_fail", UserRole.Staff, isActive: true);

        var session1 = NewSession(staffUserId, TokenService.CreateToken());
        var session2 = NewSession(staffUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(session1, session2);
            await seedContext.SaveChangesAsync();
        }

        const string constraintName = "CK_Users_IsActive_Failure_Test";

        await using (var schemaContext = _database.CreateContext())
        {
            var addSql = "ALTER TABLE Users WITH NOCHECK ADD CONSTRAINT " + constraintName + " CHECK (IsActive = 1);";
            await schemaContext.Database.ExecuteSqlRawAsync(addSql);
        }

        try
        {
            await using (var serviceContext = _database.CreateContext())
            {
                var userRepo = new UserRepository(serviceContext);
                var sessionRepo = new UserSessionRepository(serviceContext);
                var uow = new UnitOfWork(serviceContext);
                var sut = new UserService(userRepo, uow, sessionRepo, _clock);

                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                {
                    await sut.UpdateUserAsync(
                        staffUserId,
                        new UpdateUserDto
                        {
                            UserName = "staff_deact_fail",
                            PersonId = personId,
                            IsActive = false,
                            Role = UserRole.Staff
                        });
                });
            }
        }
        finally
        {
            await using var cleanupContext = _database.CreateContext();
            var dropSql = "ALTER TABLE Users DROP CONSTRAINT " + constraintName + ";";
            await cleanupContext.Database.ExecuteSqlRawAsync(dropSql);
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var user = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == staffUserId);

            Assert.True(user.IsActive);
            Assert.Equal(oldStamp, user.SecurityStamp);

            var sessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == staffUserId)
                .ToListAsync();

            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, s =>
            {
                Assert.Null(s.RevokedAtUtc);
                Assert.Null(s.RevocationReason);
            });
        }
    }

    /// <summary>
    /// Test 3: Failed Staff deletion preserves both the user record and existing sessions.
    /// Injects a database-level failure so that DELETE fails.
    /// Verifies that the user still exists and all sessions remain unrevoked.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WhenStaffDeletionFailsInSql_PreservesUserRecordAndSessions()
    {
        var (staffUserId, _, oldStamp) = await SeedUserAsync("staff_del_fail", UserRole.Staff, isActive: true);

        var session1 = NewSession(staffUserId, TokenService.CreateToken());
        var session2 = NewSession(staffUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(session1, session2);
            await seedContext.SaveChangesAsync();
        }

        // Add a child country record referencing this user to trigger an FK constraint violation on delete,
        // or add a CHECK constraint on another table or a trigger that raises an error on delete.
        // A simple trigger on Users for delete cleanly forces a real SQL failure on delete.
        const string triggerName = "TR_Users_PreventDelete_Test";

        await using (var schemaContext = _database.CreateContext())
        {
            var addTriggerSql =
                $"CREATE TRIGGER {triggerName} ON Users INSTEAD OF DELETE AS " +
                "BEGIN THROW 50001, 'Cannot delete user under test.', 1; END;";
            await schemaContext.Database.ExecuteSqlRawAsync(addTriggerSql);
        }

        try
        {
            await using (var serviceContext = _database.CreateContext())
            {
                var userRepo = new UserRepository(serviceContext);
                var sessionRepo = new UserSessionRepository(serviceContext);
                var uow = new UnitOfWork(serviceContext);
                var sut = new UserService(userRepo, uow, sessionRepo, _clock);

                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                {
                    await sut.DeleteUserAsync(staffUserId);
                });
            }
        }
        finally
        {
            await using var cleanupContext = _database.CreateContext();
            var dropTriggerSql = $"DROP TRIGGER IF EXISTS {triggerName};";
            await cleanupContext.Database.ExecuteSqlRawAsync(dropTriggerSql);
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var user = await verifyContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserId == staffUserId);

            Assert.NotNull(user);
            Assert.Equal(oldStamp, user.SecurityStamp);

            var sessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == staffUserId)
                .ToListAsync();

            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, s =>
            {
                Assert.Null(s.RevokedAtUtc);
                Assert.Null(s.RevocationReason);
            });
        }
    }

    /// <summary>
    /// Test 4: Successful Staff deletion commits deletion and session revocation atomically.
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WhenStaffDeletionSucceeds_CommitsDeletionAndSessionRevocationAtomically()
    {
        var (staffUserId, _, _) = await SeedUserAsync("staff_del_success", UserRole.Staff, isActive: true);

        var session1 = NewSession(staffUserId, TokenService.CreateToken());
        var session2 = NewSession(staffUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(session1, session2);
            await seedContext.SaveChangesAsync();
        }

        await using (var serviceContext = _database.CreateContext())
        {
            var userRepo = new UserRepository(serviceContext);
            var sessionRepo = new UserSessionRepository(serviceContext);
            var uow = new UnitOfWork(serviceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            var result = await sut.DeleteUserAsync(staffUserId);

            Assert.True(result.IsSuccess, $"{result.ErrorType}: {result.Error}");
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var user = await verifyContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserId == staffUserId);

            Assert.Null(user);

            var sessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == staffUserId)
                .ToListAsync();

            if (sessions.Count == 0)
            {
                // Cascade deletion removed all user sessions from the database
                Assert.Empty(sessions);
                Assert.False(await verifyContext.UserSessions.AnyAsync(s => s.Id == session1.Id));
                Assert.False(await verifyContext.UserSessions.AnyAsync(s => s.Id == session2.Id));
            }
            else
            {
                // Non-cascade or soft-retention: prove all seeded sessions remain and all are revoked
                Assert.Equal(2, sessions.Count);
                Assert.All(sessions, s =>
                {
                    Assert.NotNull(s.RevokedAtUtc);
                    Assert.Equal(SessionRevocationReasons.SecurityStateChanged, s.RevocationReason);
                });
            }
        }
    }

    /// <summary>
    /// Test 5: Existing Admin concurrency protections and last-admin rule remain intact.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_AdminDemotion_PreservesConcurrencyAndLastAdminProtection()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        // Consume the first identity values so the scenario administrators never
        // collide with the hardcoded primary-administrator rule (UserId == 2).
        await SeedUserAsync($"pad1_{suffix}", UserRole.Staff, isActive: true);
        await SeedUserAsync($"pad2_{suffix}", UserRole.Staff, isActive: true);

        // Normalize security state in this test DB so exactly two active admins exist for this test.
        await using (var normContext = _database.CreateContext())
        {
            await normContext.Users
                .Where(u => u.Role == UserRole.Admin && u.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Staff));
        }

        var admin1Name = $"admin_1_{suffix}";
        var admin2Name = $"admin_2_{suffix}";

        var (admin1Id, person1Id, _) = await SeedUserAsync(admin1Name, UserRole.Admin, isActive: true);
        var (admin2Id, person2Id, _) = await SeedUserAsync(admin2Name, UserRole.Admin, isActive: true);

        await using (var serviceContext = _database.CreateContext())
        {
            var userRepo = new UserRepository(serviceContext);
            var sessionRepo = new UserSessionRepository(serviceContext);
            var uow = new UnitOfWork(serviceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            // Demote admin1 to Staff when admin2 exists -> must succeed
            var result = await sut.UpdateUserAsync(
                admin1Id,
                new UpdateUserDto
                {
                    UserName = admin1Name,
                    PersonId = person1Id,
                    IsActive = true,
                    Role = UserRole.Staff
                });

            Assert.True(result.IsSuccess, $"{result.ErrorType}: {result.Error}");
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var demotedUser = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == admin1Id);

            Assert.Equal(UserRole.Staff, demotedUser.Role);
        }

        // Now admin2 is the sole remaining active admin. Attempting to demote admin2 must fail with Conflict.
        await using (var secondServiceContext = _database.CreateContext())
        {
            var userRepo = new UserRepository(secondServiceContext);
            var sessionRepo = new UserSessionRepository(secondServiceContext);
            var uow = new UnitOfWork(secondServiceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            var conflictResult = await sut.UpdateUserAsync(
                admin2Id,
                new UpdateUserDto
                {
                    UserName = admin2Name,
                    PersonId = person2Id,
                    IsActive = true,
                    Role = UserRole.Staff
                });

            Assert.False(conflictResult.IsSuccess);
            Assert.Equal(ErrorType.Conflict, conflictResult.ErrorType);
            Assert.Equal("Cannot change the role of the last active administrator.", conflictResult.Error);
        }

        await using (var finalVerifyContext = _database.CreateContext())
        {
            var remainingAdmin = await finalVerifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == admin2Id);

            Assert.Equal(UserRole.Admin, remainingAdmin.Role);
            Assert.True(remainingAdmin.IsActive);
        }
    }

    private async Task<(int UserId, int PersonId, string SecurityStamp)> SeedUserAsync(string userName, UserRole role, bool isActive)
    {
        await using var context = _database.CreateContext();

        var country = new Country
        {
            CountryName = "Country-" + userName
        };

        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var person = new Person
        {
            NationalNo = "N" + Guid.NewGuid().ToString("N")[..9],
            FirstName = "Sec",
            SecondName = "Test",
            LastName = userName,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender = Gender.Male,
            Address = "Test Address",
            Phone = "0790000000",
            NationalityCountryID = country.CountryId
        };

        context.People.Add(person);
        await context.SaveChangesAsync();

        var securityStamp = Guid.NewGuid().ToString("N");

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = userName,
            Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            SecurityStamp = securityStamp,
            IsActive = isActive,
            Role = role
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (user.UserId, person.PersonId, securityStamp);
    }

    private static UserSession NewSession(
        int userId,
        string rawToken,
        Guid? familyId = null,
        DateTime? expires = null)
    {
        var now = DateTime.UtcNow;

        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = TokenService.HashToken(rawToken),
            TokenFamilyId = familyId ?? Guid.NewGuid(),
            CreatedAtUtc = now,
            ExpiresAtUtc = expires ?? now.AddDays(14),
            LastUsedAtUtc = now
        };
    }
}
