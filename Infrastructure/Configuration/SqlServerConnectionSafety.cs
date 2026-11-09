using System.Text.RegularExpressions;

using Microsoft.Data.SqlClient;

namespace Infrastructure.Configuration;

/// <summary>
/// Enforces the boundary between application databases (notably the protected
/// <c>DVLDf</c> database) and the ephemeral, isolated databases used by
/// integration tests.
/// </summary>
/// <remarks>
/// This type is a pure connection-string validator. It never opens a connection,
/// so rejecting a protected target cannot itself touch that target.
/// </remarks>
public static class SqlServerConnectionSafety
{
    /// <summary>
    /// Environment name used by test hosts (<see cref="Microsoft.AspNetCore.Hosting.IWebHostEnvironment.EnvironmentName"/>).
    /// </summary>
    public const string TestingEnvironmentName = "Testing";

    /// <summary>
    /// Environment variable set by test assemblies so every host they create is
    /// validated as a test host, regardless of the configured environment name.
    /// </summary>
    public const string TestHostEnvironmentVariable = "DVLD_TEST_HOST";

    /// <summary>
    /// Prefix reserved for databases created and owned by the test infrastructure.
    /// </summary>
    public const string ApprovedTestDatabasePrefix = "DVLD_IntegrationTests_";

    private const string ApprovedTestDatabasePattern =
        @"^DVLD_IntegrationTests_(Api_)?[0-9a-fA-F]{32}$";

    /// <summary>
    /// Application databases that must never be reachable from an automated test host.
    /// </summary>
    private static readonly string[] ProtectedDatabaseNames = ["DVLD", "DVLDf"];

    public static bool IsProtectedApplicationDatabase(string? databaseName) =>
        !string.IsNullOrWhiteSpace(databaseName) &&
        ProtectedDatabaseNames.Contains(databaseName.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool IsApprovedIsolatedTestDatabase(string? databaseName) =>
        !string.IsNullOrWhiteSpace(databaseName) &&
        Regex.IsMatch(
            databaseName.Trim(),
            ApprovedTestDatabasePattern,
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>
    /// Validates a resolved connection string against the hosting environment.
    /// Production and development hosts keep their legitimate configured target;
    /// test hosts may only target an isolated, generated test database.
    /// </summary>
    /// <returns>The validated connection string.</returns>
    /// <exception cref="InvalidOperationException">The test host target is missing, invalid, or protected.</exception>
    public static string EnsureSafeForHost(
        string? connectionString,
        string? environmentName)
    {
        if (!IsTestHost(environmentName))
        {
            return connectionString ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A test host requires an explicit, isolated test database connection string. " +
                "The protected application database is never an acceptable default.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            // Deliberately avoids echoing the connection string into the message.
            throw new InvalidOperationException(
                "The test host connection string is not a valid SQL Server connection string.");
        }

        var catalog = builder.InitialCatalog;

        if (IsProtectedApplicationDatabase(catalog))
        {
            throw new InvalidOperationException(
                $"The test host must never target the protected application database '{catalog}'.");
        }

        if (!IsApprovedIsolatedTestDatabase(catalog))
        {
            throw new InvalidOperationException(
                "The test host must target an isolated database named " +
                $"'{ApprovedTestDatabasePrefix}<GUID>'. " +
                $"The configured target '{catalog}' is not an approved test database.");
        }

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new InvalidOperationException(
                "The test host connection string must specify a SQL Server endpoint.");
        }

        if (!string.IsNullOrWhiteSpace(builder.AttachDBFilename) ||
            !string.IsNullOrWhiteSpace(builder.FailoverPartner) ||
            builder.UserInstance)
        {
            throw new InvalidOperationException(
                "The test host connection string must not use file attachment, user instances, or failover redirection.");
        }

        return connectionString;
    }

    public static bool IsTestHost(string? environmentName) =>
        string.Equals(
            environmentName,
            TestingEnvironmentName,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            Environment.GetEnvironmentVariable(TestHostEnvironmentVariable),
            "1",
            StringComparison.Ordinal);
}
