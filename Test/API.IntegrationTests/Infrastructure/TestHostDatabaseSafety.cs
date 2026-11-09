using Infrastructure.Configuration;

namespace API.IntegrationTests.Infrastructure;

/// <summary>
/// Builds the connection string used by test hosts that never touch a database.
/// </summary>
internal static class TestHostDatabaseSafety
{
    // A LocalDB instance that is never created. Any accidental attempt to open the
    // connection fails immediately instead of reaching an application database.
    private const string UnreachableLocalDbDataSource =
        @"(localdb)\DVLD_TEST_HOST_NO_DATABASE";

    public static string CreateIsolatedPlaceholderConnectionString()
    {
        var databaseName =
            SqlServerConnectionSafety.ApprovedTestDatabasePrefix +
            "Api_" +
            Guid.NewGuid().ToString("N");

        var connectionString =
            $"Server={UnreachableLocalDbDataSource};" +
            $"Initial Catalog={databaseName};" +
            "Integrated Security=true;" +
            "TrustServerCertificate=true;" +
            "Connect Timeout=1;";

        // Fail fast if the placeholder ever stops satisfying the approved test pattern.
        return SqlServerConnectionSafety.EnsureSafeForHost(
            connectionString,
            SqlServerConnectionSafety.TestingEnvironmentName);
    }
}
