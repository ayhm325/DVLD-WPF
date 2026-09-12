using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.IntegrationTests;

public sealed class DetainedLicenseRepositoryTests
    : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public DetainedLicenseRepositoryTests(
        SqlServerTestDatabase database)
    {
        _database = database;
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DetainedLicenseRepository(null!));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDetainedLicenses()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var older =
            await SeedDetainedLicenseAsync(
                scope.Context,
                detainDate: DateTime.UtcNow.AddDays(-5));

        var newer =
            await SeedDetainedLicenseAsync(
                scope.Context,
                detainDate: DateTime.UtcNow.AddDays(-1));

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        Assert.Equal(
            2,
            result.Count);

        Assert.Contains(
            result,
            x => x.DetainID == older.DetainId);

        Assert.Contains(
            result,
            x => x.DetainID == newer.DetainId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDetainedLicensesOrderedByDetainDateDescending()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var older =
            await SeedDetainedLicenseAsync(
                scope.Context,
                detainDate: DateTime.UtcNow.AddDays(-10));

        var newer =
            await SeedDetainedLicenseAsync(
                scope.Context,
                detainDate: DateTime.UtcNow.AddDays(-2));

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        var newerIndex =
            result.FindIndex(
                x => x.DetainID == newer.DetainId);

        var olderIndex =
            result.FindIndex(
                x => x.DetainID == older.DetainId);

        Assert.True(newerIndex >= 0);
        Assert.True(olderIndex >= 0);
        Assert.True(newerIndex < olderIndex);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadLicense()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        var detainedLicense =
            result.Single(
                x => x.DetainID == seed.DetainId);

        Assert.NotNull(
            detainedLicense.License);

        Assert.Equal(
            seed.LicenseId,
            detainedLicense.LicenseID);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadDriverAndPersonThroughLicense()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        var detainedLicense =
            result.Single(
                x => x.DetainID == seed.DetainId);

        Assert.NotNull(
            detainedLicense.License);

        Assert.NotNull(
            detainedLicense.License.Driver);

        Assert.NotNull(
            detainedLicense.License.Driver.Person);

        Assert.Equal(
            seed.PersonId,
            detainedLicense.License.Driver.Person.PersonId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadCreatedByUser()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        var detainedLicense =
            result.Single(
                x => x.DetainID == seed.DetainId);

        Assert.NotNull(
            detainedLicense.CreatedByUser);

        Assert.Equal(
            seed.CreatedByUserId,
            detainedLicense.CreatedByUser.UserId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadReleasedByUserWhenPresent()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        var detainedLicense =
            result.Single(
                x => x.DetainID == seed.DetainId);

        Assert.NotNull(
            detainedLicense.ReleasedByUser);

        Assert.Equal(
            seed.ReleasedByUserId,
            detainedLicense.ReleasedByUser!.UserId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadReleaseApplicationWhenPresent()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        var detainedLicense =
            result.Single(
                x => x.DetainID == seed.DetainId);

        Assert.NotNull(
            detainedLicense.ReleaseApplication);

        Assert.Equal(
            seed.ReleaseApplicationId,
            detainedLicense.ReleaseApplication!.ApplicationID);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDetachedGraph()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetAllAsync();

        var detainedLicense =
            result.Single(
                x => x.DetainID == seed.DetainId);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(detainedLicense).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(detainedLicense.License).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(detainedLicense.License.Driver).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                detainedLicense.License.Driver.Person).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                detainedLicense.CreatedByUser).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                detainedLicense.ReleasedByUser).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                detainedLicense.ReleaseApplication).State);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExistingDetainedLicense()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdAsync(
                seed.DetainId);

        Assert.NotNull(result);

        Assert.Equal(
            seed.DetainId,
            result.DetainID);

        Assert.Equal(
            seed.LicenseId,
            result.LicenseID);

        Assert.Equal(
            seed.FineFees,
            result.FineFees);

        Assert.Equal(
            seed.IsReleased,
            result.IsReleased);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenDetainedLicenseDoesNotExist()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId(
        int id)
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLoadAllExpectedRelatedData()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdAsync(
                seed.DetainId);

        Assert.NotNull(result);

        Assert.NotNull(
            result.License);

        Assert.NotNull(
            result.License.Driver);

        Assert.NotNull(
            result.License.Driver.Person);

        Assert.NotNull(
            result.CreatedByUser);

        Assert.NotNull(
            result.ReleasedByUser);

        Assert.NotNull(
            result.ReleaseApplication);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDetachedGraph()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdAsync(
                seed.DetainId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result.License).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result.License.Driver).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                result.License.Driver.Person).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                result.CreatedByUser).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                result.ReleasedByUser).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                result.ReleaseApplication).State);
    }

    // =========================================================
    // GET ACTIVE DETAIN BY LICENSE ID
    // =========================================================

    [Fact]
    public async Task GetActiveDetainByLicenseIdAsync_ShouldReturnActiveDetain()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: false);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetActiveDetainByLicenseIdAsync(
                seed.LicenseId);

        Assert.NotNull(result);

        Assert.Equal(
            seed.DetainId,
            result.DetainID);

        Assert.False(
            result.IsReleased);
    }

    [Fact]
    public async Task GetActiveDetainByLicenseIdAsync_ShouldReturnNullWhenDetainIsReleased()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetActiveDetainByLicenseIdAsync(
                seed.LicenseId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveDetainByLicenseIdAsync_ShouldReturnNullWhenLicenseHasNoActiveDetain()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetActiveDetainByLicenseIdAsync(
                seed.LicenseId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveDetainByLicenseIdAsync_ShouldReturnNullWhenLicenseDoesNotExist()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetActiveDetainByLicenseIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetActiveDetainByLicenseIdAsync_ShouldReturnNullForInvalidLicenseId(
        int licenseId)
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetActiveDetainByLicenseIdAsync(
                licenseId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveDetainByLicenseIdAsync_ShouldReturnDetachedGraph()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetActiveDetainByLicenseIdAsync(
                seed.LicenseId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result.License).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result.License.Driver).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                result.License.Driver.Person).State);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(
                result.CreatedByUser).State);
    }

    // =========================================================
    // IS LICENSE DETAINED
    // =========================================================

    [Fact]
    public async Task IsLicenseDetainedAsync_ShouldReturnTrueWhenLicenseHasActiveDetain()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: false);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.IsLicenseDetainedAsync(
                seed.LicenseId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsLicenseDetainedAsync_ShouldReturnFalseWhenDetainIsReleased()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.IsLicenseDetainedAsync(
                seed.LicenseId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsLicenseDetainedAsync_ShouldReturnFalseWhenLicenseHasNoDetain()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.IsLicenseDetainedAsync(
                int.MaxValue);

        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IsLicenseDetainedAsync_ShouldReturnFalseForInvalidLicenseId(
        int licenseId)
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.IsLicenseDetainedAsync(
                licenseId);

        Assert.False(result);
    }

    // =========================================================
    // ADD
    // =========================================================

    [Fact]
    public async Task AddAsync_ShouldThrowWhenEntityIsNull()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntityToContext()
    {
        await using var scope =
            await CreateTestScopeAsync();

        // Existing detain is released, so the license
        // can legally receive a new active detain.
        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var entity =
            new DetainedLicense
            {
                LicenseID =
                    seed.LicenseId,

                DetainDate =
                    DateTime.UtcNow,

                FineFees =
                    125m,

                CreatedByUserID =
                    seed.CreatedByUserId,

                IsReleased =
                    false
            };

        await repository.AddAsync(entity);

        Assert.Equal(
            EntityState.Added,
            scope.Context.Entry(entity).State);

        Assert.Equal(
            0,
            entity.DetainID);
    }

    [Fact]
    public async Task AddAsync_ShouldNotPersistUntilSaveChanges()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var entity =
            new DetainedLicense
            {
                LicenseID =
                    seed.LicenseId,

                DetainDate =
                    DateTime.UtcNow,

                FineFees =
                    150m,

                CreatedByUserID =
                    seed.CreatedByUserId,

                IsReleased =
                    false
            };

        await repository.AddAsync(entity);

        var countBeforeSave =
            await scope.Context.DetainedLicenses
                .CountAsync(
                    x =>
                        x.FineFees == 150m &&
                        x.LicenseID == seed.LicenseId);

        Assert.Equal(
            0,
            countBeforeSave);

        Assert.Equal(
            EntityState.Added,
            scope.Context.Entry(entity).State);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntityWhenContextSaves()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var entity =
            new DetainedLicense
            {
                LicenseID =
                    seed.LicenseId,

                DetainDate =
                    DateTime.UtcNow,

                FineFees =
                    175m,

                CreatedByUserID =
                    seed.CreatedByUserId,

                IsReleased =
                    false
            };

        await repository.AddAsync(entity);

        await scope.Context.SaveChangesAsync();

        Assert.True(
            entity.DetainID > 0);

        var persisted =
            await scope.Context.DetainedLicenses
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.DetainID ==
                        entity.DetainID);

        Assert.Equal(
            seed.LicenseId,
            persisted.LicenseID);

        Assert.Equal(
            175m,
            persisted.FineFees);

        Assert.False(
            persisted.IsReleased);
    }

    // =========================================================
    // GET BY ID FOR UPDATE
    // =========================================================

    [Fact]
    public async Task GetByIdForUpdateAsync_ShouldReturnExistingDetainedLicense()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdForUpdateAsync(
                seed.DetainId);

        Assert.NotNull(result);

        Assert.Equal(
            seed.DetainId,
            result.DetainID);
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ShouldTrackReturnedEntity()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdForUpdateAsync(
                seed.DetainId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            scope.Context.Entry(result).State);
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ShouldLoadRelatedData()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: true);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdForUpdateAsync(
                seed.DetainId);

        Assert.NotNull(result);

        Assert.NotNull(
            result.License);

        Assert.NotNull(
            result.License.Driver);

        Assert.NotNull(
            result.License.Driver.Person);

        Assert.NotNull(
            result.CreatedByUser);

        Assert.NotNull(
            result.ReleasedByUser);

        Assert.NotNull(
            result.ReleaseApplication);
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ShouldReturnNullWhenDetainedLicenseDoesNotExist()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdForUpdateAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdForUpdateAsync_ShouldReturnNullForInvalidId(
        int id)
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdForUpdateAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_ShouldAllowModificationAndPersistence()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var seed =
            await SeedDetainedLicenseAsync(
                scope.Context,
                released: false);

        var repository =
            new DetainedLicenseRepository(scope.Context);

        var result =
            await repository.GetByIdForUpdateAsync(
                seed.DetainId);

        Assert.NotNull(result);

        result.IsReleased =
            true;

        result.ReleaseDate =
            DateTime.UtcNow;

        result.ReleasedByUserID =
            seed.CreatedByUserId;

        await scope.Context.SaveChangesAsync();

        var persisted =
            await scope.Context.DetainedLicenses
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.DetainID ==
                        seed.DetainId);

        Assert.True(
            persisted.IsReleased);

        Assert.NotNull(
            persisted.ReleaseDate);

        Assert.Equal(
            seed.CreatedByUserId,
            persisted.ReleasedByUserID);
    }

    // =========================================================
    // TEST SCOPE
    // =========================================================

    private async Task<DetainedLicenseTestScope>
        CreateTestScopeAsync()
    {
        var context =
            _database.CreateContext();

        try
        {
            var transaction =
                await context.Database
                    .BeginTransactionAsync();

            return new DetainedLicenseTestScope(
                context,
                transaction);
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    private sealed class DetainedLicenseTestScope
        : IAsyncDisposable
    {
        public DVLDDbContext Context { get; }

        private IDbContextTransaction Transaction { get; }

        public DetainedLicenseTestScope(
            DVLDDbContext context,
            IDbContextTransaction transaction)
        {
            Context = context;
            Transaction = transaction;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Transaction.RollbackAsync();
            }
            finally
            {
                await Transaction.DisposeAsync();
                await Context.DisposeAsync();
            }
        }
    }

    // =========================================================
    // SEEDING
    // =========================================================

    private static async Task<DetainedLicenseSeed>
        SeedDetainedLicenseAsync(
            DVLDDbContext context,
            DateTime? detainDate = null,
            bool released = false)
    {
        // -----------------------------------------------------
        // COUNTRY
        // -----------------------------------------------------

        var country =
            new Country
            {
                CountryName =
                    $"Test Country {Guid.NewGuid():N}"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // PEOPLE
        // -----------------------------------------------------

        var person =
            new Person
            {
                NationalNo =
                    CreateNationalNo("DET"),

                FirstName =
                    "Detained",

                SecondName =
                    "License",

                LastName =
                    "Test",

                DateOfBirth =
                    new DateTime(1990, 1, 1),

                Gender =
                    Gender.Male,

                Address =
                    "Detained License Test Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"detained-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        var createdByPerson =
            new Person
            {
                NationalNo =
                    CreateNationalNo("USR"),

                FirstName =
                    "Detained",

                SecondName =
                    "Test",

                LastName =
                    "User",

                DateOfBirth =
                    new DateTime(1985, 1, 1),

                Gender =
                    Gender.Male,

                Address =
                    "Test User Address",

                Phone =
                    CreatePhone(),

                Email =
                    $"user-{Guid.NewGuid():N}@test.local",

                NationalityCountryID =
                    country.CountryId
            };

        Person? releasedByPerson = null;

        if (released)
        {
            releasedByPerson =
                new Person
                {
                    NationalNo =
                        CreateNationalNo("REL"),

                    FirstName =
                        "Release",

                    SecondName =
                        "Test",

                    LastName =
                        "User",

                    DateOfBirth =
                        new DateTime(1986, 1, 1),

                    Gender =
                        Gender.Female,

                    Address =
                        "Release User Address",

                    Phone =
                        CreatePhone(),

                    Email =
                        $"release-{Guid.NewGuid():N}@test.local",

                    NationalityCountryID =
                        country.CountryId
                };
        }

        context.People.AddRange(
            person,
            createdByPerson);

        if (releasedByPerson is not null)
        {
            context.People.Add(
                releasedByPerson);
        }

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // USERS
        // -----------------------------------------------------

        var createdByUser =
            new User
            {
                PersonId =
                    createdByPerson.PersonId,

                UserName =
                    $"detained-{Guid.NewGuid():N}",

                Password =
                    "test-password",

                IsActive =
                    true,

                Role =
                    UserRole.Admin
            };

        context.Users.Add(
            createdByUser);

        User? releasedByUser = null;

        if (releasedByPerson is not null)
        {
            releasedByUser =
                new User
                {
                    PersonId =
                        releasedByPerson.PersonId,

                    UserName =
                        $"release-{Guid.NewGuid():N}",

                    Password =
                        "test-password",

                    IsActive =
                        true,

                    Role =
                        UserRole.Staff
                };

            context.Users.Add(
                releasedByUser);
        }

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // APPLICATION TYPE
        // -----------------------------------------------------

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Detained Test Application {Guid.NewGuid():N}",

                ApplicationFees =
                    20m
            };

        context.ApplicationTypes.Add(
            applicationType);

        // -----------------------------------------------------
        // LICENSE CLASS
        // -----------------------------------------------------

        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    $"Detained Test Class {Guid.NewGuid():N}",

                ClassDescription =
                    "Detained license repository integration test",

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

        // -----------------------------------------------------
        // APPLICATION
        // -----------------------------------------------------

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    person.PersonId,

                ApplicationDate =
                    DateTime.UtcNow.AddDays(-10),

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.Completed,

                LastStatusDate =
                    DateTime.UtcNow.AddDays(-10),

                PaidFees =
                    20m,

                CreatedByUserID =
                    createdByUser.UserId
            };

        context.Applications.Add(
            application);

        ApplicationD? releaseApplication = null;

        if (released)
        {
            releaseApplication =
                new ApplicationD
                {
                    ApplicantPersonID =
                        person.PersonId,

                    ApplicationDate =
                        DateTime.UtcNow.AddDays(-3),

                    ApplicationTypeID =
                        applicationType.ApplicationTypeId,

                    ApplicationStatus =
                        AppStatus.Completed,

                    LastStatusDate =
                        DateTime.UtcNow.AddDays(-3),

                    PaidFees =
                        20m,

                    CreatedByUserID =
                        createdByUser.UserId
                };

            context.Applications.Add(
                releaseApplication);
        }

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // DRIVER
        // -----------------------------------------------------

        var driver =
            new Driver
            {
                PersonID =
                    person.PersonId,

                CreatedByUserID =
                    createdByUser.UserId,

                CreatedDate =
                    DateTime.UtcNow.AddDays(-20)
            };

        context.Drivers.Add(
            driver);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // LICENSE
        // -----------------------------------------------------

        var license =
            new License
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    driver.DriverID,

                LicenseClass =
                    licenseClass.LicenseClassID,

                IssueDate =
                    DateTime.UtcNow.AddYears(-1),

                ExpirationDate =
                    DateTime.UtcNow.AddYears(1),

                Notes =
                    "Detained license integration test",

                PaidFees =
                    100m,

                IsActive =
                    true,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    createdByUser.UserId
            };

        context.Licenses.Add(
            license);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // DETAINED LICENSE
        // -----------------------------------------------------

        var detainedLicense =
            new DetainedLicense
            {
                LicenseID =
                    license.LicenseID,

                DetainDate =
                    detainDate ??
                    DateTime.UtcNow.AddDays(-2),

                FineFees =
                    50m,

                CreatedByUserID =
                    createdByUser.UserId,

                IsReleased =
                    released,

                ReleaseDate =
                    released
                        ? DateTime.UtcNow.AddDays(-1)
                        : null,

                ReleasedByUserID =
                    released
                        ? releasedByUser!.UserId
                        : null,

                ReleaseApplicationID =
                    released
                        ? releaseApplication!.ApplicationID
                        : null
            };

        context.DetainedLicenses.Add(
            detainedLicense);

        await context.SaveChangesAsync();

        return new DetainedLicenseSeed
        {
            DetainId =
                detainedLicense.DetainID,

            LicenseId =
                license.LicenseID,

            PersonId =
                person.PersonId,

            CreatedByUserId =
                createdByUser.UserId,

            ReleasedByUserId =
                releasedByUser?.UserId ?? 0,

            ReleaseApplicationId =
                releaseApplication?.ApplicationID ?? 0,

            FineFees =
                detainedLicense.FineFees,

            IsReleased =
                detainedLicense.IsReleased
        };
    }

    // =========================================================
    // TEST DATA HELPERS
    // =========================================================

    private static string CreateNationalNo(
        string prefix)
    {
        // PersonConfiguration requires max 20 characters.
        // Prefix + 16 hexadecimal characters = <= 20.
        return
            $"{prefix}-{Guid.NewGuid():N}"[..20];
    }

    private static string CreatePhone()
    {
        return
            $"07{Random.Shared.Next(100000000, 999999999)}";
    }

    // =========================================================
    // SEED RESULT
    // =========================================================

    private sealed class DetainedLicenseSeed
    {
        public int DetainId { get; init; }

        public int LicenseId { get; init; }

        public int PersonId { get; init; }

        public int CreatedByUserId { get; init; }

        public int ReleasedByUserId { get; init; }

        public int ReleaseApplicationId { get; init; }

        public decimal FineFees { get; init; }

        public bool IsReleased { get; init; }
    }
}