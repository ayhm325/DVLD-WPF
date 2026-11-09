using API.IntegrationTests.Infrastructure;

using Dvld.TestSupport;

using Infrastructure;
using Infrastructure.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests.Security;

[Collection(nameof(DatabaseSafetyCollection))]
public sealed class TestHostDatabaseSafetyTests
{
    private const string ProtectedConnectionString =
        "Server=.;Database=DVLDf;Integrated Security=true;TrustServerCertificate=true";

    [Theory]
    [InlineData("DVLD")]
    [InlineData("DVLDf")]
    public void Guard_Rejects_ProtectedApplicationDatabases(string catalog)
    {
        var connectionString =
            $"Server=.;Database={catalog};Integrated Security=true;TrustServerCertificate=true";

        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlServerConnectionSafety.EnsureSafeForHost(
                connectionString,
                SqlServerConnectionSafety.TestingEnvironmentName));

        Assert.Contains("protected application database", exception.Message);
    }

    [Theory]
    [InlineData("master")]
    [InlineData("DVLD_IntegrationTests_")]
    [InlineData("DVLD_IntegrationTests_not-a-guid")]
    [InlineData("SomeOtherDatabase")]
    public void Guard_Rejects_UnapprovedTestDatabases(string catalog)
    {
        var connectionString =
            $"Server=.;Database={catalog};Integrated Security=true;TrustServerCertificate=true";

        Assert.Throws<InvalidOperationException>(
            () => SqlServerConnectionSafety.EnsureSafeForHost(
                connectionString,
                SqlServerConnectionSafety.TestingEnvironmentName));
    }

    [Fact]
    public void Guard_Accepts_GeneratedIsolatedTestDatabase()
    {
        var databaseName =
            SqlServerConnectionSafety.ApprovedTestDatabasePrefix +
            "Api_" +
            Guid.NewGuid().ToString("N");

        var connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=true;TrustServerCertificate=true";

        var validated = SqlServerConnectionSafety.EnsureSafeForHost(
            connectionString,
            SqlServerConnectionSafety.TestingEnvironmentName);

        Assert.Equal(connectionString, validated);
        Assert.True(SqlServerConnectionSafety.IsApprovedIsolatedTestDatabase(databaseName));
        Assert.False(SqlServerConnectionSafety.IsProtectedApplicationDatabase(databaseName));
    }

    [Fact]
    public void Guard_IsInert_ForNonTestHosts()
    {
        // Production and development hosts legitimately target the application database.
        // The process-wide test-host switch set by this assembly is cleared for this test.
        var original = Environment.GetEnvironmentVariable(
            SqlServerConnectionSafety.TestHostEnvironmentVariable);

        Environment.SetEnvironmentVariable(
            SqlServerConnectionSafety.TestHostEnvironmentVariable,
            null);

        try
        {
            var validated = SqlServerConnectionSafety.EnsureSafeForHost(
                ProtectedConnectionString,
                "Production");

            Assert.Equal(ProtectedConnectionString, validated);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                SqlServerConnectionSafety.TestHostEnvironmentVariable,
                original);
        }
    }

    [Fact]
    public async Task MockTestHost_StartsAndServesRequests_WithoutOpeningAnyConnection()
    {
        await using var factory = new ApiWebApplicationFactory();

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/Auth/me");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, factory.DatabaseConnectionRecorder.ConnectionAttempts);
    }

    [Fact]
    public void MockTestHost_DbContextTargetsIsolatedTestDatabase_NotApplicationDatabase()
    {
        using var factory = new ApiWebApplicationFactory();
        _ = factory.CreateClient();

        var options = factory.Services
            .GetRequiredService<DbContextOptions<DVLDDbContext>>();

        var connectionString = RelationalOptionsExtension
            .Extract(options)?
            .ConnectionString;

        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var builder = new SqlConnectionStringBuilder(connectionString!);

        Assert.StartsWith(
            SqlServerConnectionSafety.ApprovedTestDatabasePrefix,
            builder.InitialCatalog);

        Assert.False(
            SqlServerConnectionSafety.IsProtectedApplicationDatabase(
                builder.InitialCatalog));
    }

    [Fact]
    public void TestHost_ConfiguredWithProtectedDatabase_FailsFast()
    {
        using var factory = new UnsafeTestHostFactory(ProtectedConnectionString);

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);

        var messages = Flatten(exception);
        Assert.Contains("protected application database", messages);
        Assert.Contains("DVLDf", messages);
    }

    [Theory]
    [InlineData("DVLD")]
    [InlineData("DVLDf")]
    public void Cleanup_Rejects_ApplicationDatabaseNames(string databaseName)
    {
        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateGeneratedDatabaseName(databaseName));
    }

    [Fact]
    public void Cleanup_Accepts_GeneratedTestDatabaseName()
    {
        var generated = SqlServerTestEnvironment.BuildDatabaseName(
            SqlServerConnectionSafety.ApprovedTestDatabasePrefix);

        SqlServerTestEnvironment.ValidateGeneratedDatabaseName(generated);
    }

    [Fact]
    public void Cleanup_Rejects_NonGeneratedSuffix()
    {
        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateGeneratedDatabaseName(
                "DVLD_IntegrationTests_not-a-guid"));
    }

    [Fact]
    public void TestConnectionConfiguration_Rejects_ApplicationDatabaseTarget()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(
                "Server=localhost;Database=DVLDf;Integrated Security=True;TrustServerCertificate=True",
                "localhost|MSSQLSERVER"));

        Assert.Contains("application database", exception.Message);
    }

    [Fact]
    public void TestConnectionConfiguration_Rejects_MissingConfiguration()
    {
        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateConfiguration(null, null));
    }

    private static string Flatten(Exception exception)
    {
        var messages = new List<string>();
        var queue = new Queue<Exception>();
        queue.Enqueue(exception);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            messages.Add(current.Message);

            if (current.InnerException is not null)
            {
                queue.Enqueue(current.InnerException);
            }

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    queue.Enqueue(inner);
                }
            }
        }

        return string.Join(" | ", messages);
    }

    private sealed class UnsafeTestHostFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(SqlServerConnectionSafety.TestingEnvironmentName);

            // Satisfy the JWT prerequisite so the database guard is the failing gate.
            builder.UseSetting(
                "Jwt:SecretKey",
                "dvld-unsafe-host-test-signing-key-0123456789abcdef");

            builder.UseSetting("ConnectionStrings:DVLDConnection", connectionString);
        }
    }
}

[CollectionDefinition(nameof(DatabaseSafetyCollection), DisableParallelization = true)]
public sealed class DatabaseSafetyCollection
{
}
