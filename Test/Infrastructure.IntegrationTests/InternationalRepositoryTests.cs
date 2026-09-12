using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class InternationalRepositoryTests
    : IClassFixture<SqlServerTestDatabase>
{
    private readonly SqlServerTestDatabase _database;

    public InternationalRepositoryTests(
        SqlServerTestDatabase database)
    {
        _database = database;
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new InternationalRepository(null!));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllInternationalLicenses()
    {
        // Arrange
        var first =
            await SeedInternationalLicenseAsync();

        var second =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        Assert.Contains(
            result,
            x =>
                x.InternationalLicenseID ==
                first.InternationalLicenseId);

        Assert.Contains(
            result,
            x =>
                x.InternationalLicenseID ==
                second.InternationalLicenseId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnLicensesOrderedByIdDescending()
    {
        // Arrange
        var first =
            await SeedInternationalLicenseAsync();

        var second =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        var firstIndex =
            result.FindIndex(
                x =>
                    x.InternationalLicenseID ==
                    first.InternationalLicenseId);

        var secondIndex =
            result.FindIndex(
                x =>
                    x.InternationalLicenseID ==
                    second.InternationalLicenseId);

        Assert.True(firstIndex >= 0);
        Assert.True(secondIndex >= 0);

        Assert.True(
            secondIndex < firstIndex);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadApplication()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.InternationalLicenseID ==
                        seed.InternationalLicenseId));

        Assert.NotNull(entity.Application);

        Assert.Equal(
            seed.ApplicationId,
            entity.Application.ApplicationID);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadDriverAndPerson()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.InternationalLicenseID ==
                        seed.InternationalLicenseId));

        Assert.NotNull(entity.Driver);

        Assert.Equal(
            seed.DriverId,
            entity.Driver.DriverID);

        Assert.NotNull(entity.Driver.Person);

        Assert.Equal(
            seed.PersonId,
            entity.Driver.Person.PersonId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadIssuedUsingLocalLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.InternationalLicenseID ==
                        seed.InternationalLicenseId));

        Assert.NotNull(
            entity.IssuedUsingLocalLicense);

        Assert.Equal(
            seed.LocalLicenseId,
            entity.IssuedUsingLocalLicense.LicenseID);
    }

    [Fact]
    public async Task GetAllAsync_ShouldLoadCreatedByUser()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.InternationalLicenseID ==
                        seed.InternationalLicenseId));

        Assert.NotNull(entity.CreatedByUser);

        Assert.Equal(
            seed.CreatedByUserId,
            entity.CreatedByUser.UserId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnDetachedEntities()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetAllAsync();

        // Assert
        var entity =
            Assert.Single(
                result.Where(
                    x =>
                        x.InternationalLicenseID ==
                        seed.InternationalLicenseId));

        Assert.Equal(
            EntityState.Detached,
            context.Entry(entity).State);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnExistingLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(
                seed.InternationalLicenseId);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            seed.InternationalLicenseId,
            result.InternationalLicenseID);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForMissingLicense()
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(
                int.MaxValue);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_ShouldReturnNullForInvalidId(
        int id)
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(id);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldLoadAllExpectedNavigationProperties()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(
                seed.InternationalLicenseId);

        // Assert
        Assert.NotNull(result);

        Assert.NotNull(result.Application);

        Assert.NotNull(result.Driver);
        Assert.NotNull(result.Driver.Person);

        Assert.NotNull(
            result.IssuedUsingLocalLicense);

        Assert.NotNull(result.CreatedByUser);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDetachedEntity()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByIdAsync(
                seed.InternationalLicenseId);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);
    }

    // =========================================================
    // GET BY DRIVER ID
    // =========================================================

    [Fact]
    public async Task GetByDriverIdAsync_ShouldReturnOnlyDriverLicenses()
    {
        // Arrange
        var first =
            await SeedInternationalLicenseAsync(
                isActive: false);

        var second =
            await SeedInternationalLicenseForExistingDriverAsync(
                first,
                isActive: false);

        var other =
            await SeedInternationalLicenseAsync(
                isActive: false);

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByDriverIdAsync(
                first.DriverId);

        // Assert
        Assert.Contains(
            result,
            x =>
                x.InternationalLicenseID ==
                first.InternationalLicenseId);

        Assert.Contains(
            result,
            x =>
                x.InternationalLicenseID ==
                second.InternationalLicenseId);

        Assert.DoesNotContain(
            result,
            x =>
                x.InternationalLicenseID ==
                other.InternationalLicenseId);
    }

    [Fact]
    public async Task GetByDriverIdAsync_ShouldReturnOrderedByIdDescending()
    {
        // Arrange
        var first =
            await SeedInternationalLicenseAsync(
                isActive: false);

        var second =
            await SeedInternationalLicenseForExistingDriverAsync(
                first,
                isActive: false);

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByDriverIdAsync(
                first.DriverId);

        // Assert
        var firstIndex =
            result.FindIndex(
                x =>
                    x.InternationalLicenseID ==
                    first.InternationalLicenseId);

        var secondIndex =
            result.FindIndex(
                x =>
                    x.InternationalLicenseID ==
                    second.InternationalLicenseId);

        Assert.True(firstIndex >= 0);
        Assert.True(secondIndex >= 0);

        Assert.True(
            secondIndex < firstIndex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByDriverIdAsync_ShouldReturnEmptyForInvalidDriverId(
        int driverId)
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByDriverIdAsync(
                driverId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByDriverIdAsync_ShouldReturnDetachedEntities()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByDriverIdAsync(
                seed.DriverId);

        // Assert
        Assert.Single(result);

        Assert.All(
            result,
            entity =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(entity).State));
    }

    // =========================================================
    // GET BY APPLICATION ID
    // =========================================================

    [Fact]
    public async Task GetByApplicationIdAsync_ShouldReturnMatchingLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByApplicationIdAsync(
                seed.ApplicationId);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            seed.InternationalLicenseId,
            result.InternationalLicenseID);
    }

    [Fact]
    public async Task GetByApplicationIdAsync_ShouldReturnNullForMissingApplication()
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByApplicationIdAsync(
                int.MaxValue);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByApplicationIdAsync_ShouldReturnNullForInvalidApplicationId(
        int applicationId)
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByApplicationIdAsync(
                applicationId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByApplicationIdAsync_ShouldReturnDetachedEntity()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByApplicationIdAsync(
                seed.ApplicationId);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);
    }

    // =========================================================
    // GET BY LOCAL LICENSE ID
    // =========================================================

    [Fact]
    public async Task GetByLocalLicenseIdAsync_ShouldReturnMatchingLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByLocalLicenseIdAsync(
                seed.LocalLicenseId);

        // Assert
        var entity =
            Assert.Single(result);

        Assert.Equal(
            seed.InternationalLicenseId,
            entity.InternationalLicenseID);
    }

    [Fact]
    public async Task GetByLocalLicenseIdAsync_ShouldReturnEmptyForMissingLocalLicense()
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByLocalLicenseIdAsync(
                int.MaxValue);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByLocalLicenseIdAsync_ShouldReturnEmptyForInvalidLocalLicenseId(
        int localLicenseId)
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByLocalLicenseIdAsync(
                localLicenseId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByLocalLicenseIdAsync_ShouldReturnDetachedEntities()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.GetByLocalLicenseIdAsync(
                seed.LocalLicenseId);

        // Assert
        Assert.Single(result);

        Assert.All(
            result,
            entity =>
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(entity).State));
    }

    // =========================================================
    // EXISTS BY LOCAL LICENSE
    // =========================================================

    [Fact]
    public async Task ExistsByLocalLicenseAsync_ShouldReturnTrueForExistingLocalLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync();

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.ExistsByLocalLicenseAsync(
                seed.LocalLicenseId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByLocalLicenseAsync_ShouldReturnFalseForMissingLocalLicense()
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.ExistsByLocalLicenseAsync(
                int.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExistsByLocalLicenseAsync_ShouldReturnFalseForInvalidId(
        int localLicenseId)
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.ExistsByLocalLicenseAsync(
                localLicenseId);

        // Assert
        Assert.False(result);
    }

    // =========================================================
    // HAS ACTIVE INTERNATIONAL LICENSE
    // =========================================================

    [Fact]
    public async Task HasActiveInternationalLicenseAsync_ShouldReturnTrueForActiveUnexpiredLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync(
                isActive: true,
                expirationDate:
                    DateTime.UtcNow.AddYears(1));

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.HasActiveInternationalLicenseAsync(
                seed.DriverId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasActiveInternationalLicenseAsync_ShouldReturnFalseForInactiveLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync(
                isActive: false,
                expirationDate:
                    DateTime.UtcNow.AddYears(1));

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.HasActiveInternationalLicenseAsync(
                seed.DriverId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasActiveInternationalLicenseAsync_ShouldReturnFalseForExpiredLicense()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync(
                isActive: true,
                expirationDate:
                    DateTime.UtcNow.AddDays(-1));

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.HasActiveInternationalLicenseAsync(
                seed.DriverId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasActiveInternationalLicenseAsync_ShouldReturnFalseForMissingDriver()
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.HasActiveInternationalLicenseAsync(
                int.MaxValue);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HasActiveInternationalLicenseAsync_ShouldReturnFalseForInvalidDriverId(
        int driverId)
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        var result =
            await repository.HasActiveInternationalLicenseAsync(
                driverId);

        // Assert
        Assert.False(result);
    }

    // =========================================================
    // ADD
    // =========================================================

    [Fact]
    public async Task AddAsync_ShouldThrowWhenEntityIsNull()
    {
        // Arrange
        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_ShouldMarkEntityAsAdded()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync(
                isActive: false);

        var localLicenseId =
            await CreateAdditionalLocalLicenseAsync(
                seed);

        var entity =
            new InternationalLicense
            {
                ApplicationID =
                    seed.ApplicationId,

                DriverID =
                    seed.DriverId,

                IssuedUsingLocalLicenseID =
                    localLicenseId,

                IssueDate =
                    DateTime.UtcNow,

                ExpirationDate =
                    DateTime.UtcNow.AddYears(1),

                IsActive =
                    false,

                CreatedByUserID =
                    seed.CreatedByUserId
            };

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        await repository.AddAsync(entity);

        // Assert
        Assert.Equal(
            EntityState.Added,
            context.Entry(entity).State);
    }

    [Fact]
    public async Task AddAsync_ShouldNotPersistUntilSaveChanges()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync(
                isActive: false);

        var localLicenseId =
            await CreateAdditionalLocalLicenseAsync(
                seed);

        var entity =
            new InternationalLicense
            {
                ApplicationID =
                    seed.ApplicationId,

                DriverID =
                    seed.DriverId,

                IssuedUsingLocalLicenseID =
                    localLicenseId,

                IssueDate =
                    DateTime.UtcNow,

                ExpirationDate =
                    DateTime.UtcNow.AddYears(1),

                IsActive =
                    false,

                CreatedByUserID =
                    seed.CreatedByUserId
            };

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        await repository.AddAsync(entity);

        await using var verificationContext =
            _database.CreateContext();

        var persistedBeforeSave =
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.IssuedUsingLocalLicenseID ==
                        localLicenseId);

        // Assert
        Assert.False(persistedBeforeSave);

        Assert.Equal(
            EntityState.Added,
            context.Entry(entity).State);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistAfterSaveChanges()
    {
        // Arrange
        var seed =
            await SeedInternationalLicenseAsync(
                isActive: false);

        var localLicenseId =
            await CreateAdditionalLocalLicenseAsync(
                seed);

        var entity =
            new InternationalLicense
            {
                ApplicationID =
                    seed.ApplicationId,

                DriverID =
                    seed.DriverId,

                IssuedUsingLocalLicenseID =
                    localLicenseId,

                IssueDate =
                    DateTime.UtcNow,

                ExpirationDate =
                    DateTime.UtcNow.AddYears(1),

                IsActive =
                    false,

                CreatedByUserID =
                    seed.CreatedByUserId
            };

        await using var context =
            _database.CreateContext();

        var repository =
            new InternationalRepository(context);

        // Act
        await repository.AddAsync(entity);

        await context.SaveChangesAsync();

        // Assert
        Assert.True(
            entity.InternationalLicenseID > 0);

        await using var verificationContext =
            _database.CreateContext();

        var persisted =
            await verificationContext
                .InternationalLicenses
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.InternationalLicenseID ==
                        entity.InternationalLicenseID);

        Assert.NotNull(persisted);

        Assert.Equal(
            entity.DriverID,
            persisted.DriverID);

        Assert.Equal(
            entity.IssuedUsingLocalLicenseID,
            persisted.IssuedUsingLocalLicenseID);
    }

    // =========================================================
    // SEED
    // =========================================================

    private async Task<InternationalLicenseSeed>
        SeedInternationalLicenseAsync(
            bool isActive = false,
            DateTime? expirationDate = null)
    {
        await using var context =
            _database.CreateContext();

        var suffix =
            Guid.NewGuid().ToString("N");

        // -----------------------------------------------------
        // Country
        // -----------------------------------------------------

        var country =
            new Country
            {
                CountryName =
                    $"Test Country {suffix}"
            };

        context.Countries.Add(country);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // Person
        // -----------------------------------------------------

        var person =
            new Person
            {
                NationalNo =
                    $"INT-{suffix[..16]}",

                FirstName =
                    "International",

                SecondName =
                    "Integration",

                ThirdName =
                    "Test",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Test Address",

                Phone =
                    $"079{Random.Shared.Next(
                        1000000,
                        9999999)}",

                Email =
                    $"{suffix}@example.com",

                NationalityCountryID =
                    country.CountryId
            };

        context.People.Add(person);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // User
        // -----------------------------------------------------

        var user =
            new User
            {
                PersonId =
                    person.PersonId,

                UserName =
                    $"int_user_{suffix}",

                Password =
                    "TestPassword",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // Application Type
        // -----------------------------------------------------

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    $"International Test {suffix}",

                ApplicationFees =
                    50m
            };

        context.ApplicationTypes.Add(
            applicationType);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // License Class
        // -----------------------------------------------------

        var licenseClass =
            new LicenseClass
            {
                ClassName =
                    $"Class {suffix}",

                ClassDescription =
                    "Integration test license class",

                MinimumAllowedAge =
                    18,

                DefaultValidityLength =
                    5,

                ClassFees =
                    100m
            };

        context.LicenseClasses.Add(
            licenseClass);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // Application
        // -----------------------------------------------------

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

                PaidFees =
                    applicationType.ApplicationFees,

                CreatedByUserID =
                    user.UserId
            };

        context.Applications.Add(application);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // Driver
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // Local Driving License Application
        // -----------------------------------------------------

        var localApplication =
            new LocalDrivingLicenseApplication
            {
                ApplicationID =
                    application.ApplicationID,

                LicenseClassID =
                    licenseClass.LicenseClassID
            };

        context.LocalDrivingLicenseApplications.Add(
            localApplication);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // Local License
        // -----------------------------------------------------

        var localLicense =
            new License
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    driver.DriverID,

                LicenseClass =
                    licenseClass.LicenseClassID,

                IssueDate =
                    DateTime.UtcNow.AddYears(-1),

                ExpirationDate =
                    DateTime.UtcNow.AddYears(4),

                Notes =
                    "International integration test",

                PaidFees =
                    100m,

                IsActive =
                    true,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    user.UserId
            };

        context.Licenses.Add(localLicense);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // International License
        // -----------------------------------------------------

        var internationalLicense =
            new InternationalLicense
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    driver.DriverID,

                IssuedUsingLocalLicenseID =
                    localLicense.LicenseID,

                IssueDate =
                    DateTime.UtcNow,

                ExpirationDate =
                    expirationDate ??
                    DateTime.UtcNow.AddYears(1),

                IsActive =
                    isActive,

                CreatedByUserID =
                    user.UserId
            };

        context.InternationalLicenses.Add(
            internationalLicense);

        await context.SaveChangesAsync();

        return new InternationalLicenseSeed
        {
            InternationalLicenseId =
                internationalLicense.InternationalLicenseID,

            ApplicationId =
                application.ApplicationID,

            DriverId =
                driver.DriverID,

            PersonId =
                person.PersonId,

            LocalLicenseId =
                localLicense.LicenseID,

            CreatedByUserId =
                user.UserId
        };
    }

    // =========================================================
    // SEED SECOND INTERNATIONAL LICENSE
    // FOR SAME DRIVER
    // =========================================================

    private async Task<InternationalLicenseSeed>
        SeedInternationalLicenseForExistingDriverAsync(
            InternationalLicenseSeed existing,
            bool isActive = false)
    {
        await using var context =
            _database.CreateContext();

        var suffix =
            Guid.NewGuid().ToString("N");

        var applicationType =
            await context.ApplicationTypes
                .AsNoTracking()
                .FirstOrDefaultAsync();

        if (applicationType is null)
        {
            applicationType =
                new ApplicationType
                {
                    ApplicationTypeTitle =
                        $"International Additional {suffix}",

                    ApplicationFees =
                        50m
                };

            context.ApplicationTypes.Add(
                applicationType);

            await context.SaveChangesAsync();
        }

        var licenseClass =
            await context.LicenseClasses
                .AsNoTracking()
                .FirstAsync();

        // -----------------------------------------------------
        // Application
        // -----------------------------------------------------

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    existing.PersonId,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    applicationType.ApplicationFees,

                CreatedByUserID =
                    existing.CreatedByUserId
            };

        context.Applications.Add(application);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // Local License
        // -----------------------------------------------------

        var localLicense =
            new License
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    existing.DriverId,

                LicenseClass =
                    licenseClass.LicenseClassID,

                IssueDate =
                    DateTime.UtcNow.AddYears(-1),

                ExpirationDate =
                    DateTime.UtcNow.AddYears(4),

                Notes =
                    "Additional local license",

                PaidFees =
                    100m,

                IsActive =
                    true,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    existing.CreatedByUserId
            };

        context.Licenses.Add(localLicense);

        await context.SaveChangesAsync();

        // -----------------------------------------------------
        // International License
        // -----------------------------------------------------

        var internationalLicense =
            new InternationalLicense
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    existing.DriverId,

                IssuedUsingLocalLicenseID =
                    localLicense.LicenseID,

                IssueDate =
                    DateTime.UtcNow,

                ExpirationDate =
                    DateTime.UtcNow.AddYears(1),

                IsActive =
                    isActive,

                CreatedByUserID =
                    existing.CreatedByUserId
            };

        context.InternationalLicenses.Add(
            internationalLicense);

        await context.SaveChangesAsync();

        return new InternationalLicenseSeed
        {
            InternationalLicenseId =
                internationalLicense.InternationalLicenseID,

            ApplicationId =
                application.ApplicationID,

            DriverId =
                existing.DriverId,

            PersonId =
                existing.PersonId,

            LocalLicenseId =
                localLicense.LicenseID,

            CreatedByUserId =
                existing.CreatedByUserId
        };
    }

    // =========================================================
    // CREATE ADDITIONAL LOCAL LICENSE
    // =========================================================

    private async Task<int>
        CreateAdditionalLocalLicenseAsync(
            InternationalLicenseSeed existing)
    {
        await using var context =
            _database.CreateContext();

        var driver =
            await context.Drivers
                .AsNoTracking()
                .FirstAsync(
                    x =>
                        x.DriverID ==
                        existing.DriverId);

        var applicationType =
            await context.ApplicationTypes
                .AsNoTracking()
                .FirstAsync();

        var licenseClass =
            await context.LicenseClasses
                .AsNoTracking()
                .FirstAsync();

        var application =
            new ApplicationD
            {
                ApplicantPersonID =
                    driver.PersonID,

                ApplicationDate =
                    DateTime.UtcNow,

                ApplicationTypeID =
                    applicationType.ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    DateTime.UtcNow,

                PaidFees =
                    applicationType.ApplicationFees,

                CreatedByUserID =
                    existing.CreatedByUserId
            };

        context.Applications.Add(application);

        await context.SaveChangesAsync();

        var license =
            new License
            {
                ApplicationID =
                    application.ApplicationID,

                DriverID =
                    existing.DriverId,

                LicenseClass =
                    licenseClass.LicenseClassID,

                IssueDate =
                    DateTime.UtcNow,

                ExpirationDate =
                    DateTime.UtcNow.AddYears(5),

                Notes =
                    "Additional local license for test",

                PaidFees =
                    100m,

                IsActive =
                    true,

                IssueReason =
                    IssueReason.FirstTime,

                CreatedByUserID =
                    existing.CreatedByUserId
            };

        context.Licenses.Add(license);

        await context.SaveChangesAsync();

        return license.LicenseID;
    }

    // =========================================================
    // SEED RESULT
    // =========================================================

    private sealed class InternationalLicenseSeed
    {
        public int InternationalLicenseId { get; init; }

        public int ApplicationId { get; init; }

        public int DriverId { get; init; }

        public int PersonId { get; init; }

        public int LocalLicenseId { get; init; }

        public int CreatedByUserId { get; init; }
    }
}