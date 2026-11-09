using Application.Common.Results;
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

namespace Infrastructure.IntegrationTests.Concurrency;

/// <summary>
/// CONCURRENCY-01: the last-active-administrator invariant must survive two
/// concurrent demotions executed through independent contexts and transactions.
/// </summary>
public sealed class LastAdministratorConcurrencyTests
    : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public LastAdministratorConcurrencyTests(SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task TwoConcurrentAdminDemotions_LeaveAtLeastOneActiveAdministrator()
    {
        var (firstAdmin, secondAdmin) = await SeedTwoAdministratorsAsync();

        var gate = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Result> DemoteAsync(
            int actorId,
            User target,
            CancellationToken cancellationToken)
        {
            await using var context = _database.CreateContext();

            var service = new UserService(
                new UserRepository(context),
                new UnitOfWork(context),
                new UserSessionRepository(context),
                new JordanClock(),
                new TestCurrentUser(actorId));

            await gate.Task;

            return await service.UpdateUserAsync(
                target.UserId,
                new UpdateUserDto
                {
                    UserName = target.UserName,
                    PersonId = target.PersonId,
                    IsActive = true,
                    Role = UserRole.Staff
                },
                cancellationToken);
        }

        var first = DemoteAsync(firstAdmin.UserId, secondAdmin, CancellationToken.None);
        var second = DemoteAsync(secondAdmin.UserId, firstAdmin, CancellationToken.None);

        gate.SetResult();

        var results = await Task.WhenAll(first, second);

        Assert.True(
            results.Count(result => result.IsSuccess) == 1,
            string.Join(
                " | ",
                results.Select(result => $"{result.ErrorType}:{result.Error}")));

        var loser = results.Single(result => !result.IsSuccess);

        Assert.True(
            loser.ErrorType == ErrorType.Conflict,
            $"Expected a conflict for the losing demotion, got {loser.ErrorType}: {loser.Error}");

        await using var verify = _database.CreateContext();

        var activeAdmins = await verify.Users
            .AsNoTracking()
            .CountAsync(user =>
                (user.UserId == firstAdmin.UserId || user.UserId == secondAdmin.UserId) &&
                user.Role == UserRole.Admin &&
                user.IsActive);

        Assert.Equal(1, activeAdmins);

        var demoted = await verify.Users
            .AsNoTracking()
            .Where(user =>
                user.UserId == firstAdmin.UserId ||
                user.UserId == secondAdmin.UserId)
            .ToListAsync();

        Assert.Equal(1, demoted.Count(user => user.Role == UserRole.Staff));
        Assert.Equal(1, demoted.Count(user => user.Role == UserRole.Admin));
    }

    [Fact]
    public async Task ConcurrentAdminDemotion_WhenOnlyOneAdminRemains_RejectsWithoutMutation()
    {
        var (admin, second) = await SeedTwoAdministratorsAsync();

        // Demote the second administrator sequentially so exactly one remains.
        await using (var context = _database.CreateContext())
        {
            var secondary = await context.Users
                .SingleAsync(user => user.UserId == second.UserId);

            secondary.Role = UserRole.Staff;
            await context.SaveChangesAsync();
        }

        await using (var context = _database.CreateContext())
        {
            var service = new UserService(
                new UserRepository(context),
                new UnitOfWork(context),
                new UserSessionRepository(context),
                new JordanClock(),
                new TestCurrentUser(999_999));

            var result = await service.UpdateUserAsync(
                admin.UserId,
                new UpdateUserDto
                {
                    UserName = admin.UserName,
                    PersonId = admin.PersonId,
                    IsActive = false,
                    Role = UserRole.Admin
                });

            Assert.Equal(ErrorType.Conflict, result.ErrorType);
        }

        await using var verify = _database.CreateContext();

        Assert.Equal(
            1,
            await verify.Users.AsNoTracking()
                .CountAsync(user =>
                    (user.UserId == admin.UserId || user.UserId == second.UserId) &&
                    user.Role == UserRole.Admin &&
                    user.IsActive));
    }

    private async Task<(User First, User Second)> SeedTwoAdministratorsAsync()
    {
        await using var context = _database.CreateContext();

        // Tests in this class share one ephemeral database. Normalize the security
        // state so exactly two active administrators exist for this scenario.
        await context.Users
            .Where(user => user.Role == UserRole.Admin && user.IsActive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Role, UserRole.Staff));

        var suffix = Guid.NewGuid().ToString("N")[..8];

        // Consume the first identity values so the scenario administrators never
        // collide with the hardcoded primary-administrator rule (UserId == 2).
        await CreateUserAsync(context, $"staff_one_{suffix}", UserRole.Staff);
        await CreateUserAsync(context, $"staff_two_{suffix}", UserRole.Staff);

        var first = await CreateUserAsync(context, $"admin_one_{suffix}", UserRole.Admin);
        var second = await CreateUserAsync(context, $"admin_two_{suffix}", UserRole.Admin);

        return (first, second);
    }

    private static async Task<User> CreateUserAsync(
        DVLDDbContext context,
        string userName,
        UserRole role)
    {
        var country = new Country { CountryName = "Country-" + userName };
        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var person = new Person
        {
            NationalNo = "N" + Guid.NewGuid().ToString("N")[..9],
            FirstName = "Admin",
            SecondName = "Concurrent",
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
            Role = role
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private sealed class TestCurrentUser(int userId) : ICurrentUserService
    {
        public int UserId => userId;

        public string Username => "actor" + userId;

        public string FullName => "Concurrency Actor";

        public UserRole Role => UserRole.Admin;

        public string AccessToken => string.Empty;

        public bool IsLoggedIn => true;

        public void SetSession(
            int userId,
            string username,
            string fullName,
            UserRole role,
            string accessToken) =>
            throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();
    }
}
