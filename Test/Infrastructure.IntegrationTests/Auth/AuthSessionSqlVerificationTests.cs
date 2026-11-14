using Application.Interfaces;
using Application.Services;

using Domain.Entities;
using Domain.Enums;

using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Infrastructure.IntegrationTests.Auth;

/// <summary>
/// AUTH-01-POST: real SQL Server verification of refresh-session persistence,
/// schema, rotation chain, atomic consume, and revocation. Uses the ownership-verified
/// ephemeral database fixture exclusively.
/// </summary>
public sealed class AuthSessionSqlVerificationTests
    : IClassFixture<SqlServerTestDatabase>
{
    private static readonly IRefreshTokenService TokenService = new RefreshTokenService();

    private readonly SqlServerTestDatabase _database;

    public AuthSessionSqlVerificationTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Migration_CreatesExpectedSchema_WithIndexesAndCascade()
    {
        await using var context = _database.CreateContext();
        await context.Database.OpenConnectionAsync();

        try
        {
            var connection = (SqlConnection)context.Database.GetDbConnection();

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.tables WHERE name = 'UserSessions';"));

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('Users') " +
                "AND name = 'SecurityStamp' AND is_nullable = 0 AND max_length = 128;"));

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.default_constraints dc " +
                "INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id " +
                "AND c.column_id = dc.parent_column_id " +
                "WHERE dc.parent_object_id = OBJECT_ID('Users') " +
                "AND c.name = 'SecurityStamp' AND dc.definition LIKE '%NEWID%';"));

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('UserSessions') " +
                "AND name = 'IX_UserSessions_RefreshTokenHash' AND is_unique = 1;"));

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('UserSessions') " +
                "AND name = 'IX_UserSessions_TokenFamilyId';"));

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('UserSessions') " +
                "AND name = 'IX_UserSessions_UserId_RevokedAtUtc';"));

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.foreign_keys WHERE name = 'FK_UserSessions_Users_UserId' " +
                "AND delete_referential_action_desc = 'CASCADE';"));

            Assert.Equal(1, await ScalarAsync(
                connection,
                "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('UserSessions') " +
                "AND name = 'RefreshTokenHash' AND max_length = 128 AND is_nullable = 0;"));
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    [Fact]
    public async Task Session_RoundTrips_WithHashOnly_AndRejectsDuplicateHash()
    {
        var userId = await SeedUserAsync("auth.roundtrip");
        var rawToken = TokenService.CreateToken();
        var session = NewSession(userId, rawToken);

        await using (var write = _database.CreateContext())
        {
            write.UserSessions.Add(session);
            await write.SaveChangesAsync();
        }

        await using (var read = _database.CreateContext())
        {
            var stored = await read.UserSessions
                .AsNoTracking()
                .SingleAsync(item => item.RefreshTokenHash == session.RefreshTokenHash);

            Assert.Equal(TokenService.HashToken(rawToken), stored.RefreshTokenHash);
            Assert.NotEqual(rawToken, stored.RefreshTokenHash);
            Assert.Equal(43, stored.RefreshTokenHash.Length);
            Assert.Equal(session.TokenFamilyId, stored.TokenFamilyId);
            Assert.Null(stored.RevokedAtUtc);
        }

        await using (var duplicate = _database.CreateContext())
        {
            duplicate.UserSessions.Add(NewSession(userId, rawToken));

            await Assert.ThrowsAsync<DbUpdateException>(
                () => duplicate.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task TryConsume_IsAtomic_AcrossIndependentContexts()
    {
        var userId = await SeedUserAsync("auth.atomic");
        var rawToken = TokenService.CreateToken();
        var session = NewSession(userId, rawToken);

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.Add(session);
            await seed.SaveChangesAsync();
        }

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<(bool Won, Guid ReplacementId)> AttemptAsync()
        {
            var replacementId = Guid.NewGuid();

            await using var context = _database.CreateContext();
            var repository = new UserSessionRepository(context);

            await gate.Task;

            var won = await repository.TryConsumeAsync(
                session.Id,
                DateTime.UtcNow,
                replacementId);

            return (won, replacementId);
        }

        var first = AttemptAsync();
        var second = AttemptAsync();
        gate.SetResult();

        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(result => result.Won));

        var winner = results.Single(result => result.Won);

        await using var verify = _database.CreateContext();
        var reloaded = await verify.UserSessions
            .AsNoTracking()
            .SingleAsync(item => item.Id == session.Id);

        Assert.NotNull(reloaded.RevokedAtUtc);
        Assert.Equal(SessionRevocationReasons.Rotated, reloaded.RevocationReason);
        Assert.Equal(winner.ReplacementId, reloaded.ReplacedBySessionId);
    }

    [Fact]
    public async Task Rotation_PreservesAbsoluteExpiry_AndUpdatesIdleTimestamp()
    {
        var userId = await SeedUserAsync("auth.rotation");
        var now = DateTime.UtcNow;
        var absoluteExpiry = now.AddDays(10);

        var firstToken = TokenService.CreateToken();
        var first = NewSession(userId, firstToken, expires: absoluteExpiry);
        first.LastUsedAtUtc = now.AddHours(-2);

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.Add(first);
            await seed.SaveChangesAsync();
        }

        var secondToken = TokenService.CreateToken();
        var second = NewSession(
            userId,
            secondToken,
            familyId: first.TokenFamilyId,
            expires: first.ExpiresAtUtc);
        second.CreatedAtUtc = now;
        second.LastUsedAtUtc = now;

        await using (var rotate = _database.CreateContext())
        {
            var repository = new UserSessionRepository(rotate);
            var consumed = await repository.TryConsumeAsync(
                first.Id, now, second.Id);

            Assert.True(consumed);

            await repository.AddAsync(second);
            await rotate.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();

        var storedFirst = await verify.UserSessions
            .AsNoTracking()
            .SingleAsync(item => item.Id == first.Id);

        var storedSecond = await verify.UserSessions
            .AsNoTracking()
            .SingleAsync(item => item.Id == second.Id);

        Assert.Equal(absoluteExpiry, storedFirst.ExpiresAtUtc);
        Assert.Equal(absoluteExpiry, storedSecond.ExpiresAtUtc);
        Assert.Equal(first.TokenFamilyId, storedSecond.TokenFamilyId);
        Assert.Equal(second.Id, storedFirst.ReplacedBySessionId);
        Assert.True(storedSecond.LastUsedAtUtc > storedFirst.LastUsedAtUtc);
        Assert.Null(storedSecond.RevokedAtUtc);
    }

    [Fact]
    public async Task FailedReplacementInsert_RollsBackConsumption()
    {
        var userId = await SeedUserAsync("auth.rollback");
        var familyId = Guid.NewGuid();

        var active = NewSession(userId, TokenService.CreateToken(), familyId);
        var unrelated = NewSession(userId, TokenService.CreateToken());

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.AddRange(active, unrelated);
            await seed.SaveChangesAsync();
        }

        Guid rejectedId;

        await using (var attempt = _database.CreateContext())
        {
            await using var transaction = await attempt.Database.BeginTransactionAsync();
            var repository = new UserSessionRepository(attempt);

            Assert.True(await repository.TryConsumeAsync(
                active.Id, DateTime.UtcNow, Guid.NewGuid()));

            rejectedId = Guid.NewGuid();

            // Force a unique-index violation on the replacement insert.
            var replacement = NewSession(
                userId,
                TokenService.CreateToken(),
                familyId);

            replacement.Id = rejectedId;
            replacement.RefreshTokenHash = unrelated.RefreshTokenHash;

            await repository.AddAsync(replacement);

            await Assert.ThrowsAsync<DbUpdateException>(
                () => attempt.SaveChangesAsync());

            await transaction.RollbackAsync();
        }

        await using var verify = _database.CreateContext();

        var reloaded = await verify.UserSessions
            .AsNoTracking()
            .SingleAsync(item => item.Id == active.Id);

        // The consumption was rolled back atomically with the failed insert.
        Assert.Null(reloaded.RevokedAtUtc);
        Assert.False(
            await verify.UserSessions.AsNoTracking()
                .AnyAsync(item => item.Id == rejectedId));
    }

    [Fact]
    public async Task RevokeFamily_RevokesEveryGeneration_AndLeavesOtherFamiliesActive()
    {
        var userId = await SeedUserAsync("auth.family");

        var familyOne = Guid.NewGuid();
        var familyTwo = Guid.NewGuid();

        var first = NewSession(userId, TokenService.CreateToken(), familyOne);
        var second = NewSession(userId, TokenService.CreateToken(), familyOne);
        var other = NewSession(userId, TokenService.CreateToken(), familyTwo);

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.AddRange(first, second, other);
            await seed.SaveChangesAsync();
        }

        await using (var revoke = _database.CreateContext())
        {
            var repository = new UserSessionRepository(revoke);
            var affected = await repository.RevokeFamilyAsync(
                familyOne,
                DateTime.UtcNow,
                SessionRevocationReasons.ReuseDetected);

            Assert.Equal(2, affected);
        }

        await using var verify = _database.CreateContext();

        Assert.All(
            await verify.UserSessions.AsNoTracking()
                .Where(item => item.TokenFamilyId == familyOne)
                .ToListAsync(),
            item =>
            {
                Assert.NotNull(item.RevokedAtUtc);
                Assert.Equal(SessionRevocationReasons.ReuseDetected, item.RevocationReason);
            });

        var untouched = await verify.UserSessions
            .AsNoTracking()
            .SingleAsync(item => item.Id == other.Id);

        Assert.Null(untouched.RevokedAtUtc);
    }

    [Fact]
    public async Task RevokeAllForUser_DoesNotAffectOtherUsers()
    {
        var targetUserId = await SeedUserAsync("auth.target");
        var otherUserId = await SeedUserAsync("auth.other");

        var targetSession = NewSession(targetUserId, TokenService.CreateToken());
        var otherSession = NewSession(otherUserId, TokenService.CreateToken());

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.AddRange(targetSession, otherSession);
            await seed.SaveChangesAsync();
        }

        await using (var revoke = _database.CreateContext())
        {
            var repository = new UserSessionRepository(revoke);
            await repository.RevokeAllForUserAsync(
                targetUserId,
                DateTime.UtcNow,
                SessionRevocationReasons.LogoutAll);
        }

        await using var verify = _database.CreateContext();

        Assert.NotNull(
            (await verify.UserSessions.AsNoTracking()
                .SingleAsync(item => item.Id == targetSession.Id)).RevokedAtUtc);

        Assert.Null(
            (await verify.UserSessions.AsNoTracking()
                .SingleAsync(item => item.Id == otherSession.Id)).RevokedAtUtc);
    }

    [Fact]
    public async Task LogoutByHash_RevokesOnlyTheMatchingSession()
    {
        var userId = await SeedUserAsync("auth.logout");
        var rawToken = TokenService.CreateToken();
        var target = NewSession(userId, rawToken);
        var sibling = NewSession(userId, TokenService.CreateToken());

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.AddRange(target, sibling);
            await seed.SaveChangesAsync();
        }

        await using (var revoke = _database.CreateContext())
        {
            var repository = new UserSessionRepository(revoke);
            var affected = await repository.RevokeByTokenHashAsync(
                TokenService.HashToken(rawToken),
                DateTime.UtcNow,
                SessionRevocationReasons.Logout);

            Assert.Equal(1, affected);
        }

        await using var verify = _database.CreateContext();

        Assert.Equal(
            SessionRevocationReasons.Logout,
            (await verify.UserSessions.AsNoTracking()
                .SingleAsync(item => item.Id == target.Id)).RevocationReason);

        Assert.Null(
            (await verify.UserSessions.AsNoTracking()
                .SingleAsync(item => item.Id == sibling.Id)).RevokedAtUtc);
    }

    [Fact]
    public async Task UserDeletion_CascadesSessions()
    {
        var userId = await SeedUserAsync("auth.cascade");
        var session = NewSession(userId, TokenService.CreateToken());

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.Add(session);
            await seed.SaveChangesAsync();
        }

        await using (var delete = _database.CreateContext())
        {
            var user = await delete.Users.SingleAsync(item => item.UserId == userId);
            delete.Users.Remove(user);
            await delete.SaveChangesAsync();
        }

        await using var verify = _database.CreateContext();

        Assert.False(
            await verify.UserSessions.AsNoTracking()
                .AnyAsync(item => item.Id == session.Id));
    }

    [Fact]
    public async Task SecurityStamp_Persists_AndRotates()
    {
        var userId = await SeedUserAsync("auth.stamp");
        var originalStamp = await ReadSecurityStampAsync(userId);

        Assert.False(string.IsNullOrWhiteSpace(originalStamp));

        await using (var update = _database.CreateContext())
        {
            var user = await update.Users.SingleAsync(item => item.UserId == userId);
            user.SecurityStamp = "rotated-stamp";
            await update.SaveChangesAsync();
        }

        Assert.Equal("rotated-stamp", await ReadSecurityStampAsync(userId));
        Assert.NotEqual("rotated-stamp", originalStamp);
    }

    private async Task<int> SeedUserAsync(string userName)
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
            FirstName = "Auth",
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

        var user = new User
        {
            PersonId = person.PersonId,
            UserName = userName,
            Password = "not-a-real-hash",
            IsActive = true,
            Role = UserRole.Staff
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.UserId;
    }

    private async Task<string> ReadSecurityStampAsync(int userId)
    {
        await using var context = _database.CreateContext();

        return await context.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId)
            .Select(user => user.SecurityStamp)
            .SingleAsync();
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

    private static async Task<int> ScalarAsync(
        SqlConnection connection,
        string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
}
