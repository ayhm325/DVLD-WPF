using Application.Interfaces;
using Application.Options;
using DVLD.Api.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace API.IntegrationTests.BackgroundServices;

public sealed class LicenseExpirationBackgroundServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly Mock<IServiceScope> _scope = new();
    private readonly Mock<IServiceProvider> _serviceProvider = new();
    private readonly Mock<ILicenseRepository> _licenseRepository = new();
    private readonly Mock<IJordanClock> _clock = new();
    private readonly Mock<ILogger<LicenseExpirationBackgroundService>> _logger = new();

    public LicenseExpirationBackgroundServiceTests()
    {
        _clock.SetupGet(x => x.Today).Returns(new DateOnly(2026, 11, 4));

        _serviceProvider
            .Setup(sp => sp.GetService(typeof(ILicenseRepository)))
            .Returns(_licenseRepository.Object);
        _serviceProvider
            .Setup(sp => sp.GetService(typeof(IJordanClock)))
            .Returns(_clock.Object);

        _scope.SetupGet(s => s.ServiceProvider).Returns(_serviceProvider.Object);
        _scopeFactory.Setup(f => f.CreateScope()).Returns(_scope.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_DeactivatesExpiredLicensesIndependentlyOfNoShowProcessorOptions()
    {
        // Arrange
        _licenseRepository
            .Setup(r => r.DeactivateExpiredLicensesAsync(new DateOnly(2026, 11, 4)))
            .ReturnsAsync(3);

        var options = Options.Create(new LicenseExpirationOptions
        {
            IsEnabled = true,
            PollingIntervalSeconds = 1
        });

        var service = new LicenseExpirationBackgroundService(
            _scopeFactory.Object,
            options,
            _logger.Object);

        using var cts = new CancellationTokenSource();

        // Act: Start background worker, let it tick once, then cancel
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert: Verification that deactivation ran independently of NoShowProcessorOptions
        _licenseRepository.Verify(
            r => r.DeactivateExpiredLicensesAsync(new DateOnly(2026, 11, 4)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotDeactivateLicenses()
    {
        // Arrange
        var options = Options.Create(new LicenseExpirationOptions
        {
            IsEnabled = false, // Disabled
            PollingIntervalSeconds = 1
        });

        var service = new LicenseExpirationBackgroundService(
            _scopeFactory.Object,
            options,
            _logger.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert: Never called when IsEnabled = false
        _licenseRepository.Verify(
            r => r.DeactivateExpiredLicensesAsync(It.IsAny<DateOnly>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrowsException_CatchesLogsAndContinues()
    {
        // Arrange
        _licenseRepository
            .Setup(r => r.DeactivateExpiredLicensesAsync(It.IsAny<DateOnly>()))
            .ThrowsAsync(new InvalidOperationException("Simulated database connection failure"));

        var options = Options.Create(new LicenseExpirationOptions
        {
            IsEnabled = true,
            PollingIntervalSeconds = 1
        });

        var service = new LicenseExpirationBackgroundService(
            _scopeFactory.Object,
            options,
            _logger.Object);

        using var cts = new CancellationTokenSource();

        // Act & Assert: Should not throw out of the service loop
        var task = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await task;

        // Assert: Logger was called with Error level
        _logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
