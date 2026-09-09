using Application.Common.Results;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using Application.Services;
using Moq;

namespace Application.UnitTests.Services;

public sealed class LicenseServiceTests
{
    private readonly Mock<ILicenseQueryService> _queryService = new();

    private LicenseService CreateService() =>
        new(_queryService.Object);

    [Fact]
    public void Constructor_WhenQueryServiceIsNull_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new LicenseService(null!));

        Assert.Equal("queryService", exception.ParamName);
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<LicenseDto>.FromNotFound("not found");
        _queryService
            .Setup(x => x.GetByIdAsync(42))
            .ReturnsAsync(expected);

        var result = await CreateService().GetByIdAsync(42);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetByIdAsync(42),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<List<LicenseDto>>.FromFailure("failure");
        _queryService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(expected);

        var result = await CreateService().GetAllAsync();

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetByDriverIdAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<List<LicenseDto>>.FromNotFound("not found");
        _queryService
            .Setup(x => x.GetByDriverIdAsync(15))
            .ReturnsAsync(expected);

        var result = await CreateService().GetByDriverIdAsync(15);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetByDriverIdAsync(15),
            Times.Once);
    }

    [Fact]
    public async Task GetByApplicationIdAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<List<LicenseDto>>.FromFailure("failure");
        _queryService
            .Setup(x => x.GetByApplicationIdAsync(25))
            .ReturnsAsync(expected);

        var result = await CreateService().GetByApplicationIdAsync(25);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetByApplicationIdAsync(25),
            Times.Once);
    }

    [Fact]
    public async Task GetByLicenseClassIdAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<List<LicenseDto>>.FromFailure("failure");
        _queryService
            .Setup(x => x.GetByLicenseClassIdAsync(7))
            .ReturnsAsync(expected);

        var result = await CreateService().GetByLicenseClassIdAsync(7);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetByLicenseClassIdAsync(7),
            Times.Once);
    }

    [Fact]
    public async Task GetLicensesByPersonIdAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<List<LicenseDto>>.FromNotFound("not found");
        _queryService
            .Setup(x => x.GetLicensesByPersonIdAsync(31))
            .ReturnsAsync(expected);

        var result = await CreateService().GetLicensesByPersonIdAsync(31);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetLicensesByPersonIdAsync(31),
            Times.Once);
    }

    [Fact]
    public async Task IsLicenseExistsAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<bool>.Success(true);
        _queryService
            .Setup(x => x.IsLicenseExistsAsync(50))
            .ReturnsAsync(expected);

        var result = await CreateService().IsLicenseExistsAsync(50);

        Assert.Same(expected, result);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        _queryService.Verify(
            x => x.IsLicenseExistsAsync(50),
            Times.Once);
    }

    [Fact]
    public async Task IsDriverHasLicenseAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<bool>.Success(false);
        _queryService
            .Setup(x => x.IsDriverHasLicenseAsync(60))
            .ReturnsAsync(expected);

        var result = await CreateService().IsDriverHasLicenseAsync(60);

        Assert.Same(expected, result);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        _queryService.Verify(
            x => x.IsDriverHasLicenseAsync(60),
            Times.Once);
    }

    [Fact]
    public async Task IsApplicationHasLicenseAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected = Result<bool>.FromNotFound("application not found");
        _queryService
            .Setup(x => x.IsApplicationHasLicenseAsync(70))
            .ReturnsAsync(expected);

        var result = await CreateService().IsApplicationHasLicenseAsync(70);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.IsApplicationHasLicenseAsync(70),
            Times.Once);
    }

    [Fact]
    public async Task GetDetailsAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected =
            Result<DriverLicenseInfoDto>.FromNotFound("details not found");

        _queryService
            .Setup(x => x.GetDetailsAsync(80))
            .ReturnsAsync(expected);

        var result = await CreateService().GetDetailsAsync(80);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetDetailsAsync(80),
            Times.Once);
    }

    [Fact]
    public async Task GetLicenseDetailsByIdAsync_DelegatesToQueryServiceAndReturnsSameResult()
    {
        var expected =
            Result<DriverLicenseInfoDto>.FromNotFound("license details not found");

        _queryService
            .Setup(x => x.GetLicenseDetailsByIdAsync(90))
            .ReturnsAsync(expected);

        var result =
            await CreateService().GetLicenseDetailsByIdAsync(90);

        Assert.Same(expected, result);

        _queryService.Verify(
            x => x.GetLicenseDetailsByIdAsync(90),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenQueryServiceThrows_PropagatesException()
    {
        _queryService
            .Setup(x => x.GetByIdAsync(42))
            .ThrowsAsync(new InvalidOperationException("query failed"));

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateService().GetByIdAsync(42));

        Assert.Equal("query failed", exception.Message);
    }
}
