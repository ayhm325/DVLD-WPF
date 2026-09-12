using Domain.Entities;
using Infrastructure.IntegrationTests.Fixtures;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.IntegrationTests;

public sealed class ApplicationTypeRepositoryTests
    : IClassFixture<ApplicationTypeRepositoryDatabaseFixture>
{
    private readonly ApplicationTypeRepositoryDatabaseFixture _fixture;

    public ApplicationTypeRepositoryTests(
        ApplicationTypeRepositoryDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    [Fact]
    public void Constructor_ShouldThrowWhenContextIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ApplicationTypeRepository(null!));
    }

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllApplicationTypesAsync_ShouldReturnAllSeededApplicationTypes()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetAllApplicationTypesAsync();

        Assert.Equal(
            3,
            result.Count);

        Assert.Contains(
            result,
            x =>
                x.ApplicationTypeId ==
                _fixture.FirstApplicationTypeId);

        Assert.Contains(
            result,
            x =>
                x.ApplicationTypeId ==
                _fixture.SecondApplicationTypeId);

        Assert.Contains(
            result,
            x =>
                x.ApplicationTypeId ==
                _fixture.ThirdApplicationTypeId);
    }

    [Fact]
    public async Task GetAllApplicationTypesAsync_ShouldReturnApplicationTypesOrderedByIdAscending()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetAllApplicationTypesAsync();

        Assert.Equal(
            _fixture.FirstApplicationTypeId,
            result[0].ApplicationTypeId);

        Assert.Equal(
            _fixture.SecondApplicationTypeId,
            result[1].ApplicationTypeId);

        Assert.Equal(
            _fixture.ThirdApplicationTypeId,
            result[2].ApplicationTypeId);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i - 1].ApplicationTypeId <
                result[i].ApplicationTypeId);
        }
    }

    [Fact]
    public async Task GetAllApplicationTypesAsync_ShouldReturnExpectedData()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetAllApplicationTypesAsync();

        var first =
            result.Single(
                x =>
                    x.ApplicationTypeId ==
                    _fixture.FirstApplicationTypeId);

        var second =
            result.Single(
                x =>
                    x.ApplicationTypeId ==
                    _fixture.SecondApplicationTypeId);

        var third =
            result.Single(
                x =>
                    x.ApplicationTypeId ==
                    _fixture.ThirdApplicationTypeId);

        Assert.Equal(
            "New Local Driving License",
            first.ApplicationTypeTitle);

        Assert.Equal(
            20m,
            first.ApplicationFees);

        Assert.Equal(
            "Renew Driving License",
            second.ApplicationTypeTitle);

        Assert.Equal(
            15m,
            second.ApplicationFees);

        Assert.Equal(
            "Replacement for Damaged License",
            third.ApplicationTypeTitle);

        Assert.Equal(
            10m,
            third.ApplicationFees);
    }

    [Fact]
    public async Task GetAllApplicationTypesAsync_ShouldReturnEntitiesWithoutTracking()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetAllApplicationTypesAsync();

        Assert.NotEmpty(result);

        Assert.All(
            result,
            applicationType =>
                Assert.Equal(
                    EntityState.Detached,
                    scope.Context.Entry(applicationType).State));
    }

    [Fact]
    public async Task GetAllApplicationTypesAsync_ShouldNotReturnUncommittedEntities()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var uncommittedApplicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "Uncommitted Application Type",

                ApplicationFees =
                    999m
            };

        scope.Context.ApplicationTypes.Add(
            uncommittedApplicationType);

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetAllApplicationTypesAsync();

        Assert.DoesNotContain(
            result,
            x =>
                x.ApplicationTypeTitle ==
                "Uncommitted Application Type");
    }

    [Fact]
    public async Task GetAllApplicationTypesAsync_ShouldNotModifyTrackedStateOfExistingEntities()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var trackedApplicationType =
            await scope.Context.ApplicationTypes
                .SingleAsync(
                    x =>
                        x.ApplicationTypeId ==
                        _fixture.FirstApplicationTypeId);

        Assert.Equal(
            EntityState.Unchanged,
            scope.Context.Entry(trackedApplicationType).State);

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetAllApplicationTypesAsync();

        Assert.Contains(
            result,
            x =>
                x.ApplicationTypeId ==
                _fixture.FirstApplicationTypeId);

        Assert.Equal(
            EntityState.Unchanged,
            scope.Context.Entry(trackedApplicationType).State);

        var returnedApplicationType =
            result.Single(
                x =>
                    x.ApplicationTypeId ==
                    _fixture.FirstApplicationTypeId);

        Assert.NotSame(
            trackedApplicationType,
            returnedApplicationType);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(returnedApplicationType).State);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Fact]
    public async Task GetApplicationTypeByIdAsync_ShouldReturnExistingApplicationType()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetApplicationTypeByIdAsync(
                _fixture.SecondApplicationTypeId);

        Assert.NotNull(result);

        Assert.Equal(
            _fixture.SecondApplicationTypeId,
            result.ApplicationTypeId);

        Assert.Equal(
            "Renew Driving License",
            result.ApplicationTypeTitle);

        Assert.Equal(
            15m,
            result.ApplicationFees);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_ShouldReturnNullWhenApplicationTypeDoesNotExist()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetApplicationTypeByIdAsync(
                int.MaxValue);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_ShouldReturnNullForZeroId()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetApplicationTypeByIdAsync(0);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_ShouldReturnNullForNegativeId()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetApplicationTypeByIdAsync(-1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_ShouldReturnEntityWithoutTracking()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetApplicationTypeByIdAsync(
                _fixture.ThirdApplicationTypeId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result).State);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_ShouldNotReturnUncommittedEntity()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var uncommittedApplicationType =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "Uncommitted By Id",

                ApplicationFees =
                    500m
            };

        scope.Context.ApplicationTypes.Add(
            uncommittedApplicationType);

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetApplicationTypeByIdAsync(
                uncommittedApplicationType.ApplicationTypeId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationTypeByIdAsync_ShouldNotModifyTrackedStateOfExistingEntity()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var trackedApplicationType =
            await scope.Context.ApplicationTypes
                .SingleAsync(
                    x =>
                        x.ApplicationTypeId ==
                        _fixture.FirstApplicationTypeId);

        Assert.Equal(
            EntityState.Unchanged,
            scope.Context.Entry(trackedApplicationType).State);

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var result =
            await repository.GetApplicationTypeByIdAsync(
                _fixture.FirstApplicationTypeId);

        Assert.NotNull(result);

        Assert.Equal(
            EntityState.Unchanged,
            scope.Context.Entry(trackedApplicationType).State);

        Assert.NotSame(
            trackedApplicationType,
            result);

        Assert.Equal(
            EntityState.Detached,
            scope.Context.Entry(result).State);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldThrowWhenApplicationTypeIsNull()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () =>
                repository.UpdateApplicationTypeAsync(
                    null!));
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldReturnFalseForZeroId()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeId = 0,
                ApplicationTypeTitle =
                    "Invalid Application Type",
                ApplicationFees =
                    100m
            };

        var result =
            await repository.UpdateApplicationTypeAsync(
                applicationType);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldReturnFalseForNegativeId()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeId = -1,
                ApplicationTypeTitle =
                    "Invalid Application Type",
                ApplicationFees =
                    100m
            };

        var result =
            await repository.UpdateApplicationTypeAsync(
                applicationType);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldReturnFalseWhenApplicationTypeDoesNotExist()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeId =
                    int.MaxValue,

                ApplicationTypeTitle =
                    "Non Existing Application Type",

                ApplicationFees =
                    100m
            };

        var result =
            await repository.UpdateApplicationTypeAsync(
                applicationType);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldReturnTrueForExistingApplicationType()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeId =
                    _fixture.FirstApplicationTypeId,

                ApplicationTypeTitle =
                    "Updated Application Type",

                ApplicationFees =
                    123.45m
            };

        var result =
            await repository.UpdateApplicationTypeAsync(
                applicationType);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldUpdateTrackedEntityValues()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeId =
                    _fixture.SecondApplicationTypeId,

                ApplicationTypeTitle =
                    "Updated Renewal Application",

                ApplicationFees =
                    77.75m
            };

        var result =
            await repository.UpdateApplicationTypeAsync(
                applicationType);

        Assert.True(result);

        var trackedEntity =
            await scope.Context.ApplicationTypes
                .SingleAsync(
                    x =>
                        x.ApplicationTypeId ==
                        _fixture.SecondApplicationTypeId);

        Assert.Equal(
            "Updated Renewal Application",
            trackedEntity.ApplicationTypeTitle);

        Assert.Equal(
            77.75m,
            trackedEntity.ApplicationFees);

        Assert.Equal(
            EntityState.Modified,
            scope.Context.Entry(trackedEntity).State);
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldPersistChangesWhenUnitOfWorkSaves()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var unitOfWork =
            new Infrastructure.UnitOfWork(scope.Context);

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeId =
                    _fixture.ThirdApplicationTypeId,

                ApplicationTypeTitle =
                    "Updated Replacement Application",

                ApplicationFees =
                    88.80m
            };

        var result =
            await repository.UpdateApplicationTypeAsync(
                applicationType);

        Assert.True(result);

        await unitOfWork.SaveChangesAsync();

        var persistedApplicationType =
            await scope.Context.ApplicationTypes
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.ApplicationTypeId ==
                        _fixture.ThirdApplicationTypeId);

        Assert.Equal(
            "Updated Replacement Application",
            persistedApplicationType.ApplicationTypeTitle);

        Assert.Equal(
            88.80m,
            persistedApplicationType.ApplicationFees);

        // No explicit commit.
        // The surrounding SQL transaction is rolled back
        // when the test scope is disposed.
    }

    [Fact]
    public async Task UpdateApplicationTypeAsync_ShouldNotChangeApplicationTypeId()
    {
        await using var scope =
            await CreateTestScopeAsync();

        var repository =
            new ApplicationTypeRepository(scope.Context);

        var originalId =
            _fixture.FirstApplicationTypeId;

        var applicationType =
            new ApplicationType
            {
                ApplicationTypeId =
                    originalId,

                ApplicationTypeTitle =
                    "Updated Without Changing Id",

                ApplicationFees =
                    55m
            };

        var result =
            await repository.UpdateApplicationTypeAsync(
                applicationType);

        Assert.True(result);

        var trackedEntity =
            await scope.Context.ApplicationTypes
                .SingleAsync(
                    x =>
                        x.ApplicationTypeId ==
                        originalId);

        Assert.Equal(
            originalId,
            trackedEntity.ApplicationTypeId);

        Assert.Equal(
            "Updated Without Changing Id",
            trackedEntity.ApplicationTypeTitle);

        Assert.Equal(
            55m,
            trackedEntity.ApplicationFees);
    }

    // =========================================================
    // TEST DATABASE SCOPE
    // =========================================================

    private async Task<ApplicationTypeTestScope>
        CreateTestScopeAsync()
    {
        var context =
            _fixture.Database.CreateContext();

        try
        {
            var transaction =
                await context.Database.BeginTransactionAsync();

            return new ApplicationTypeTestScope(
                context,
                transaction);
        }
        catch
        {
            await context.DisposeAsync();
            throw;
        }
    }

    private sealed class ApplicationTypeTestScope
        : IAsyncDisposable
    {
        public DVLDDbContext Context { get; }

        private IDbContextTransaction Transaction { get; }

        public ApplicationTypeTestScope(
            DVLDDbContext context,
            IDbContextTransaction transaction)
        {
            Context = context;
            Transaction = transaction;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Transaction.RollbackAsync();
            }
            finally
            {
                await Transaction.DisposeAsync();
                await Context.DisposeAsync();
            }
        }
    }
}

// =============================================================
// DATABASE FIXTURE
// =============================================================

public sealed class ApplicationTypeRepositoryDatabaseFixture
    : IAsyncLifetime
{
    private SqlServerTestDatabase? _database;

    public SqlServerTestDatabase Database { get; private set; } = null!;

    public int FirstApplicationTypeId { get; private set; }

    public int SecondApplicationTypeId { get; private set; }

    public int ThirdApplicationTypeId { get; private set; }

    public async Task InitializeAsync()
    {
        _database =
            new SqlServerTestDatabase();

        await _database.InitializeAsync();

        Database =
            _database;

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync();
        }
    }

    private async Task SeedAsync()
    {
        await using var context =
            Database.CreateContext();

        var first =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "New Local Driving License",

                ApplicationFees =
                    20m
            };

        var second =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "Renew Driving License",

                ApplicationFees =
                    15m
            };

        var third =
            new ApplicationType
            {
                ApplicationTypeTitle =
                    "Replacement for Damaged License",

                ApplicationFees =
                    10m
            };

        context.ApplicationTypes.AddRange(
            first,
            second,
            third);

        await context.SaveChangesAsync();

        FirstApplicationTypeId =
            first.ApplicationTypeId;

        SecondApplicationTypeId =
            second.ApplicationTypeId;

        ThirdApplicationTypeId =
            third.ApplicationTypeId;

        Assert.True(
            FirstApplicationTypeId > 0);

        Assert.True(
            SecondApplicationTypeId > 0);

        Assert.True(
            ThirdApplicationTypeId > 0);

        Assert.NotEqual(
            FirstApplicationTypeId,
            SecondApplicationTypeId);

        Assert.NotEqual(
            FirstApplicationTypeId,
            ThirdApplicationTypeId);

        Assert.NotEqual(
            SecondApplicationTypeId,
            ThirdApplicationTypeId);
    }
}