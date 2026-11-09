using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.UnitTests.Common;
using Moq;

namespace Application.UnitTests.Services;

public sealed class DashboardServiceTests
{
    private readonly Mock<IDashboardRepository> _repository = new();
    private readonly FakeJordanClock _clock = new();

    private DashboardService CreateService() =>
        new(_repository.Object, _clock);

    [Fact]
    public void Constructor_AcceptsRepositoryAndClock()
    {
        var service = CreateService();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetStatisticsAsync_DelegatesToRepositoryWithJordanTodayAndReturnsSameDto()
    {
        var expected = new DashboardDto();

        _repository.Setup(x => x.GetStatisticsAsync(_clock.Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateService().GetStatisticsAsync();

        Assert.Same(expected, result);
        _repository.Verify(x => x.GetStatisticsAsync(_clock.Today, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatisticsAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repository.Setup(x => x.GetStatisticsAsync(_clock.Today, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("dashboard failed"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetStatisticsAsync());

        Assert.Equal("dashboard failed", ex.Message);
    }

    [Fact]
    public async Task GetStatisticsAsync_PassesCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expected = new DashboardDto();

        _repository.Setup(x => x.GetStatisticsAsync(_clock.Today, token))
            .ReturnsAsync(expected);

        var result = await CreateService().GetStatisticsAsync(token);

        Assert.Same(expected, result);
        _repository.Verify(x => x.GetStatisticsAsync(_clock.Today, token), Times.Once);
    }
}
