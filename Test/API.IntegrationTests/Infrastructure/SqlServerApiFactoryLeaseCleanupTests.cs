using Dvld.TestSupport;

namespace API.IntegrationTests.Infrastructure;

public sealed class SqlServerApiFactoryLeaseCleanupTests
{
    [Fact]
    public async Task CleanupFailure_RetainsLeaseAndRetryCanClearIt()
    {
        var lease = CreateLease();
        var cleanupCalls = 0;
        var factory = CreateFactory(
            lease,
            _ =>
            {
                cleanupCalls++;
                return cleanupCalls == 1
                    ? Task.FromException<SqlServerTestDatabaseCleanupOutcome>(
                        new IOException("temporary cleanup failure"))
                    : Task.FromResult(SqlServerTestDatabaseCleanupOutcome.Dropped);
            });

        await Assert.ThrowsAsync<IOException>(
            async () => await factory.DisposeAsync());

        Assert.Same(lease, factory.DatabaseLeaseForTesting);

        await factory.DisposeAsync();

        Assert.Null(factory.DatabaseLeaseForTesting);
        Assert.Equal(2, cleanupCalls);
    }

    [Fact]
    public async Task AlreadyAbsent_ClearsLeaseIdempotentlyWithoutAnotherCleanup()
    {
        var lease = CreateLease();
        var cleanupCalls = 0;
        var factory = CreateFactory(
            lease,
            _ =>
            {
                cleanupCalls++;
                return Task.FromResult(
                    SqlServerTestDatabaseCleanupOutcome.AlreadyAbsent);
            });

        await factory.DisposeAsync();
        await factory.DisposeAsync();

        Assert.Null(factory.DatabaseLeaseForTesting);
        Assert.Equal(1, cleanupCalls);
    }

    [Fact]
    public async Task OwnershipMismatch_RetainsLeaseAndNeverInvokesDropAction()
    {
        var lease = CreateLease();
        var ownershipVerified = false;
        var dropCalls = 0;
        var factory = CreateFactory(
            lease,
            _ =>
            {
                if (!ownershipVerified)
                {
                    return Task.FromResult(
                        SqlServerTestDatabaseCleanupOutcome.OwnershipMismatch);
                }

                dropCalls++;
                return Task.FromResult(SqlServerTestDatabaseCleanupOutcome.Dropped);
            });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await factory.DisposeAsync());

        Assert.Same(lease, factory.DatabaseLeaseForTesting);
        Assert.Equal(0, dropCalls);

        ownershipVerified = true;
        await factory.DisposeAsync();

        Assert.Null(factory.DatabaseLeaseForTesting);
        Assert.Equal(1, dropCalls);
    }

    [Fact]
    public async Task UnknownCleanupOutcome_RetainsLease()
    {
        var lease = CreateLease();
        var factory = CreateFactory(
            lease,
            _ => Task.FromResult(
                (SqlServerTestDatabaseCleanupOutcome)0));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await factory.DisposeAsync());

        Assert.Same(lease, factory.DatabaseLeaseForTesting);
    }

    [Fact]
    public async Task ConcurrentDisposal_CleansTheSameLeaseOnce()
    {
        var lease = CreateLease();
        var cleanupCalls = 0;
        var cleanupStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowCleanupToFinish = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var factory = new SqlServerApiWebApplicationFactory(
            lease,
            async receivedLease =>
            {
                Assert.Same(lease, receivedLease);
                Interlocked.Increment(ref cleanupCalls);
                cleanupStarted.TrySetResult(true);
                await allowCleanupToFinish.Task;
                return SqlServerTestDatabaseCleanupOutcome.Dropped;
            },
            () => ValueTask.CompletedTask,
            () => { });

        var firstAttempt = factory.DisposeAsync().AsTask();
        await cleanupStarted.Task;
        var concurrentAttempt = factory.DisposeAsync().AsTask();
        allowCleanupToFinish.TrySetResult(true);

        await Task.WhenAll(firstAttempt, concurrentAttempt);

        Assert.Equal(1, cleanupCalls);
        Assert.Null(factory.DatabaseLeaseForTesting);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid-token")]
    public void LeaseValidation_RejectsMissingOrMalformedRunToken(string? token)
    {
        var lease = CreateLease() with { RunToken = token! };

        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateOwnedDatabaseLease(lease));
    }

    [Fact]
    public void LeaseValidation_RejectsInvalidDatabaseIdentity()
    {
        var lease = CreateLease() with
        {
            DatabaseId = 0,
            DatabaseIdentity = Guid.Empty
        };

        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateOwnedDatabaseLease(lease));
    }

    [Fact]
    public void LeaseValidation_RejectsMalformedDatabaseName()
    {
        var lease = CreateLease() with
        {
            DatabaseName = "DVLD_IntegrationTests_untrusted"
        };

        Assert.Throws<InvalidOperationException>(
            () => SqlServerTestEnvironment.ValidateOwnedDatabaseLease(lease));
    }

    [Fact]
    public void LeaseValidation_RejectsNullLease()
    {
        Assert.Throws<ArgumentNullException>(
            () => SqlServerTestEnvironment.ValidateOwnedDatabaseLease(null));
    }

    [Fact]
    public void InitializationFailureException_RetainsLeaseAndBothFailures()
    {
        var lease = CreateLease();
        var migrationFailure = new InvalidOperationException("migration failed");
        var cleanupFailure = new IOException("cleanup failed");
        var exception = new SqlServerApiFactoryInitializationException(
            migrationFailure,
            cleanupFailure,
            lease,
            "TESTHOST|MSSQLSERVER");

        Assert.Same(lease, exception.DatabaseLease);
        Assert.Equal("TESTHOST|MSSQLSERVER", exception.ExpectedInstanceIdentity);
        Assert.Collection(
            exception.InnerExceptions,
            inner => Assert.Same(migrationFailure, inner),
            inner => Assert.Same(cleanupFailure, inner));
    }

    private static SqlServerApiWebApplicationFactory CreateFactory(
        SqlServerTestDatabaseLease lease,
        Func<SqlServerTestDatabaseLease,
            Task<SqlServerTestDatabaseCleanupOutcome>> cleanup) =>
        new(
            lease,
            cleanup,
            () => ValueTask.CompletedTask,
            () => { });

    private static SqlServerTestDatabaseLease CreateLease() =>
        new(
            $"DVLD_IntegrationTests_Api_{Guid.NewGuid():N}",
            Guid.NewGuid().ToString("N"),
            42,
            Guid.NewGuid());
}
