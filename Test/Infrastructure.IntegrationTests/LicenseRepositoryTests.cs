using Application.Common.Pagination;
using Application.DTOs.LicenseDTO;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class LicenseRepositoryTests
    : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public LicenseRepositoryTests(
        SqlServerTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task GetLicenseByIdAsync_ShouldReturnLicenseWithRelatedData()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicenseByIdAsync(seed.LicenseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seed.LicenseId, result.LicenseID);

        Assert.NotNull(result.Driver);
        Assert.NotNull(result.Driver.Person);
        Assert.NotNull(result.LicenseClassInfo);
        Assert.NotNull(result.CreatedByUser);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLicenseByIdAsync_ShouldReturnNullForInvalidId(
        int id)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicenseByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetForUpdateAsync_ShouldReturnLicenseWithRelatedData()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetForUpdateAsync(seed.LicenseId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seed.LicenseId, result.LicenseID);

        Assert.NotNull(result.Driver);
        Assert.NotNull(result.Driver.Person);
        Assert.NotNull(result.LicenseClassInfo);
        Assert.NotNull(result.CreatedByUser);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetForUpdateAsync_ShouldReturnNullForInvalidId(
        int id)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetForUpdateAsync(id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLicenseByIdAsync_WhenMultipleReadersExecuteConcurrently_CompleteSuccessfullyWithoutBlocking()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        // Spin up multiple concurrent readers simulating concurrent GET /api/licenses/{id} requests
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var context = _database.CreateContext();
            var repo = new LicenseRepository(context);
            var lic = await repo.GetLicenseByIdAsync(seed.LicenseId);
            Assert.NotNull(lic);
            Assert.Equal(seed.LicenseId, lic.LicenseID);
        });

        // Act & Assert
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task GetByDriverIdAsync_ShouldReturnMostRecentLicense()
    {
        // Arrange
        var older =
            await SeedLicenseAsync(
                issueDate: DateTime.UtcNow.AddYears(-2),
                isActive: false);

        var newer =
            await SeedAdditionalLicenseAsync(
                older,
                issueDate: DateTime.UtcNow.AddYears(-1),
                isActive: true);

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetByDriverIdAsync(
                older.DriverId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(
            newer.LicenseId,
            result.LicenseID);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByDriverIdAsync_ShouldReturnNullForInvalidDriverId(
        int driverId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetByDriverIdAsync(driverId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllLicensesAsync_ShouldReturnLicensesOrderedByIssueDateDescending()
    {
        // Arrange
        var older =
            await SeedLicenseAsync(
                issueDate: DateTime.UtcNow.AddYears(-3));

        var newer =
            await SeedLicenseAsync(
                issueDate: DateTime.UtcNow.AddYears(-1));

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var licenses =
            await repository.GetAllLicensesAsync();

        // Assert
        var newerIndex =
            licenses.FindIndex(
                x => x.LicenseID == newer.LicenseId);

        var olderIndex =
            licenses.FindIndex(
                x => x.LicenseID == older.LicenseId);

        Assert.True(newerIndex >= 0);
        Assert.True(olderIndex >= 0);
        Assert.True(newerIndex < olderIndex);
    }

    [Fact]
    public async Task GetLicensesByDriverIdAsync_ShouldReturnOnlyDriverLicenses()
    {
        // Arrange
        var first =
            await SeedLicenseAsync();

        var second =
            await SeedAdditionalLicenseAsync(
                first,
                issueDate: DateTime.UtcNow.AddDays(1),
                isActive: false);

        var other =
            await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByDriverIdAsync(
                first.DriverId);

        // Assert
        Assert.Contains(
            result,
            x => x.LicenseID == first.LicenseId);

        Assert.Contains(
            result,
            x => x.LicenseID == second.LicenseId);

        Assert.DoesNotContain(
            result,
            x => x.LicenseID == other.LicenseId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLicensesByDriverIdAsync_ShouldReturnEmptyForInvalidDriverId(
        int driverId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByDriverIdAsync(driverId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLicensesByApplicationIdAsync_ShouldReturnMatchingLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByApplicationIdAsync(
                seed.ApplicationId);

        // Assert
        var license =
            Assert.Single(result);

        Assert.Equal(
            seed.LicenseId,
            license.LicenseID);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLicensesByApplicationIdAsync_ShouldReturnEmptyForInvalidApplicationId(
        int applicationId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByApplicationIdAsync(
                applicationId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLicensesByLicenseClassIdAsync_ShouldReturnOnlyMatchingClass()
    {
        // Arrange
        var first =
            await SeedLicenseAsync();

        var second =
            await SeedLicenseAsync(
                licenseClassIdOverride: first.LicenseClassId);

        var other =
            await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByLicenseClassIdAsync(
                first.LicenseClassId);

        // Assert
        Assert.Contains(
            result,
            x => x.LicenseID == first.LicenseId);

        Assert.Contains(
            result,
            x => x.LicenseID == second.LicenseId);

        Assert.DoesNotContain(
            result,
            x => x.LicenseID == other.LicenseId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLicensesByLicenseClassIdAsync_ShouldReturnEmptyForInvalidLicenseClassId(
        int licenseClassId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByLicenseClassIdAsync(
                licenseClassId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLicensesByPersonIdAsync_ShouldReturnOnlyPersonLicenses()
    {
        // Arrange
        var first =
            await SeedLicenseAsync();

        var second =
            await SeedAdditionalLicenseAsync(
                first,
                issueDate: DateTime.UtcNow.AddDays(1),
                isActive: false,
                createNewLicenseClass: true);

        var other =
            await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByPersonIdAsync(
                first.PersonId);

        // Assert
        Assert.Contains(
            result,
            x => x.LicenseID == first.LicenseId);

        Assert.Contains(
            result,
            x => x.LicenseID == second.LicenseId);

        Assert.DoesNotContain(
            result,
            x => x.LicenseID == other.LicenseId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetLicensesByPersonIdAsync_ShouldReturnEmptyForInvalidPersonId(
        int personId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetLicensesByPersonIdAsync(personId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task IsLicenseExistsAsync_ShouldReturnTrueForExistingLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsLicenseExistsAsync(
                seed.LicenseId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsLicenseExistsAsync_ShouldReturnFalseForMissingLicense()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsLicenseExistsAsync(
                int.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IsLicenseExistsAsync_ShouldReturnFalseForInvalidId(
        int id)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsLicenseExistsAsync(id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsDriverHasLicenseAsync_ShouldReturnTrueWhenDriverHasLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsDriverHasLicenseAsync(
                seed.DriverId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsDriverHasLicenseAsync_ShouldReturnFalseWhenDriverHasNoLicense()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsDriverHasLicenseAsync(
                int.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IsDriverHasLicenseAsync_ShouldReturnFalseForInvalidDriverId(
        int driverId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsDriverHasLicenseAsync(driverId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsApplicationHasLicenseAsync_ShouldReturnTrueWhenApplicationHasLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsApplicationHasLicenseAsync(
                seed.ApplicationId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsApplicationHasLicenseAsync_ShouldReturnFalseWhenApplicationHasNoLicense()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsApplicationHasLicenseAsync(
                int.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IsApplicationHasLicenseAsync_ShouldReturnFalseForInvalidApplicationId(
        int applicationId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsApplicationHasLicenseAsync(
                applicationId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsActiveLicenseExistsAsync_ShouldReturnTrueForActiveLicense()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: true);

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsActiveLicenseExistsAsync(
                seed.DriverId,
                seed.LicenseClassId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsActiveLicenseExistsAsync_ShouldReturnFalseForInactiveLicense()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: false);

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsActiveLicenseExistsAsync(
                seed.DriverId,
                seed.LicenseClassId);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    public async Task IsActiveLicenseExistsAsync_ShouldReturnFalseForInvalidIds(
        int driverId,
        int licenseClassId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.IsActiveLicenseExistsAsync(
                driverId,
                licenseClassId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetApplicationIdsWithLicensesAsync_ShouldReturnDistinctMatchingIds()
    {
        // Arrange
        var first =
            await SeedLicenseAsync();

        var second =
            await SeedLicenseAsync();

        var requestedIds = new[]
        {
            first.ApplicationId,
            second.ApplicationId,
            first.ApplicationId,
            0,
            -1,
            int.MaxValue
        };

        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetApplicationIdsWithLicensesAsync(
                requestedIds);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(first.ApplicationId, result);
        Assert.Contains(second.ApplicationId, result);
    }

    [Fact]
    public async Task GetApplicationIdsWithLicensesAsync_ShouldReturnEmptyWhenNoValidIdsExist()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act
        var result =
            await repository.GetApplicationIdsWithLicensesAsync(
                [0, -1, -10]);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationIdsWithLicensesAsync_ShouldThrowForNullInput()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository = new LicenseRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.GetApplicationIdsWithLicensesAsync(
                    null!));
    }

    [Fact]
    public async Task AddLicenseAsync_ShouldPersistLicenseAfterSaveChanges()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync();

        var application =
            await SeedAdditionalApplicationAsync(
                seed.PersonId,
                seed.UserId);

        var license =
            new License
            {
                ApplicationID =
                    application.ApplicationId,
                DriverID =
                    seed.DriverId,
                LicenseClass =
                    seed.LicenseClassId,
                IssueDate =
                    DateTime.UtcNow,
                ExpirationDate =
                    DateTime.UtcNow.AddYears(1),
                Notes =
                    "AddLicenseAsync integration test",
                PaidFees = 25,
                IsActive = false,
                IssueReason =
                    IssueReason.Renew,
                CreatedByUserID =
                    seed.UserId
            };

        await using (var context = _database.CreateContext())
        {
            var repository =
                new LicenseRepository(context);

            // Act
            await repository.AddLicenseAsync(license);

            await context.SaveChangesAsync();
        }

        // Assert
        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext.Licenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.LicenseID == license.LicenseID);

        Assert.NotNull(persisted);
        Assert.Equal(
            application.ApplicationId,
            persisted.ApplicationID);
        Assert.Equal(
            seed.DriverId,
            persisted.DriverID);
        Assert.Equal(
            seed.LicenseClassId,
            persisted.LicenseClass);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task AddLicenseAsync_ShouldThrowForNullLicense()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.AddLicenseAsync(null!));
    }

    [Fact]
    public async Task DeactivateLicenseAsync_ShouldDeactivateActiveLicense()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: true);

        await using (var context = _database.CreateContext())
        {
            var repository =
                new LicenseRepository(context);

            // Act
            var result =
                await repository.DeactivateLicenseAsync(
                    seed.LicenseId);

            // Assert
            Assert.True(result);
        }

        // Verify against a new DbContext.
        await using var verificationContext =
            _database.CreateContext();

        var license =
            await verificationContext.Licenses
                .AsNoTracking()
                .SingleAsync(
                    x => x.LicenseID == seed.LicenseId);

        Assert.False(license.IsActive);
    }

    [Fact]
    public async Task DeactivateLicenseAsync_ShouldReturnFalseForInactiveLicense()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: false);

        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.DeactivateLicenseAsync(
                seed.LicenseId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateLicenseAsync_ShouldReturnFalseForMissingLicense()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.DeactivateLicenseAsync(
                int.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeactivateLicenseAsync_ShouldReturnFalseForInvalidId(
        int licenseId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.DeactivateLicenseAsync(
                licenseId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasAnotherActiveLicenseAsync_ShouldReturnFalseWhenExcludedLicenseIsTheOnlyActiveLicense()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: true);

        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.HasAnotherActiveLicenseAsync(
                seed.DriverId,
                seed.LicenseClassId,
                seed.LicenseId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasAnotherActiveLicenseAsync_ShouldReturnTrueWhenActiveLicenseExistsAndExcludedIdDoesNotMatch()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: true);

        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.HasAnotherActiveLicenseAsync(
                seed.DriverId,
                seed.LicenseClassId,
                int.MaxValue);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(-1, 1, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 1, -1)]
    public async Task HasAnotherActiveLicenseAsync_ShouldReturnFalseForInvalidIds(
        int driverId,
        int licenseClassId,
        int excludedLicenseId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.HasAnotherActiveLicenseAsync(
                driverId,
                licenseClassId,
                excludedLicenseId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ActivateLicenseAsync_ShouldActivateValidUnexpiredInactiveLicense()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: false,
                expirationDate:
                    new DateTime(2027, 11, 4));

        await using (var context = _database.CreateContext())
        {
            var repository =
                new LicenseRepository(context);

            // Act
            var result =
                await repository.ActivateLicenseAsync(
                    seed.LicenseId,
                    new DateOnly(2026, 11, 4));

            // Assert
            Assert.True(result);
        }

        // Verify against a new DbContext.
        await using var verificationContext =
            _database.CreateContext();

        var license =
            await verificationContext.Licenses
                .AsNoTracking()
                .SingleAsync(
                    x => x.LicenseID == seed.LicenseId);

        Assert.True(license.IsActive);
    }

    [Fact]
    public async Task ActivateLicenseAsync_ShouldReturnFalseForExpiredLicense()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: false,
                expirationDate:
                    new DateTime(2026, 11, 3, 23, 59, 59));

        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.ActivateLicenseAsync(
                seed.LicenseId,
                new DateOnly(2026, 11, 4));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ActivateLicenseAsync_ShouldReturnFalseWhenLicenseIsAlreadyActive()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: true,
                expirationDate:
                    new DateTime(2027, 11, 4));

        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.ActivateLicenseAsync(
                seed.LicenseId,
                new DateOnly(2026, 11, 4));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ActivateLicenseAsync_ShouldReturnFalseForMissingLicense()
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.ActivateLicenseAsync(
                int.MaxValue,
                new DateOnly(2026, 11, 4));

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ActivateLicenseAsync_ShouldReturnFalseForInvalidId(
        int licenseId)
    {
        // Arrange
        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.ActivateLicenseAsync(
                licenseId,
                new DateOnly(2026, 11, 4));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeactivateLicenseAsync_WhenLicenseIsTracked_ShouldSynchronizeTrackedEntityState()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: true);

        await using (var context = _database.CreateContext())
        {
            var trackedLicense =
                await context.Licenses
                    .SingleAsync(x => x.LicenseID == seed.LicenseId);

            Assert.True(trackedLicense.IsActive);

            var repository =
                new LicenseRepository(context);

            // Act
            var result =
                await repository.DeactivateLicenseAsync(
                    seed.LicenseId);

            // Assert
            Assert.True(result);
            Assert.False(trackedLicense.IsActive);
            Assert.False(context.Entry(trackedLicense).Property(x => x.IsActive).IsModified);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext.Licenses
                .AsNoTracking()
                .SingleAsync(x => x.LicenseID == seed.LicenseId);

        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task ActivateLicenseAsync_WhenLicenseIsTracked_ShouldSynchronizeTrackedEntityState()
    {
        // Arrange
        var seed =
            await SeedLicenseAsync(
                isActive: false,
                expirationDate: new DateTime(2027, 11, 4));

        await using (var context = _database.CreateContext())
        {
            var trackedLicense =
                await context.Licenses
                    .SingleAsync(x => x.LicenseID == seed.LicenseId);

            Assert.False(trackedLicense.IsActive);

            var repository =
                new LicenseRepository(context);

            // Act
            var result =
                await repository.ActivateLicenseAsync(
                    seed.LicenseId,
                    new DateOnly(2026, 11, 4));

            // Assert
            Assert.True(result);
            Assert.True(trackedLicense.IsActive);
            Assert.False(context.Entry(trackedLicense).Property(x => x.IsActive).IsModified);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext.Licenses
                .AsNoTracking()
                .SingleAsync(x => x.LicenseID == seed.LicenseId);

        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task ActivateAndDeactivateExpiredLicensesAsync_ShouldHonorOfficialJordanCalendarDateBoundary()
    {
        var sameDaySeed =
            await SeedLicenseAsync(
                isActive: false,
                expirationDate: new DateTime(2026, 11, 4, 0, 0, 0));

        var priorDaySeed =
            await SeedLicenseAsync(
                isActive: true,
                expirationDate: new DateTime(2026, 11, 3, 23, 59, 59));

        await using (var context = _database.CreateContext())
        {
            var repository = new LicenseRepository(context);

            // On 2026-11-04 Jordan, sameDaySeed (expiring 2026-11-04) can still be reactivated
            var activated = await repository.ActivateLicenseAsync(
                sameDaySeed.LicenseId,
                new DateOnly(2026, 11, 4));
            Assert.True(activated);

            // DeactivateExpiredLicensesAsync on 2026-11-04 must keep sameDaySeed active and deactivate priorDaySeed
            var deactivatedCount = await repository.DeactivateExpiredLicensesAsync(
                new DateOnly(2026, 11, 4));
            Assert.True(deactivatedCount >= 1);
        }

        await using var verifyContext = _database.CreateContext();
        var reloadedSameDay = await verifyContext.Licenses.AsNoTracking()
            .SingleAsync(x => x.LicenseID == sameDaySeed.LicenseId);
        var reloadedPriorDay = await verifyContext.Licenses.AsNoTracking()
            .SingleAsync(x => x.LicenseID == priorDaySeed.LicenseId);

        Assert.True(reloadedSameDay.IsActive);
        Assert.False(reloadedPriorDay.IsActive);
    }

    private async Task<LicenseSeed> SeedLicenseAsync(
        DateTime? issueDate = null,
        DateTime? expirationDate = null,
        bool isActive = true,
        int? licenseClassIdOverride = null)
    {
        await using var context =
            _database.CreateContext();

        var uniqueId =
            Guid.NewGuid().ToString("N");

        var country =
            new Country
            {
                CountryName =
                    $"Test Country {uniqueId}"
            };

        context.Countries.Add(country);
        await context.SaveChangesAsync();

        var person =
            new Person
            {
                NationalNo =
                    $"IT{uniqueId[..18]}",
                FirstName = "Integration",
                SecondName = "Test",
                ThirdName = "License",
                LastName = uniqueId[..8],
                DateOfBirth =
                    new DateTime(1990, 1, 1),
                Gender = Gender.Male,
                Address =
                    "Integration Test Address",
                Phone =
                    $"07{uniqueId[..8]}",
                Email =
                    $"{uniqueId}@integration.test",
                NationalityCountryID =
                    country.CountryId
            };

        context.People.Add(person);
        await context.SaveChangesAsync();

        var user =
            new User
            {
                PersonId =
                    person.PersonId,
                UserName =
                    $"test_{uniqueId[..20]}",
                Password =
                    "IntegrationTestPassword",
                IsActive = true,
                Role = UserRole.Staff
            };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Integration Test Type {uniqueId}",
                ApplicationFees = 10
            };

        context.ApplicationTypes.Add(applicationType);
        await context.SaveChangesAsync();

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    person.PersonId,
                ApplicationDate =
                    DateTime.UtcNow,
                ApplicationTypeID =
                    applicationType.ApplicationTypeId,
                ApplicationStatus =
                    AppStatus.New,
                LastStatusDate =
                    DateTime.UtcNow,
                PaidFees = 10,
                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var driver =
            new Driver
            {
                PersonID =
                    person.PersonId,
                CreatedByUserID =
                    user.UserId,
                CreatedDate =
                    DateTime.UtcNow
            };

        context.Drivers.Add(driver);
        await context.SaveChangesAsync();

        var licenseClass =
            licenseClassIdOverride.HasValue
                ? await context.LicenseClasses
                    .SingleAsync(
                        x =>
                            x.LicenseClassID ==
                            licenseClassIdOverride.Value)
                : await CreateLicenseClassAsync(
                    context,
                    uniqueId);

        var license =
            new License
            {
                ApplicationID =
                    application.ApplicationID,
                DriverID =
                    driver.DriverID,
                LicenseClass =
                    licenseClass.LicenseClassID,
                IssueDate =
                    issueDate ??
                    DateTime.UtcNow,
                ExpirationDate =
                    expirationDate ??
                    DateTime.UtcNow.AddYears(1),
                Notes =
                    "Integration Test License",
                PaidFees = 100,
                IsActive =
                    isActive,
                IssueReason =
                    IssueReason.FirstTime,
                CreatedByUserID =
                    user.UserId
            };

        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        return new LicenseSeed(
            license.LicenseID,
            application.ApplicationID,
            driver.DriverID,
            licenseClass.LicenseClassID,
            person.PersonId,
            user.UserId);
    }

    private async Task<LicenseSeed> SeedAdditionalLicenseAsync(
        LicenseSeed source,
        DateTime? issueDate = null,
        bool isActive = false,
        bool createNewLicenseClass = false)
    {
        await using var context =
            _database.CreateContext();

        var uniqueId =
            Guid.NewGuid().ToString("N");

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Additional Test Type {uniqueId}",
                ApplicationFees = 10
            };

        context.ApplicationTypes.Add(applicationType);
        await context.SaveChangesAsync();

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    source.PersonId,
                ApplicationDate =
                    DateTime.UtcNow,
                ApplicationTypeID =
                    applicationType.ApplicationTypeId,
                ApplicationStatus =
                    AppStatus.New,
                LastStatusDate =
                    DateTime.UtcNow,
                PaidFees = 10,
                CreatedByUserID =
                    source.UserId
            };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        var licenseClass =
            createNewLicenseClass
                ? await CreateLicenseClassAsync(
                    context,
                    uniqueId)
                : await context.LicenseClasses
                    .SingleAsync(
                        x =>
                            x.LicenseClassID ==
                            source.LicenseClassId);

        var license =
            new License
            {
                ApplicationID =
                    application.ApplicationID,
                DriverID =
                    source.DriverId,
                LicenseClass =
                    licenseClass.LicenseClassID,
                IssueDate =
                    issueDate ??
                    DateTime.UtcNow,
                ExpirationDate =
                    DateTime.UtcNow.AddYears(1),
                Notes =
                    "Additional Integration Test License",
                PaidFees = 100,
                IsActive =
                    isActive,
                IssueReason =
                    IssueReason.Renew,
                CreatedByUserID =
                    source.UserId
            };

        context.Licenses.Add(license);
        await context.SaveChangesAsync();

        return new LicenseSeed(
            license.LicenseID,
            application.ApplicationID,
            source.DriverId,
            licenseClass.LicenseClassID,
            source.PersonId,
            source.UserId);
    }

    private async Task<ApplicationSeed> SeedAdditionalApplicationAsync(
        int personId,
        int userId)
    {
        await using var context =
            _database.CreateContext();

        var uniqueId =
            Guid.NewGuid().ToString("N");

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"Add License Type {uniqueId}",
                ApplicationFees = 10
            };

        context.ApplicationTypes.Add(applicationType);
        await context.SaveChangesAsync();

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    personId,
                ApplicationDate =
                    DateTime.UtcNow,
                ApplicationTypeID =
                    applicationType.ApplicationTypeId,
                ApplicationStatus =
                    AppStatus.New,
                LastStatusDate =
                    DateTime.UtcNow,
                PaidFees = 10,
                CreatedByUserID =
                    userId
            };

        context.Applications.Add(application);
        await context.SaveChangesAsync();

        return new ApplicationSeed(
            application.ApplicationID);
    }

    private static async Task<LicenseClass> CreateLicenseClassAsync(
        DVLDDbContext context,
        string uniqueId)
    {
        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    $"Integration Test Class {uniqueId}",
                ClassDescription =
                    "Integration test license class",
                MinimumAllowedAge = 18,
                DefaultValidityLength = 10,
                ClassFees = 100
            };

        context.LicenseClasses.Add(licenseClass);

        await context.SaveChangesAsync();

        return licenseClass;
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenRequestingBeyondPage1_ReturnsCorrectPageAndItems()
    {
        // Arrange
        await SeedLicenseAsync();
        await SeedLicenseAsync();
        await SeedLicenseAsync();

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 2,
            PageSize = 1
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 3);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenSearchingByLicenseId_MatchesLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchQuery = seed.LicenseId.ToString(),
            SearchBy = "licenseid"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Items, l => l.LicenseID == seed.LicenseId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenSearchingByAppId_MatchesLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchQuery = seed.ApplicationId.ToString(),
            SearchBy = "applicationid"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Items, l => l.LicenseID == seed.LicenseId && l.ApplicationID == seed.ApplicationId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenSearchingByDriverName_MatchesLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);
        var person = await context.People.SingleAsync(p => p.PersonId == seed.PersonId);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchQuery = person.LastName,
            SearchBy = "drivername"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Items, l => l.LicenseID == seed.LicenseId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenSearchingByClassName_MatchesLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);
        var licenseClass = await context.LicenseClasses.SingleAsync(c => c.LicenseClassID == seed.LicenseClassId);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchQuery = licenseClass.ClassName,
            SearchBy = "classname"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Items, l => l.LicenseID == seed.LicenseId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenSearchingAllFields_MatchesLicense()
    {
        // Arrange
        var seed = await SeedLicenseAsync();

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);
        var person = await context.People.SingleAsync(p => p.PersonId == seed.PersonId);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchQuery = person.LastName,
            SearchBy = "all"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.Items, l => l.LicenseID == seed.LicenseId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenFilteringByActiveStatus_ReturnsOnlyActive()
    {
        // Arrange
        var activeSeed = await SeedLicenseAsync(isActive: true);
        var inactiveSeed = await SeedLicenseAsync(isActive: false);

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 50,
            StatusFilter = "active"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, l => Assert.True(l.IsActive));
        Assert.Contains(result.Items, l => l.LicenseID == activeSeed.LicenseId);
        Assert.DoesNotContain(result.Items, l => l.LicenseID == inactiveSeed.LicenseId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenFilteringByInactiveStatus_ReturnsOnlyInactive()
    {
        // Arrange
        var activeSeed = await SeedLicenseAsync(isActive: true);
        var inactiveSeed = await SeedLicenseAsync(isActive: false);

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 50,
            StatusFilter = "inactive"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, l => Assert.False(l.IsActive));
        Assert.Contains(result.Items, l => l.LicenseID == inactiveSeed.LicenseId);
        Assert.DoesNotContain(result.Items, l => l.LicenseID == activeSeed.LicenseId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenFilteringByExpired_ReturnsOnlyExpired()
    {
        // Arrange
        var today = new DateOnly(2026, 11, 8);
        var expiredSeed = await SeedLicenseAsync(
            issueDate: new DateTime(2020, 1, 1),
            expirationDate: new DateTime(2026, 11, 7));
        var validSeed = await SeedLicenseAsync(
            issueDate: new DateTime(2026, 1, 1),
            expirationDate: new DateTime(2028, 1, 1));

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 50,
            IsExpired = true
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, today);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, l => Assert.True(DateOnly.FromDateTime(l.ExpirationDate) < today));
        Assert.Contains(result.Items, l => l.LicenseID == expiredSeed.LicenseId);
        Assert.DoesNotContain(result.Items, l => l.LicenseID == validSeed.LicenseId);
    }

    [Fact]
    public async Task GetAllLicensesAsync_WhenSearchYieldsNoMatches_ReturnsEmptyResultWithZeroCount()
    {
        // Arrange
        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchQuery = "NON_EXISTENT_LICENSE_SEARCH_XYZ_9999",
            SearchBy = "all"
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllLicensesAsync_DeterministicOrdering_ReturnsSortedByIssueDateAndIdDescending()
    {
        // Arrange
        var seed1 = await SeedLicenseAsync(issueDate: new DateTime(2024, 1, 1));
        var seed2 = await SeedLicenseAsync(issueDate: new DateTime(2026, 6, 1));
        var seed3 = await SeedLicenseAsync(issueDate: new DateTime(2025, 1, 1));

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = 1,
            PageSize = 50
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            var current = result.Items[i];
            var next = result.Items[i + 1];
            Assert.True(
                current.IssueDate > next.IssueDate ||
                (current.IssueDate == next.IssueDate && current.LicenseID > next.LicenseID),
                $"Items at {i} and {i + 1} violated deterministic ordering.");
        }
    }

    [Fact]
    public async Task GetAllLicensesAsync_BoundaryPageSize_ClampsToMax100()
    {
        // Arrange
        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var request = new LicenseQueryRequest
        {
            PageNumber = -5,
            PageSize = 500
        };

        // Act
        var result = await repository.GetAllLicensesAsync(request, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task GetRenewalSummaryAsync_ComputesAccurateCounts()
    {
        var cutoff = new DateOnly(2026, 11, 8);
        var expiredDate = new DateTime(2026, 11, 7);
        var futureDate = new DateTime(2028, 1, 1);

        // 1. Pure expired, active license, no newer license, not detained => Eligible
        var pureExpired = await SeedLicenseAsync(
            issueDate: new DateTime(2020, 1, 1),
            expirationDate: expiredDate,
            isActive: true);

        // 2. Expired license that has been renewed (newer license exists for same driver & class) => Ineligible
        var renewedOlder = await SeedLicenseAsync(
            issueDate: new DateTime(2018, 1, 1),
            expirationDate: expiredDate,
            isActive: false);
        var renewedNewer = await SeedAdditionalLicenseAsync(
            renewedOlder,
            issueDate: new DateTime(2026, 1, 1),
            isActive: true);

        // 3. Expired license that is actively detained => Ineligible
        var detainedExpired = await SeedLicenseAsync(
            issueDate: new DateTime(2020, 1, 1),
            expirationDate: expiredDate,
            isActive: true);
        await using (var ctx = _database.CreateContext())
        {
            ctx.DetainedLicenses.Add(new DetainedLicense
            {
                LicenseID = detainedExpired.LicenseId,
                DetainDate = DateTime.UtcNow,
                FineFees = 20,
                CreatedByUserID = detainedExpired.UserId,
                IsReleased = false
            });
            await ctx.SaveChangesAsync();
        }

        // 4. Expired license whose detention is released => Eligible
        var releasedExpired = await SeedLicenseAsync(
            issueDate: new DateTime(2020, 1, 1),
            expirationDate: expiredDate,
            isActive: true);
        await using (var ctx = _database.CreateContext())
        {
            ctx.DetainedLicenses.Add(new DetainedLicense
            {
                LicenseID = releasedExpired.LicenseId,
                DetainDate = DateTime.UtcNow.AddMonths(-1),
                FineFees = 20,
                CreatedByUserID = releasedExpired.UserId,
                IsReleased = true,
                ReleaseDate = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        // 5. Active non-expired license (expires in future) => Not counted in TotalExpired
        var nonExpired = await SeedLicenseAsync(
            issueDate: new DateTime(2026, 1, 1),
            expirationDate: futureDate,
            isActive: true);

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        // Act
        var summary = await repository.GetRenewalSummaryAsync(cutoff);

        // Assert
        Assert.NotNull(summary);
        Assert.True(summary.TotalExpired >= 4);
        Assert.True(summary.EligibleForRenewal >= 2);
        Assert.Equal(summary.TotalExpired - summary.EligibleForRenewal, summary.IneligibleForRenewal);
    }

    [Fact]
    public async Task GetRenewalSummaryAsync_RespectsJordanDateBoundary()
    {
        var cutoff = new DateOnly(2026, 11, 8);

        var expiredBeforeCutoff = await SeedLicenseAsync(
            issueDate: new DateTime(2020, 1, 1),
            expirationDate: new DateTime(2026, 11, 7, 23, 59, 59),
            isActive: true);

        var expiresTomorrow = await SeedLicenseAsync(
            issueDate: new DateTime(2020, 1, 1),
            expirationDate: new DateTime(2026, 11, 9, 0, 0, 0),
            isActive: true);

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var summary = await repository.GetRenewalSummaryAsync(cutoff);

        Assert.NotNull(summary);
        Assert.True(summary.TotalExpired >= 1);
    }

    [Fact]
    public async Task GetRenewalSummaryAsync_WhenExpiredLicenseHasAnotherActiveLicenseForSameDriverAndClass_ExcludesFromEligible()
    {
        var cutoff = new DateOnly(2026, 11, 8);
        var expiredDate = new DateTime(2026, 11, 7);

        // Standard eligible expired license (different driver/class)
        var legitimateEligible = await SeedLicenseAsync(
            issueDate: new DateTime(2020, 1, 1),
            expirationDate: expiredDate,
            isActive: true);

        // Driver with active license (License A, active, not expired)
        var activeLicense = await SeedLicenseAsync(
            issueDate: new DateTime(2026, 1, 1),
            expirationDate: new DateTime(2028, 1, 1),
            isActive: true);

        // Same driver and same class has an expired inactive license (License B, inactive, expired)
        var expiredInactiveLicense = await SeedAdditionalLicenseAsync(
            activeLicense,
            issueDate: new DateTime(2020, 1, 1),
            isActive: false,
            createNewLicenseClass: false);

        // Ensure expiredInactiveLicense expiration date is before cutoff
        await using (var ctx = _database.CreateContext())
        {
            var lic = await ctx.Licenses.SingleAsync(l => l.LicenseID == expiredInactiveLicense.LicenseId);
            lic.ExpirationDate = expiredDate;
            await ctx.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var summary = await repository.GetRenewalSummaryAsync(cutoff);

        Assert.NotNull(summary);
        // The expiredInactiveLicense is in TotalExpired, but NOT in EligibleForRenewal because activeLicense is active for the same driver and class
        Assert.True(summary.TotalExpired >= 2);
        Assert.True(summary.IneligibleForRenewal >= 1);
        Assert.Equal(summary.TotalExpired - summary.EligibleForRenewal, summary.IneligibleForRenewal);
    }

    [Fact]
    public async Task GetNewerLicensesBatchAsync_WhenEmptyOrNull_ReturnsEmptyDictionary()
    {
        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var nullResult = await repository.GetNewerLicensesBatchAsync(null!);
        var emptyResult = await repository.GetNewerLicensesBatchAsync([]);

        Assert.NotNull(nullResult);
        Assert.Empty(nullResult);
        Assert.NotNull(emptyResult);
        Assert.Empty(emptyResult);
    }

    [Fact]
    public async Task GetNewerLicensesBatchAsync_WhenMultipleDriversAndClasses_ReturnsCorrectNewestPerDriverAndClass()
    {
        // Arrange
        // Driver 1, Class A: older (lic1), newer (lic2)
        var seed1 = await SeedLicenseAsync(issueDate: DateTime.UtcNow.AddYears(-3), isActive: false);
        var seed2 = await SeedAdditionalLicenseAsync(seed1, issueDate: DateTime.UtcNow.AddYears(-1), isActive: true, createNewLicenseClass: false);

        // Driver 2, Class B: single license (no newer)
        var seed3 = await SeedLicenseAsync(issueDate: DateTime.UtcNow.AddYears(-2), isActive: true);

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var cand1 = new License { LicenseID = seed1.LicenseId, DriverID = seed1.DriverId, LicenseClass = seed1.LicenseClassId };
        var cand3 = new License { LicenseID = seed3.LicenseId, DriverID = seed3.DriverId, LicenseClass = seed3.LicenseClassId };

        // Act
        var result = await repository.GetNewerLicensesBatchAsync([cand1, cand3]);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ContainsKey(seed1.LicenseId));
        Assert.Equal(seed2.LicenseId, result[seed1.LicenseId].LicenseID);

        // cand3 has no newer license
        Assert.False(result.ContainsKey(seed3.LicenseId));
    }

    [Fact]
    public async Task GetNewerLicensesBatchAsync_WhenMultipleLicensesForSameDriverAndClass_SelectsNewestDescending()
    {
        // Arrange: Driver has 3 sequential licenses in the same class: Lic1 < Lic2 < Lic3
        var seed1 = await SeedLicenseAsync(issueDate: DateTime.UtcNow.AddYears(-4), isActive: false);
        var seed2 = await SeedAdditionalLicenseAsync(seed1, issueDate: DateTime.UtcNow.AddYears(-2), isActive: false, createNewLicenseClass: false);
        var seed3 = await SeedAdditionalLicenseAsync(seed1, issueDate: DateTime.UtcNow.AddMonths(-6), isActive: true, createNewLicenseClass: false);

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var cand1 = new License { LicenseID = seed1.LicenseId, DriverID = seed1.DriverId, LicenseClass = seed1.LicenseClassId };
        var cand2 = new License { LicenseID = seed2.LicenseId, DriverID = seed2.DriverId, LicenseClass = seed2.LicenseClassId };

        // Act
        var result = await repository.GetNewerLicensesBatchAsync([cand1, cand2]);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seed3.LicenseId, result[seed1.LicenseId].LicenseID);
        Assert.Equal(seed3.LicenseId, result[seed2.LicenseId].LicenseID);
    }

    [Fact]
    public async Task GetNewerLicensesBatchAsync_WhenUnrelatedDriverAndClassCombinations_DoesNotCrossMatch()
    {
        // Arrange
        // Driver 1: has Lic1 in Class A (older), Lic2 in Class B (different class, newer)
        var seed1 = await SeedLicenseAsync(issueDate: DateTime.UtcNow.AddYears(-3), isActive: false);
        var seed2 = await SeedAdditionalLicenseAsync(seed1, issueDate: DateTime.UtcNow.AddYears(-1), isActive: true, createNewLicenseClass: true);

        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        var cand1 = new License { LicenseID = seed1.LicenseId, DriverID = seed1.DriverId, LicenseClass = seed1.LicenseClassId };

        // Act: cand1 is Class A. Lic2 is Class B. Must not cross-match.
        var result = await repository.GetNewerLicensesBatchAsync([cand1]);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ContainsKey(seed1.LicenseId));
    }

    [Fact]
    public async Task GetNewerLicensesBatchAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        await using var context = _database.CreateContext();
        var repository = new LicenseRepository(context);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cand = new License { LicenseID = 1, DriverID = 1, LicenseClass = 1 };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetNewerLicensesBatchAsync([cand], cts.Token));
    }

    private sealed record LicenseSeed(
        int LicenseId,
        int ApplicationId,
        int DriverId,
        int LicenseClassId,
        int PersonId,
        int UserId);

    private sealed record ApplicationSeed(
        int ApplicationId);
}