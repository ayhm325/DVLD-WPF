using Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests.Fixtures;

public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    private const string ConnectionEnvironmentVariable =
        "DVLD_TEST_CONNECTION";

    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                ConnectionEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                $"Environment variable '{ConnectionEnvironmentVariable}' " +
                "is required to run infrastructure integration tests.");
        }

        var builder =
            new SqlConnectionStringBuilder(baseConnectionString);

        _databaseName =
            $"DVLD_IntegrationTests_{Guid.NewGuid():N}";

        builder.InitialCatalog = _databaseName;

        ConnectionString = builder.ConnectionString;

        var options =
            new DbContextOptionsBuilder<DVLDDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

        await using var context =
            new DVLDDbContext(options);

        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            return;

        var builder =
            new SqlConnectionStringBuilder(ConnectionString);

        builder.InitialCatalog = "master";

        await using var connection =
            new SqlConnection(builder.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            connection.CreateCommand();

        command.CommandText =
            $"""
            IF DB_ID(N'{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}]
                SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

                DROP DATABASE [{_databaseName}];
            END
            """;

        await command.ExecuteNonQueryAsync();
    }

    public DVLDDbContext CreateContext()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "The test database has not been initialized.");
        }

        var options =
            new DbContextOptionsBuilder<DVLDDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

        return new DVLDDbContext(options);
    }
}