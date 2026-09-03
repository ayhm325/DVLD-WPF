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

        public UnitOfWorkTransaction(
            IDbContextTransaction transaction)
        {
            _transaction = transaction
                ?? throw new ArgumentNullException(nameof(transaction));
        }

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        public Task RollbackAsync(
            CancellationToken cancellationToken = default)
        {
            return _transaction.RollbackAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}