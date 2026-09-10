using Infrastructure.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;

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
}