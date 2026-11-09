using Dvld.TestSupport;
using Infrastructure;
using System.Runtime.ExceptionServices;
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
    private const int DatabaseLifecycleCommandTimeoutSeconds = 120;

    private static readonly SemaphoreSlim DatabaseLifecycleLock =
        new(2, 2);

    private SqlServerTestTarget? _target;
    private SqlServerTestDatabaseLease? _databaseLease;
    private readonly Func<
        SqlServerTestDatabaseLease,
        Task<SqlServerTestDatabaseCleanupOutcome>>? _dropOwnedDatabaseAsyncOverride;
    private readonly SqlServerApiFactoryDisposal _disposal;

    public string ConnectionString { get; private set; } = string.Empty;

    public SqlServerApiWebApplicationFactory()
    {
        _disposal = CreateDisposalCoordinator(
            DisposeBaseAsync,
            DisposeBaseSynchronously,
            DropDatabaseAsync,
            DropDatabase);
        InitializeDatabase();
    }

    internal SqlServerApiWebApplicationFactory(
        Func<Task> cleanupAsync,
        Action cleanupSynchronously,
        Func<ValueTask>? disposeBaseAsync = null,
        Action? disposeBaseSynchronously = null)
    {
        _disposal = CreateDisposalCoordinator(
            disposeBaseAsync ?? DisposeBaseAsync,
            disposeBaseSynchronously ?? DisposeBaseSynchronously,
            cleanupAsync,
            cleanupSynchronously);
    }

    internal SqlServerApiWebApplicationFactory(
        SqlServerTestDatabaseLease databaseLease,
        Func<SqlServerTestDatabaseLease,
            Task<SqlServerTestDatabaseCleanupOutcome>> dropOwnedDatabaseAsync,
        Func<ValueTask>? disposeBaseAsync = null,
        Action? disposeBaseSynchronously = null)
    {
        _databaseLease = databaseLease;
        _dropOwnedDatabaseAsyncOverride = dropOwnedDatabaseAsync;
        _disposal = CreateDisposalCoordinator(
            disposeBaseAsync ?? DisposeBaseAsync,
            disposeBaseSynchronously ?? DisposeBaseSynchronously,
            DropDatabaseAsync,
            DropDatabase);
    }

    internal SqlServerTestDatabaseLease? DatabaseLeaseForTesting =>
        Volatile.Read(ref _databaseLease);

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseSetting(
            "ConnectionStrings:DVLDConnection",
            ConnectionString);

        builder.ConfigureTestServices(services =>
        {
            services.Configure<Application.Options.NoShowProcessorOptions>(opts => opts.IsEnabled = false);
            services.Configure<Application.Options.LicenseExpirationOptions>(opts => opts.IsEnabled = false);

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

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new DVLDDbContext(options);
    }

    public override ValueTask DisposeAsync() => _disposal.DisposeAsync();

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(false);
            return;
        }

        _disposal.DisposeSynchronously();
    }

    private ValueTask DisposeBaseAsync() => base.DisposeAsync();

    private void DisposeBaseSynchronously() => base.Dispose(true);

    private static SqlServerApiFactoryDisposal CreateDisposalCoordinator(
        Func<ValueTask> disposeBaseAsync,
        Action disposeBaseSynchronously,
        Func<Task> cleanupAsync,
        Action cleanupSynchronously) =>
        new(
            disposeBaseAsync,
            disposeBaseSynchronously,
            cleanupAsync,
            cleanupSynchronously);

    private void InitializeDatabase()
    {
        _target = SqlServerTestEnvironment.ReadConfigurationFromEnvironment();
        var databaseName = SqlServerTestEnvironment.BuildDatabaseName(
            "DVLD_IntegrationTests_Api_");

        using (var adminConnection = new SqlConnection(
                   _target.AdminConnectionString))
        {
            adminConnection.Open();
            _databaseLease = SqlServerTestEnvironment.CreateDatabaseAsync(
                    adminConnection,
                    _target,
                    databaseName)
                .GetAwaiter()
                .GetResult();
        }

        ConnectionString = SqlServerTestEnvironment.BuildDatabaseConnectionString(
            _target,
            _databaseLease.DatabaseName);

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(
                ConnectionString,
                sqlOptions => sqlOptions.CommandTimeout(
                    DatabaseLifecycleCommandTimeoutSeconds))
            .Options;

        DatabaseLifecycleLock.Wait();

        Exception? migrationException = null;

        try
        {
            using var context = new DVLDDbContext(options);
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            migrationException = ex;
        }
        finally
        {
            DatabaseLifecycleLock.Release();
        }

        if (migrationException is not null)
        {
            try
            {
                DropDatabase();
            }
            catch (Exception cleanupException)
            {
                SqlServerTestEnvironment.ReportCleanupFailureSafely(
                    "API test database cleanup failed after migration failed. Primary exception type={0}; cleanup exception type={1}.",
                    migrationException,
                    cleanupException);

                if (_databaseLease is not null && _target is not null)
                {
                    throw new SqlServerApiFactoryInitializationException(
                        migrationException,
                        cleanupException,
                        _databaseLease,
                        _target.ExpectedInstanceIdentity);
                }

                throw new AggregateException(
                    "API test database migration and cleanup both failed.",
                    migrationException,
                    cleanupException);
            }

            ExceptionDispatchInfo.Capture(migrationException).Throw();
        }
    }

    private async Task DropDatabaseAsync()
    {
        var lease = Volatile.Read(ref _databaseLease);
        var target = _target;
        if (lease is null)
        {
            return;
        }

        if (_dropOwnedDatabaseAsyncOverride is not null)
        {
            var testOutcome = await _dropOwnedDatabaseAsyncOverride(lease)
                .ConfigureAwait(false);
            CompleteLeaseCleanup(lease, testOutcome);
            return;
        }

        if (target is null)
        {
            var exception = new InvalidOperationException(
                $"The target SQL Server configuration is unavailable for test database cleanup. Database={lease.DatabaseName}.");
            RetainLeaseEvidence(exception, lease);
            throw exception;
        }

        try
        {
            ClearDatabaseConnectionPool();
            await DatabaseLifecycleLock.WaitAsync().ConfigureAwait(false);
        }
        catch (Exception cleanupException)
        {
            RetainLeaseEvidence(cleanupException, lease);
            throw;
        }

        try
        {
            await using var connection = new SqlConnection(
                target.AdminConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            var outcome = await SqlServerTestEnvironment.DropOwnedDatabaseAsync(
                connection,
                target,
                lease).ConfigureAwait(false);
            CompleteLeaseCleanup(lease, outcome);
        }
        catch (Exception cleanupException)
        {
            RetainLeaseEvidence(cleanupException, lease);
            throw;
        }
        finally
        {
            DatabaseLifecycleLock.Release();
        }
    }

    private void CompleteLeaseCleanup(
        SqlServerTestDatabaseLease lease,
        SqlServerTestDatabaseCleanupOutcome outcome)
    {
        if (outcome == SqlServerTestDatabaseCleanupOutcome.OwnershipMismatch)
        {
            var exception = new InvalidOperationException(
                $"The API test database was not dropped because ownership could not be confirmed. The lease is retained for diagnosis and a later retry. Database={lease.DatabaseName}; database ID={lease.DatabaseId}; identity={lease.DatabaseIdentity}.");
            RetainLeaseEvidence(exception, lease);
            throw exception;
        }

        if (outcome is not SqlServerTestDatabaseCleanupOutcome.Dropped and
            not SqlServerTestDatabaseCleanupOutcome.AlreadyAbsent)
        {
            var exception = new InvalidOperationException(
                "The API test database cleanup returned an unknown outcome; the lease is retained.");
            RetainLeaseEvidence(exception, lease);
            throw exception;
        }

        Interlocked.CompareExchange(ref _databaseLease, null, lease);
    }

    private static void RetainLeaseEvidence(
        Exception exception,
        SqlServerTestDatabaseLease lease)
    {
        exception.Data["SqlServerTestDatabaseLease"] = lease;
    }

    private void DropDatabase() =>
        DropDatabaseAsync().GetAwaiter().GetResult();

    private void ClearDatabaseConnectionPool()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        using var pooledConnection = new SqlConnection(ConnectionString);
        SqlConnection.ClearPool(pooledConnection);
    }
}
