using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.IntegrationTests.Infrastructure;

/// <summary>
/// Records every attempt to open a database connection from the API test host.
/// The mock host must never open one; a non-zero count fails the safety tests.
/// </summary>
public sealed class ConnectionAttemptRecorder : DbConnectionInterceptor
{
    private int _connectionAttempts;

    public int ConnectionAttempts => Volatile.Read(ref _connectionAttempts);

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        Interlocked.Increment(ref _connectionAttempts);
        return base.ConnectionOpening(connection, eventData, result);
    }

    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _connectionAttempts);
        return base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }
}
