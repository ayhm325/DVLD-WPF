using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.Data.SqlClient;

namespace Dvld.TestSupport;

public sealed record SqlServerTestTarget(
    string AdminConnectionString,
    string ExpectedInstanceIdentity);

public sealed record SqlServerTestDatabaseLease(
    string DatabaseName,
    string RunToken,
    int DatabaseId,
    Guid DatabaseIdentity);

public enum SqlServerTestDatabaseCleanupOutcome
{
    Dropped = 1,
    AlreadyAbsent = 2,
    OwnershipMismatch = 3
}

public static class SqlServerTestEnvironment
{
    private const string RunTokenProperty = "DVLD_Test_Run_Token";
    private const string ExpectedInstanceEnvironmentVariable =
        "DVLD_TEST_EXPECTED_INSTANCE";
    private const string InfrastructureDatabasePrefix =
        "DVLD_IntegrationTests_";
    private const string ApiDatabasePrefix =
        "DVLD_IntegrationTests_Api_";

    public static SqlServerTestTarget ValidateConfiguration(
        string? connectionString,
        string? expectedInstanceIdentity)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION must be explicitly configured.");
        }

        if (string.IsNullOrWhiteSpace(expectedInstanceIdentity))
        {
            throw new InvalidOperationException(
                $"{ExpectedInstanceEnvironmentVariable} must identify the approved SQL Server instance.");
        }

        DbConnectionStringBuilder rawBuilder;
        try
        {
            rawBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION is not a valid connection string.");
        }

        if (rawBuilder.ContainsKey("AttachDBFilename"))
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION must not include AttachDBFilename.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION is not a valid SQL Server connection string.");
        }

        if (builder.UserInstance ||
            !string.IsNullOrWhiteSpace(builder.FailoverPartner))
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION must not use user instances or failover redirection.");
        }

        if (builder.InitialCatalog.Equals(
                "DVLD",
                StringComparison.OrdinalIgnoreCase) ||
            builder.InitialCatalog.Equals(
                "DVLDf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION cannot target an application database.");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION must specify a SQL Server endpoint.");
        }

        if (builder.ApplicationIntent != ApplicationIntent.ReadWrite)
        {
            throw new InvalidOperationException(
                "DVLD_TEST_CONNECTION must use a writable test SQL Server endpoint.");
        }

        if (!builder.IntegratedSecurity)
        {
            throw new InvalidOperationException(
                "This SQL Server instance is configured for Windows Authentication; use Integrated Security.");
        }

        if (!string.IsNullOrWhiteSpace(builder.UserID) ||
            !string.IsNullOrWhiteSpace(builder.Password))
        {
            throw new InvalidOperationException(
                "Windows-authenticated test connections must not include SQL login credentials.");
        }

        builder.InitialCatalog = "master";
        builder.AttachDBFilename = string.Empty;
        builder.Enlist = false;

        return new SqlServerTestTarget(
            builder.ConnectionString,
            expectedInstanceIdentity.Trim());
    }

    public static SqlServerTestTarget ReadConfigurationFromEnvironment()
    {
        return ValidateConfiguration(
            Environment.GetEnvironmentVariable("DVLD_TEST_CONNECTION"),
            Environment.GetEnvironmentVariable(
                ExpectedInstanceEnvironmentVariable));
    }

    public static string BuildDatabaseName(string prefix)
    {
        if (!string.Equals(prefix, InfrastructureDatabasePrefix, StringComparison.Ordinal) &&
            !string.Equals(prefix, ApiDatabasePrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Only approved Infrastructure and API integration-test prefixes are allowed.",
                nameof(prefix));
        }

        var databaseName = $"{prefix}{Guid.NewGuid():N}";
        ValidateGeneratedDatabaseName(databaseName);
        return databaseName;
    }

    public static string BuildDatabaseConnectionString(
        SqlServerTestTarget target,
        string databaseName)
    {
        ValidateGeneratedDatabaseName(databaseName);
        var builder = new SqlConnectionStringBuilder(
            target.AdminConnectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    public static async Task VerifyExpectedInstanceAsync(
        SqlConnection connection,
        SqlServerTestTarget target,
        CancellationToken cancellationToken = default)
    {
        EnsureAdministrativeConnection(connection);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ServerName')) + N'|' + CONVERT(nvarchar(128), @@SERVICENAME);";

        var actualIdentity = Convert.ToString(
            await command.ExecuteScalarAsync(cancellationToken));

        if (!string.Equals(
                actualIdentity,
                target.ExpectedInstanceIdentity,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The connected SQL Server instance does not match DVLD_TEST_EXPECTED_INSTANCE.");
        }
    }

    public static async Task<SqlServerTestDatabaseLease> CreateDatabaseAsync(
        SqlConnection connection,
        SqlServerTestTarget target,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        EnsureAdministrativeConnection(connection);
        ValidateGeneratedDatabaseName(databaseName);
        await VerifyExpectedInstanceAsync(
            connection,
            target,
            cancellationToken);
        var runToken = Guid.NewGuid().ToString("N");

        await using (var checkCommand = connection.CreateCommand())
        {
            checkCommand.CommandText = "SELECT DB_ID(@databaseName);";
            checkCommand.Parameters.AddWithValue("@databaseName", databaseName);
            var existingId = await checkCommand.ExecuteScalarAsync(
                cancellationToken);

            if (existingId is not null and not DBNull)
            {
                throw new InvalidOperationException(
                    "The generated test database name already exists; it will not be reused or overwritten.");
            }
        }

        await using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText =
                $"CREATE DATABASE [{databaseName}];";
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        int databaseId;
        Guid databaseIdentity;
        try
        {
            await using var identityCommand = connection.CreateCommand();
            identityCommand.CommandText =
                "SELECT database_id, service_broker_guid FROM sys.databases WHERE name = @databaseName;";
            identityCommand.Parameters.AddWithValue("@databaseName", databaseName);

            await using var reader = await identityCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "The newly created test database could not be identified for ownership tracking.");
            }

            databaseId = reader.GetInt32(0);
            databaseIdentity = reader.GetGuid(1);
        }
        catch (Exception identityException)
        {
            ReportFailureSafely(
                "A generated test database was created but its ownership identity could not be captured; it was left untouched. Database={0}; exception type={1}.",
                databaseName,
                identityException.GetType().FullName);
            throw;
        }

        var lease = new SqlServerTestDatabaseLease(
            databaseName,
            runToken,
            databaseId,
            databaseIdentity);
        try
        {
            await using var markerCommand = connection.CreateCommand();
            markerCommand.CommandText =
                """
                DECLARE @markerSql nvarchar(max) =
                    N'USE ' + QUOTENAME(@databaseName) +
                    N'; EXEC sys.sp_addextendedproperty @name=@propertyName, @value=@runToken;';
                EXEC sys.sp_executesql
                    @markerSql,
                    N'@propertyName sysname, @runToken nvarchar(64)',
                    @propertyName = @propertyName,
                    @runToken = @runToken;
                """;
            markerCommand.Parameters.AddWithValue(
                "@databaseName",
                databaseName);
            markerCommand.Parameters.AddWithValue(
                "@propertyName",
                RunTokenProperty);
            markerCommand.Parameters.AddWithValue("@runToken", runToken);
            await markerCommand.ExecuteNonQueryAsync(cancellationToken);
            EnsureAdministrativeConnection(connection);
            return lease;
        }
        catch (Exception markerException)
        {
            try
            {
                await using var cleanupConnection = new SqlConnection(
                    target.AdminConnectionString);
                await cleanupConnection.OpenAsync(CancellationToken.None);
                await VerifyExpectedInstanceAsync(
                    cleanupConnection,
                    target,
                    CancellationToken.None);

                var cleanupOutcome = await DropGeneratedDatabaseAsync(
                    cleanupConnection,
                    target,
                    databaseName,
                    databaseId,
                    databaseIdentity,
                    runToken: null,
                    CancellationToken.None);
                if (cleanupOutcome ==
                    SqlServerTestDatabaseCleanupOutcome.OwnershipMismatch)
                {
                    ReportFailureSafely(
                        "Test database cleanup was not confirmed after marker creation failed. Database={0}; primary exception type={1}.",
                        databaseName,
                        markerException.GetType().FullName);
                }
            }
            catch (Exception cleanupException)
            {
                ReportFailureSafely(
                    "Test database cleanup failed after marker creation failed. Database={0}; primary exception type={1}; cleanup exception type={2}.",
                    databaseName,
                    markerException.GetType().FullName,
                    cleanupException.GetType().FullName);
            }

            ExceptionDispatchInfo.Capture(markerException).Throw();
            throw;
        }
    }

    public static async Task<SqlServerTestDatabaseCleanupOutcome>
        DropOwnedDatabaseAsync(
        SqlConnection connection,
        SqlServerTestTarget target,
        SqlServerTestDatabaseLease? lease,
        CancellationToken cancellationToken = default)
    {
        if (lease is null)
        {
            throw new ArgumentNullException(nameof(lease));
        }

        ValidateOwnedDatabaseLease(lease);
        return await DropGeneratedDatabaseAsync(
            connection,
            target,
            lease.DatabaseName,
            lease.DatabaseId,
            lease.DatabaseIdentity,
            lease.RunToken,
            cancellationToken);
    }

    public static void ValidateOwnedDatabaseLease(
        SqlServerTestDatabaseLease? lease)
    {
        if (lease is null)
        {
            throw new ArgumentNullException(nameof(lease));
        }

        ValidateGeneratedDatabaseName(lease.DatabaseName);
        if (lease.DatabaseId <= 0 || lease.DatabaseIdentity == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Database cleanup requires a valid database identity lease.");
        }

        if (string.IsNullOrWhiteSpace(lease.RunToken) ||
            !Guid.TryParseExact(lease.RunToken, "N", out _))
        {
            throw new InvalidOperationException(
                "Database cleanup requires a valid run-token ownership marker.");
        }
    }

    private static async Task<SqlServerTestDatabaseCleanupOutcome>
        DropGeneratedDatabaseAsync(
        SqlConnection connection,
        SqlServerTestTarget target,
        string databaseName,
        int databaseId,
        Guid databaseIdentity,
        string? runToken,
        CancellationToken cancellationToken)
    {
        EnsureAdministrativeConnection(connection);
        ValidateGeneratedDatabaseName(databaseName);
        await VerifyExpectedInstanceAsync(
            connection,
            target,
            cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DECLARE @databaseId int = DB_ID(@databaseName);
            DECLARE @outcome int = 3;

            IF @databaseId IS NULL
            BEGIN
                DECLARE @canConfirmAbsence bit = CASE
                    WHEN HAS_PERMS_BY_NAME(NULL, NULL, N'VIEW ANY DATABASE') = 1
                      OR HAS_PERMS_BY_NAME(DB_NAME(), N'DATABASE', N'CREATE DATABASE') = 1
                    THEN 1 ELSE 0 END;

                IF @canConfirmAbsence = 1
                   AND NOT EXISTS
                       (SELECT 1 FROM sys.databases WHERE name = @databaseName)
                BEGIN
                    SET @outcome = 2;
                END;

                SELECT @outcome;
                RETURN;
            END;

            DECLARE @isOwned bit = CASE WHEN @runToken IS NULL THEN 1 ELSE 0 END;
            DECLARE @actualDatabaseIdentity uniqueidentifier =
                (SELECT service_broker_guid FROM sys.databases WHERE database_id = @databaseId);

            IF @databaseId = @expectedDatabaseId
               AND @actualDatabaseIdentity = @expectedDatabaseIdentity
               AND @runToken IS NOT NULL
            BEGIN
                DECLARE @ownershipSql nvarchar(max) =
                    N'USE ' + QUOTENAME(@databaseName) + N'; '
                    + N'SELECT @isOwned = CASE WHEN EXISTS '
                    + N'(SELECT 1 FROM sys.extended_properties '
                    + N'WHERE class = 0 AND major_id = 0 '
                    + N'AND name = @markerName '
                    + N'AND CONVERT(nvarchar(64), value) = @expectedToken) '
                    + N'THEN 1 ELSE 0 END;';
                EXEC sys.sp_executesql
                    @ownershipSql,
                    N'@expectedToken nvarchar(64), @markerName sysname, @isOwned bit OUTPUT',
                    @expectedToken = @runToken,
                    @markerName = @markerName,
                    @isOwned = @isOwned OUTPUT;
            END;

            IF @databaseId = @expectedDatabaseId
               AND @actualDatabaseIdentity = @expectedDatabaseIdentity
               AND @isOwned = 1
            BEGIN
                DECLARE @dropSql nvarchar(max) =
                    N'ALTER DATABASE ' + QUOTENAME(@databaseName) +
                    N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ' +
                    QUOTENAME(@databaseName) + N';';
                EXEC sys.sp_executesql @dropSql;
                SET @outcome = 1;
            END;

            SELECT @outcome;
            """;
        command.Parameters.AddWithValue("@databaseName", databaseName);
        command.Parameters.AddWithValue("@markerName", RunTokenProperty);
        command.Parameters.AddWithValue("@expectedDatabaseId", databaseId);
        command.Parameters.AddWithValue(
            "@expectedDatabaseIdentity",
            databaseIdentity);
        command.Parameters.AddWithValue(
            "@runToken",
            (object?)runToken ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return (SqlServerTestDatabaseCleanupOutcome)Convert.ToInt32(result);
    }

    public static void ValidateGeneratedDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Length > 128)
        {
            throw new InvalidOperationException(
                "Database lifecycle operations require a generated DVLD_IntegrationTests_<GUID> name.");
        }

        var approvedPrefix = databaseName.StartsWith(
                ApiDatabasePrefix,
                StringComparison.Ordinal)
            ? ApiDatabasePrefix
            : InfrastructureDatabasePrefix;

        if (!databaseName.StartsWith(approvedPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Database lifecycle operations require an approved integration-test prefix.");
        }

        var guidText = databaseName[approvedPrefix.Length..];
        if (guidText.Length != 32 ||
            !Guid.TryParseExact(guidText, "N", out _))
        {
            throw new InvalidOperationException(
                "Database lifecycle operations require a 32-character GUID suffix.");
        }
    }

    private static void EnsureAdministrativeConnection(
        SqlConnection connection)
    {
        if (connection.State != ConnectionState.Open ||
            !connection.Database.Equals(
                "master",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Database lifecycle operations require an open administrative connection to master.");
        }
    }

    public static void ReportCleanupFailureSafely(
        string message,
        Exception primaryException,
        Exception cleanupException)
    {
        ReportFailureSafely(
            message,
            primaryException.GetType().FullName,
            cleanupException.GetType().FullName);
    }

    private static void ReportFailureSafely(
        string message,
        params object?[] values)
    {
        try
        {
            Trace.TraceError(message, values);
        }
        catch
        {
            // A diagnostic listener must never replace the primary failure.
        }
    }
}
