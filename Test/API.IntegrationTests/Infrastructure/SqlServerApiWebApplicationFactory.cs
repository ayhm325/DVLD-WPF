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

    protected override void Dispose(
        bool disposing)
    {
        if (disposing)
        {
            DropDatabase();
        }

        base.Dispose(disposing);
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
            $"DVLD_ApiIntegrationTests_{Guid.NewGuid():N}";

        builder.InitialCatalog =
            _databaseName;

        ConnectionString =
            builder.ConnectionString;

        var options =
            new DbContextOptionsBuilder<DVLDDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

        using var context =
            new DVLDDbContext(options);

        context.Database
            .Migrate();
    }

    private void DropDatabase()
    {
        if (string.IsNullOrWhiteSpace(
                _databaseName))
        {
            return;
        }

        var builder =
            new SqlConnectionStringBuilder(
                ConnectionString);

        builder.InitialCatalog =
            "master";

        using var connection =
            new SqlConnection(
                builder.ConnectionString);

        connection.Open();

        using var command =
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

        command.ExecuteNonQuery();
    }
}