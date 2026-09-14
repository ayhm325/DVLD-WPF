using Infrastructure.IntegrationTests.Fixtures;
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
                new Domain.Entities.LicenseClass
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
    public async Task RollbackAsync_ShouldNotPersistEntity()
    {
        // Arrange
        const string className =
            "Rollback Integration Test Class";

        await using (var context = _database.CreateContext())
        {
            var unitOfWork =
                new UnitOfWork(context);

            await using var transaction =
                await unitOfWork.BeginTransactionAsync();

            var licenseClass =
                new Domain.Entities.LicenseClass
                {
                    ClassName = className,
                    ClassDescription =
                        "Rollback integration test license class",
                    MinimumAllowedAge = 18,
                    DefaultValidityLength = 10,
                    ClassFees = 100
                };

            // Act
            context.LicenseClasses.Add(licenseClass);

            await unitOfWork.SaveChangesAsync();

            await transaction.RollbackAsync();
        }

        // Assert
        await using var verificationContext =
            _database.CreateContext();

        var persistedLicenseClass =
            await verificationContext.LicenseClasses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.ClassName == className);

        Assert.Null(persistedLicenseClass);
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
                            new Domain.Entities.LicenseClass
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
}