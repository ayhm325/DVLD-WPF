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
                    DateTime.UtcNow.AddYears(1));

        await using (var context = _database.CreateContext())
        {
            var repository =
                new LicenseRepository(context);

            // Act
            var result =
                await repository.ActivateLicenseAsync(
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
                    DateTime.UtcNow.AddDays(-1));

        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.ActivateLicenseAsync(
                seed.LicenseId);

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
                    DateTime.UtcNow.AddYears(1));

        await using var context = _database.CreateContext();

        var repository =
            new LicenseRepository(context);

        // Act
        var result =
            await repository.ActivateLicenseAsync(
                seed.LicenseId);

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
                int.MaxValue);

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
                licenseId);

        // Assert
        Assert.False(result);
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