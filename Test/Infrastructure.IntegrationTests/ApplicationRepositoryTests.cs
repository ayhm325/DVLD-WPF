using Domain.Entities;
using Domain.Enums;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.IntegrationTests;

public sealed class ApplicationRepositoryTests
    : IClassFixture<ApplicationRepositoryDatabaseFixture>, IAsyncLifetime
{
    private readonly ApplicationRepositoryDatabaseFixture _fixture;

    public ApplicationRepositoryTests(
        ApplicationRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() =>
        _fixture.ResetAsync();

    public Task DisposeAsync() =>
        Task.CompletedTask;

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ApplicationRepository(null!));
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldReturnExistingApplication()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.NewApplicationId,
            result.ApplicationID);

        Assert.Equal(
            _fixture.AdminPersonId,
            result.ApplicantPersonID);

        Assert.Equal(
            _fixture.ApplicationTypeId,
            result.ApplicationTypeID);

        Assert.Equal(
            AppStatus.New,
            result.ApplicationStatus);

        Assert.Equal(
            100m,
            result.PaidFees);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldLoadPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);
        Assert.NotNull(result.Person);

        Assert.Equal(
            _fixture.AdminPersonId,
            result.Person.PersonId);

        Assert.Equal(
            "Admin",
            result.Person.FirstName);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldLoadApplicationType()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);
        Assert.NotNull(result.ApplicationType);

        Assert.Equal(
            _fixture.ApplicationTypeId,
            result.ApplicationType.ApplicationTypeId);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldLoadCreatedByUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);
        Assert.NotNull(result.CreatedByUser);

        Assert.Equal(
            _fixture.AdminUserId,
            result.CreatedByUser.UserId);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldReturnDetachedGraph()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result).State);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result.Person).State);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result.ApplicationType).State);

        Assert.Equal(
            EntityState.Detached,
            context.Entry(result.CreatedByUser).State);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldReturnNullWhenApplicationDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldReturnNullForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_ShouldReturnNullForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationByIdAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationForUpdateAsync_ShouldReturnExistingApplication()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationForUpdateAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.NewApplicationId,
            result.ApplicationID);
    }

    [Fact]
    public async Task GetApplicationForUpdateAsync_ShouldTrackEntity()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationForUpdateAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            context.Entry(result).State);
    }

    [Fact]
    public async Task GetApplicationForUpdateAsync_ShouldNotLoadNavigationProperties()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationForUpdateAsync(
                _fixture.NewApplicationId);

        Assert.NotNull(result);

        Assert.Null(result.Person);
        Assert.Null(result.ApplicationType);
        Assert.Null(result.CreatedByUser);
    }

    [Fact]
    public async Task GetApplicationForUpdateAsync_ShouldReturnNullWhenApplicationDoesNotExist()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationForUpdateAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationForUpdateAsync_ShouldReturnNullForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationForUpdateAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationForUpdateAsync_ShouldReturnNullForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationForUpdateAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllApplicationsAsync_ShouldReturnAllApplicationsOrderedDescending()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetAllApplicationsAsync();

        Assert.Equal(
            3,
            result.Count);

        Assert.Equal(
            _fixture.CompletedApplicationId,
            result[0].ApplicationID);

        Assert.Equal(
            _fixture.CancelledApplicationId,
            result[1].ApplicationID);

        Assert.Equal(
            _fixture.NewApplicationId,
            result[2].ApplicationID);
    }

    [Fact]
    public async Task GetAllApplicationsAsync_ShouldLoadNavigationProperties()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetAllApplicationsAsync();

        Assert.All(
            result,
            application =>
            {
                Assert.NotNull(application.Person);
                Assert.NotNull(application.ApplicationType);
                Assert.NotNull(application.CreatedByUser);
            });
    }

    [Fact]
    public async Task GetAllApplicationsAsync_ShouldReturnDetachedEntities()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetAllApplicationsAsync();

        Assert.NotEmpty(result);

        Assert.All(
            result,
            application =>
            {
                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(application).State);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(application.Person).State);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(application.ApplicationType).State);

                Assert.Equal(
                    EntityState.Detached,
                    context.Entry(application.CreatedByUser).State);
            });
    }

    [Fact]
    public async Task GetApplicationsByPersonIdAsync_ShouldReturnOnlyApplicationsForPerson()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByPersonIdAsync(
                _fixture.AdminPersonId);

        Assert.Equal(
            3,
            result.Count);

        Assert.All(
            result,
            application =>
                Assert.Equal(
                    _fixture.AdminPersonId,
                    application.ApplicantPersonID));
    }

    [Fact]
    public async Task GetApplicationsByPersonIdAsync_ShouldOrderDescendingByApplicationId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByPersonIdAsync(
                _fixture.AdminPersonId);

        Assert.Equal(
            3,
            result.Count);

        Assert.Equal(
            _fixture.CompletedApplicationId,
            result[0].ApplicationID);

        Assert.Equal(
            _fixture.CancelledApplicationId,
            result[1].ApplicationID);

        Assert.Equal(
            _fixture.NewApplicationId,
            result[2].ApplicationID);
    }

    [Fact]
    public async Task GetApplicationsByPersonIdAsync_ShouldReturnEmptyWhenPersonHasNoApplications()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByPersonIdAsync(
                _fixture.StaffPersonId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByPersonIdAsync_ShouldReturnEmptyForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByPersonIdAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByPersonIdAsync_ShouldReturnEmptyForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByPersonIdAsync(-1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByApplicationTypeIdAsync_ShouldReturnOnlyApplicationsOfType()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByApplicationTypeIdAsync(
                _fixture.ApplicationTypeId);

        Assert.Equal(
            2,
            result.Count);

        Assert.All(
            result,
            application =>
                Assert.Equal(
                    _fixture.ApplicationTypeId,
                    application.ApplicationTypeID));
    }

    [Fact]
    public async Task GetApplicationsByApplicationTypeIdAsync_ShouldOrderDescendingByApplicationId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByApplicationTypeIdAsync(
                _fixture.ApplicationTypeId);

        Assert.Equal(
            _fixture.CompletedApplicationId,
            result[0].ApplicationID);

        Assert.Equal(
            _fixture.NewApplicationId,
            result[1].ApplicationID);
    }

    [Fact]
    public async Task GetApplicationsByApplicationTypeIdAsync_ShouldReturnEmptyForUnknownType()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByApplicationTypeIdAsync(
                int.MaxValue);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByApplicationTypeIdAsync_ShouldReturnEmptyForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByApplicationTypeIdAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByApplicationTypeIdAsync_ShouldReturnEmptyForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByApplicationTypeIdAsync(-1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByUserIdAsync_ShouldReturnOnlyApplicationsCreatedByUser()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByUserIdAsync(
                _fixture.AdminUserId);

        Assert.Equal(
            3,
            result.Count);

        Assert.All(
            result,
            application =>
                Assert.Equal(
                    _fixture.AdminUserId,
                    application.CreatedByUserID));
    }

    [Fact]
    public async Task GetApplicationsByUserIdAsync_ShouldOrderDescendingByApplicationId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByUserIdAsync(
                _fixture.AdminUserId);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i - 1].ApplicationID >
                result[i].ApplicationID);
        }
    }

    [Fact]
    public async Task GetApplicationsByUserIdAsync_ShouldReturnEmptyForUserWithoutApplications()
    {
        var personId =
            await _fixture.AddPersonAsync(
                "NoApplication",
                "User",
                $"NOAPP-{Guid.NewGuid():N}");

        var userId =
            await _fixture.AddUserAsync(
                personId,
                $"no-app-{Guid.NewGuid():N}");

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByUserIdAsync(
                userId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByUserIdAsync_ShouldReturnEmptyForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByUserIdAsync(0);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByUserIdAsync_ShouldReturnEmptyForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByUserIdAsync(-1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByStatusAsync_ShouldReturnOnlyMatchingStatus()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByStatusAsync(
                AppStatus.New);

        Assert.Single(result);

        Assert.Equal(
            _fixture.NewApplicationId,
            result[0].ApplicationID);

        Assert.Equal(
            AppStatus.New,
            result[0].ApplicationStatus);
    }

    [Fact]
    public async Task GetApplicationsByStatusAsync_ShouldReturnCancelledApplications()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByStatusAsync(
                AppStatus.Cancelled);

        Assert.Single(result);

        Assert.Equal(
            _fixture.CancelledApplicationId,
            result[0].ApplicationID);
    }

    [Fact]
    public async Task GetApplicationsByStatusAsync_ShouldReturnCompletedApplications()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByStatusAsync(
                AppStatus.Completed);

        Assert.Single(result);

        Assert.Equal(
            _fixture.CompletedApplicationId,
            result[0].ApplicationID);
    }

    [Fact]
    public async Task GetApplicationsByStatusAsync_ShouldReturnEmptyForUndefinedStatus()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var invalidStatus =
            (AppStatus)99;

        var result =
            await repository.GetApplicationsByStatusAsync(
                invalidStatus);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicationsByStatusAsync_ShouldOrderDescendingByApplicationId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.GetApplicationsByStatusAsync(
                AppStatus.New);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i - 1].ApplicationID >
                result[i].ApplicationID);
        }
    }

    [Fact]
    public async Task IsApplicationExistsByIdAsync_ShouldReturnTrueForExistingApplication()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsApplicationExistsByIdAsync(
                _fixture.NewApplicationId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsApplicationExistsByIdAsync_ShouldReturnFalseForMissingApplication()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsApplicationExistsByIdAsync(
                int.MaxValue);

        Assert.False(result);
    }

    [Fact]
    public async Task IsApplicationExistsByIdAsync_ShouldReturnFalseForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsApplicationExistsByIdAsync(0);

        Assert.False(result);
    }

    [Fact]
    public async Task IsApplicationExistsByIdAsync_ShouldReturnFalseForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsApplicationExistsByIdAsync(-1);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationAsync_ShouldReturnTrueForNewApplication()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationAsync(
                _fixture.AdminPersonId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationAsync_ShouldReturnFalseWhenOnlyCompletedOrCancelled()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationAsync(
                _fixture.StaffPersonId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationAsync_ShouldReturnFalseForZeroId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationAsync(0);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationAsync_ShouldReturnFalseForNegativeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationAsync(-1);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationOfTypeAsync_ShouldReturnTrueForMatchingActiveApplication()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationOfTypeAsync(
                _fixture.AdminPersonId,
                _fixture.ApplicationTypeId);

        Assert.True(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationOfTypeAsync_ShouldReturnFalseForDifferentType()
    {
        var differentTypeId =
            await _fixture.AddApplicationTypeAsync(
                "Different Application Type");

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationOfTypeAsync(
                _fixture.AdminPersonId,
                differentTypeId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationOfTypeAsync_ShouldReturnFalseWhenApplicationIsNotNew()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationOfTypeAsync(
                _fixture.AdminPersonId,
                _fixture.CancelledApplicationTypeId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationOfTypeAsync_ShouldReturnFalseForZeroPersonId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationOfTypeAsync(
                0,
                _fixture.ApplicationTypeId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationOfTypeAsync_ShouldReturnFalseForZeroApplicationTypeId()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var result =
            await repository.IsPersonHasActiveApplicationOfTypeAsync(
                _fixture.AdminPersonId,
                0);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPersonHasActiveApplicationOfTypeAsync_ShouldReturnFalseForNegativeIds()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        Assert.False(
            await repository.IsPersonHasActiveApplicationOfTypeAsync(
                -1,
                _fixture.ApplicationTypeId));

        Assert.False(
            await repository.IsPersonHasActiveApplicationOfTypeAsync(
                _fixture.AdminPersonId,
                -1));
    }

    [Fact]
    public async Task AddNewApplicationAsync_ShouldAddApplicationToContext()
    {
        var application =
            _fixture.CreateApplication(
                _fixture.StaffPersonId,
                _fixture.ApplicationTypeId,
                AppStatus.New);

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        await repository.AddNewApplicationAsync(
            application);

        Assert.Equal(
            EntityState.Added,
            context.Entry(application).State);
    }

    [Fact]
    public async Task AddNewApplicationAsync_ShouldNotPersistBeforeSaveChanges()
    {
        var application =
            _fixture.CreateApplication(
                _fixture.StaffPersonId,
                _fixture.ApplicationTypeId,
                AppStatus.New);

        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        await repository.AddNewApplicationAsync(
            application);

        Assert.Equal(
            EntityState.Added,
            context.Entry(application).State);

        var persisted =
            await context.Applications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ApplicationID ==
                        application.ApplicationID);

        Assert.False(persisted);
    }

    [Fact]
    public async Task AddNewApplicationAsync_ShouldPersistAfterSaveChanges()
    {
        var application =
            _fixture.CreateApplication(
                _fixture.StaffPersonId,
                _fixture.ApplicationTypeId,
                AppStatus.New);

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new ApplicationRepository(context);

            await repository.AddNewApplicationAsync(
                application);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var persisted =
            await verificationContext.Applications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.ApplicationID ==
                        application.ApplicationID);

        Assert.NotNull(persisted);

        Assert.Equal(
            _fixture.StaffPersonId,
            persisted.ApplicantPersonID);

        Assert.Equal(
            AppStatus.New,
            persisted.ApplicationStatus);
    }

    [Fact]
    public async Task AddNewApplicationAsync_ShouldThrowWhenApplicationIsNull()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.AddNewApplicationAsync(
                    null!));
    }

    [Fact]
    public async Task DeleteApplication_ShouldMarkApplicationAsDeleted()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var application =
            await context.Applications
                .SingleAsync(
                    x =>
                        x.ApplicationID ==
                        _fixture.CancelledApplicationId);

        repository.DeleteApplication(
            application);

        Assert.Equal(
            EntityState.Deleted,
            context.Entry(application).State);
    }

    [Fact]
    public async Task DeleteApplication_ShouldNotPersistBeforeSaveChanges()
    {
        await using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        var application =
            await context.Applications
                .SingleAsync(
                    x =>
                        x.ApplicationID ==
                        _fixture.CancelledApplicationId);

        repository.DeleteApplication(
            application);

        Assert.Equal(
            EntityState.Deleted,
            context.Entry(application).State);

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var stillExists =
            await verificationContext.Applications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ApplicationID ==
                        _fixture.CancelledApplicationId);

        Assert.True(stillExists);
    }

    [Fact]
    public async Task DeleteApplication_ShouldPersistDeletionAfterSaveChanges()
    {
        var application =
            _fixture.CreateApplication(
                _fixture.StaffPersonId,
                _fixture.ApplicationTypeId,
                AppStatus.Cancelled);

        await using (var context =
            _fixture.Database.CreateContext())
        {
            var repository =
                new ApplicationRepository(context);

            await repository.AddNewApplicationAsync(
                application);

            await context.SaveChangesAsync();

            repository.DeleteApplication(
                application);

            await context.SaveChangesAsync();
        }

        await using var verificationContext =
            _fixture.Database.CreateContext();

        var exists =
            await verificationContext.Applications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ApplicationID ==
                        application.ApplicationID);

        Assert.False(exists);
    }

    [Fact]
    public void DeleteApplication_ShouldThrowWhenApplicationIsNull()
    {
        using var context =
            _fixture.Database.CreateContext();

        var repository =
            new ApplicationRepository(context);

        Assert.Throws<ArgumentNullException>(
            () =>
                repository.DeleteApplication(
                    null!));
    }
}

public sealed class ApplicationRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int CountryId { get; private set; }

    public int AdminPersonId { get; private set; }

    public int StaffPersonId { get; private set; }

    public int AdminUserId { get; private set; }

    public int StaffUserId { get; private set; }

    public int ApplicationTypeId { get; private set; }

    public int CancelledApplicationTypeId { get; private set; }

    public int NewApplicationId { get; private set; }

    public int CancelledApplicationId { get; private set; }

    public int CompletedApplicationId { get; private set; }

    public async Task InitializeAsync()
    {
        _database =
            new SqlServerTestDatabase();

        await _database.InitializeAsync();

        Database =
            _database;

        await SeedAsync();
    }

    public async Task ResetAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }

        _database = new SqlServerTestDatabase();

        await _database.InitializeAsync();

        Database = _database;

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    public ApplicationD CreateApplication(
        int personId,
        int applicationTypeId,
        AppStatus status)
    {
        var now =
            DateTime.UtcNow;

        return new ApplicationD
        {
            ApplicantPersonID =
                personId,

            ApplicationDate =
                now,

            ApplicationTypeID =
                applicationTypeId,

            ApplicationStatus =
                status,

            LastStatusDate =
                now,

            PaidFees =
                125.50m,

            CreatedByUserID =
                StaffUserId
        };
    }

    public async Task<int> AddPersonAsync(
        string firstName,
        string lastName,
        string nationalNo)
    {
        await using var context =
            Database.CreateContext();

        var person =
            new Person
            {
                NationalNo =
                    nationalNo[..Math.Min(
                        nationalNo.Length,
                        20)],

                FirstName =
                    firstName,

                SecondName =
                    "Integration",

                LastName =
                    lastName,

                DateOfBirth =
                    new DateTime(
                        1990,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Application Repository Test Address",

                Phone =
                    $"07{Random.Shared.Next(
                        10000000,
                        99999999)}",

                Email =
                    $"{Guid.NewGuid():N}@example.com",

                NationalityCountryID =
                    CountryId
            };

        context.People.Add(
            person);

        await context.SaveChangesAsync();

        return person.PersonId;
    }

    public async Task<int> AddUserAsync(
        int personId,
        string username)
    {
        await using var context =
            Database.CreateContext();

        var user =
            new User
            {
                PersonId =
                    personId,

                UserName =
                    username[..Math.Min(
                        username.Length,
                        50)],

                Password =
                    "hashed-password",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.Add(
            user);

        await context.SaveChangesAsync();

        return user.UserId;
    }

    public async Task<int> AddApplicationTypeAsync(
        string title)
    {
        await using var context =
            Database.CreateContext();

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    title[..Math.Min(
                        title.Length,
                        100)],

                ApplicationFees =
                    50m
            };

        context.ApplicationTypes.Add(
            applicationType);

        await context.SaveChangesAsync();

        return applicationType.ApplicationTypeId;
    }

    private async Task SeedAsync()
    {
        await using var context =
            Database.CreateContext();

        await SeedCountryAsync(context);

        await SeedPeopleAsync(context);

        await SeedUsersAsync(context);

        await SeedApplicationTypesAsync(context);

        await SeedApplicationsAsync(context);

        Assert.True(
            CountryId > 0);

        Assert.True(
            AdminPersonId > 0);

        Assert.True(
            StaffPersonId > 0);

        Assert.True(
            AdminUserId > 0);

        Assert.True(
            ApplicationTypeId > 0);

        Assert.True(
            CancelledApplicationTypeId > 0);

        Assert.True(
            NewApplicationId > 0);

        Assert.True(
            CancelledApplicationId >
            NewApplicationId);

        Assert.True(
            CompletedApplicationId >
            CancelledApplicationId);
    }

    private async Task SeedCountryAsync(
        DVLDDbContext context)
    {
        var country =
            new Country
            {
                CountryName =
                    $"Application Repository Country {Guid.NewGuid():N}"
            };

        context.Countries.Add(
            country);

        await context.SaveChangesAsync();

        CountryId =
            country.CountryId;
    }

    private async Task SeedPeopleAsync(
        DVLDDbContext context)
    {
        var adminPerson =
            new Person
            {
                NationalNo =
                    "APP-ADMIN-01",

                FirstName =
                    "Admin",

                SecondName =
                    "Application",

                LastName =
                    "Person",

                DateOfBirth =
                    new DateTime(
                        1985,
                        1,
                        1),

                Gender =
                    Gender.Male,

                Address =
                    "Admin Application Address",

                Phone =
                    "0791111111",

                Email =
                    "application.admin@example.com",

                NationalityCountryID =
                    CountryId
            };

        var staffPerson =
            new Person
            {
                NationalNo =
                    "APP-STAFF-01",

                FirstName =
                    "Staff",

                SecondName =
                    "Application",

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
                    "Staff Application Address",

                Phone =
                    "0792222222",

                Email =
                    "application.staff@example.com",

                NationalityCountryID =
                    CountryId
            };

        context.People.AddRange(
            adminPerson,
            staffPerson);

        await context.SaveChangesAsync();

        AdminPersonId =
            adminPerson.PersonId;

        StaffPersonId =
            staffPerson.PersonId;
    }

    private async Task SeedUsersAsync(
        DVLDDbContext context)
    {
        var adminUser =
            new User
            {
                PersonId =
                    AdminPersonId,

                UserName =
                    "application.repository.admin",

                Password =
                    "hashed-admin-password",

                IsActive =
                    true,

                Role =
                    UserRole.Admin
            };

        var staffUser =
            new User
            {
                PersonId =
                    StaffPersonId,

                UserName =
                    "application.repository.staff",

                Password =
                    "hashed-staff-password",

                IsActive =
                    true,

                Role =
                    UserRole.Staff
            };

        context.Users.AddRange(
            adminUser,
            staffUser);

        await context.SaveChangesAsync();

        AdminUserId =
            adminUser.UserId;

        StaffUserId =
            staffUser.UserId;
    }

    private async Task SeedApplicationTypesAsync(
        DVLDDbContext context)
    {
        var mainType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "Application Repository Main Type",

                ApplicationFees =
                    100m
            };

        var cancelledType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "Application Repository Cancelled Type",

                ApplicationFees =
                    150m
            };

        context.ApplicationTypes.AddRange(
            mainType,
            cancelledType);

        await context.SaveChangesAsync();

        ApplicationTypeId =
            mainType.ApplicationTypeId;

        CancelledApplicationTypeId =
            cancelledType.ApplicationTypeId;
    }

    private async Task SeedApplicationsAsync(
        DVLDDbContext context)
    {
        var baseDate =
            new DateTime(
                2026,
                3,
                1,
                10,
                0,
                0,
                DateTimeKind.Utc);

        var newApplication =
            new ApplicationD
            {
                ApplicantPersonID =
                    AdminPersonId,

                ApplicationDate =
                    baseDate,

                ApplicationTypeID =
                    ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.New,

                LastStatusDate =
                    baseDate,

                PaidFees =
                    100m,

                CreatedByUserID =
                    AdminUserId
            };

        var cancelledApplication =
            new ApplicationD
            {
                ApplicantPersonID =
                    AdminPersonId,

                ApplicationDate =
                    baseDate.AddDays(1),

                ApplicationTypeID =
                    CancelledApplicationTypeId,

                ApplicationStatus =
                    AppStatus.Cancelled,

                LastStatusDate =
                    baseDate.AddDays(1),

                PaidFees =
                    150m,

                CreatedByUserID =
                    AdminUserId
            };

        var completedApplication =
            new ApplicationD
            {
                ApplicantPersonID =
                    AdminPersonId,

                ApplicationDate =
                    baseDate.AddDays(2),

                ApplicationTypeID =
                    ApplicationTypeId,

                ApplicationStatus =
                    AppStatus.Completed,

                LastStatusDate =
                    baseDate.AddDays(2),

                PaidFees =
                    100m,

                CreatedByUserID =
                    AdminUserId
            };

        context.Applications.AddRange(
            newApplication,
            cancelledApplication,
            completedApplication);

        await context.SaveChangesAsync();

        NewApplicationId =
            newApplication.ApplicationID;

        CancelledApplicationId =
            cancelledApplication.ApplicationID;

        CompletedApplicationId =
            completedApplication.ApplicationID;
    }
}