using Application.Common.Results;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DVLDDbContext _context;

    public UnitOfWork(DVLDDbContext context)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        return new UnitOfWorkTransaction(_context, transaction);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        var transaction =
            await _context.Database.BeginTransactionAsync(
                isolationLevel,
                cancellationToken);

        return new UnitOfWorkTransaction(_context, transaction);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<IUnitOfWorkTransaction, Task<T>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var dbTransaction =
                await _context.Database.BeginTransactionAsync(
                    isolationLevel,
                    cancellationToken);

            await using var transaction =
                new UnitOfWorkTransaction(
                    _context,
                    dbTransaction);

            try
            {
                var result = await operation(transaction);

                if (result is Result { IsFailure: true })
                {
                    if (!transaction.IsCompleted)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    _context.ChangeTracker.Clear();

                    return result;
                }

                if (!transaction.IsCompleted)
                    await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch
            {
                try
                {
                    if (!transaction.IsCompleted)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                    }
                }
                catch
                {
                    // Preserve the original exception if rollback fails.
                }
                finally
                {
                    _context.ChangeTracker.Clear();
                }

                throw;
            }
        });
    }

    private sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly DVLDDbContext _context;
        private readonly IDbContextTransaction _transaction;

        private bool _isCompleted;
        private bool _isRolledBack;

        public UnitOfWorkTransaction(
            DVLDDbContext context,
            IDbContextTransaction transaction)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));

            _transaction = transaction
                ?? throw new ArgumentNullException(nameof(transaction));
        }

        internal bool IsCompleted => _isCompleted;

        public async Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isRolledBack)
            {
                throw new InvalidOperationException(
                    "Cannot commit a transaction that has already been rolled back.");
            }

            if (_isCompleted)
                return;

            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);

            _isCompleted = true;
        }

        public async Task RollbackAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isCompleted)
                return;

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                _context.ChangeTracker.Clear();
                _isCompleted = true;
                _isRolledBack = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!_isCompleted)
                {
                    _context.ChangeTracker.Clear();
                }
            }
            finally
            {
                await _transaction.DisposeAsync();
            }
        }
    }
}
