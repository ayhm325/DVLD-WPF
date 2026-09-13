using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.IntegrationTests;

public sealed class PersonRepositoryTests
    : IClassFixture<PersonRepositoryDatabaseFixture>
{
    private readonly PersonRepositoryDatabaseFixture _fixture;

    public PersonRepositoryTests(
        PersonRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new PersonRepository(null!));
    }

    [Fact]
    public async Task GetPersonByIdAsync_ShouldReturnExistingPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByIdAsync(
                _fixture.AdminPersonId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.AdminPersonId,
            result.PersonId);

        Assert.Equal(
            "ADMIN-TEST-NATIONAL",
            result.NationalNo);

        Assert.Equal(
            "Admin",
            result.FirstName);

        Assert.Equal(
            "Integration",
            result.SecondName);

        Assert.Equal(
            "Person",
            result.LastName);
    }

    [Fact]
    public async Task GetPersonByIdAsync_ShouldLoadCountry()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByIdAsync(
                _fixture.AdminPersonId);

        Assert.NotNull(result);
        Assert.NotNull(result.Country);

        Assert.Equal(
            _fixture.CountryId,
            result.NationalityCountryID);

        Assert.Equal(
            _fixture.CountryId,
            result.Country.CountryId);

        Assert.Equal(
            _fixture.CountryName,
            result.Country.CountryName);
    }

    [Fact]
    public async Task GetPersonByIdAsync_ShouldReturnNullWhenPersonDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPersonByIdAsync_ShouldReturnDetachedEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByIdAsync(
                _fixture.AdminPersonId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);

        Assert.NotNull(result.Country);

        var country =
            result.Country;

        Assert.Equal(
            EntityState.Detached,
            context.Entry(country).State);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_ShouldReturnExistingPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByNationalNoAsync(
                "STAFF-TEST-NATIONAL");

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffPersonId,
            result.PersonId);

        Assert.Equal(
            "STAFF-TEST-NATIONAL",
            result.NationalNo);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_ShouldReturnNullWhenNationalNoDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByNationalNoAsync(
                "MISSING-NATIONAL");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_ShouldLoadCountry()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByNationalNoAsync(
                "STAFF-TEST-NATIONAL");

        Assert.NotNull(result);
        Assert.NotNull(result.Country);

        Assert.Equal(
            _fixture.CountryId,
            result.Country.CountryId);
    }

    [Fact]
    public async Task GetPersonByNationalNoAsync_ShouldReturnDetachedEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonByNationalNoAsync(
                "STAFF-TEST-NATIONAL");

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);

        Assert.NotNull(result.Country);

        var country =
            result.Country;

        Assert.Equal(
            EntityState.Detached,
            context.Entry(country).State);
    }

    [Fact]
    public async Task GetAllPersonsAsync_ShouldReturnSeededPersonsOrderedByIdAscending()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetAllPersonsAsync();

        Assert.Contains(
            result,
            person =>
                person.PersonId ==
                _fixture.AdminPersonId);

        Assert.Contains(
            result,
            person =>
                person.PersonId ==
                _fixture.StaffPersonId);

        Assert.Contains(
            result,
            person =>
                person.PersonId ==
                _fixture.UnassignedPersonId);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i - 1].PersonId <
                result[i].PersonId);
        }
    }

    [Fact]
    public async Task GetAllPersonsAsync_ShouldLoadCountryForEveryPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetAllPersonsAsync();

        Assert.NotEmpty(result);

        Assert.All(
            result,
            person =>
            {
                Assert.NotNull(person.Country);

                Assert.Equal(
                    person.NationalityCountryID,
                    person.Country.CountryId);
            });
    }

    [Fact]
    public async Task GetAllPersonsAsync_ShouldReturnDetachedEntities()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetAllPersonsAsync();

        Assert.NotEmpty(result);

        Assert.All(
            result,
            person =>
            {
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(person).State);

                Assert.NotNull(person.Country);

                var country =
                    person.Country;

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(country).State);
            });
    }

    [Fact]
    public async Task GetPersonForUpdateAsync_ShouldReturnExistingPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonForUpdateAsync(
                _fixture.StaffPersonId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.StaffPersonId,
            result.PersonId);

        Assert.Equal(
            "STAFF-TEST-NATIONAL",
            result.NationalNo);
    }

    [Fact]
    public async Task GetPersonForUpdateAsync_ShouldTrackEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonForUpdateAsync(
                _fixture.StaffPersonId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetPersonForUpdateAsync_ShouldNotLoadCountry()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonForUpdateAsync(
                _fixture.StaffPersonId);

        Assert.NotNull(result);

        Assert.Null(result.Country);
    }

    [Fact]
    public async Task GetPersonForUpdateAsync_ShouldReturnNullWhenPersonDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.GetPersonForUpdateAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task IsPersonExistsByIdAsync_ShouldReturnTrueForExistingPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.IsPersonExistsByIdAsync(
                _fixture.AdminPersonId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsPersonExistsByIdAsync_ShouldReturnFalseForMissingPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.IsPersonExistsByIdAsync(
                int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task IsNationalNoDuplicatedAsync_ShouldReturnTrueForAnotherPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.IsNationalNoDuplicatedAsync(
                "STAFF-TEST-NATIONAL",
                _fixture.AdminPersonId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsNationalNoDuplicatedAsync_ShouldReturnFalseForSamePerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.IsNationalNoDuplicatedAsync(
                "ADMIN-TEST-NATIONAL",
                _fixture.AdminPersonId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsNationalNoDuplicatedAsync_ShouldReturnFalseWhenNationalNoDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.IsNationalNoDuplicatedAsync(
                "MISSING-NATIONAL",
                _fixture.AdminPersonId);

        Assert.False(result);
    }

    [Fact]
    public async Task HasApplicationsAsync_ShouldReturnTrueWhenPersonHasApplications()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.HasApplicationsAsync(
                _fixture.AdminPersonId);

        Assert.True(result);
    }

    [Fact]
    public async Task HasApplicationsAsync_ShouldReturnFalseWhenPersonHasNoApplications()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.HasApplicationsAsync(
                _fixture.StaffPersonId);

        Assert.False(result);
    }

    [Fact]
    public async Task AddPersonAsync_ShouldPersistPerson()
    {
        var nationalNo =
            $"ADD-{Guid.NewGuid():N}"[..20];

        var person =
            _fixture.CreatePerson(
                nationalNo);

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new PersonRepository(context);

            await repository.AddPersonAsync(person);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var persistedPerson =
            await verificationContext.People
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.NationalNo ==
                        nationalNo);

        Assert.NotNull(persistedPerson);

        Assert.Equal(
            "Added",
            persistedPerson.FirstName);

        Assert.Equal(
            "Integration",
            persistedPerson.SecondName);

        Assert.Equal(
            "Person",
            persistedPerson.LastName);

        Assert.Equal(
            _fixture.CountryId,
            persistedPerson.NationalityCountryID);
    }

    [Fact]
    public async Task AddPersonAsync_ShouldThrowWhenPersonIsNull()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.AddPersonAsync(null!));
    }

    [Fact]
    public async Task DeletePersonAsync_ShouldReturnFalseWhenPersonDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new PersonRepository(context);

        var result =
            await repository.DeletePersonAsync(
                int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task DeletePersonAsync_ShouldRemoveExistingPerson()
    {
        var nationalNo =
            $"DEL-{Guid.NewGuid():N}"[..20];

        var person =
            _fixture.CreatePerson(
                nationalNo);

        int personId;

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new PersonRepository(context);

            await repository.AddPersonAsync(person);

            await context.SaveChangesAsync();

            personId =
                person.PersonId;
        }

        await using (var deleteContext =
            _fixture.Database.CreateContext())
        {
            var repository =
                new PersonRepository(deleteContext);

            var result =
                await repository.DeletePersonAsync(
                    personId);

            Assert.True(result);

            await deleteContext.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var deletedPerson =
            await verificationContext.People
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.PersonId ==
                        personId);

        Assert.Null(deletedPerson);
    }

    [Fact]
    public async Task DeletePersonAsync_ShouldNotPersistDeletionBeforeSaveChanges()
    {
        var nationalNo =
            $"DEL2-{Guid.NewGuid():N}"[..20];

        var person =
            _fixture.CreatePerson(
                nationalNo);

        int personId;

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new PersonRepository(context);

            await repository.AddPersonAsync(person);

            await context.SaveChangesAsync();

            personId =
                person.PersonId;
        }

        await using (var deleteContext =
            _fixture.Database.CreateContext())
        {
            var repository =
                new PersonRepository(deleteContext);

            var result =
                await repository.DeletePersonAsync(
                    personId);

            Assert.True(result);

            Assert.Equal(
                EntityState.Deleted,
                deleteContext.Entry(
                    await deleteContext.People
                        .SingleAsync(
                            x =>
                                x.PersonId ==
                                personId))
                    .State);
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var stillExists =
            await verificationContext.People
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.PersonId ==
                        personId);

        Assert.True(stillExists);

        await using var cleanupContext =
            _fixture.Database.CreateContext();

        var cleanupRepository =
            new PersonRepository(cleanupContext);

        Assert.True(
            await cleanupRepository.DeletePersonAsync(
                personId));

        await cleanupContext.SaveChangesAsync();
    }
}

public sealed class PersonRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int CountryId { get; private set; }

    public string CountryName { get; private set; } = string.Empty;

    public int AdminPersonId { get; private set; }

    public int StaffPersonId { get; private set; }

    public int UnassignedPersonId { get; private set; }

    public int AdminUserId { get; private set; }

    public int ApplicationTypeId { get; private set; }

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

    public Person CreatePerson(
        string nationalNo)
    {
        return new Person
        {
            NationalNo =
                nationalNo,

            FirstName =
                "Added",

            SecondName =
                "Integration",

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
                "Integration Test Address",

            Phone =
                "0790000000",

            Email =
                $"{Guid.NewGuid():N}@example.com",

            NationalityCountryID =
                CountryId
        };
    }

    private async Task SeedAsync()
    {
        await using var context =
            Database.CreateContext();

        await SeedCountryAsync(context);

        var adminPerson =
            new Person
            {
                NationalNo =
                    "ADMIN-TEST-NATIONAL",

                FirstName =
                    "Admin",

                SecondName =
                    "Integration",

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
                    "Admin Integration Address",

                Phone =
                    "0791111111",

                Email =
                    "admin.person@example.com",

                NationalityCountryID =
                    CountryId
            };

        var staffPerson =
            new Person
            {
                NationalNo =
                    "STAFF-TEST-NATIONAL",

                FirstName =
                    "Staff",

                SecondName =
                    "Integration",

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
                    "Staff Integration Address",

                Phone =
                    "0792222222",

                Email =
                    "staff.person@example.com",

                NationalityCountryID =
                    CountryId
            };

        var unassignedPerson =
            new Person
            {
                NationalNo =
                    "UNASSIGNED-TEST-NO",

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
                    Gender.Female,

                Address =
                    "Unassigned Integration Address",

                Phone =
                    "0793333333",

                Email =
                    "unassigned.person@example.com",

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

        await SeedApplicationTypeAsync(context);

        var adminUser =
            new User
            {
                PersonId =
                    AdminPersonId,

                UserName =
                    "person.repository.admin",

                Password =
                    "hashed-password",

                IsActive =
                    true,

                Role =
                    UserRole.Admin
            };

        context.Users.Add(adminUser);

        await context.SaveChangesAsync();

        AdminUserId =
            adminUser.UserId;

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    AdminPersonId,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationTypeID =
                    ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    100m,

                CreatedByUserID =
                    AdminUserId
            };

        context.Applications.Add(application);

        await context.SaveChangesAsync();

        Assert.True(
            CountryId > 0);

        Assert.True(
            AdminPersonId > 0);

        Assert.True(
            StaffPersonId > AdminPersonId);

        Assert.True(
            UnassignedPersonId > StaffPersonId);

        Assert.True(
            AdminUserId > 0);

        Assert.True(
            ApplicationTypeId > 0);
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
                        $"Person Repository Country {Guid.NewGuid():N}"
                };

            context.Countries.Add(country);

            await context.SaveChangesAsync();
        }

        CountryId =
            country.CountryId;

        CountryName =
            country.CountryName;
    }

    private async Task SeedApplicationTypeAsync(
        DVLDDbContext context)
    {
        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Person Repository Application {Guid.NewGuid():N}"[..50],

                ApplicationFees =
                    100m
            };

        context.ApplicationTypes.Add(
            applicationType);

        await context.SaveChangesAsync();

        ApplicationTypeId =
            applicationType.ApplicationTypeId;
    }
}