using Domain.Entities;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class LicenseClassRepositoryTests
    : IClassFixture<LicenseClassRepositoryDatabaseFixture>
{
    private readonly LicenseClassRepositoryDatabaseFixture _fixture;

    public LicenseClassRepositoryTests(
        LicenseClassRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new LicenseClassRepository(null!));
    }

    [Fact]
    public async Task GetAllLicenseClassAsync_ShouldReturnAllLicenseClasses()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetAllLicenseClassAsync();

        Assert.Equal(
            3,
            result.Count);

        Assert.Contains(
            result,
            x =>
                x.LicenseClassID ==
                _fixture.FirstLicenseClassId);

        Assert.Contains(
            result,
            x =>
                x.LicenseClassID ==
                _fixture.SecondLicenseClassId);

        Assert.Contains(
            result,
            x =>
                x.LicenseClassID ==
                _fixture.ThirdLicenseClassId);
    }

    [Fact]
    public async Task GetAllLicenseClassAsync_ShouldReturnExpectedData()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetAllLicenseClassAsync();

        var first =
            result.Single(
                x =>
                    x.LicenseClassID ==
                    _fixture.FirstLicenseClassId);

        var second =
            result.Single(
                x =>
                    x.LicenseClassID ==
                    _fixture.SecondLicenseClassId);

        var third =
            result.Single(
                x =>
                    x.LicenseClassID ==
                    _fixture.ThirdLicenseClassId);

        Assert.Equal(
            "Class A",
            first.ClassName);

        Assert.Equal(
            "License class A integration test",
            first.ClassDescription);

        Assert.Equal(
            100m,
            first.ClassFees);

        Assert.Equal(
            "Class B",
            second.ClassName);

        Assert.Equal(
            "License class B integration test",
            second.ClassDescription);

        Assert.Equal(
            200.50m,
            second.ClassFees);

        Assert.Equal(
            "Class C",
            third.ClassName);

        Assert.Equal(
            "License class C integration test",
            third.ClassDescription);

        Assert.Equal(
            300.75m,
            third.ClassFees);
    }

    [Fact]
    public async Task GetAllLicenseClassAsync_ShouldReturnEntitiesWithoutTracking()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetAllLicenseClassAsync();

        Assert.NotEmpty(result);

        Assert.All(
            result,
            licenseClass =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(licenseClass).State));
    }

    [Fact]
    public async Task GetAllLicenseClassAsync_ShouldNotReturnUncommittedEntities()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var uncommittedLicenseClass =
            new LicenseClass
            {
                ClassName =
                    "Uncommitted Class",

                ClassDescription =
                    "This entity must not appear in repository query",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    10,

                ClassFees =
                    999m
            };

        context.LicenseClasses.Add(
            uncommittedLicenseClass);

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetAllLicenseClassAsync();

        Assert.DoesNotContain(
            result,
            x =>
                x.ClassName ==
                "Uncommitted Class");
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_ShouldReturnExistingLicenseClass()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetLicenseClassByIdAsync(
                _fixture.SecondLicenseClassId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.SecondLicenseClassId,
            result.LicenseClassID);

        Assert.Equal(
            "Class B",
            result.ClassName);

        Assert.Equal(
            "License class B integration test",
            result.ClassDescription);

        Assert.Equal(
            200.50m,
            result.ClassFees);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_ShouldReturnNullWhenLicenseClassDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetLicenseClassByIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_ShouldReturnNullForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetLicenseClassByIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_ShouldReturnNullForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetLicenseClassByIdAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_ShouldReturnEntityWithoutTracking()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetLicenseClassByIdAsync(
                _fixture.ThirdLicenseClassId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_ShouldNotReturnUncommittedEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var uncommittedLicenseClass =
            new LicenseClass
            {
                ClassName =
                    "Uncommitted By Id",

                ClassDescription =
                    "This entity must not appear by id query",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    10,

                ClassFees =
                    500m
            };

        context.LicenseClasses.Add(
            uncommittedLicenseClass);

        var repository =
            new LicenseClassRepository(context);

        var result =
            await repository.GetLicenseClassByIdAsync(
                uncommittedLicenseClass.LicenseClassID);

        Assert.Null(result);
    }
}

public sealed class LicenseClassRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int FirstLicenseClassId { get; private set; }

    public int SecondLicenseClassId { get; private set; }

    public int ThirdLicenseClassId { get; private set; }

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

    private async Task SeedAsync()
    {
        await using var context =
            Database.CreateContext();

        var first =
            new LicenseClass
            {
                ClassName =
                    "Class A",

                ClassDescription =
                    "License class A integration test",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    10,

                ClassFees =
                    100m
            };

        var second =
            new LicenseClass
            {
                ClassName =
                    "Class B",

                ClassDescription =
                    "License class B integration test",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    10,

                ClassFees =
                    200.50m
            };

        var third =
            new LicenseClass
            {
                ClassName =
                    "Class C",

                ClassDescription =
                    "License class C integration test",

                MinimumAllowedAge =
                    21,

                DefaultValidityLength =
                    5,

                ClassFees =
                    300.75m
            };

        context.LicenseClasses.AddRange(
            first,
            second,
            third);

        await context.SaveChangesAsync();

        FirstLicenseClassId =
            first.LicenseClassID;

        SecondLicenseClassId =
            second.LicenseClassID;

        ThirdLicenseClassId =
            third.LicenseClassID;

        Assert.True(
            FirstLicenseClassId > 0);

        Assert.True(
            SecondLicenseClassId > 0);

        Assert.True(
            ThirdLicenseClassId > 0);

        Assert.NotEqual(
            FirstLicenseClassId,
            SecondLicenseClassId);

        Assert.NotEqual(
            FirstLicenseClassId,
            ThirdLicenseClassId);

        Assert.NotEqual(
            SecondLicenseClassId,
            ThirdLicenseClassId);
    }
}