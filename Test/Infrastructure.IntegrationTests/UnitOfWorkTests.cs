using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;
using System.Data.Common;

namespace Infrastructure.IntegrationTests;

public sealed class UnitOfWorkTests
    : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public UnitOfWorkTests(
        SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task CommitAsync_ShouldPersistEntity()
    {
        // Arrange
        const string className =
            "Commit Integration Test Class";

        await using (var context = _database.CreateContext())
        {
            var unitOfWork =
                new UnitOfWork(context);

            await using var transaction =
                await unitOfWork.BeginTransactionAsync();

            var licenseClass =
                new LicenseClass
                {
                    ClassName = className,
                    ClassDescription =
                        "Commit integration test license class",
                    MinimumAllowedAge = 18,
                    DefaultValidityLength = 10,
                    ClassFees = 100
                };

            // Act
            context.LicenseClasses.Add(licenseClass);

            await unitOfWork.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        // Assert
        await using var verificationContext =
            _database.CreateContext();

        var persistedLicenseClass =
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ClassName == className);

        Assert.NotNull(persistedLicenseClass);
        Assert.Equal(
            className,
            persistedLicenseClass.ClassName);
    }

    [Fact]
    public async Task RollbackAsync_ShouldNotPersistEntity_AndShouldClearChangeTracker()
    {
        // Arrange
        var className =
            $"Rollback Integration Test Class {Guid.NewGuid():N}";
        var subsequentClassName =
            $"Subsequent Integration Test Class {Guid.NewGuid():N}";

        await using (var context = _database.CreateContext())
        {
            var unitOfWork =
                new UnitOfWork(context);

            await using (var transaction =
                await unitOfWork.BeginTransactionAsync())
            {
                context.LicenseClasses.Add(
                    new LicenseClass
                    {
                        ClassName = className,
                        ClassDescription =
                            "Rollback integration test license class",
                        MinimumAllowedAge = 18,
                        DefaultValidityLength = 10,
                        ClassFees = 100
                    });

                await transaction.RollbackAsync();

                Assert.Empty(context.ChangeTracker.Entries());
            }

            // A subsequent operation on the same UnitOfWork must not persist the rolled-back entity.
            context.LicenseClasses.Add(
                new LicenseClass
                {
                    ClassName = subsequentClassName,
                    ClassDescription =
                        "Subsequent valid license class",
                    MinimumAllowedAge = 18,
                    DefaultValidityLength = 10,
                    ClassFees = 100
                });

            await unitOfWork.SaveChangesAsync();
        }

        // Assert
        await using var verificationContext =
            _database.CreateContext();

        var rolledBackEntity =
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ClassName == className);

        var subsequentEntity =
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ClassName == subsequentClassName);

        Assert.Null(rolledBackEntity);
        Assert.NotNull(subsequentEntity);
    }

    [Fact]
    public async Task DisposeAsync_WithoutCommit_ShouldRollbackAndClearChangeTracker()
    {
        // Arrange
        var uncommittedClassName =
            $"Uncommitted Dispose Class {Guid.NewGuid():N}";
        var subsequentClassName =
            $"Subsequent Dispose Class {Guid.NewGuid():N}";

        await using (var context = _database.CreateContext())
        {
            var unitOfWork =
                new UnitOfWork(context);

            await using (var transaction =
                await unitOfWork.BeginTransactionAsync())
            {
                context.LicenseClasses.Add(
                    new LicenseClass
                    {
                        ClassName = uncommittedClassName,
                        ClassDescription = "Uncommitted class",
                        MinimumAllowedAge = 18,
                        DefaultValidityLength = 10,
                        ClassFees = 100
                    });
            }

            Assert.Empty(context.ChangeTracker.Entries());

            context.LicenseClasses.Add(
                new LicenseClass
                {
                    ClassName = subsequentClassName,
                    ClassDescription = "Committed class",
                    MinimumAllowedAge = 18,
                    DefaultValidityLength = 10,
                    ClassFees = 100
                });

            await unitOfWork.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        Assert.False(
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .AnyAsync(x => x.ClassName == uncommittedClassName));

        Assert.True(
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .AnyAsync(x => x.ClassName == subsequentClassName));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldPersistEntity()
    {
        // Arrange
        const string className =
            "Execute Transaction Integration Test Class";

        await using (var context = _database.CreateContext())
        {
            var unitOfWork =
                new UnitOfWork(context);

            // Act
            var result =
                await unitOfWork.ExecuteInTransactionAsync(
                    async transaction =>
                    {
                        var licenseClass =
                            new LicenseClass
                            {
                                ClassName = className,
                                ClassDescription =
                                    "Execute transaction integration test class",
                                MinimumAllowedAge = 18,
                                DefaultValidityLength = 10,
                                ClassFees = 100
                            };

                        context.LicenseClasses.Add(licenseClass);

                        await unitOfWork.SaveChangesAsync();

                        await transaction.CommitAsync();

                        var exists =
                            await context.LicenseClasses
                                .AnyAsync(
                                    x => x.ClassName == className);

                        Assert.True(exists);

                        return true;
                    },
                    IsolationLevel.Serializable);

            // Assert
            Assert.True(result);
        }

        await using var verificationContext =
            _database.CreateContext();

        var persistedLicenseClass =
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ClassName == className);

        Assert.NotNull(persistedLicenseClass);
        Assert.Equal(
            className,
            persistedLicenseClass.ClassName);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationReturnsFailureResult_ShouldRollbackAndClearChangeTracker()
    {
        // Arrange
        var failedClassName =
            $"Failed Result Class {Guid.NewGuid():N}";
        var subsequentClassName =
            $"Subsequent Result Class {Guid.NewGuid():N}";

        await using (var context = _database.CreateContext())
        {
            var unitOfWork = new UnitOfWork(context);

            var result =
                await unitOfWork.ExecuteInTransactionAsync(
                    async _ =>
                    {
                        context.LicenseClasses.Add(
                            new LicenseClass
                            {
                                ClassName = failedClassName,
                                ClassDescription = "First step saved before failure",
                                MinimumAllowedAge = 18,
                                DefaultValidityLength = 10,
                                ClassFees = 100
                            });

                        await unitOfWork.SaveChangesAsync();

                        return Result.Failure("Second step failed.");
                    },
                    IsolationLevel.Serializable);

            Assert.True(result.IsFailure);
            Assert.Empty(context.ChangeTracker.Entries());

            // Subsequent operation on the same UnitOfWork must work cleanly and not save failed changes.
            context.LicenseClasses.Add(
                new LicenseClass
                {
                    ClassName = subsequentClassName,
                    ClassDescription = "Subsequent valid operation",
                    MinimumAllowedAge = 18,
                    DefaultValidityLength = 10,
                    ClassFees = 100
                });

            await unitOfWork.SaveChangesAsync();
        }

        await using var verificationContext = _database.CreateContext();

        Assert.False(
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .AnyAsync(x => x.ClassName == failedClassName));

        Assert.True(
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .AnyAsync(x => x.ClassName == subsequentClassName));
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenExceptionOccursAfterFirstSave_ShouldRollbackAllChangesAndClearChangeTracker()
    {
        // Arrange
        var uniqueId = Guid.NewGuid().ToString("N");
        var countryName = $"TxExceptionCountry_{uniqueId}";

        await using (var context = _database.CreateContext())
        {
            var unitOfWork = new UnitOfWork(context);
            var countryRepo = new CountryRepository(context);
            var personRepo = new PersonRepository(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                unitOfWork.ExecuteInTransactionAsync<Result>(
                    async _ =>
                    {
                        var country = new Country
                        {
                            CountryName = countryName
                        };

                        context.Countries.Add(country);
                        await unitOfWork.SaveChangesAsync();

                        await personRepo.AddPersonAsync(
                            new Person
                            {
                                NationalNo = $"EX{uniqueId[..18]}",
                                FirstName = "Tx",
                                SecondName = "Exception",
                                LastName = "Test",
                                DateOfBirth = new DateTime(1990, 1, 1),
                                Gender = Gender.Male,
                                Address = "Test Address",
                                Phone = $"07{uniqueId[..8]}",
                                Email = $"{uniqueId}@tx.test",
                                NationalityCountryID = country.CountryId
                            });

                        throw new InvalidOperationException("Simulated step 2 failure.");
                    },
                    IsolationLevel.Serializable));

            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var verificationContext = _database.CreateContext();

        Assert.False(
            await verificationContext.Countries
                .AsNoTracking()
                .AnyAsync(c => c.CountryName == countryName));
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenTransactionAlreadyActive_ShouldThrowInvalidOperationException()
    {
        await using var context = _database.CreateContext();
        var unitOfWork = new UnitOfWork(context);

        await using var outerTransaction =
            await unitOfWork.BeginTransactionAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.BeginTransactionAsync());

        Assert.Contains("Nested transactions are not supported", exception.Message);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenTransactionAlreadyActive_ShouldThrowInvalidOperationException()
    {
        await using var context = _database.CreateContext();
        var unitOfWork = new UnitOfWork(context);

        await using var outerTransaction =
            await unitOfWork.BeginTransactionAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => unitOfWork.ExecuteInTransactionAsync(
                _ => Task.FromResult(Result.Success()),
                IsolationLevel.Serializable));

        Assert.Contains("Nested transactions are not supported", exception.Message);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenTransientFailureOccursBeforeCommit_ShouldRetryAndPersistSingleEntity()
    {
        var className = $"PreCommitTransientRetry_{Guid.NewGuid():N}";
        var attemptCount = 0;

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(
                _database.ConnectionString,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromMilliseconds(20),
                    errorNumbersToAdd: null))
            .Options;

        await using (var context = new DVLDDbContext(options))
        {
            var unitOfWork = new UnitOfWork(context);

            var result = await unitOfWork.ExecuteInTransactionAsync(
                async transaction =>
                {
                    var currentAttempt = Interlocked.Increment(ref attemptCount);

                    // Ensure ChangeTracker was cleared after the failed first attempt.
                    Assert.Empty(context.ChangeTracker.Entries());

                    var licenseClass = new LicenseClass
                    {
                        ClassName = className,
                        ClassDescription = "Pre-commit transient failure test",
                        MinimumAllowedAge = 18,
                        DefaultValidityLength = 10,
                        ClassFees = 100
                    };

                    context.LicenseClasses.Add(licenseClass);
                    await unitOfWork.SaveChangesAsync();

                    if (currentAttempt == 1)
                    {
                        throw new TimeoutException("Simulated transient failure before commit.");
                    }

                    await transaction.CommitAsync();
                    return Result<int>.Success(licenseClass.LicenseClassID);
                },
                IsolationLevel.Serializable);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value > 0);
            Assert.Equal(2, attemptCount);
        }

        await using var verificationContext = _database.CreateContext();
        var persisted = await verificationContext.LicenseClasses
            .AsNoTracking()
            .Where(x => x.ClassName == className)
            .ToListAsync();

        Assert.Single(persisted);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenConnectionDropsAfterServerCommit_ShouldVerifyCommitAndReturnSuccessWithoutRetry()
    {
        var className = $"PostCommitDrop_{Guid.NewGuid():N}";
        var interceptor = new FaultInjectingTransactionInterceptor(
            failAfterServerCommitOnAttempt: 1);
        var operationAttempts = 0;

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(
                _database.ConnectionString,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromMilliseconds(20),
                    errorNumbersToAdd: null))
            .AddInterceptors(interceptor)
            .Options;

        int returnedLicenseClassId;

        await using (var context = new DVLDDbContext(options))
        {
            var unitOfWork = new UnitOfWork(context);

            var result = await unitOfWork.ExecuteInTransactionAsync(
                async transaction =>
                {
                    Interlocked.Increment(ref operationAttempts);

                    var licenseClass = new LicenseClass
                    {
                        ClassName = className,
                        ClassDescription = "Post-commit ambiguous failure test",
                        MinimumAllowedAge = 18,
                        DefaultValidityLength = 10,
                        ClassFees = 150
                    };

                    context.LicenseClasses.Add(licenseClass);
                    await unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<int>.Success(licenseClass.LicenseClassID);
                },
                IsolationLevel.Serializable);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value > 0);
            Assert.Equal(1, operationAttempts);
            Assert.Empty(context.ChangeTracker.Entries());
            returnedLicenseClassId = result.Value;
        }

        await using var verificationContext = _database.CreateContext();
        var persisted = await verificationContext.LicenseClasses
            .AsNoTracking()
            .Where(x => x.ClassName == className)
            .ToListAsync();

        Assert.Single(persisted);
        Assert.Equal(returnedLicenseClassId, persisted[0].LicenseClassID);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenCustomVerifySucceededCallbackConfirmsCommit_ShouldReturnResultWithoutRetry()
    {
        var className = $"CustomVerifyCommit_{Guid.NewGuid():N}";
        var interceptor = new FaultInjectingTransactionInterceptor(
            failAfterServerCommitOnAttempt: 1);
        var operationAttempts = 0;
        var verificationCalls = 0;

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(
                _database.ConnectionString,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromMilliseconds(20),
                    errorNumbersToAdd: null))
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new DVLDDbContext(options);
        var unitOfWork = new UnitOfWork(context);

        var result = await unitOfWork.ExecuteInTransactionAsync(
            async transaction =>
            {
                Interlocked.Increment(ref operationAttempts);

                var licenseClass = new LicenseClass
                {
                    ClassName = className,
                    ClassDescription = "Custom verification callback test",
                    MinimumAllowedAge = 18,
                    DefaultValidityLength = 10,
                    ClassFees = 120
                };

                context.LicenseClasses.Add(licenseClass);
                await unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return Result<int>.Success(licenseClass.LicenseClassID);
            },
            verifySucceeded: async ct =>
            {
                Interlocked.Increment(ref verificationCalls);
                return await context.LicenseClasses
                    .AsNoTracking()
                    .AnyAsync(x => x.ClassName == className, ct);
            },
            IsolationLevel.Serializable);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, operationAttempts);
        Assert.Equal(1, verificationCalls);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenTransientFailureOccursBeforeServerCommitsDuringCommitCall_ShouldVerifyNotCommittedAndRetrySafely()
    {
        var className = $"RollbackOnCommitAttempt_{Guid.NewGuid():N}";
        var interceptor = new FaultInjectingTransactionInterceptor(
            rollbackAndFailBeforeServerCommitOnAttempt: 1);
        var operationAttempts = 0;

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(
                _database.ConnectionString,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromMilliseconds(20),
                    errorNumbersToAdd: null))
            .AddInterceptors(interceptor)
            .Options;

        int returnedLicenseClassId;

        await using (var context = new DVLDDbContext(options))
        {
            var unitOfWork = new UnitOfWork(context);

            var result = await unitOfWork.ExecuteInTransactionAsync(
                async transaction =>
                {
                    Interlocked.Increment(ref operationAttempts);
                    Assert.Empty(context.ChangeTracker.Entries());

                    var licenseClass = new LicenseClass
                    {
                        ClassName = className,
                        ClassDescription = "Commit rolled back on attempt 1",
                        MinimumAllowedAge = 18,
                        DefaultValidityLength = 10,
                        ClassFees = 100
                    };

                    context.LicenseClasses.Add(licenseClass);
                    await unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<int>.Success(licenseClass.LicenseClassID);
                },
                IsolationLevel.Serializable);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, operationAttempts);
            returnedLicenseClassId = result.Value;
        }

        await using var verificationContext = _database.CreateContext();
        var persisted = await verificationContext.LicenseClasses
            .AsNoTracking()
            .Where(x => x.ClassName == className)
            .ToListAsync();

        Assert.Single(persisted);
        Assert.Equal(returnedLicenseClassId, persisted[0].LicenseClassID);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenCommitAndVerificationBothFail_ShouldThrowInvalidOperationExceptionWithoutRepeatingOperation()
    {
        var className = $"UnverifiableCommit_{Guid.NewGuid():N}";
        var interceptor = new FaultInjectingTransactionInterceptor(
            failAfterServerCommitOnAttempt: 1);
        var operationAttempts = 0;

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(
                _database.ConnectionString,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 2,
                    maxRetryDelay: TimeSpan.FromMilliseconds(10),
                    errorNumbersToAdd: null))
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new DVLDDbContext(options);
        var unitOfWork = new UnitOfWork(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.ExecuteInTransactionAsync(
                async transaction =>
                {
                    Interlocked.Increment(ref operationAttempts);

                    var licenseClass = new LicenseClass
                    {
                        ClassName = className,
                        ClassDescription = "Verification failure test",
                        MinimumAllowedAge = 18,
                        DefaultValidityLength = 10,
                        ClassFees = 100
                    };

                    context.LicenseClasses.Add(licenseClass);
                    await unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result<int>.Success(licenseClass.LicenseClassID);
                },
                verifySucceeded: _ => throw new TimeoutException("Database unavailable during verification."),
                IsolationLevel.Serializable));

        Assert.Contains("could not be verified", exception.Message);
        Assert.Equal(1, operationAttempts);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenModifiedEntityCommitConfirmationIsLost_ShouldVerifyUpdatedPropertyAndReturnSuccessWithoutRetry()
    {
        var initialName = $"UpdateVerifyInitial_{Guid.NewGuid():N}";
        var updatedDescription = $"UpdatedDesc_{Guid.NewGuid():N}";
        int licenseClassId;

        await using (var seedContext = _database.CreateContext())
        {
            var entity = new LicenseClass
            {
                ClassName = initialName,
                ClassDescription = "Initial description",
                MinimumAllowedAge = 18,
                DefaultValidityLength = 10,
                ClassFees = 100
            };

            seedContext.LicenseClasses.Add(entity);
            await seedContext.SaveChangesAsync();
            licenseClassId = entity.LicenseClassID;
        }

        var interceptor = new FaultInjectingTransactionInterceptor(
            failAfterServerCommitOnAttempt: 1);
        var operationAttempts = 0;

        var options = new DbContextOptionsBuilder<DVLDDbContext>()
            .UseSqlServer(
                _database.ConnectionString,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromMilliseconds(20),
                    errorNumbersToAdd: null))
            .AddInterceptors(interceptor)
            .Options;

        await using (var context = new DVLDDbContext(options))
        {
            var unitOfWork = new UnitOfWork(context);

            var result = await unitOfWork.ExecuteInTransactionAsync(
                async transaction =>
                {
                    Interlocked.Increment(ref operationAttempts);

                    var entity = await context.LicenseClasses
                        .FirstAsync(x => x.LicenseClassID == licenseClassId);

                    entity.ClassDescription = updatedDescription;
                    await unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Result.Success();
                },
                IsolationLevel.Serializable);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, operationAttempts);
            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var verificationContext = _database.CreateContext();
        var updatedEntity = await verificationContext.LicenseClasses
            .AsNoTracking()
            .SingleAsync(x => x.LicenseClassID == licenseClassId);

        Assert.Equal(updatedDescription, updatedEntity.ClassDescription);
    }

    private sealed class FaultInjectingTransactionInterceptor(
        int? failAfterServerCommitOnAttempt = null,
        int? rollbackAndFailBeforeServerCommitOnAttempt = null)
        : DbTransactionInterceptor
    {
        private int _committingCount;
        private int _committedCount;

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _committingCount);

            if (rollbackAndFailBeforeServerCommitOnAttempt == attempt)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new TimeoutException(
                    "Simulated transient failure before SQL Server committed the transaction.");
            }

            return result;
        }

        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _committedCount);

            if (failAfterServerCommitOnAttempt == attempt)
            {
                throw new TimeoutException(
                    "Simulated transient connection drop after SQL Server committed the transaction.");
            }

            return Task.CompletedTask;
        }
    }
}
