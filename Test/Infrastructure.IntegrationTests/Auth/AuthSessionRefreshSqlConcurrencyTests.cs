using System.Data;

using Application.Common.Results;
using Application.DTOs.AuthDTO;
using Application.Interfaces;
using Application.Options;
using Application.Services;

using Domain.Entities;
using Domain.Enums;

using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Infrastructure.IntegrationTests.Auth;

/// <summary>
/// AUTH-01-POST: real SQL Server concurrency for refresh rotation. Each attempt uses
/// its own DbContext, repositories, and UnitOfWork, so only the database atomic
/// conditional UPDATE can decide the winner.
/// </summary>
public sealed class AuthSessionRefreshSqlConcurrencyTests
    : IClassFixture<SqlServerTestDatabase>
{
    private static readonly IRefreshTokenService TokenService = new RefreshTokenService();

    private readonly SqlServerTestDatabase _database;

    public AuthSessionRefreshSqlConcurrencyTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task ConcurrentRefresh_OnSqlServer_ExactlyOneRotationWins()
    {
        var userId = await SeedUserAsync("auth.sql.concurrent");
        var rawToken = TokenService.CreateToken();
        var familyId = Guid.NewGuid();
        var session = NewSession(userId, rawToken, familyId);

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.Add(session);
            await seed.SaveChangesAsync();
        }

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Result<LoginResponseDto>> AttemptAsync()
        {
            await using var context = _database.CreateContext();
            var service = BuildAuthService(context, new MutableClock(DateTime.UtcNow));

            await gate.Task;

            return await service.RefreshAsync(rawToken);
        }

        var first = AttemptAsync();
        var second = AttemptAsync();
        gate.SetResult();

        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, results.Count(result => result.IsSuccess));
        Assert.Equal(1, results.Count(result => result.ErrorType == ErrorType.Conflict));

        await using var verify = _database.CreateContext();
        var family = await verify.UserSessions
            .AsNoTracking()
            .Where(item => item.TokenFamilyId == familyId)
            .ToListAsync();

        // Exactly the consumed generation and one replacement: no second chain.
        Assert.Equal(2, family.Count);
        Assert.Single(family.Where(item => item.RevokedAtUtc is null));

        var consumed = family.Single(item => item.Id == session.Id);
        Assert.NotNull(consumed.RevokedAtUtc);
        Assert.Equal(SessionRevocationReasons.Rotated, consumed.RevocationReason);
        Assert.NotNull(consumed.ReplacedBySessionId);

        var replacement = family.Single(item => item.Id != session.Id);
        Assert.Equal(consumed.ReplacedBySessionId, replacement.Id);
        Assert.Equal(session.ExpiresAtUtc, replacement.ExpiresAtUtc);
    }

    [Fact]
    public async Task ReuseAfterGrace_OnSqlServer_RevokesTheWholeFamily()
    {
        var userId = await SeedUserAsync("auth.sql.reuse");
        var initialToken = TokenService.CreateToken();
        var familyId = Guid.NewGuid();
        var session = NewSession(userId, initialToken, familyId);

        await using (var seed = _database.CreateContext())
        {
            seed.UserSessions.Add(session);
            await seed.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;

        LoginResponseDto rotated;
        await using (var rotateContext = _database.CreateContext())
        {
            var service = BuildAuthService(rotateContext, new MutableClock(now));
            var rotation = await service.RefreshAsync(initialToken);

            Assert.True(rotation.IsSuccess);
            rotated = rotation.Value!;
        }

        // Beyond the 30-second grace window: replaying the consumed token is theft.
        await using (var replayContext = _database.CreateContext())
        {
            var service = BuildAuthService(
                replayContext,
                new MutableClock(now.AddSeconds(61)));

            var replay = await service.RefreshAsync(initialToken);

            Assert.Equal(ErrorType.Unauthorized, replay.ErrorType);
        }

        await using var verify = _database.CreateContext();
        var family = await verify.UserSessions
            .AsNoTracking()
            .Where(item => item.TokenFamilyId == familyId)
            .ToListAsync();

        Assert.Equal(2, family.Count);
        Assert.All(family, item => Assert.NotNull(item.RevokedAtUtc));

        // The generation that was still active is revoked for reuse; the already
        // consumed generation keeps its original "Rotated" audit reason.
        Assert.Contains(
            family,
            item => item.RevocationReason == SessionRevocationReasons.ReuseDetected);

        // The legitimate replacement token is dead as well: re-authentication is required.
        await using (var afterContext = _database.CreateContext())
        {
            var service = BuildAuthService(
                afterContext,
                new MutableClock(now.AddSeconds(62)));

            var after = await service.RefreshAsync(rotated.RefreshToken);

            Assert.Equal(ErrorType.Unauthorized, after.ErrorType);
        }
    }

    private static AuthService BuildAuthService(
        DVLDDbContext context,
        IJordanClock clock) =>
        new(
            new UserRepository(context),
            new UserSessionRepository(context),
            new StubJwtTokenService(),
            TokenService,
            new UnitOfWork(context),
            clock,
            Options.Create(new AuthSessionOptions()),
            NullLogger<AuthService>.Instance);

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
            SecondName = "Sql",
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

    private static UserSession NewSession(
        int userId,
        string rawToken,
        Guid familyId)
    {
        var now = DateTime.UtcNow;

        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = TokenService.HashToken(rawToken),
            TokenFamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(14),
            LastUsedAtUtc = now
        };
    }

    private sealed class StubJwtTokenService : IJwtTokenService
    {
        public JwtTokenResult GenerateToken(JwtTokenRequest request) =>
            new()
            {
                AccessToken = "access-" + Guid.NewGuid().ToString("N"),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            };
    }

    private sealed class MutableClock : IJordanClock
    {
        public MutableClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }

        public DateTime Now =>
            Application.Common.Time.JordanTimeZoneProvider.ConvertUtcToJordan(UtcNow);

        public DateOnly Today => DateOnly.FromDateTime(Now);
    }
}
