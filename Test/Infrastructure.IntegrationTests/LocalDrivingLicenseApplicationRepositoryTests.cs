using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class LocalDrivingLicenseApplicationRepositoryTests
    : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public LocalDrivingLicenseApplicationRepositoryTests(
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
            () => new LocalDrivingLicenseApplicationRepository(null!));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllLocalApplications()
    {
        var first = await SeedLocalApplicationAsync();
        var second = await SeedLocalApplicationAsync();

        await using var context = _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetAllAsync();

        Assert.Contains(
            result,
            x => x.LocalDrivingLicenseApplicationID ==
                 first.LocalApplicationId);

        Assert.Contains(
            result,
            x => x.LocalDrivingLicenseApplicationID ==
                 second.LocalApplicationId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadApplicationPersonAndLicenseClass()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context = _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var entity =
            Assert.Single(
                (await repository.GetAllAsync())
                    .Where(
                        x =>
                            x.LocalDrivingLicenseApplicationID ==
                            seed.LocalApplicationId));

        Assert.NotNull(entity.Application);

        Assert.Equal(
            seed.ApplicationId,
            entity.Application.ApplicationID);

        Assert.NotNull(entity.Application.Person);

        Assert.Equal(
            seed.PersonId,
            entity.Application.Person.PersonId);

        Assert.NotNull(entity.LicenseClass);

        Assert.Equal(
            seed.LicenseClassId,
            entity.LicenseClass.LicenseClassID);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDetachedEntities()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetAllAsync();

        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        seed.LocalApplicationId));

        Assert.Equal(
            EntityState.Detached,
            context.Entry(entity).State);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExistingEntity()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByIdAsync(
                seed.LocalApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            seed.LocalApplicationId,
            result.LocalDrivingLicenseApplicationID);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForMissingId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

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
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLoadExpectedNavigationProperties()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByIdAsync(
                seed.LocalApplicationId);

        Assert.NotNull(result);
        Assert.NotNull(result.Application);
        Assert.NotNull(result.Application.Person);
        Assert.NotNull(result.LicenseClass);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDetachedEntity()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByIdAsync(
                seed.LocalApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);
    }

    // =========================================================
    // GET FOR UPDATE
    // =========================================================

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnExistingEntity()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                seed.LocalApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            seed.LocalApplicationId,
            result.LocalDrivingLicenseApplicationID);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldTrackEntity()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                seed.LocalApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldLoadApplication()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                seed.LocalApplicationId);

        Assert.NotNull(result);
        Assert.NotNull(result.Application);

        Assert.Equal(
            seed.ApplicationId,
            result.Application.ApplicationID);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldAllowModificationAndSave()
    {
        var seed =
            await SeedLocalApplicationAsync();

        var newLicenseClass =
            await CreateLicenseClassAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                seed.LocalApplicationId);

        Assert.NotNull(result);

        result.LicenseClassID =
            newLicenseClass.LicenseClassId;

        await context.SaveChangesAsync();

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        seed.LocalApplicationId);

        Assert.Equal(
            newLicenseClass.LicenseClassId,
            persisted.LicenseClassID);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnNullForMissingId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetForUpdateAsync_ShouldReturnNullForInvalidId(
        int id)
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetForUpdateAsync(id);

        Assert.Null(result);
    }

    // =========================================================
    // GET BY PERSON ID
    // =========================================================

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnOnlyApplicationsForPerson()
    {
        var first =
            await SeedLocalApplicationAsync();

        var second =
            await SeedLocalApplicationForExistingPersonAsync(
                first.PersonId);

        var other =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByPersonIdAsync(
                first.PersonId);

        Assert.Contains(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                first.LocalApplicationId);

        Assert.Contains(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                second.LocalApplicationId);

        Assert.DoesNotContain(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                other.LocalApplicationId);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldLoadExpectedNavigationProperties()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByPersonIdAsync(
                seed.PersonId);

        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        seed.LocalApplicationId));

        Assert.NotNull(entity.Application);
        Assert.NotNull(entity.Application.Person);
        Assert.NotNull(entity.LicenseClass);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByPersonIdAsync_ShouldReturnEmptyForInvalidPersonId(
        int personId)
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByPersonIdAsync(personId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPersonIdAsync_ShouldReturnDetachedEntities()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByPersonIdAsync(
                seed.PersonId);

        Assert.Contains(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                seed.LocalApplicationId);

        Assert.All(
            result,
            entity =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(entity).State));
    }

    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    [Fact]
    public async Task GetByApplicationIdAsync_ShouldReturnMatchingApplication()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByApplicationIdAsync(
                seed.ApplicationId);

        var entity =
            Assert.Single(result);

        Assert.Equal(
            seed.LocalApplicationId,
            entity.LocalDrivingLicenseApplicationID);
    }

    [Fact]
    public async Task GetByApplicationIdAsync_ShouldReturnEmptyForMissingApplication()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByApplicationIdAsync(
                int.MaxValue);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByApplicationIdAsync_ShouldReturnEmptyForInvalidApplicationId(
        int applicationId)
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByApplicationIdAsync(
                applicationId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByApplicationIdAsync_ShouldReturnDetachedEntities()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByApplicationIdAsync(
                seed.ApplicationId);

        Assert.Single(result);

        Assert.All(
            result,
            entity =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(entity).State));
    }

    // =========================================================
    // GET BY LICENSE CLASS ID
    // =========================================================

    [Fact]
    public async Task GetByLicenseClassIdAsync_ShouldReturnMatchingApplications()
    {
        var first =
            await SeedLocalApplicationAsync();

        var second =
            await SeedLocalApplicationAsync(
                licenseClassId:
                    first.LicenseClassId);

        var other =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByLicenseClassIdAsync(
                first.LicenseClassId);

        Assert.Contains(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                first.LocalApplicationId);

        Assert.Contains(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                second.LocalApplicationId);

        Assert.DoesNotContain(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                other.LocalApplicationId);
    }

    [Fact]
    public async Task GetByLicenseClassIdAsync_ShouldLoadLicenseClass()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByLicenseClassIdAsync(
                seed.LicenseClassId);

        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        seed.LocalApplicationId));

        Assert.NotNull(entity.LicenseClass);

        Assert.Equal(
            seed.LicenseClassId,
            entity.LicenseClass.LicenseClassID);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByLicenseClassIdAsync_ShouldReturnEmptyForInvalidId(
        int licenseClassId)
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByLicenseClassIdAsync(
                licenseClassId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByLicenseClassIdAsync_ShouldReturnDetachedEntities()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetByLicenseClassIdAsync(
                seed.LicenseClassId);

        Assert.Contains(
            result,
            x =>
                x.LocalDrivingLicenseApplicationID ==
                seed.LocalApplicationId);

        Assert.All(
            result,
            entity =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(entity).State));
    }

    // =========================================================
    // GET PASSED TEST COUNTS
    // =========================================================

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldCountOnlyPassedTests()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await SeedTestAsync(
            seed.LocalApplicationId,
            true);

        await SeedTestAsync(
            seed.LocalApplicationId,
            true);

        await SeedTestAsync(
            seed.LocalApplicationId,
            false);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetPassedTestCountsAsync(
                [seed.LocalApplicationId]);

        Assert.True(
            result.ContainsKey(
                seed.LocalApplicationId));

        Assert.Equal(
            2,
            result[seed.LocalApplicationId]);
    }

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldReturnCountsForMultipleApplications()
    {
        var first =
            await SeedLocalApplicationAsync();

        var second =
            await SeedLocalApplicationAsync();

        await SeedTestAsync(
            first.LocalApplicationId,
            true);

        await SeedTestAsync(
            first.LocalApplicationId,
            true);

        await SeedTestAsync(
            second.LocalApplicationId,
            true);

        await SeedTestAsync(
            second.LocalApplicationId,
            false);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetPassedTestCountsAsync(
                [
                    first.LocalApplicationId,
                    second.LocalApplicationId
                ]);

        Assert.Equal(
            2,
            result[first.LocalApplicationId]);

        Assert.Equal(
            1,
            result[second.LocalApplicationId]);
    }

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldIgnoreDuplicateIds()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await SeedTestAsync(
            seed.LocalApplicationId,
            true);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetPassedTestCountsAsync(
                [
                    seed.LocalApplicationId,
                    seed.LocalApplicationId,
                    seed.LocalApplicationId
                ]);

        Assert.Single(result);

        Assert.Equal(
            1,
            result[seed.LocalApplicationId]);
    }

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldIgnoreInvalidIds()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await SeedTestAsync(
            seed.LocalApplicationId,
            true);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetPassedTestCountsAsync(
                [
                    seed.LocalApplicationId,
                    0,
                    -1
                ]);

        Assert.Single(result);

        Assert.Equal(
            1,
            result[seed.LocalApplicationId]);
    }

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldReturnEmptyForEmptyInput()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetPassedTestCountsAsync([]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldReturnEmptyForOnlyInvalidIds()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetPassedTestCountsAsync(
                [
                    0,
                    -1,
                    -100
                ]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldThrowWhenIdsAreNull()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.GetPassedTestCountsAsync(null!));
    }

    [Fact]
    public async Task GetPassedTestCountsAsync_ShouldNotReturnApplicationsWithoutPassedTests()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await SeedTestAsync(
            seed.LocalApplicationId,
            false);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetPassedTestCountsAsync(
                [seed.LocalApplicationId]);

        Assert.Empty(result);
    }

    // =========================================================
    // GET APPLICATION ID BY LOCAL ID
    // =========================================================

    [Fact]
    public async Task GetApplicationIdByLocalIdAsync_ShouldReturnApplicationId()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetApplicationIdByLocalIdAsync(
                seed.LocalApplicationId);

        Assert.Equal(
            seed.ApplicationId,
            result);
    }

    [Fact]
    public async Task GetApplicationIdByLocalIdAsync_ShouldReturnNullForMissingId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetApplicationIdByLocalIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetApplicationIdByLocalIdAsync_ShouldReturnNullForInvalidId(
        int localId)
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.GetApplicationIdByLocalIdAsync(localId);

        Assert.Null(result);
    }

    // =========================================================
    // HAS DUPLICATE APPLICATION
    // =========================================================

    [Fact]
    public async Task HasDuplicateApplicationAsync_ShouldReturnApplicationIdForNewApplication()
    {
        var seed =
            await SeedLocalApplicationAsync(
                AppStatus.New);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.HasDuplicateApplicationAsync(
                seed.PersonId,
                seed.LicenseClassId);

        Assert.Equal(
            seed.ApplicationId,
            result);
    }

    [Fact]
    public async Task HasDuplicateApplicationAsync_ShouldReturnApplicationIdForCompletedApplication()
    {
        var seed =
            await SeedLocalApplicationAsync(
                AppStatus.Completed);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.HasDuplicateApplicationAsync(
                seed.PersonId,
                seed.LicenseClassId);

        Assert.Equal(
            seed.ApplicationId,
            result);
    }

    [Fact]
    public async Task HasDuplicateApplicationAsync_ShouldIgnoreCancelledApplication()
    {
        var seed =
            await SeedLocalApplicationAsync(
                AppStatus.Cancelled);

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.HasDuplicateApplicationAsync(
                seed.PersonId,
                seed.LicenseClassId);

        Assert.Null(result);
    }

    [Fact]
    public async Task HasDuplicateApplicationAsync_ShouldReturnNullForDifferentPerson()
    {
        var seed =
            await SeedLocalApplicationAsync();

        var otherPerson =
            await CreatePersonAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.HasDuplicateApplicationAsync(
                otherPerson.PersonId,
                seed.LicenseClassId);

        Assert.Null(result);
    }

    [Fact]
    public async Task HasDuplicateApplicationAsync_ShouldReturnNullForDifferentLicenseClass()
    {
        var seed =
            await SeedLocalApplicationAsync();

        var otherLicenseClass =
            await CreateLicenseClassAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.HasDuplicateApplicationAsync(
                seed.PersonId,
                otherLicenseClass.LicenseClassId);

        Assert.Null(result);
    }

    [Fact]
    public async Task HasDuplicateApplicationAsync_ShouldReturnNullWhenNoApplicationExists()
    {
        var person =
            await CreatePersonAsync();

        var licenseClass =
            await CreateLicenseClassAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.HasDuplicateApplicationAsync(
                person.PersonId,
                licenseClass.LicenseClassId);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task HasDuplicateApplicationAsync_ShouldReturnNullForInvalidIds(
        int personId,
        int licenseClassId)
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.HasDuplicateApplicationAsync(
                personId,
                licenseClassId);

        Assert.Null(result);
    }

    // =========================================================
    // ADD
    // =========================================================

    [Fact]
    public async Task AddAsync_ShouldThrowWhenEntityIsNull()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_ShouldMarkEntityAsAdded()
    {
        var seed =
            await SeedLocalApplicationAsync();

        var application =
            await CreateApplicationAsync(
                seed.PersonId,
                seed.CreatedByUserId);

        var entity =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    application.ApplicationId,

                LicenseClassID =
                    seed.LicenseClassId
            };

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        await repository.AddAsync(entity);

        Assert.Equal(
            EntityState.Added,
            context.Entry(entity).State);
    }

    [Fact]
    public async Task AddAsync_ShouldNotPersistUntilSaveChanges()
    {
        var seed =
            await SeedLocalApplicationAsync();

        var application =
            await CreateApplicationAsync(
                seed.PersonId,
                seed.CreatedByUserId);

        var entity =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    application.ApplicationId,

                LicenseClassID =
                    seed.LicenseClassId
            };

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        await repository.AddAsync(entity);

        await using var verificationContext =
            _database.CreateContext();

        var persistedBeforeSave =
            await verificationContext
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ApplicationID ==
                        application.ApplicationId);

        Assert.False(persistedBeforeSave);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAfterSaveChanges()
    {
        var seed =
            await SeedLocalApplicationAsync();

        var application =
            await CreateApplicationAsync(
                seed.PersonId,
                seed.CreatedByUserId);

        var entity =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    application.ApplicationId,

                LicenseClassID =
                    seed.LicenseClassId
            };

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        await repository.AddAsync(entity);

        await context.SaveChangesAsync();

        Assert.True(
            entity.LocalDrivingLicenseApplicationID > 0);

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        entity.LocalDrivingLicenseApplicationID);

        Assert.NotNull(persisted);

        Assert.Equal(
            application.ApplicationId,
            persisted.ApplicationID);

        Assert.Equal(
            seed.LicenseClassId,
            persisted.LicenseClassID);
    }

    // =========================================================
    // DELETE
    // =========================================================

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalseForInvalidId()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        Assert.False(
            await repository.DeleteAsync(0));

        Assert.False(
            await repository.DeleteAsync(-1));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalseWhenEntityDoesNotExist()
    {
        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.DeleteAsync(
                int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldMarkExistingEntityAsDeleted()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.DeleteAsync(
                seed.LocalApplicationId);

        Assert.True(result);

        var entity =
            await context
                .LocalDrivingLicenseApplications
                .FindAsync(
                    seed.LocalApplicationId);

        Assert.NotNull(entity);

        Assert.Equal(
            EntityState.Deleted,
            context.Entry(entity).State);
    }

    [Fact]
    public async Task DeleteAsync_ShouldPersistDeletionAfterSaveChanges()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.DeleteAsync(
                seed.LocalApplicationId);

        Assert.True(result);

        await context.SaveChangesAsync();

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        seed.LocalApplicationId);

        Assert.Null(persisted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotPersistBeforeSaveChanges()
    {
        var seed =
            await SeedLocalApplicationAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new LocalDrivingLicenseApplicationRepository(context);

        var result =
            await repository.DeleteAsync(
                seed.LocalApplicationId);

        Assert.True(result);

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext
                .LocalDrivingLicenseApplications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.LocalDrivingLicenseApplicationID ==
                        seed.LocalApplicationId);

        Assert.NotNull(persisted);
    }

    // =========================================================
    // SEED LOCAL APPLICATION
    // =========================================================

    private async Task<LocalApplicationSeed>
        SeedLocalApplicationAsync(
            AppStatus applicationStatus = AppStatus.New,
            int? licenseClassId = null)
    {
        await using var context =
            _database.CreateContext();

        var suffix =
            Guid.NewGuid().ToString("N");

        // -----------------------------------------------------
        // COUNTRY
        // -----------------------------------------------------

        var country =
            new Country
            {
                CountryName =
                    $"LDA Country {suffix}"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // PERSON
        // -----------------------------------------------------

        var person =
            new Person
            {
                // <= 20 characters
                NationalNo =
                    $"LDA{suffix[..17]}",

                FirstName =
                    "Local",

                SecondName =
                    "Driving",

                ThirdName =
                    "Application",

                LastName =
                    "Test",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Test Address",

                Phone =
                    $"079{Random.Shared.Next(
                        1000000,
                        9999999)}",

                Email =
                    $"{suffix}@example.com",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.Add(person);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // USER
        // -----------------------------------------------------

        var user =
            new User
            {
                PersonId =
                    person.PersonId,

                UserName =
                    $"lda_user_{suffix}",

                Password =
                    "TestPassword",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // APPLICATION TYPE
        // -----------------------------------------------------

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"LDA Application {suffix}",

                ApplicationFees =
                    50m
            };

        context.ApplicationTypes.Add(
            applicationType);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // LICENSE CLASS
        // -----------------------------------------------------

        LicenseClass licenseClass;

        if (licenseClassId.HasValue)
        {
            licenseClass =
                await context.LicenseClasses
                    .FirstAsync(
                        x =>
                            x.LicenseClassID ==
                            licenseClassId.Value);
        }
        else
        {
            licenseClass =
                new LicenseClass
                {
                    ClassName =
                        $"LDA Class {suffix}",

                    ClassDescription =
                        "Integration test license class",

                    MinimumAllowedAge =
                        18,

                    DefaultValidityLength =
                        5,

                    ClassFees =
                        100m
                };

            context.LicenseClasses.Add(
                licenseClass);

            await context.SaveChangesAsync();
        }

        // -----------------------------------------------------
        // APPLICATION
        // -----------------------------------------------------

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    person.PersonId,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    applicationStatus,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    applicationType.ApplicationFees,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(
            application);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // LOCAL APPLICATION
        // -----------------------------------------------------

        var localApplication =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    application.ApplicationID,

                LicenseClassID =
                    licenseClass.LicenseClassID
            };

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();

        return new LocalApplicationSeed
        {
            LocalApplicationId =
                localApplication.LocalDrivingLicenseApplicationID,

            ApplicationId =
                application.ApplicationID,

            PersonId =
                person.PersonId,

            LicenseClassId =
                licenseClass.LicenseClassID,

            CreatedByUserId =
                user.UserId
        };
    }

    // =========================================================
    // SEED FOR EXISTING PERSON
    // =========================================================

    private async Task<LocalApplicationSeed>
        SeedLocalApplicationForExistingPersonAsync(
            int personId)
    {
        await using var context =
            _database.CreateContext();

        var person =
            await context.People
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.PersonId ==
                        personId);

        var user =
            await context.Users
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.PersonId ==
                        personId);

        var applicationType =
            await context.ApplicationTypes
                .AsNoTracking()
                .FirstAsync();

        var licenseClass =
            await CreateLicenseClassUsingContextAsync(
                context);

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    person.PersonId,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    applicationType.ApplicationFees,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(application);

        await context.SaveChangesAsync();

        var localApplication =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    application.ApplicationID,

                LicenseClassID =
                    licenseClass.LicenseClassId
            };

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();

        return new LocalApplicationSeed
        {
            LocalApplicationId =
                localApplication.LocalDrivingLicenseApplicationID,

            ApplicationId =
                application.ApplicationID,

            PersonId =
                person.PersonId,

            LicenseClassId =
                licenseClass.LicenseClassId,

            CreatedByUserId =
                user.UserId
        };
    }

    // =========================================================
    // CREATE PERSON
    // =========================================================

    private async Task<PersonSeed>
        CreatePersonAsync()
    {
        await using var context =
            _database.CreateContext();

        var suffix =
            Guid.NewGuid().ToString("N");

        var country =
            new Country
            {
                CountryName =
                    $"Person Country {suffix}"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        var person =
            new Person
            {
                // 7 + 13 = 20
                NationalNo =
                    $"PERSON-{suffix[..13]}",

                FirstName =
                    "Test",

                SecondName =
                    "Person",

                LastName =
                    "ForLDA",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Test Address",

                Phone =
                    $"078{Random.Shared.Next(
                        1000000,
                        9999999)}",

                Email =
                    $"{suffix}@example.com",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.Add(person);

        await context.SaveChangesAsync();

        return new PersonSeed
        {
            PersonId =
                person.PersonId
        };
    }

    // =========================================================
    // CREATE LICENSE CLASS
    // =========================================================

    private async Task<LicenseClassSeed>
        CreateLicenseClassAsync()
    {
        await using var context =
            _database.CreateContext();

        return await CreateLicenseClassUsingContextAsync(
            context);
    }

    private static async Task<LicenseClassSeed>
        CreateLicenseClassUsingContextAsync(
            DVLDDbContext context)
    {
        var suffix =
            Guid.NewGuid().ToString("N");

        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    $"Extra Class {suffix}",

                ClassDescription =
                    "Additional integration test class",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    5,

                ClassFees =
                    100m
            };

        context.LicenseClasses.Add(
            licenseClass);

        await context.SaveChangesAsync();

        return new LicenseClassSeed
        {
            LicenseClassId =
                licenseClass.LicenseClassID
        };
    }

    // =========================================================
    // CREATE APPLICATION
    // =========================================================

    private async Task<ApplicationSeed>
        CreateApplicationAsync(
            int personId,
            int createdByUserId)
    {
        await using var context =
            _database.CreateContext();

        var applicationType =
            await context.ApplicationTypes
                .AsNoTracking()
                .FirstAsync();

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    personId,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    applicationType.ApplicationFees,

                CreatedByUserID =
                    createdByUserId
            };

        context.Applications.Add(application);

        await context.SaveChangesAsync();

        return new ApplicationSeed
        {
            ApplicationId =
                application.ApplicationID
        };
    }

    // =========================================================
    // CREATE TEST
    // =========================================================

    private async Task SeedTestAsync(
        int localApplicationId,
        bool testResult)
    {
        await using var context =
            _database.CreateContext();

        // -----------------------------------------------------
        // USER
        // -----------------------------------------------------

        var user =
            await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync();

        if (user is null)
        {
            throw new InvalidOperationException(
                "The test database does not contain a User. " +
                "SeedLocalApplicationAsync should create one.");
        }

        // -----------------------------------------------------
        // TEST TYPE
        // -----------------------------------------------------

        var testType =
            await context.TestTypes
                .FirstOrDefaultAsync();

        if (testType is null)
        {
            testType =
                new TestType
                {
                    TestTypeTitle =
                        $"Integration Test {Guid.NewGuid():N}",

                    TestTypeDescription =
                        "Local driving license repository integration test",

                    TestTypeFees =
                        10m
                };

            context.TestTypes.Add(testType);

            await context.SaveChangesAsync();
        }

        // -----------------------------------------------------
        // APPOINTMENT
        // -----------------------------------------------------

        var appointment =
            new TestAppointment
            {
                TestTypeID =
                    testType.TestTypeId,

                LocalDrivingLicenseApplicationID =
                    localApplicationId,

                AppointmentDate =
                    DateTime.UtcNow,

                PaidFees =
                    10m,

                CreatedByUserID =
                    user.UserId,

                IsLocked =
                    true
            };

        context.TestAppointments.Add(
            appointment);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // TEST
        // -----------------------------------------------------

        var test =
            new Test
            {
                TestAppointmentID =
                    appointment.TestAppointmentID,

                TestResult =
                    testResult,

                Notes =
                    "Repository integration test",

                CreatedByUserID =
                    user.UserId
            };

        context.Tests.Add(test);

        await context.SaveChangesAsync();
    }

    // =========================================================
    // SEED RESULT TYPES
    // =========================================================

    private sealed class LocalApplicationSeed
    {
        public int LocalApplicationId { get; init; }

        public int ApplicationId { get; init; }

        public int PersonId { get; init; }

        public int LicenseClassId { get; init; }

        public int CreatedByUserId { get; init; }
    }

    private sealed class PersonSeed
    {
        public int PersonId { get; init; }
    }

    private sealed class LicenseClassSeed
    {
        public int LicenseClassId { get; init; }
    }

    private sealed class ApplicationSeed
    {
        public int ApplicationId { get; init; }
    }
}