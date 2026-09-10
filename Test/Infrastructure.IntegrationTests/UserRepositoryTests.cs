using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class UserRepositoryTests
    : IClassFixture<UserRepositoryDatabaseFixture>
{
    private readonly UserRepositoryDatabaseFixture _fixture;

    public UserRepositoryTests(
        UserRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new UserRepository(null!));
    }

    [Fact]
    public async Task GetUserByUserIdAsync_ShouldReturnExistingUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUserIdAsync(
                _fixture.AdminUserId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.AdminUserId,
            result.UserId);

        Assert.Equal(
            "admin.integration",
            result.UserName);

        Assert.Equal(
            UserRole.Admin,
            result.Role);
    }

    [Fact]
    public async Task GetUserByUserIdAsync_ShouldLoadPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUserIdAsync(
                _fixture.AdminUserId);

        Assert.NotNull(result);
        Assert.NotNull(result.Person);

        Assert.Equal(
            _fixture.AdminPersonId,
            result.PersonId);

        Assert.Equal(
            _fixture.AdminPersonId,
            result.Person.PersonId);

        Assert.Equal(
            "Admin",
            result.Person.FirstName);
    }

    [Fact]
    public async Task GetUserByUserIdAsync_ShouldReturnNullWhenUserDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUserIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByUserIdAsync_ShouldReturnDetachedEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUserIdAsync(
                _fixture.AdminUserId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result.Person).State);
    }

    [Fact]
    public async Task GetUserByPersonIdAsync_ShouldReturnExistingUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByPersonIdAsync(
                _fixture.StaffPersonId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffUserId,
            result.UserId);

        Assert.Equal(
            _fixture.StaffPersonId,
            result.PersonId);

        Assert.Equal(
            "staff.integration",
            result.UserName);
    }

    [Fact]
    public async Task GetUserByPersonIdAsync_ShouldReturnNullWhenPersonHasNoUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByPersonIdAsync(
                _fixture.UnassignedPersonId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByPersonIdAsync_ShouldLoadPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByPersonIdAsync(
                _fixture.StaffPersonId);

        Assert.NotNull(result);
        Assert.NotNull(result.Person);

        Assert.Equal(
            _fixture.StaffPersonId,
            result.Person.PersonId);

        Assert.Equal(
            "Staff",
            result.Person.FirstName);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ShouldReturnExistingUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUsernameAsync(
                "staff.integration");

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffUserId,
            result.UserId);

        Assert.Equal(
            "staff.integration",
            result.UserName);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ShouldFollowDatabaseCollationForUsernameComparison()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUsernameAsync(
                "STAFF.INTEGRATION");

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffUserId,
            result.UserId);

        Assert.Equal(
            "staff.integration",
            result.UserName);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ShouldReturnNullWhenUsernameDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUsernameAsync(
                "missing.integration");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByUsernameAsync_ShouldReturnDetachedEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserByUsernameAsync(
                "admin.integration");

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result.Person).State);
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnAllUsersOrderedByIdAscending()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetAllUsersAsync();

        Assert.Contains(
            result,
            user =>
                user.UserId ==
                _fixture.AdminUserId);

        Assert.Contains(
            result,
            user =>
                user.UserId ==
                _fixture.StaffUserId);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i - 1].UserId <
                result[i].UserId);
        }
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldLoadPersonForEveryUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetAllUsersAsync();

        Assert.All(
            result,
            user =>
            {
                Assert.NotNull(user.Person);
                Assert.True(user.PersonId > 0);
            });
    }

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnDetachedEntities()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetAllUsersAsync();

        Assert.All(
            result,
            user =>
            {
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(user).State);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(user.Person).State);
            });
    }

    [Fact]
    public async Task GetUserForUpdateAsync_ShouldReturnExistingUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserForUpdateAsync(
                _fixture.StaffUserId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffUserId,
            result.UserId);

        Assert.Equal(
            "staff.integration",
            result.UserName);
    }

    [Fact]
    public async Task GetUserForUpdateAsync_ShouldTrackEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserForUpdateAsync(
                _fixture.StaffUserId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetUserForUpdateAsync_ShouldReturnNullWhenUserDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.GetUserForUpdateAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task IsUsernameTakenAsync_ShouldReturnTrueWhenUsernameExists()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.IsUsernameTakenAsync(
                "admin.integration");

        Assert.True(result);
    }

    [Fact]
    public async Task IsUsernameTakenAsync_ShouldReturnFalseWhenUsernameDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.IsUsernameTakenAsync(
                "available.integration");

        Assert.False(result);
    }

    [Fact]
    public async Task IsUsernameTakenForAnotherUserAsync_ShouldReturnTrueForAnotherUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.IsUsernameTakenForAnotherUserAsync(
                "admin.integration",
                _fixture.StaffUserId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUsernameTakenForAnotherUserAsync_ShouldReturnFalseForSameUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.IsUsernameTakenForAnotherUserAsync(
                "admin.integration",
                _fixture.AdminUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsUsernameTakenForAnotherUserAsync_ShouldReturnFalseWhenUsernameDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.IsUsernameTakenForAnotherUserAsync(
                "missing.integration",
                _fixture.StaffUserId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsUserExistsByPersonIdAsync_ShouldReturnTrueWhenPersonHasUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.IsUserExistsByPersonIdAsync(
                _fixture.AdminPersonId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsUserExistsByPersonIdAsync_ShouldReturnFalseWhenPersonHasNoUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        var result =
            await repository.IsUserExistsByPersonIdAsync(
                _fixture.UnassignedPersonId);

        Assert.False(result);
    }

    [Fact]
    public async Task AddUserAsync_ShouldPersistUser()
    {
        var personId =
            await _fixture.AddPersonAsync(
                "Added",
                "User",
                "added.integration");

        var username =
            $"added.integration.{Guid.NewGuid():N}";

        var user =
            new User
            {
                PersonId = personId,
                UserName = username,
                Password = "hashed-password",
                IsActive = true,
                Role = UserRole.Staff
            };

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new UserRepository(context);

            await repository.AddUserAsync(user);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var persistedUser =
            await verificationContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.UserName == username);

        Assert.NotNull(persistedUser);

        Assert.Equal(
            personId,
            persistedUser.PersonId);

        Assert.Equal(
            username,
            persistedUser.UserName);

        Assert.Equal(
            UserRole.Staff,
            persistedUser.Role);
    }

    [Fact]
    public async Task AddUserAsync_ShouldThrowWhenUserIsNull()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.AddUserAsync(null!));
    }

    [Fact]
    public async Task DeleteUser_ShouldRemoveExistingUser()
    {
        var personId =
            await _fixture.AddPersonAsync(
                "Delete",
                "User",
                "delete.integration");

        var username = $"del-{Guid.NewGuid():N}";

        var userId =
            await _fixture.AddUserAsync(
                personId,
                username);

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new UserRepository(context);

            var user =
                await context.Users
                    .SingleAsync(
                        x => x.UserId == userId);

            repository.DeleteUser(user);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var deletedUser =
            await verificationContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.UserId == userId);

        Assert.Null(deletedUser);
    }

    [Fact]
    public void DeleteUser_ShouldThrowWhenUserIsNull()
    {
        using var context =
            _fixture.Database.CreateContext();

        var repository =
            new UserRepository(context);

        Assert.Throws<ArgumentNullException>(
            () => repository.DeleteUser(null!));
    }
}

public sealed class UserRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int AdminUserId { get; private set; }

    public int StaffUserId { get; private set; }

    public int AdminPersonId { get; private set; }

    public int StaffPersonId { get; private set; }

    public int UnassignedPersonId { get; private set; }

    public async Task InitializeAsync()
    {
        _database =
            new SqlServerTestDatabase();

        await _database.InitializeAsync();

        Database =
            _database;

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    public async Task<int> AddPersonAsync(
        string firstName,
        string lastName,
        string nationalNo)
    {
        await using var context =
            Database.CreateContext();

        var person =
            new Person
            {
                NationalNo =
                    nationalNo,

                FirstName =
                    firstName,

                SecondName =
                    "Integration",

                LastName =
                    lastName,

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Integration Test Address",

                Phone =
                    "0790000000",

                Email =
                    $"{nationalNo}@example.com",

                NationalityCountryID =
                    await GetCountryIdAsync(context)
            };

        context.People.Add(person);

        await context.SaveChangesAsync();

        return person.PersonId;
    }

    public async Task<int> AddUserAsync(
        int personId,
        string username)
    {
        await using var context =
            Database.CreateContext();

        var user =
            new User
            {
                PersonId =
                    personId,

                UserName =
                    username,

                Password =
                    "hashed-password",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        return user.UserId;
    }

    private async Task SeedAsync()
    {
        await using var context =
            Database.CreateContext();

        var countryId =
            await GetCountryIdAsync(context);

        var adminPerson =
            new Person
            {
                NationalNo =
                    "USER-TEST-ADMIN",

                FirstName =
                    "Admin",

                SecondName =
                    "Integration",

                LastName =
                    "User",

                DateOfBirth =
                    new DateTime(
                        1985,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Admin Integration Address",

                Phone =
                    "0791111111",

                Email =
                    "admin.integration@example.com",

                NationalityCountryID =
                    countryId
            };

        var staffPerson =
            new Person
            {
                NationalNo =
                    "USER-TEST-STAFF",

                FirstName =
                    "Staff",

                SecondName =
                    "Integration",

                LastName =
                    "User",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Staff Integration Address",

                Phone =
                    "0792222222",

                Email =
                    "staff.integration@example.com",

                NationalityCountryID =
                    countryId
            };

        var unassignedPerson =
            new Person
            {
                NationalNo =
                    "USER-TEST-NONE",

                FirstName =
                    "Unassigned",

                SecondName =
                    "Integration",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1995,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Unassigned Integration Address",

                Phone =
                    "0793333333",

                Email =
                    "unassigned.integration@example.com",

                NationalityCountryID =
                    countryId
            };

        context.People.AddRange(
            adminPerson,
            staffPerson,
            unassignedPerson);

        await context.SaveChangesAsync();

        var adminUser =
            new User
            {
                PersonId =
                    adminPerson.PersonId,

                UserName =
                    "admin.integration",

                Password =
                    "hashed-admin-password",

                IsActive =
                    true,

                Role =
                    UserRole.Admin
            };

        var staffUser =
            new User
            {
                PersonId =
                    staffPerson.PersonId,

                UserName =
                    "staff.integration",

                Password =
                    "hashed-staff-password",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.AddRange(
            adminUser,
            staffUser);

        await context.SaveChangesAsync();

        AdminPersonId =
            adminPerson.PersonId;

        StaffPersonId =
            staffPerson.PersonId;

        UnassignedPersonId =
            unassignedPerson.PersonId;

        AdminUserId =
            adminUser.UserId;

        StaffUserId =
            staffUser.UserId;

        Assert.True(AdminUserId > 0);
        Assert.True(StaffUserId > AdminUserId);
        Assert.True(AdminPersonId > 0);
        Assert.True(StaffPersonId > AdminPersonId);
        Assert.True(UnassignedPersonId > StaffPersonId);
    }

    private static async Task<int> GetCountryIdAsync(
        DVLDDbContext context)
    {
        var country =
            await context.Countries
                .OrderBy(x => x.CountryId)
                .FirstOrDefaultAsync();

        if (country is not null)
        {
            return country.CountryId;
        }

        country =
            new Country
            {
                CountryName =
                    $"Integration Test Country {Guid.NewGuid():N}"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        return country.CountryId;
    }
}