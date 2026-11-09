using Dvld.TestSupport;
using Microsoft.Data.SqlClient;

namespace API.IntegrationTests.Infrastructure;

internal sealed class SqlServerApiFactoryInitializationException
    : AggregateException
{
    public SqlServerTestDatabaseLease DatabaseLease { get; }

    public string ExpectedInstanceIdentity { get; }

    public SqlServerApiFactoryInitializationException(
        Exception migrationException,
        Exception cleanupException,
        SqlServerTestDatabaseLease databaseLease,
        string expectedInstanceIdentity)
        : base(
            "API test database migration and cleanup both failed. The lease is retained for diagnosis and explicit recovery.",
            migrationException,
            cleanupException)
    {
        DatabaseLease = databaseLease;
        ExpectedInstanceIdentity = expectedInstanceIdentity;
    }

    public async Task<SqlServerTestDatabaseCleanupOutcome> RetryCleanupAsync(
        CancellationToken cancellationToken = default)
    {
        var target = SqlServerTestEnvironment.ReadConfigurationFromEnvironment();
        if (!string.Equals(
                target.ExpectedInstanceIdentity,
                ExpectedInstanceIdentity,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The configured expected SQL Server instance no longer matches the failed test database lease.");
        }

        SqlServerTestEnvironment.ValidateOwnedDatabaseLease(DatabaseLease);

        await using var connection = new SqlConnection(
            target.AdminConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await SqlServerTestEnvironment.DropOwnedDatabaseAsync(
            connection,
            target,
            DatabaseLease,
            cancellationToken).ConfigureAwait(false);
    }
}
