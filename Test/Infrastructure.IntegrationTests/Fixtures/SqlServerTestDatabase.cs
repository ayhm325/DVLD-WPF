using Dvld.TestSupport;
using System.Runtime.ExceptionServices;
using Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests.Fixtures;

public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    private SqlServerTestTarget? _target;
    private SqlServerTestDatabaseLease? _databaseLease;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _target = SqlServerTestEnvironment.ReadConfigurationFromEnvironment();

        await using (var adminConnection = new SqlConnection(
                         _target.AdminConnectionString))
        {
            await adminConnection.OpenAsync();
            var databaseName = SqlServerTestEnvironment.BuildDatabaseName(
                "DVLD_IntegrationTests_");
            _databaseLease = await SqlServerTestEnvironment.CreateDatabaseAsync(
                adminConnection,
                _target,
                databaseName);
        }

        ConnectionString = SqlServerTestEnvironment.BuildDatabaseConnectionString(
            _target,
            _databaseLease.DatabaseName);

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        try
        {
            await using var context = new DVLDDbContext(options);
            await context.Database.MigrateAsync();
        }
        catch (Exception migrationException)
        {
            try
            {
                await DisposeAsync();
            }
            catch (Exception cleanupException)
            {
                SqlServerTestEnvironment.ReportCleanupFailureSafely(
                    "Test database cleanup failed after migration failed. Primary exception type={0}; cleanup exception type={1}.",
                    migrationException,
                    cleanupException);
            }

            ExceptionDispatchInfo.Capture(migrationException).Throw();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        var lease = Volatile.Read(ref _databaseLease);
        var target = _target;
        if (lease is null)
        {
            return;
        }

        if (target is null)
        {
            throw new InvalidOperationException(
                "The test database lease cannot be cleaned because its SQL Server target is unavailable.");
        }

        await using var connection = new SqlConnection(
            target.AdminConnectionString);
        await connection.OpenAsync();
        var outcome = await SqlServerTestEnvironment.DropOwnedDatabaseAsync(
            connection,
            target,
            lease);
        if (outcome == SqlServerTestDatabaseCleanupOutcome.OwnershipMismatch)
        {
            throw new InvalidOperationException(
                "The test database was not dropped because ownership could not be confirmed.");
        }

        if (outcome is not SqlServerTestDatabaseCleanupOutcome.Dropped and
            not SqlServerTestDatabaseCleanupOutcome.AlreadyAbsent)
        {
            throw new InvalidOperationException(
                "The test database cleanup returned an unknown outcome; its lease is retained.");
        }

        Interlocked.CompareExchange(ref _databaseLease, null, lease);
    }

    public DVLDDbContext CreateContext()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "The test database has not been initialized.");
        }

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new DVLDDbContext(options);
    }
}
