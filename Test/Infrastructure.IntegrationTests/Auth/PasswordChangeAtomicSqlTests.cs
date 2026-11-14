using System.Data.Common;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Infrastructure.IntegrationTests.Auth;

/// <summary>
/// Real SQL Server integration tests verifying transactional atomicity and rollback
/// for password changes and session revocations against an isolated test database.
/// </summary>
public sealed class PasswordChangeAtomicSqlTests : IClassFixture<SqlServerTestDatabase>
{
    private static readonly IRefreshTokenService TokenService = new RefreshTokenService();
    private readonly SqlServerTestDatabase _database;
    private readonly IJordanClock _clock = new JordanClock();

    public PasswordChangeAtomicSqlTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    /// <summary>
    /// Test A — Successful atomic password change:
    /// Seeds one user with multiple active sessions and another user with active sessions.
    /// Executes the real UserService with real repositories and UnitOfWork.
    /// Verifies new password hash, rotated SecurityStamp, revoked target sessions, and untouched other sessions.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenSuccessful_PersistsUserChangesAndRevokesAllSessionsAtomically()
    {
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword123!";

        var (targetUserId, targetOldHash, targetOldStamp) = await SeedUserAsync("pwd.target.success", oldPassword);
        var (otherUserId, otherOldHash, otherOldStamp) = await SeedUserAsync("pwd.other.success", oldPassword);

        var targetSession1 = NewSession(targetUserId, TokenService.CreateToken());
        var targetSession2 = NewSession(targetUserId, TokenService.CreateToken());
        var otherSession = NewSession(otherUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(targetSession1, targetSession2, otherSession);
            await seedContext.SaveChangesAsync();
        }

        await using (var serviceContext = _database.CreateContext())
        {
            var userRepo = new UserRepository(serviceContext);
            var sessionRepo = new UserSessionRepository(serviceContext);
            var uow = new UnitOfWork(serviceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            var result = await sut.ChangePasswordAsync(
                targetUserId,
                new ChangePasswordDto
                {
                    CurrentPassword = oldPassword,
                    NewPassword = newPassword
                });

            Assert.True(result.IsSuccess);
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var updatedTarget = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == targetUserId);

            Assert.NotEqual(targetOldHash, updatedTarget.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedTarget.Password));
            Assert.False(BCrypt.Net.BCrypt.Verify(oldPassword, updatedTarget.Password));

            Assert.NotEqual(targetOldStamp, updatedTarget.SecurityStamp);
            Assert.False(string.IsNullOrWhiteSpace(updatedTarget.SecurityStamp));

            var targetSessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == targetUserId)
                .ToListAsync();

            Assert.Equal(2, targetSessions.Count);
            Assert.All(targetSessions, s =>
            {
                Assert.NotNull(s.RevokedAtUtc);
                Assert.Equal(SessionRevocationReasons.PasswordChanged, s.RevocationReason);
            });

            var updatedOther = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == otherUserId);

            Assert.Equal(otherOldHash, updatedOther.Password);
            Assert.Equal(otherOldStamp, updatedOther.SecurityStamp);

            var otherSessionReloaded = await verifyContext.UserSessions
                .AsNoTracking()
                .SingleAsync(s => s.Id == otherSession.Id);

            Assert.Null(otherSessionReloaded.RevokedAtUtc);
        }
    }

    /// <summary>
    /// Test B — Rollback when persistence fails after revocation:
    /// Forces a real database-level constraint failure on the Users table after session revocation has executed.
    /// Verifies the transaction rolls back completely: original password, security stamp, and sessions remain active.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenPersistenceFailsAfterRevocation_RollsBackSessionRevocationAndUserChanges()
    {
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword123!";

        var (targetUserId, targetOldHash, targetOldStamp) = await SeedUserAsync("pwd.target.rollback", oldPassword);
        var (otherUserId, _, _) = await SeedUserAsync("pwd.other.rollback", oldPassword);

        var targetSession1 = NewSession(targetUserId, TokenService.CreateToken());
        var targetSession2 = NewSession(targetUserId, TokenService.CreateToken());
        var otherSession = NewSession(otherUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(targetSession1, targetSession2, otherSession);
            await seedContext.SaveChangesAsync();
        }

        const string constraintName = "CK_Users_Password_Failure_Test";

        await using (var schemaContext = _database.CreateContext())
        {
            var addSql = "ALTER TABLE Users WITH NOCHECK ADD CONSTRAINT " + constraintName + " CHECK (Password = '" + targetOldHash + "');";
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
                    await sut.ChangePasswordAsync(
                        targetUserId,
                        new ChangePasswordDto
                        {
                            CurrentPassword = oldPassword,
                            NewPassword = newPassword
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
            var verifiedTarget = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == targetUserId);

            Assert.Equal(targetOldHash, verifiedTarget.Password);
            Assert.Equal(targetOldStamp, verifiedTarget.SecurityStamp);
            Assert.True(BCrypt.Net.BCrypt.Verify(oldPassword, verifiedTarget.Password));

            var targetSessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == targetUserId)
                .ToListAsync();

            Assert.Equal(2, targetSessions.Count);
            Assert.All(targetSessions, s =>
            {
                Assert.Null(s.RevokedAtUtc);
                Assert.Null(s.RevocationReason);
            });

            var otherSessionReloaded = await verifyContext.UserSessions
                .AsNoTracking()
                .SingleAsync(s => s.Id == otherSession.Id);

            Assert.Null(otherSessionReloaded.RevokedAtUtc);
        }
    }

    /// <summary>
    /// Test C — Isolation between users:
    /// Verifies that changing password and revoking sessions for one user leaves all sessions of other users intact.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_IsolatesBetweenUsers_LeavesOtherUserSessionsUnchanged()
    {
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword123!";

        var (targetUserId, _, _) = await SeedUserAsync("pwd.target.iso", oldPassword);
        var (otherUserId, otherOldHash, otherOldStamp) = await SeedUserAsync("pwd.other.iso", oldPassword);

        var targetSession = NewSession(targetUserId, TokenService.CreateToken());
        var otherSession1 = NewSession(otherUserId, TokenService.CreateToken());
        var otherSession2 = NewSession(otherUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(targetSession, otherSession1, otherSession2);
            await seedContext.SaveChangesAsync();
        }

        await using (var serviceContext = _database.CreateContext())
        {
            var userRepo = new UserRepository(serviceContext);
            var sessionRepo = new UserSessionRepository(serviceContext);
            var uow = new UnitOfWork(serviceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            var result = await sut.ChangePasswordAsync(
                targetUserId,
                new ChangePasswordDto
                {
                    CurrentPassword = oldPassword,
                    NewPassword = newPassword
                });

            Assert.True(result.IsSuccess);
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var otherUser = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == otherUserId);

            Assert.Equal(otherOldHash, otherUser.Password);
            Assert.Equal(otherOldStamp, otherUser.SecurityStamp);

            var otherSessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == otherUserId)
                .ToListAsync();

            Assert.Equal(2, otherSessions.Count);
            Assert.All(otherSessions, s => Assert.Null(s.RevokedAtUtc));
        }
    }

    /// <summary>
    /// Test D — Early cancellation check:
    /// Verifies that an operation with a pre-cancelled token immediately exits without persisting state.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenCancelledEarly_RollsBackAndCommitsNoPartialState()
    {
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword123!";

        var (targetUserId, targetOldHash, targetOldStamp) = await SeedUserAsync("pwd.target.cancel", oldPassword);
        var targetSession = NewSession(targetUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.Add(targetSession);
            await seedContext.SaveChangesAsync();
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await using (var serviceContext = _database.CreateContext())
        {
            var userRepo = new UserRepository(serviceContext);
            var sessionRepo = new UserSessionRepository(serviceContext);
            var uow = new UnitOfWork(serviceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await sut.ChangePasswordAsync(
                    targetUserId,
                    new ChangePasswordDto
                    {
                        CurrentPassword = oldPassword,
                        NewPassword = newPassword
                    },
                    cts.Token);
            });
        }

        await using (var verifyContext = _database.CreateContext())
        {
            var target = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == targetUserId);

            Assert.Equal(targetOldHash, target.Password);
            Assert.Equal(targetOldStamp, target.SecurityStamp);

            var session = await verifyContext.UserSessions
                .AsNoTracking()
                .SingleAsync(s => s.Id == targetSession.Id);

            Assert.Null(session.RevokedAtUtc);
        }
    }

    /// <summary>
    /// Test E — Deterministic mid-flight cancellation test:
    /// Triggers cancellation strictly AFTER the session-revocation UPDATE command has executed on SQL Server,
    /// but BEFORE the transaction commits.
    /// Uses an EF Core DbCommandInterceptor to deterministically cancel CancellationTokenSource upon observing
    /// completion of the UPDATE command targeting UserSessions.
    /// Verifies that the physical transaction is rolled back, leaving password hash, SecurityStamp, and sessions
    /// in their original active states with zero committed partial changes.
    /// </summary>
    [Fact]
    public async Task ChangePasswordAsync_WhenCancelledAfterSessionRevocationBeforeCommit_RollsBackAllChangesAndLeavesSessionsActive()
    {
        const string oldPassword = "OldPassword123!";
        const string newPassword = "NewPassword123!";

        var (targetUserId, targetOldHash, targetOldStamp) = await SeedUserAsync("pwd.target.midcancel", oldPassword);
        var targetSession1 = NewSession(targetUserId, TokenService.CreateToken());
        var targetSession2 = NewSession(targetUserId, TokenService.CreateToken());

        await using (var seedContext = _database.CreateContext())
        {
            seedContext.UserSessions.AddRange(targetSession1, targetSession2);
            await seedContext.SaveChangesAsync();
        }

        using var cts = new CancellationTokenSource();
        var interceptor = new CancelAfterSessionRevocationCommandInterceptor(cts);

        await using (var serviceContext = CreateContextWithInterceptors(interceptor))
        {
            var userRepo = new UserRepository(serviceContext);
            var sessionRepo = new UserSessionRepository(serviceContext);
            var uow = new UnitOfWork(serviceContext);
            var sut = new UserService(userRepo, uow, sessionRepo, _clock);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await sut.ChangePasswordAsync(
                    targetUserId,
                    new ChangePasswordDto
                    {
                        CurrentPassword = oldPassword,
                        NewPassword = newPassword
                    },
                    cts.Token);
            });
        }

        Assert.True(interceptor.RevocationExecuted, "The session revocation SQL command must have executed before cancellation was triggered.");

        await using (var verifyContext = _database.CreateContext())
        {
            var target = await verifyContext.Users
                .AsNoTracking()
                .SingleAsync(u => u.UserId == targetUserId);

            Assert.Equal(targetOldHash, target.Password);
            Assert.Equal(targetOldStamp, target.SecurityStamp);

            var sessions = await verifyContext.UserSessions
                .AsNoTracking()
                .Where(s => s.UserId == targetUserId)
                .ToListAsync();

            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, s => Assert.Null(s.RevokedAtUtc));
        }
    }

    private sealed class CancelAfterSessionRevocationCommandInterceptor(
        CancellationTokenSource cts) : DbCommandInterceptor
    {
        public bool RevocationExecuted { get; private set; }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            CheckAndCancel(command);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            CheckAndCancel(command);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<object?> ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result,
            CancellationToken cancellationToken = default)
        {
            CheckAndCancel(command);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        private void CheckAndCancel(DbCommand command)
        {
            if (command.CommandText.Contains("UserSessions", StringComparison.OrdinalIgnoreCase) &&
                command.CommandText.Contains("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                RevocationExecuted = true;
                cts.Cancel();
            }
        }
    }

    private DVLDDbContext CreateContextWithInterceptors(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(_database.ConnectionString);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new DVLDDbContext(builder.Options);
    }

    private async Task<(int UserId, string PasswordHash, string SecurityStamp)> SeedUserAsync(string userName, string plainPassword)
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
            FirstName = "Pwd",
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

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        var securityStamp = Guid.NewGuid().ToString("N");

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = userName,
            Password = passwordHash,
            SecurityStamp = securityStamp,
            IsActive = true,
            Role = UserRole.Staff
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (user.UserId, passwordHash, securityStamp);
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
