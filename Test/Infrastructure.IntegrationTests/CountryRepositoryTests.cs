using Domain.Entities;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class CountryRepositoryTests
    : IClassFixture<CountryRepositoryDatabaseFixture>
{
    private readonly CountryRepositoryDatabaseFixture _fixture;

    public CountryRepositoryTests(
        CountryRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CountryRepository(null!));
    }

    [Fact]
    public async Task GetAllCountriesAsync_ShouldReturnAllSeededCountries()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new CountryRepository(context);

        var result =
            await repository.GetAllCountriesAsync();

        Assert.Equal(
            3,
            result.Count);

        Assert.Contains(
            result,
            country =>
                country.CountryId ==
                _fixture.JordanId);

        Assert.Contains(
            result,
            country =>
                country.CountryId ==
                _fixture.CanadaId);

        Assert.Contains(
            result,
            country =>
                country.CountryId ==
                _fixture.GermanyId);
    }

    [Fact]
    public async Task GetAllCountriesAsync_ShouldReturnCountriesOrderedByNameAscending()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new CountryRepository(context);

        var result =
            await repository.GetAllCountriesAsync();

        Assert.Equal(
            "Canada",
            result[0].CountryName);

        Assert.Equal(
            "Germany",
            result[1].CountryName);

        Assert.Equal(
            "Jordan",
            result[2].CountryName);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                string.Compare(
                    result[i - 1].CountryName,
                    result[i].CountryName,
                    StringComparison.Ordinal) < 0);
        }
    }

    [Fact]
    public async Task GetAllCountriesAsync_ShouldReturnEntitiesWithoutTracking()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new CountryRepository(context);

        var result =
            await repository.GetAllCountriesAsync();

        Assert.NotEmpty(result);

        Assert.All(
            result,
            country =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(country).State));
    }

    [Fact]
    public async Task GetAllCountriesAsync_ShouldReturnCorrectCountryData()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new CountryRepository(context);

        var result =
            await repository.GetAllCountriesAsync();

        var jordan =
            result.Single(
                country =>
                    country.CountryId ==
                    _fixture.JordanId);

        var canada =
            result.Single(
                country =>
                    country.CountryId ==
                    _fixture.CanadaId);

        var germany =
            result.Single(
                country =>
                    country.CountryId ==
                    _fixture.GermanyId);

        Assert.Equal(
            "Jordan",
            jordan.CountryName);

        Assert.Equal(
            "Canada",
            canada.CountryName);

        Assert.Equal(
            "Germany",
            germany.CountryName);
    }

    [Fact]
    public async Task GetAllCountriesAsync_ShouldNotReturnUncommittedCountries()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var uncommittedCountry =
            new Country
            {
                CountryName =
                    "Uncommitted Country"
            };

        context.Countries.Add(
            uncommittedCountry);

        var repository =
            new CountryRepository(context);

        var result =
            await repository.GetAllCountriesAsync();

        Assert.DoesNotContain(
            result,
            country =>
                country.CountryName ==
                "Uncommitted Country");
    }

    [Fact]
    public async Task GetAllCountriesAsync_ShouldNotModifyTrackedStateOfExistingEntities()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var trackedCountry =
            await context.Countries
                .SingleAsync(
                    country =>
                        country.CountryId ==
                        _fixture.JordanId);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(trackedCountry).State);

        var repository =
            new CountryRepository(context);

        var result =
            await repository.GetAllCountriesAsync();

        Assert.Contains(
            result,
            country =>
                country.CountryId ==
                _fixture.JordanId);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(trackedCountry).State);

        var returnedJordan =
            result.Single(
                country =>
                    country.CountryId ==
                    _fixture.JordanId);

        Assert.NotSame(
            trackedCountry,
            returnedJordan);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(returnedJordan).State);
    }
}

public sealed class CountryRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int JordanId { get; private set; }

    public int CanadaId { get; private set; }

    public int GermanyId { get; private set; }

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

        var jordan =
            new Country
            {
                CountryName =
                    "Jordan"
            };

        var canada =
            new Country
            {
                CountryName =
                    "Canada"
            };

        var germany =
            new Country
            {
                CountryName =
                    "Germany"
            };

        context.Countries.AddRange(
            jordan,
            canada,
            germany);

        await context.SaveChangesAsync();

        JordanId =
            jordan.CountryId;

        CanadaId =
            canada.CountryId;

        GermanyId =
            germany.CountryId;

        Assert.True(
            JordanId > 0);

        Assert.True(
            CanadaId > 0);

        Assert.True(
            GermanyId > 0);

        Assert.NotEqual(
            JordanId,
            CanadaId);

        Assert.NotEqual(
            JordanId,
            GermanyId);

        Assert.NotEqual(
            CanadaId,
            GermanyId);
    }
}