using System.Runtime.ExceptionServices;

namespace API.IntegrationTests.Infrastructure;

internal sealed class SqlServerApiFactoryDisposal
{
    private const int Active = 0;
    private const int Synchronous = 1;
    private const int Asynchronous = 2;

    private readonly Func<ValueTask> _disposeBaseAsync;
    private readonly Action _disposeBaseSynchronously;
    private readonly Func<Task> _cleanupAsync;
    private readonly Action _cleanupSynchronously;
    private readonly SemaphoreSlim _cleanupGate = new(1, 1);

    private int _mode;
    private int _asyncBaseDisposeInProgress;
    private int _synchronousBaseDisposeInProgress;
    private bool _cleanupCompleted;

    public SqlServerApiFactoryDisposal(
        Func<ValueTask> disposeBaseAsync,
        Action disposeBaseSynchronously,
        Func<Task> cleanupAsync,
        Action cleanupSynchronously)
    {
        _disposeBaseAsync = disposeBaseAsync;
        _disposeBaseSynchronously = disposeBaseSynchronously;
        _cleanupAsync = cleanupAsync;
        _cleanupSynchronously = cleanupSynchronously;
    }

    public async ValueTask DisposeAsync()
    {
        var priorMode = Interlocked.CompareExchange(
            ref _mode,
            Asynchronous,
            Active);
        var synchronousDisposalOwnsCleanup =
            priorMode == Synchronous &&
            Volatile.Read(ref _synchronousBaseDisposeInProgress) > 0;
        Exception? disposalException = null;

        Interlocked.Increment(ref _asyncBaseDisposeInProgress);
        try
        {
            await _disposeBaseAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            disposalException = exception;
        }
        finally
        {
            Interlocked.Decrement(ref _asyncBaseDisposeInProgress);
        }

        // WebApplicationFactory.DisposeAsync calls Dispose(true) virtually.
        // During synchronous disposal, that re-enters this method; the outer
        // synchronous path owns cleanup in that case.
        if (synchronousDisposalOwnsCleanup)
        {
            Rethrow(disposalException);
            return;
        }

        Exception? cleanupException = null;
        try
        {
            await CleanupAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            cleanupException = exception;
        }

        ThrowFailures(disposalException, cleanupException);
    }

    public void DisposeSynchronously()
    {
        var priorMode = Interlocked.CompareExchange(
            ref _mode,
            Synchronous,
            Active);
        var cleanupOwnedByAnotherDisposal =
            (priorMode == Asynchronous &&
             Volatile.Read(ref _asyncBaseDisposeInProgress) > 0) ||
            (priorMode == Synchronous &&
             Volatile.Read(ref _synchronousBaseDisposeInProgress) > 0);
        Exception? disposalException = null;

        Interlocked.Increment(ref _synchronousBaseDisposeInProgress);
        try
        {
            _disposeBaseSynchronously();
        }
        catch (Exception exception)
        {
            disposalException = exception;
        }
        finally
        {
            Interlocked.Decrement(ref _synchronousBaseDisposeInProgress);
        }

        // An async disposal already owns database cleanup, or this call is
        // the virtual Dispose(bool) callback nested inside base disposal.
        if (cleanupOwnedByAnotherDisposal)
        {
            Rethrow(disposalException);
            return;
        }

        Exception? cleanupException = null;
        try
        {
            CleanupSynchronously();
        }
        catch (Exception exception)
        {
            cleanupException = exception;
        }

        ThrowFailures(disposalException, cleanupException);
    }

    private async Task CleanupAsync()
    {
        await _cleanupGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cleanupCompleted)
            {
                return;
            }

            await _cleanupAsync().ConfigureAwait(false);
            _cleanupCompleted = true;
        }
        finally
        {
            _cleanupGate.Release();
        }
    }

    private void CleanupSynchronously()
    {
        _cleanupGate.Wait();
        try
        {
            if (_cleanupCompleted)
            {
                return;
            }

            _cleanupSynchronously();
            _cleanupCompleted = true;
        }
        finally
        {
            _cleanupGate.Release();
        }
    }

    private static void ThrowFailures(
        Exception? disposalException,
        Exception? cleanupException)
    {
        if (disposalException is not null && cleanupException is not null)
        {
            throw new AggregateException(
                "Base factory disposal and test database cleanup both failed.",
                disposalException,
                cleanupException);
        }

        Rethrow(disposalException ?? cleanupException);
    }

    private static void Rethrow(Exception? exception)
    {
        if (exception is not null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
