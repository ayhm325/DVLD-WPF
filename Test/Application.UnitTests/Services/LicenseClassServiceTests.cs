using Application.Common.Results;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Moq;

namespace Application.UnitTests.Services;

public sealed class LicenseClassServiceTests
{
    private readonly Mock<ILicenseClassRepository> _repository = new();

    private LicenseClassService CreateService() =>
        new(_repository.Object);

    private static LicenseClass CreateEntity(
        int id = 1,
        string name = "Small Motorcycle",
        string description = "Motorcycle license",
        byte minAge = 18,
        byte validity = 10,
        decimal fees = 20m)
        => new()
        {
            LicenseClassID = id,
            ClassName = name,
            ClassDescription = description,
            MinimumAllowedAge = minAge,
            DefaultValidityLength = validity,
            ClassFees = fees
        };

    // =========================================================
    // GET ALL
    // =========================================================

    [Fact]
    public async Task GetAllLicenseClassesAsync_WhenRepositoryReturnsData_ReturnsMappedDtos()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllLicenseClassAsync())
            .ReturnsAsync(
            [
                CreateEntity(
                    1,
                    "Small Motorcycle",
                    "Motorcycle license",
                    18,
                    10,
                    20m),
                CreateEntity(
                    2,
                    "Private Car",
                    "Private vehicle license",
                    18,
                    10,
                    50m)
            ]);

        var result =
            await service.GetAllLicenseClassesAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(
            1,
            result.Value[0].LicenseClassID);
        Assert.Equal(
            "Small Motorcycle",
            result.Value[0].LicenseClassName);
        Assert.Equal(
            "Motorcycle license",
            result.Value[0].LicenseClassDescription);
        Assert.Equal(
            (byte)18,
            result.Value[0].MinAllowedAge);
        Assert.Equal(
            (byte)10,
            result.Value[0].DefaultValidityLength);
        Assert.Equal(
            20m,
            result.Value[0].LicenseClassFees);

        Assert.Equal(
            2,
            result.Value[1].LicenseClassID);
    }

    [Fact]
    public async Task GetAllLicenseClassesAsync_WhenRepositoryReturnsEmpty_ReturnsEmptyList()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllLicenseClassAsync())
            .ReturnsAsync([]);

        var result =
            await service.GetAllLicenseClassesAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAllLicenseClassesAsync_PreservesRepositoryOrder()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllLicenseClassAsync())
            .ReturnsAsync(
            [
                CreateEntity(30, "C", "C", 21, 5, 30m),
                CreateEntity(10, "A", "A", 18, 10, 10m),
                CreateEntity(20, "B", "B", 19, 7, 20m)
            ]);

        var result =
            await service.GetAllLicenseClassesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(
            30,
            result.Value![0].LicenseClassID);
        Assert.Equal(
            10,
            result.Value[1].LicenseClassID);
        Assert.Equal(
            20,
            result.Value[2].LicenseClassID);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetLicenseClassByIdAsync_WhenIdIsInvalid_ReturnsValidation(
        int id)
    {
        var service = CreateService();

        var result =
            await service.GetLicenseClassByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.Validation,
            result.ErrorType);
        Assert.Equal(
            "Invalid license class ID.",
            result.Error);

        _repository.Verify(
            x => x.GetLicenseClassByIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetLicenseClassByIdAsync(10))
            .ReturnsAsync((LicenseClass?)null);

        var result =
            await service.GetLicenseClassByIdAsync(10);

        Assert.True(result.IsFailure);
        Assert.Equal(
            ErrorType.NotFound,
            result.ErrorType);
        Assert.Equal(
            "License class not found.",
            result.Error);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_WhenFound_ReturnsMappedDto()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetLicenseClassByIdAsync(10))
            .ReturnsAsync(
                CreateEntity(
                    10,
                    "Private Car",
                    "Private vehicle license",
                    18,
                    10,
                    75m));

        var result =
            await service.GetLicenseClassByIdAsync(10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(
            10,
            result.Value!.LicenseClassID);
        Assert.Equal(
            "Private Car",
            result.Value.LicenseClassName);
        Assert.Equal(
            "Private vehicle license",
            result.Value.LicenseClassDescription);
        Assert.Equal(
            (byte)18,
            result.Value.MinAllowedAge);
        Assert.Equal(
            (byte)10,
            result.Value.DefaultValidityLength);
        Assert.Equal(
            75m,
            result.Value.LicenseClassFees);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_WhenFound_DoesNotModifyEntityValues()
    {
        var service = CreateService();

        var entity =
            CreateEntity(
                7,
                "  Private Car  ",
                "  Private vehicle  ",
                18,
                10,
                50m);

        _repository
            .Setup(x => x.GetLicenseClassByIdAsync(7))
            .ReturnsAsync(entity);

        var result =
            await service.GetLicenseClassByIdAsync(7);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            entity.ClassName,
            result.Value!.LicenseClassName);
        Assert.Equal(
            entity.ClassDescription,
            result.Value.LicenseClassDescription);
        Assert.Equal(
            entity.ClassFees,
            result.Value.LicenseClassFees);
    }

    // =========================================================
    // NULL DEPENDENCY
    // =========================================================

    [Fact]
    public void Constructor_WhenRepositoryIsNull_ThrowsArgumentNullException()
    {
        var exception =
            Assert.Throws<ArgumentNullException>(
                () => new LicenseClassService(null!));

        Assert.Equal(
            "licenseClassRepository",
            exception.ParamName);
    }

    // =========================================================
    // EXCEPTION PROPAGATION
    // =========================================================

    [Fact]
    public async Task GetAllLicenseClassesAsync_WhenRepositoryThrows_PropagatesException()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetAllLicenseClassAsync())
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database error"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetAllLicenseClassesAsync());

        Assert.Equal(
            "Database error",
            exception.Message);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_WhenRepositoryThrows_PropagatesException()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetLicenseClassByIdAsync(1))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Database error"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GetLicenseClassByIdAsync(1));

        Assert.Equal(
            "Database error",
            exception.Message);
    }

    // =========================================================
    // SPECIAL VALUES
    // =========================================================

    [Fact]
    public async Task GetLicenseClassByIdAsync_WhenFeesAreZero_MapsZeroFees()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetLicenseClassByIdAsync(1))
            .ReturnsAsync(
                CreateEntity(
                    fees: 0m));

        var result =
            await service.GetLicenseClassByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            0m,
            result.Value!.LicenseClassFees);
    }

    [Fact]
    public async Task GetLicenseClassByIdAsync_WhenDescriptionIsEmpty_PreservesEmptyDescription()
    {
        var service = CreateService();

        _repository
            .Setup(x => x.GetLicenseClassByIdAsync(1))
            .ReturnsAsync(
                CreateEntity(
                    description: string.Empty));

        var result =
            await service.GetLicenseClassByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            string.Empty,
            result.Value!.LicenseClassDescription);
    }
}
