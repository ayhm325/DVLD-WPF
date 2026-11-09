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

    public override async ValueTask DisposeAsync()
    {
        Exception? disposalException = null;

        try
        {
            await base.DisposeAsync();
        }
        catch (Exception ex)
        {
            disposalException = ex;
        }

        try
        {
            await DropDatabaseAsync();
        }
        catch (Exception cleanupException)
        {
            if (disposalException is not null)
            {
                SqlServerTestEnvironment.ReportCleanupFailureSafely(
                    "API test database cleanup failed during disposal. Primary exception type={0}; cleanup exception type={1}.",
                    disposalException,
                    cleanupException);
            }
            else
            {
                throw;
            }
        }

        if (disposalException is not null)
        {
            ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(false);
            return;
        }

        Exception? disposalException = null;

        try
        {
            base.Dispose(true);
        }
        catch (Exception ex)
        {
            disposalException = ex;
        }

        try
        {
            DropDatabase();
        }
        catch (Exception cleanupException)
        {
            if (disposalException is not null)
            {
                SqlServerTestEnvironment.ReportCleanupFailureSafely(
                    "API test database cleanup failed during synchronous disposal. Primary exception type={0}; cleanup exception type={1}.",
                    disposalException,
                    cleanupException);
            }
            else
            {
                throw;
            }
        }

        if (disposalException is not null)
        {
            ExceptionDispatchInfo.Capture(disposalException).Throw();
        }
    }

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
            }

            ExceptionDispatchInfo.Capture(migrationException).Throw();
        }
    }

    private async Task DropDatabaseAsync()
    {
        var lease = Interlocked.Exchange(ref _databaseLease, null);
        var target = _target;
        if (lease is null || target is null)
        {
            return;
        }

        ClearDatabaseConnectionPool();
        await DatabaseLifecycleLock.WaitAsync();
        try
        {
            await using var connection = new SqlConnection(
                target.AdminConnectionString);
            await connection.OpenAsync();
            var dropped = await SqlServerTestEnvironment.DropOwnedDatabaseAsync(
                connection,
                target,
                lease);
            if (!dropped)
            {
                throw new InvalidOperationException(
                    "The API test database was not dropped because ownership could not be confirmed.");
            }
        }
        finally
        {
            DatabaseLifecycleLock.Release();
        }
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
