using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
}
