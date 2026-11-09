namespace API.IntegrationTests.Infrastructure;

public sealed class SqlServerApiFactoryDisposalTests
{
    [Fact]
    public async Task DisposeAsync_UsesAsyncCleanupOnlyAndOnlyOnce()
    {
        var asyncCleanupCalls = 0;
        var syncCleanupCalls = 0;
        var factory = new SqlServerApiWebApplicationFactory(
            () =>
            {
                asyncCleanupCalls++;
                return Task.CompletedTask;
            },
            () => syncCleanupCalls++);

        await factory.DisposeAsync();
        await factory.DisposeAsync();

        Assert.Equal(1, asyncCleanupCalls);
        Assert.Equal(0, syncCleanupCalls);
    }

    [Fact]
    public void Dispose_UsesSynchronousCleanupOnlyAndOnlyOnce()
    {
        var asyncCleanupCalls = 0;
        var syncCleanupCalls = 0;
        var factory = new SqlServerApiWebApplicationFactory(
            () =>
            {
                asyncCleanupCalls++;
                return Task.CompletedTask;
            },
            () => syncCleanupCalls++);

        factory.Dispose();
        factory.Dispose();

        Assert.Equal(0, asyncCleanupCalls);
        Assert.Equal(1, syncCleanupCalls);
    }

    [Fact]
    public async Task DisposeAsync_BaseFailureStillAttemptsCleanup()
    {
        var baseFailure = new InvalidOperationException("base disposal failed");
        var cleanupCalls = 0;
        var factory = new SqlServerApiWebApplicationFactory(
            () =>
            {
                cleanupCalls++;
                return Task.CompletedTask;
            },
            () => { },
            () => ValueTask.FromException(baseFailure),
            () => { });

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await factory.DisposeAsync());

        Assert.Same(baseFailure, thrown);
        Assert.Equal(1, cleanupCalls);
    }

    [Fact]
    public void Dispose_BaseFailureStillAttemptsSynchronousCleanup()
    {
        var baseFailure = new InvalidOperationException("base disposal failed");
        var cleanupCalls = 0;
        var factory = new SqlServerApiWebApplicationFactory(
            () => Task.CompletedTask,
            () => cleanupCalls++,
            () => ValueTask.FromException(baseFailure),
            () => throw baseFailure);

        var thrown = Assert.Throws<InvalidOperationException>(
            factory.Dispose);

        Assert.Same(baseFailure, thrown);
        Assert.Equal(1, cleanupCalls);
    }

    [Fact]
    public async Task DisposeAsync_CleanupFailureDoesNotHideBaseFailure()
    {
        var baseFailure = new InvalidOperationException("base disposal failed");
        var cleanupFailure = new IOException("database cleanup failed");
        var factory = new SqlServerApiWebApplicationFactory(
            () => Task.FromException(cleanupFailure),
            () => { },
            () => ValueTask.FromException(baseFailure),
            () => { });

        var thrown = await Assert.ThrowsAsync<AggregateException>(
            async () => await factory.DisposeAsync());

        Assert.Collection(
            thrown.InnerExceptions,
            exception => Assert.Same(baseFailure, exception),
            exception => Assert.Same(cleanupFailure, exception));
    }

    [Fact]
    public async Task CleanupFailureCanBeRetriedThroughAnotherDisposalPath()
    {
        var cleanupAttempts = 0;
        var factory = new SqlServerApiWebApplicationFactory(
            () =>
            {
                cleanupAttempts++;
                return cleanupAttempts == 1
                    ? Task.FromException(new IOException("temporary cleanup failure"))
                    : Task.CompletedTask;
            },
            () => cleanupAttempts++);

        await Assert.ThrowsAsync<IOException>(
            async () => await factory.DisposeAsync());
        factory.Dispose();

        Assert.Equal(2, cleanupAttempts);
    }

    [Fact]
    public async Task ConcurrentCleanupAttempts_DoNotRunSameCleanupTwice()
    {
        var cleanupCalls = 0;
        var cleanupStarted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var allowCleanupToFinish = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new SqlServerApiFactoryDisposal(
            () => ValueTask.CompletedTask,
            () => { },
            async () =>
            {
                Interlocked.Increment(ref cleanupCalls);
                cleanupStarted.SetResult(true);
                await allowCleanupToFinish.Task;
            },
            () => { });

        var firstAttempt = coordinator.DisposeAsync().AsTask();
        await cleanupStarted.Task;
        var concurrentAttempt = coordinator.DisposeAsync().AsTask();
        allowCleanupToFinish.SetResult(true);

        await Task.WhenAll(firstAttempt, concurrentAttempt);

        Assert.Equal(1, cleanupCalls);
    }

    [Fact]
    public async Task OwnershipVerificationFailure_DoesNotInvokeDropAndRetainsRetryPath()
    {
        var ownershipVerified = false;
        var dropCalls = 0;
        var factory = new SqlServerApiWebApplicationFactory(
            () =>
            {
                if (!ownershipVerified)
                {
                    return Task.FromException(
                        new InvalidOperationException("ownership was not verified"));
                }

                Interlocked.Increment(ref dropCalls);
                return Task.CompletedTask;
            },
            () => { });

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await factory.DisposeAsync());
        Assert.Equal(0, dropCalls);

        ownershipVerified = true;
        await factory.DisposeAsync();

        Assert.Equal(1, dropCalls);
    }
}
