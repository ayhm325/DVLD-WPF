using Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public sealed class UnitOfWork
    : IUnitOfWork
{
    private readonly DVLDDbContext _context;


    public UnitOfWork(
        DVLDDbContext context)
    {
        _context =
            context
            ?? throw new ArgumentNullException(
                nameof(context));
    }


    // =========================================================
    // SAVE CHANGES
    // =========================================================

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(
            cancellationToken);
    }


    // =========================================================
    // BEGIN TRANSACTION
    // =========================================================

    public async Task<IUnitOfWorkTransaction>
        BeginTransactionAsync(
            CancellationToken cancellationToken = default)
    {
        var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    cancellationToken);

        return new UnitOfWorkTransaction(
            transaction);
    }


    // =========================================================
    // TRANSACTION IMPLEMENTATION
    // =========================================================

    private sealed class UnitOfWorkTransaction
        : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;


        public UnitOfWorkTransaction(
            IDbContextTransaction transaction)
        {
            _transaction =
                transaction
                ?? throw new ArgumentNullException(
                    nameof(transaction));
        }


        // =====================================================
        // COMMIT
        // =====================================================

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            return _transaction.CommitAsync(
                cancellationToken);
        }


        // =====================================================
        // ROLLBACK
        // =====================================================

        public Task RollbackAsync(
            CancellationToken cancellationToken = default)
        {
            return _transaction.RollbackAsync(
                cancellationToken);
        }


        // =====================================================
        // DISPOSE
        // =====================================================

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}