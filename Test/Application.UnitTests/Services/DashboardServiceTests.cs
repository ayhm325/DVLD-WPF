using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Moq;

namespace Application.UnitTests.Services;

public sealed class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository> _repository = new();

    private DashboardService CreateService() =>
        new(_repository.Object);

    [Fact]
    public void Constructor_AcceptsRepository()
    {
        var service = CreateService();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetStatisticsAsync_DelegatesToRepositoryAndReturnsSameDto()
    {
        var expected = new DashboardDto();

        _repository.Setup(x => x.GetStatisticsAsync())
            .ReturnsAsync(expected);

        var result = await CreateService().GetStatisticsAsync();

        Assert.Same(expected, result);
        _repository.Verify(x => x.GetStatisticsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repository.Setup(x => x.GetStatisticsAsync())
            .ThrowsAsync(new InvalidOperationException("dashboard failed"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetStatisticsAsync());

        Assert.Equal("dashboard failed", ex.Message);
    }
}
