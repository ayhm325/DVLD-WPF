using Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class SqlServerTestDatabaseTests
    : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public SqlServerTestDatabaseTests(
        SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task Database_ShouldBeReachable()
    {
        await using var context =
            _database.CreateContext();

        var canConnect =
            await context.Database.CanConnectAsync();

        Assert.True(canConnect);
    }

    [Fact]
    public async Task Database_ShouldHaveNoPendingMigrations()
    {
        await using var context =
            _database.CreateContext();

        var pendingMigrations =
            await context.Database
                .GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);
    }
}