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
        // Arrange
        await using var context =
            _database.CreateContext();

        // Act
        var canConnect =
            await context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task Database_ShouldHaveNoPendingMigrations()
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        // Act
        var pendingMigrations =
            await context.Database
                .GetPendingMigrationsAsync();

        // Assert
        Assert.Empty(pendingMigrations);
    }
}
