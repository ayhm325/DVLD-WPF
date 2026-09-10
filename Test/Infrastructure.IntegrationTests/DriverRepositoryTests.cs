using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class DriverRepositoryTests
    : IClassFixture<DriverRepositoryDatabaseFixture>
{
    private readonly DriverRepositoryDatabaseFixture _fixture;

    public DriverRepositoryTests(
        DriverRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DriverRepository(null!));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExistingDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.AdminDriverId,
            result.DriverID);

        Assert.Equal(
            _fixture.AdminPersonId,
            result.PersonID);

        Assert.Equal(
            _fixture.AdminUserId,
            result.CreatedByUserID);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLoadPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);
        Assert.NotNull(result.Person);

        Assert.Equal(
            _fixture.AdminPersonId,
            result.Person.PersonId);

        Assert.Equal(
            "Admin",
            result.Person.FirstName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLoadCreatedByUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);
        Assert.NotNull(result.CreatedByUser);

        Assert.Equal(
            _fixture.AdminUserId,
            result.CreatedByUser.UserId);

        Assert.Equal(
            "driver.repository.admin",
            result.CreatedByUser.UserName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLoadOnlyActiveLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);

        Assert.Single(result.Licenses);

        var license =
            result.Licenses.Single();

        Assert.Equal(
            _fixture.ActiveLicenseId,
            license.LicenseID);

        Assert.True(
            license.IsActive);

        Assert.DoesNotContain(
            result.Licenses,
            x =>
                x.LicenseID ==
                _fixture.InactiveLicenseId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDetachedEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result.Person).State);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result.CreatedByUser).State);

        Assert.All(
            result.Licenses,
            license =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(license).State));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenDriverDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByIdAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnExistingDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                _fixture.StaffDriverId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffDriverId,
            result.DriverID);

        Assert.Equal(
            _fixture.StaffPersonId,
            result.PersonID);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldTrackDriverAndIncludedNavigations()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result).State);

        Assert.NotNull(result.Person);
        Assert.NotNull(result.CreatedByUser);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result.Person).State);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result.CreatedByUser).State);

        Assert.Single(result.Licenses);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(
                result.Licenses.Single()).State);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnOnlyActiveLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);

        Assert.Single(result.Licenses);

        Assert.Equal(
            _fixture.ActiveLicenseId,
            result.Licenses.Single().LicenseID);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnNullWhenDriverDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnNullForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForUpdateAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnNullForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForUpdateAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldReturnExistingDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForDeleteAsync(
                _fixture.DeleteDriverId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.DeleteDriverId,
            result.DriverID);
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldLoadAllLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForDeleteAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);

        Assert.Equal(
            2,
            result.Licenses.Count);

        Assert.Contains(
            result.Licenses,
            x =>
                x.LicenseID ==
                _fixture.ActiveLicenseId);

        Assert.Contains(
            result.Licenses,
            x =>
                x.LicenseID ==
                _fixture.InactiveLicenseId);
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldLoadInternationalLicensesCollection()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForDeleteAsync(
                _fixture.AdminDriverId);

        Assert.NotNull(result);

        Assert.NotNull(
            result.InternationalLicenses);

        Assert.Empty(
            result.InternationalLicenses);
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldReturnNullWhenDriverDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForDeleteAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldReturnNullForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForDeleteAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetForDeleteAsync_ShouldReturnNullForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetForDeleteAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSeededDriversOrderedByIdAscending()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetAllAsync();

        Assert.Contains(
            result,
            driver =>
                driver.DriverID ==
                _fixture.AdminDriverId);

        Assert.Contains(
            result,
            driver =>
                driver.DriverID ==
                _fixture.StaffDriverId);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i - 1].DriverID <
                result[i].DriverID);
        }
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadBasicInfoAndOnlyActiveLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetAllAsync();

        Assert.NotEmpty(result);

        var adminDriver =
            result.Single(
                x =>
                    x.DriverID ==
                    _fixture.AdminDriverId);

        Assert.NotNull(adminDriver.Person);
        Assert.NotNull(adminDriver.CreatedByUser);

        Assert.Single(
            adminDriver.Licenses);

        Assert.Equal(
            _fixture.ActiveLicenseId,
            adminDriver.Licenses.Single().LicenseID);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDetachedEntities()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetAllAsync();

        Assert.NotEmpty(result);

        Assert.All(
            result,
            driver =>
            {
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(driver).State);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(driver.Person).State);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(driver.CreatedByUser).State);

                Assert.All(
                    driver.Licenses,
                    license =>
                        Assert.Equal(
                            EntityState.Detached,
                            context.Entry(license).State));
            });
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnExistingDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByPersonIdAsync(
                _fixture.StaffPersonId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffDriverId,
            result.DriverID);

        Assert.Equal(
            _fixture.StaffPersonId,
            result.PersonID);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldLoadBasicInfo()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByPersonIdAsync(
                _fixture.StaffPersonId);

        Assert.NotNull(result);

        Assert.NotNull(result.Person);
        Assert.NotNull(result.CreatedByUser);

        Assert.Equal(
            _fixture.StaffPersonId,
            result.Person.PersonId);

        Assert.Equal(
            _fixture.StaffUserId,
            result.CreatedByUser.UserId);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnNullWhenPersonHasNoDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByPersonIdAsync(
                _fixture.UnassignedPersonId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnNullForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByPersonIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnNullForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByPersonIdAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_ShouldReturnDriversCreatedByUserOrderedById()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByCreatedUserIdAsync(
                _fixture.AdminUserId);

        Assert.Contains(
            result,
            driver =>
                driver.DriverID ==
                _fixture.AdminDriverId);

        Assert.Contains(
            result,
            driver =>
                driver.DriverID ==
                _fixture.SecondAdminDriverId);

        Assert.DoesNotContain(
            result,
            driver =>
                driver.DriverID ==
                _fixture.StaffDriverId);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i - 1].DriverID <
                result[i].DriverID);
        }
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_ShouldReturnBasicInfoAndOnlyActiveLicenses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByCreatedUserIdAsync(
                _fixture.AdminUserId);

        Assert.NotEmpty(result);

        var driver =
            result.Single(
                x =>
                    x.DriverID ==
                    _fixture.AdminDriverId);

        Assert.NotNull(driver.Person);
        Assert.NotNull(driver.CreatedByUser);

        Assert.Single(
            driver.Licenses);

        Assert.Equal(
            _fixture.ActiveLicenseId,
            driver.Licenses.Single().LicenseID);
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_ShouldReturnEmptyForUserWithoutDrivers()
    {
        var newPersonId =
            await _fixture.AddPersonAsync(
                "NoDriver",
                "User",
                $"NODRIVER-{Guid.NewGuid():N}");

        var newUserId =
            await _fixture.AddUserAsync(
                newPersonId,
                $"no-driver-{Guid.NewGuid():N}");

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByCreatedUserIdAsync(
                newUserId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_ShouldReturnEmptyForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByCreatedUserIdAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByCreatedUserIdAsync_ShouldReturnEmptyForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.GetByCreatedUserIdAsync(-1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExistsByIdAsync_ShouldReturnTrueForExistingDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByIdAsync(
                _fixture.AdminDriverId);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByIdAsync_ShouldReturnFalseForMissingDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByIdAsync(
                int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByIdAsync_ShouldReturnFalseForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByIdAsync(0);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByIdAsync_ShouldReturnFalseForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByIdAsync(-1);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByPersonIdAsync_ShouldReturnTrueForExistingDriverPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByPersonIdAsync(
                _fixture.AdminPersonId);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByPersonIdAsync_ShouldReturnFalseWhenPersonHasNoDriver()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByPersonIdAsync(
                _fixture.UnassignedPersonId);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByPersonIdAsync_ShouldReturnFalseForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByPersonIdAsync(0);

        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByPersonIdAsync_ShouldReturnFalseForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var result =
            await repository.ExistsByPersonIdAsync(-1);

        Assert.False(result);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistDriver()
    {
        var personId =
            await _fixture.AddPersonAsync(
                "Added",
                "Driver",
                $"ADD-{Guid.NewGuid():N}");

        var driver =
            new Driver
            {
                PersonID =
                    personId,

                CreatedByUserID =
                    _fixture.AdminUserId,

                CreatedDate =
                    DateTime.UtcNow
            };

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new DriverRepository(context);

            await repository.AddAsync(driver);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var persistedDriver =
            await verificationContext.Drivers
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.PersonID ==
                        personId);

        Assert.NotNull(persistedDriver);

        Assert.Equal(
            _fixture.AdminUserId,
            persistedDriver.CreatedByUserID);

        Assert.Equal(
            driver.CreatedDate,
            persistedDriver.CreatedDate);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowWhenDriverIsNull()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.AddAsync(null!));
    }

    [Fact]
    public async Task Delete_ShouldMarkDriverAsDeleted()
    {
        var personId =
            await _fixture.AddPersonAsync(
                "Delete",
                "Driver",
                $"DELETE-{Guid.NewGuid():N}");

        var driverId =
            await _fixture.AddDriverAsync(
                personId,
                _fixture.AdminUserId);

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        var driver =
            await context.Drivers
                .SingleAsync(
                    x =>
                        x.DriverID ==
                        driverId);

        repository.Delete(driver);

        Assert.Equal(
            EntityState.Deleted,
            context.Entry(driver).State);
    }

    [Fact]
    public async Task Delete_ShouldNotPersistDeletionBeforeSaveChanges()
    {
        var personId =
            await _fixture.AddPersonAsync(
                "Delete2",
                "Driver",
                $"DELETE2-{Guid.NewGuid():N}");

        var driverId =
            await _fixture.AddDriverAsync(
                personId,
                _fixture.AdminUserId);

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new DriverRepository(context);

            var driver =
                await context.Drivers
                    .SingleAsync(
                        x =>
                            x.DriverID ==
                            driverId);

            repository.Delete(driver);

            Assert.Equal(
                EntityState.Deleted,
                context.Entry(driver).State);
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var stillExists =
            await verificationContext.Drivers
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.DriverID ==
                        driverId);

        Assert.True(stillExists);

        await using var cleanupContext =
            _fixture.Database.CreateContext();

        var cleanupRepository =
            new DriverRepository(cleanupContext);

        var cleanupDriver =
            await cleanupContext.Drivers
                .SingleAsync(
                    x =>
                        x.DriverID ==
                        driverId);

        cleanupRepository.Delete(
            cleanupDriver);

        await cleanupContext.SaveChangesAsync();
    }

    [Fact]
    public void Delete_ShouldThrowWhenDriverIsNull()
    {
        using var context =
            _fixture.Database.CreateContext();

        var repository =
            new DriverRepository(context);

        Assert.Throws<ArgumentNullException>(
            () => repository.Delete(null!));
    }
}

public sealed class DriverRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int CountryId { get; private set; }

    public int ApplicationTypeId { get; private set; }

    public int LicenseClassId { get; private set; }

    public int AdminPersonId { get; private set; }

    public int StaffPersonId { get; private set; }

    public int UnassignedPersonId { get; private set; }

    public int AdminUserId { get; private set; }

    public int StaffUserId { get; private set; }

    public int AdminDriverId { get; private set; }

    public int StaffDriverId { get; private set; }

    public int SecondAdminDriverId { get; private set; }

    public int DeleteDriverId { get; private set; }

    public int ActiveLicenseId { get; private set; }

    public int InactiveLicenseId { get; private set; }

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
                    nationalNo[..Math.Min(
                        nationalNo.Length,
                        20)],

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
                    "Driver Repository Test Address",

                Phone =
                    $"07{Random.Shared.Next(
                        10000000,
                        99999999)}",

                Email =
                    $"{Guid.NewGuid():N}@example.com",

                NationalityCountryID =
                    CountryId
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
                    username[..Math.Min(
                        username.Length,
                        50)],

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

    public async Task<int> AddDriverAsync(
        int personId,
        int createdByUserId)
    {
        await using var context =
            Database.CreateContext();

        var driver =
            new Driver
            {
                PersonID =
                    personId,

                CreatedByUserID =
                    createdByUserId,

                CreatedDate =
                    DateTime.UtcNow
            };

        context.Drivers.Add(driver);

        await context.SaveChangesAsync();

        return driver.DriverID;
    }

    private async Task SeedAsync()
    {
        await using var context =
            Database.CreateContext();

        await SeedCountryAsync(context);

        await SeedApplicationTypeAsync(context);

        await SeedLicenseClassAsync(context);

        var adminPerson =
            new Person
            {
                NationalNo =
                    "DRIVER-ADMIN-PERSON",

                FirstName =
                    "Admin",

                SecondName =
                    "Driver",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1985,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Admin Driver Address",

                Phone =
                    "0791111111",

                Email =
                    "driver.admin.person@example.com",

                NationalityCountryID =
                    CountryId
            };

        var staffPerson =
            new Person
            {
                NationalNo =
                    "DRIVER-STAFF-PERSON",

                FirstName =
                    "Staff",

                SecondName =
                    "Driver",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Staff Driver Address",

                Phone =
                    "0792222222",

                Email =
                    "driver.staff.person@example.com",

                NationalityCountryID =
                    CountryId
            };

        var unassignedPerson =
            new Person
            {
                NationalNo =
                    "DRIVER-NO-DRIVER",

                FirstName =
                    "Unassigned",

                SecondName =
                    "Driver",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1995,
                        1,
                        1),

                Gender =
                    Gender.Female,

                Address =
                    "Unassigned Driver Address",

                Phone =
                    "0793333333",

                Email =
                    "driver.unassigned.person@example.com",

                NationalityCountryID =
                    CountryId
            };

        context.People.AddRange(
            adminPerson,
            staffPerson,
            unassignedPerson);

        await context.SaveChangesAsync();

        AdminPersonId =
            adminPerson.PersonId;

        StaffPersonId =
            staffPerson.PersonId;

        UnassignedPersonId =
            unassignedPerson.PersonId;

        var adminUser =
            new User
            {
                PersonId =
                    AdminPersonId,

                UserName =
                    "driver.repository.admin",

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
                    StaffPersonId,

                UserName =
                    "driver.repository.staff",

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

        AdminUserId =
            adminUser.UserId;

        StaffUserId =
            staffUser.UserId;

        var adminDriver =
            new Driver
            {
                PersonID =
                    AdminPersonId,

                CreatedByUserID =
                    AdminUserId,

                CreatedDate =
                    new DateTime(
                        2026,
                        1,
                        1)
            };

        var staffDriver =
            new Driver
            {
                PersonID =
                    StaffPersonId,

                CreatedByUserID =
                    StaffUserId,

                CreatedDate =
                    new DateTime(
                        2026,
                        1,
                        2)
            };

        context.Drivers.AddRange(
            adminDriver,
            staffDriver);

        await context.SaveChangesAsync();

        AdminDriverId =
            adminDriver.DriverID;

        StaffDriverId =
            staffDriver.DriverID;

        var secondAdminPerson =
            new Person
            {
                NationalNo =
                    "DRIVER-SECOND-ADMIN",

                FirstName =
                    "Second",

                SecondName =
                    "Admin",

                LastName =
                    "Driver",

                DateOfBirth =
                    new DateTime(
                        1988,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Second Admin Driver Address",

                Phone =
                    "0794444444",

                Email =
                    "driver.second.admin@example.com",

                NationalityCountryID =
                    CountryId
            };

        await context.People.AddAsync(
            secondAdminPerson);

        await context.SaveChangesAsync();

        var secondAdminDriver =
            new Driver
            {
                PersonID =
                    secondAdminPerson.PersonId,

                CreatedByUserID =
                    AdminUserId,

                CreatedDate =
                    new DateTime(
                        2026,
                        1,
                        3)
            };

        context.Drivers.Add(
            secondAdminDriver);

        await context.SaveChangesAsync();

        SecondAdminDriverId =
            secondAdminDriver.DriverID;

        var deletePerson =
            new Person
            {
                NationalNo =
                    "DRIVER-DELETE-PERSON",

                FirstName =
                    "Delete",

                SecondName =
                    "Driver",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1992,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Delete Driver Address",

                Phone =
                    "0795555555",

                Email =
                    "driver.delete@example.com",

                NationalityCountryID =
                    CountryId
            };

        await context.People.AddAsync(
            deletePerson);

        await context.SaveChangesAsync();

        var deleteDriver =
            new Driver
            {
                PersonID =
                    deletePerson.PersonId,

                CreatedByUserID =
                    AdminUserId,

                CreatedDate =
                    new DateTime(
                        2026,
                        1,
                        4)
            };

        context.Drivers.Add(
            deleteDriver);

        await context.SaveChangesAsync();

        DeleteDriverId =
            deleteDriver.DriverID;

        await SeedLicensesAsync(
            context);

        Assert.True(
            AdminDriverId > 0);

        Assert.True(
            StaffDriverId >
            AdminDriverId);

        Assert.True(
            SecondAdminDriverId >
            StaffDriverId);

        Assert.True(
            DeleteDriverId >
            SecondAdminDriverId);

        Assert.True(
            ActiveLicenseId > 0);

        Assert.True(
            InactiveLicenseId > 0);
    }

    private async Task SeedLicensesAsync(
        DVLDDbContext context)
    {
        var activeApplication =
            new ApplicationD
            {
                ApplicantPersonID =
                    AdminPersonId,

                ApplicationDate =
                    new DateTime(
                        2026,
                        2,
                        1),

                ApplicationTypeID =
                    ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    new DateTime(
                        2026,
                        2,
                        1),

                PaidFees =
                    100m,

                CreatedByUserID =
                    AdminUserId
            };

        var inactiveApplication =
            new ApplicationD
            {
                ApplicantPersonID =
                    AdminPersonId,

                ApplicationDate =
                    new DateTime(
                        2026,
                        2,
                        2),

                ApplicationTypeID =
                    ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    new DateTime(
                        2026,
                        2,
                        2),

                PaidFees =
                    100m,

                CreatedByUserID =
                    AdminUserId
            };

        context.Applications.AddRange(
            activeApplication,
            inactiveApplication);

        await context.SaveChangesAsync();

        var activeLicense =
            new License
            {
                ApplicationID =
                    activeApplication.ApplicationID,

                DriverID =
                    AdminDriverId,

                LicenseClass =
                    LicenseClassId,

                IssueDate =
                    new DateTime(
                        2026,
                        2,
                        1),

                ExpirationDate =
                    new DateTime(
                        2036,
                        2,
                        1),

                Notes =
                    "Active driver repository test license",

                PaidFees =
                    100m,

                IsActive =
                    true,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    AdminUserId
            };

        var inactiveLicense =
            new License
            {
                ApplicationID =
                    inactiveApplication.ApplicationID,

                DriverID =
                    AdminDriverId,

                LicenseClass =
                    LicenseClassId,

                IssueDate =
                    new DateTime(
                        2025,
                        2,
                        1),

                ExpirationDate =
                    new DateTime(
                        2035,
                        2,
                        1),

                Notes =
                    "Inactive driver repository test license",

                PaidFees =
                    100m,

                IsActive =
                    false,

                IssueReason =
                    IssueReason.Renew,

                CreatedByUserID =
                    AdminUserId
            };

        context.Licenses.AddRange(
            activeLicense,
            inactiveLicense);

        await context.SaveChangesAsync();

        ActiveLicenseId =
            activeLicense.LicenseID;

        InactiveLicenseId =
            inactiveLicense.LicenseID;
    }

    private async Task SeedCountryAsync(
        DVLDDbContext context)
    {
        var country =
            await context.Countries
                .OrderBy(x => x.CountryId)
                .FirstOrDefaultAsync();

        if (country is null)
        {
            country =
                new Country
                {
                    CountryName =
                        $"Driver Repository Country {Guid.NewGuid():N}"
                };

            context.Countries.Add(
                country);

            await context.SaveChangesAsync();
        }

        CountryId =
            country.CountryId;
    }

    private async Task SeedApplicationTypeAsync(
        DVLDDbContext context)
    {
        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Driver Repository Application {Guid.NewGuid():N}"[..50],

                ApplicationFees =
                    100m
            };

        context.ApplicationTypes.Add(
            applicationType);

        await context.SaveChangesAsync();

        ApplicationTypeId =
            applicationType.ApplicationTypeId;
    }

    private async Task SeedLicenseClassAsync(
        DVLDDbContext context)
    {
        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    $"Driver Repository Class {Guid.NewGuid():N}"[..50],

                ClassDescription =
                    "Driver repository integration test class",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    10,

                ClassFees =
                    100m
            };

        context.LicenseClasses.Add(
            licenseClass);

        await context.SaveChangesAsync();

        LicenseClassId =
            licenseClass.LicenseClassID;
    }
}