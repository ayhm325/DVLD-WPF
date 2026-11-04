using Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace API.IntegrationTests.Infrastructure;

public sealed class SqlServerApiWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string ConnectionEnvironmentVariable =
        "DVLD_TEST_CONNECTION";

    private const int DatabaseLifecycleCommandTimeoutSeconds = 120;

    private static readonly SemaphoreSlim DatabaseLifecycleLock =
        new(2, 2);

    private string? _databaseName;

    public string ConnectionString { get; private set; } = string.Empty;

    public SqlServerApiWebApplicationFactory()
    {
        InitializeDatabase();
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:DVLDConnection",
            ConnectionString);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHttpContextAccessor>();

            services.AddHttpContextAccessor();

            services
                .AddAuthentication("Test")
                .AddScheme<
                    AuthenticationSchemeOptions,
                    TestAuthenticationHandler>(
                    "Test",
                    _ =>
                    {
                    });
        });
    }

    public DVLDDbContext CreateDbContext()
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

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await DropDatabaseAsync();
    }

    protected override void Dispose(
        bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            DropDatabase();
        }
    }

    private void InitializeDatabase()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(
                ConnectionEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                $"Environment variable " +
                $"'{ConnectionEnvironmentVariable}' " +
                "is required to run API integration tests.");
        }

        var builder =
            new SqlConnectionStringBuilder(
                baseConnectionString);

        _databaseName =
            $"DVLD_IntegrationTests_Api_{Guid.NewGuid():N}";

        builder.InitialCatalog =
            _databaseName;

        ConnectionString =
            builder.ConnectionString;

        var options =
            new DbContextOptionsBuilder<DVLDDbContext>()
                .UseSqlServer(
                    ConnectionString,
                    sqlOptions => sqlOptions.CommandTimeout(
                        DatabaseLifecycleCommandTimeoutSeconds))
                .Options;

        DatabaseLifecycleLock.Wait();
        try
        {
            using var context =
                new DVLDDbContext(options);

            context.Database
                .Migrate();
        }
        finally
        {
            DatabaseLifecycleLock.Release();
        }
    }

    private async Task DropDatabaseAsync()
    {
        var databaseName = Interlocked.Exchange(ref _databaseName, null);
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        ClearDatabaseConnectionPool();

        var builder =
            new SqlConnectionStringBuilder(
                ConnectionString)
            {
                InitialCatalog = "master"
            };

        await DatabaseLifecycleLock.WaitAsync();
        try
        {
            await using var connection =
                new SqlConnection(
                    builder.ConnectionString);

            await connection.OpenAsync();

            await using var command =
                connection.CreateCommand();

            command.CommandTimeout = DatabaseLifecycleCommandTimeoutSeconds;
            command.CommandText = BuildDropDatabaseSql(databaseName);

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            DatabaseLifecycleLock.Release();
        }
    }

    private void DropDatabase()
    {
        var databaseName = Interlocked.Exchange(ref _databaseName, null);
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        ClearDatabaseConnectionPool();

        var builder =
            new SqlConnectionStringBuilder(
                ConnectionString)
            {
                InitialCatalog = "master"
            };

        DatabaseLifecycleLock.Wait();
        try
        {
            using var connection =
                new SqlConnection(
                    builder.ConnectionString);

            connection.Open();

            using var command =
                connection.CreateCommand();

            command.CommandTimeout = DatabaseLifecycleCommandTimeoutSeconds;
            command.CommandText = BuildDropDatabaseSql(databaseName);

            command.ExecuteNonQuery();
        }
        finally
        {
            DatabaseLifecycleLock.Release();
        }
    }

    private void ClearDatabaseConnectionPool()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        using var pooledConnection = new SqlConnection(ConnectionString);
        SqlConnection.ClearPool(pooledConnection);
    }

    private static string BuildDropDatabaseSql(string databaseName) =>
        $"""
        IF DB_ID(N'{databaseName}') IS NOT NULL
        BEGIN
            ALTER DATABASE [{databaseName}]
            SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

            DROP DATABASE [{databaseName}];
        END
        """;
}