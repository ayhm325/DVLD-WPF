using Application.Common.Results;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Runtime.ExceptionServices;

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
        EnsureNoActiveTransaction();

        var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        return new UnitOfWorkTransaction(
            _context,
            transaction,
            deferPhysicalCommit: false);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        EnsureNoActiveTransaction();

        var transaction =
            await _context.Database.BeginTransactionAsync(
                isolationLevel,
                cancellationToken);

        return new UnitOfWorkTransaction(
            _context,
            transaction,
            deferPhysicalCommit: false);
    }

    public Task<T> ExecuteInTransactionAsync<T>(
        Func<IUnitOfWorkTransaction, Task<T>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default) =>
        ExecuteInTransactionCoreAsync(
            operation,
            verifySucceeded: null,
            isolationLevel,
            cancellationToken);

    public Task<T> ExecuteInTransactionAsync<T>(
        Func<IUnitOfWorkTransaction, Task<T>> operation,
        Func<CancellationToken, Task<bool>> verifySucceeded,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifySucceeded);

        return ExecuteInTransactionCoreAsync(
            operation,
            verifySucceeded,
            isolationLevel,
            cancellationToken);
    }

    private async Task<T> ExecuteInTransactionCoreAsync<T>(
        Func<IUnitOfWorkTransaction, Task<T>> operation,
        Func<CancellationToken, Task<bool>>? verifySucceeded,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        EnsureNoActiveTransaction();

        var strategy = _context.Database.CreateExecutionStrategy();
        var attempt = 0;

        return await strategy.ExecuteAsync(async () =>
        {
            if (++attempt > 1)
            {
                _context.ChangeTracker.Clear();
            }

            EnsureNoActiveTransaction();

            using var tracker = new TransactionVerificationTracker(_context);

            var dbTransaction =
                await _context.Database.BeginTransactionAsync(
                    isolationLevel,
                    cancellationToken);

            await using var transaction =
                new UnitOfWorkTransaction(
                    _context,
                    dbTransaction,
                    deferPhysicalCommit: true);

            var commitStageReached = false;

            try
            {
                var result = await operation(transaction);

                if (result is Result { IsFailure: true } ||
                    transaction.IsRolledBack)
                {
                    if (!transaction.IsRolledBack)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    _context.ChangeTracker.Clear();

                    return result;
                }

                if (!transaction.IsCompleted)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                tracker.Detach();
                commitStageReached = true;

                try
                {
                    await transaction.CommitPhysicalAsync(cancellationToken);
                    return result;
                }
                catch (Exception commitException)
                {
                    await transaction.DisposeAsync();
                    _context.ChangeTracker.Clear();

                    var committed = await VerifyCommitOutcomeAsync(
                        verifySucceeded,
                        tracker,
                        commitException);

                    if (committed)
                    {
                        return result;
                    }

                    ExceptionDispatchInfo.Capture(commitException).Throw();
                    throw;
                }
            }
            catch when (!commitStageReached)
            {
                try
                {
                    if (!transaction.IsRolledBack && !transaction.IsCommitted)
                    {
                        await transaction.RollbackPhysicalAsync(CancellationToken.None);
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

    private async Task<bool> VerifyCommitOutcomeAsync(
        Func<CancellationToken, Task<bool>>? verifySucceeded,
        TransactionVerificationTracker tracker,
        Exception commitException)
    {
        if (verifySucceeded is null && !tracker.CanVerify)
        {
            throw new InvalidOperationException(
                "Transaction commit failed with an ambiguous outcome and no verification probe is available to safely retry.",
                commitException);
        }

        try
        {
            var verificationStrategy = _context.Database.CreateExecutionStrategy();

            return await verificationStrategy.ExecuteAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                if (verifySucceeded is not null)
                {
                    return await verifySucceeded(CancellationToken.None);
                }

                return await tracker.VerifyCommittedAsync(
                    _context,
                    CancellationToken.None);
            });
        }
        catch (Exception verificationException)
        {
            throw new InvalidOperationException(
                "Transaction commit failed and the transaction outcome could not be verified because the database is unavailable.",
                new AggregateException(commitException, verificationException));
        }
    }

    private void EnsureNoActiveTransaction()
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active on this DbContext instance. Nested transactions are not supported.");
        }
    }

    private sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly DVLDDbContext _context;
        private readonly IDbContextTransaction _transaction;
        private readonly bool _deferPhysicalCommit;

        private bool _isCompleted;
        private bool _isCommitted;
        private bool _isRolledBack;
        private bool _isDisposed;

        public UnitOfWorkTransaction(
            DVLDDbContext context,
            IDbContextTransaction transaction,
            bool deferPhysicalCommit)
        {
            _context = context
                ?? throw new ArgumentNullException(nameof(context));

            _transaction = transaction
                ?? throw new ArgumentNullException(nameof(transaction));

            _deferPhysicalCommit = deferPhysicalCommit;
        }

        internal bool IsCompleted => _isCompleted;

        internal bool IsCommitted => _isCommitted;

        internal bool IsRolledBack => _isRolledBack;

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

            if (!_deferPhysicalCommit)
            {
                await _transaction.CommitAsync(cancellationToken);
                _isCommitted = true;
            }

            _isCompleted = true;
        }

        internal async Task CommitPhysicalAsync(
            CancellationToken cancellationToken)
        {
            if (_isCommitted)
                return;

            await _transaction.CommitAsync(cancellationToken);
            _isCommitted = true;
            _isCompleted = true;
        }

        public async Task RollbackAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isCompleted)
                return;

            await RollbackPhysicalAsync(cancellationToken);
        }

        internal async Task RollbackPhysicalAsync(
            CancellationToken cancellationToken)
        {
            if (_isRolledBack || _isCommitted)
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
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                if (!_isCommitted)
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

    private sealed class TransactionVerificationTracker : IDisposable
    {
        private readonly DVLDDbContext _context;
        private readonly List<(EntityEntry Entry, IReadOnlyList<IProperty> KeyProperties)> _stagedAddedEntries = [];
        private readonly List<StagedModifiedProbe> _stagedModifiedProbes = [];
        private readonly List<EntityProbeKey> _stagedDeletedProbes = [];

        private readonly HashSet<EntityProbeKey> _addedProbes = [];
        private readonly Dictionary<EntityProbeKey, Dictionary<string, object?>> _modifiedProbes = [];
        private readonly HashSet<EntityProbeKey> _deletedProbes = [];

        private bool _detached;

        public TransactionVerificationTracker(DVLDDbContext context)
        {
            _context = context;
            _context.SavingChanges += OnSavingChanges;
            _context.SavedChanges += OnSavedChanges;
        }

        public bool CanVerify =>
            _addedProbes.Count > 0 ||
            _modifiedProbes.Count > 0 ||
            _deletedProbes.Count > 0;

        public void Detach()
        {
            if (_detached)
                return;

            _detached = true;
            _context.SavingChanges -= OnSavingChanges;
            _context.SavedChanges -= OnSavedChanges;
            ClearStaged();
        }

        public void Dispose() => Detach();

        public async Task<bool> VerifyCommittedAsync(
            DVLDDbContext context,
            CancellationToken cancellationToken)
        {
            foreach (var added in _addedProbes)
            {
                var entity = await context.FindAsync(
                    added.EntityType,
                    added.KeyValues,
                    cancellationToken);

                context.ChangeTracker.Clear();

                if (entity is null)
                    return false;
            }

            if (_addedProbes.Count == 0)
            {
                foreach (var (probeKey, expectedProperties) in _modifiedProbes)
                {
                    var entity = await context.FindAsync(
                        probeKey.EntityType,
                        probeKey.KeyValues,
                        cancellationToken);

                    if (entity is null)
                    {
                        context.ChangeTracker.Clear();
                        return false;
                    }

                    var entry = context.Entry(entity);

                    foreach (var (propName, expectedValue) in expectedProperties)
                    {
                        var actualValue = entry.Property(propName).CurrentValue;
                        if (!ValuesMatch(actualValue, expectedValue))
                        {
                            context.ChangeTracker.Clear();
                            return false;
                        }
                    }

                    context.ChangeTracker.Clear();
                }

                foreach (var deleted in _deletedProbes)
                {
                    var entity = await context.FindAsync(
                        deleted.EntityType,
                        deleted.KeyValues,
                        cancellationToken);

                    context.ChangeTracker.Clear();

                    if (entity is not null)
                        return false;
                }
            }

            return true;
        }

        private void OnSavingChanges(object? sender, SavingChangesEventArgs e)
        {
            ClearStaged();

            foreach (var entry in _context.ChangeTracker.Entries())
            {
                var primaryKey = entry.Metadata.FindPrimaryKey();
                if (primaryKey is null)
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        _stagedAddedEntries.Add((entry, primaryKey.Properties));
                        break;

                    case EntityState.Modified:
                    {
                        var keyValues = ExtractKeyValues(entry, primaryKey.Properties);
                        var modifiedProps = entry.Properties
                            .Where(p =>
                                p.IsModified &&
                                !p.Metadata.IsPrimaryKey() &&
                                !ValuesMatch(p.OriginalValue, p.CurrentValue))
                            .Select(p => (p.Metadata.Name, p.CurrentValue))
                            .ToList();

                        if (modifiedProps.Count > 0)
                        {
                            _stagedModifiedProbes.Add(
                                new StagedModifiedProbe(
                                    entry.Metadata.ClrType,
                                    keyValues,
                                    modifiedProps));
                        }

                        break;
                    }

                    case EntityState.Deleted:
                    {
                        var keyValues = ExtractKeyValues(entry, primaryKey.Properties);
                        _stagedDeletedProbes.Add(
                            new EntityProbeKey(entry.Metadata.ClrType, keyValues));
                        break;
                    }
                }
            }
        }

        private void OnSavedChanges(object? sender, SavedChangesEventArgs e)
        {
            if (e.EntitiesSavedCount <= 0)
            {
                ClearStaged();
                return;
            }

            foreach (var (entry, keyProperties) in _stagedAddedEntries)
            {
                var keyValues = ExtractKeyValues(entry, keyProperties);
                var probeKey = new EntityProbeKey(entry.Metadata.ClrType, keyValues);
                _deletedProbes.Remove(probeKey);
                _addedProbes.Add(probeKey);
            }

            foreach (var modified in _stagedModifiedProbes)
            {
                var probeKey = new EntityProbeKey(modified.EntityType, modified.KeyValues);
                if (_addedProbes.Contains(probeKey))
                    continue;

                if (!_modifiedProbes.TryGetValue(probeKey, out var propMap))
                {
                    propMap = new Dictionary<string, object?>(StringComparer.Ordinal);
                    _modifiedProbes[probeKey] = propMap;
                }

                foreach (var (propName, currentValue) in modified.ModifiedProperties)
                {
                    propMap[propName] = currentValue;
                }
            }

            foreach (var deletedKey in _stagedDeletedProbes)
            {
                _addedProbes.Remove(deletedKey);
                _modifiedProbes.Remove(deletedKey);
                _deletedProbes.Add(deletedKey);
            }

            ClearStaged();
        }

        private void ClearStaged()
        {
            _stagedAddedEntries.Clear();
            _stagedModifiedProbes.Clear();
            _stagedDeletedProbes.Clear();
        }

        private static object?[] ExtractKeyValues(
            EntityEntry entry,
            IReadOnlyList<IProperty> keyProperties)
        {
            var values = new object?[keyProperties.Count];
            for (var i = 0; i < keyProperties.Count; i++)
            {
                values[i] = entry.Property(keyProperties[i].Name).CurrentValue;
            }

            return values;
        }

        private static bool ValuesMatch(object? actual, object? expected)
        {
            if (actual is DateTime actualDate && expected is DateTime expectedDate)
            {
                return Math.Abs((actualDate - expectedDate).TotalMilliseconds) < 10;
            }

            return Equals(actual, expected);
        }
    }

    private sealed record StagedModifiedProbe(
        Type EntityType,
        object?[] KeyValues,
        List<(string PropertyName, object? CurrentValue)> ModifiedProperties);

    private readonly struct EntityProbeKey : IEquatable<EntityProbeKey>
    {
        public EntityProbeKey(Type entityType, object?[] keyValues)
        {
            EntityType = entityType;
            KeyValues = keyValues;
        }

        public Type EntityType { get; }

        public object?[] KeyValues { get; }

        public bool Equals(EntityProbeKey other)
        {
            if (EntityType != other.EntityType ||
                KeyValues.Length != other.KeyValues.Length)
            {
                return false;
            }

            for (var i = 0; i < KeyValues.Length; i++)
            {
                if (!Equals(KeyValues[i], other.KeyValues[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) =>
            obj is EntityProbeKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(EntityType);
            foreach (var key in KeyValues)
            {
                hash.Add(key);
            }

            return hash.ToHashCode();
        }
    }
}
