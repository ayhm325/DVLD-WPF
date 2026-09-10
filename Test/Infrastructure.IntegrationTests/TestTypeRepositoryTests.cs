using Domain.Entities;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class TestTypeRepositoryTests
    : IClassFixture<TestTypeRepositoryDatabaseFixture>
{
    private readonly TestTypeRepositoryDatabaseFixture _fixture;

    public TestTypeRepositoryTests(
        TestTypeRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // ============================================================
    // Constructor
    // ============================================================

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new TestTypeRepository(null!));
    }

    // ============================================================
    // GetAllAsync
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTestTypesOrderedByIdAscending()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestTypeRepository(context);

        var result =
            await repository.GetAllAsync();

        Assert.Equal(
            3,
            result.Count);

        Assert.Equal(
            _fixture.TheoryId,
            result[0].TestTypeId);

        Assert.Equal(
            _fixture.WrittenId,
            result[1].TestTypeId);

        Assert.Equal(
            _fixture.PracticalId,
            result[2].TestTypeId);

        Assert.True(
            result[0].TestTypeId <
            result[1].TestTypeId);

        Assert.True(
            result[1].TestTypeId <
            result[2].TestTypeId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnTestTypesWithoutTracking()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestTypeRepository(context);

        var result =
            await repository.GetAllAsync();

        Assert.All(
            result,
            testType =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(testType).State));
    }

    // ============================================================
    // GetByIdAsync
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExistingTestType()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestTypeRepository(context);

        var result =
            await repository.GetByIdAsync(
                _fixture.WrittenId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.WrittenId,
            result.TestTypeId);

        Assert.Equal(
            "Written Test",
            result.TestTypeTitle);

        Assert.Equal(
            "Integration test written examination",
            result.TestTypeDescription);

        Assert.Equal(
            15m,
            result.TestTypeFees);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullWhenTestTypeDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestTypeRepository(context);

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
            new TestTypeRepository(context);

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
            new TestTypeRepository(context);

        var result =
            await repository.GetByIdAsync(-1);

        Assert.Null(result);
    }

    // ============================================================
    // GetForUpdateAsync
    // ============================================================

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnExistingTestTypeTracked()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestTypeRepository(context);

        var result =
            await repository.GetForUpdateAsync(
                _fixture.PracticalId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.PracticalId,
            result.TestTypeId);

        Assert.Equal(
            "Practical Test",
            result.TestTypeTitle);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnNullWhenTestTypeDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new TestTypeRepository(context);

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
            new TestTypeRepository(context);

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
            new TestTypeRepository(context);

        var result =
            await repository.GetForUpdateAsync(-1);

        Assert.Null(result);
    }
}

// ==================================================================
// Fixture
// ==================================================================

public sealed class TestTypeRepositoryDatabaseFixture
    : IAsyncLifetime
{
    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int TheoryId { get; private set; }

    public int WrittenId { get; private set; }

    public int PracticalId { get; private set; }

    private SqlServerTestDatabase? _database;

    public async Task InitializeAsync()
    {
        _database =
            new SqlServerTestDatabase();

        await _database.InitializeAsync();

        Database =
            _database;

        await SeedTestTypesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private async Task SeedTestTypesAsync()
    {
        await using var context =
            Database.CreateContext();

        var theory =
            new TestType
            {
                TestTypeTitle =
                    "Theory Test",

                TestTypeDescription =
                    "Integration test theory examination",

                TestTypeFees =
                    10m
            };

        var written =
            new TestType
            {
                TestTypeTitle =
                    "Written Test",

                TestTypeDescription =
                    "Integration test written examination",

                TestTypeFees =
                    15m
            };

        var practical =
            new TestType
            {
                TestTypeTitle =
                    "Practical Test",

                TestTypeDescription =
                    "Integration test practical examination",

                TestTypeFees =
                    20m
            };

        context.TestTypes.AddRange(
            theory,
            written,
            practical);

        await context.SaveChangesAsync();

        TheoryId =
            theory.TestTypeId;

        WrittenId =
            written.TestTypeId;

        PracticalId =
            practical.TestTypeId;

        Assert.True(
            TheoryId > 0);

        Assert.True(
            WrittenId > TheoryId);

        Assert.True(
            PracticalId > WrittenId);
    }
}