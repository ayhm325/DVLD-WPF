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
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        return new UnitOfWorkTransaction(transaction);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database
            .BeginTransactionAsync(
                isolationLevel,
                cancellationToken);

        return new UnitOfWorkTransaction(transaction);
    }

    private sealed class UnitOfWorkTransaction
        : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;
        private bool _isCompleted;

        public UnitOfWorkTransaction(
            IDbContextTransaction transaction)
        {
            _transaction = transaction
                ?? throw new ArgumentNullException(nameof(transaction));
        }

        internal bool IsCompleted => _isCompleted;

        public async Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isCompleted)
            {
                return;
            }

            await _transaction.CommitAsync(cancellationToken);
            _isCompleted = true;
        }

        public async Task RollbackAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isCompleted)
            {
                return;
            }

            await _transaction.RollbackAsync(cancellationToken);
            _isCompleted = true;
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
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
            await using var transaction =
                new UnitOfWorkTransaction(
                    await _context.Database.BeginTransactionAsync(
                        isolationLevel,
                        cancellationToken));

            try
            {
                var result = await operation(transaction);

                if (result is Result { IsFailure: true })
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _context.ChangeTracker.Clear();
                }
                else if (!transaction.IsCompleted)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                throw;
            }
        });
    }
}