using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DVLD.Api.BackgroundServices;

public sealed class NoShowBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<NoShowProcessorOptions> _options;
    private readonly ILogger<NoShowBackgroundService> _logger;

    public NoShowBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<NoShowProcessorOptions> options,
        ILogger<NoShowBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NoShow background processor service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = _options.Value;
                if (options.IsEnabled)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<INoShowProcessorService>();
                    var result = await processor.ProcessPendingNoShowsAsync(stoppingToken);

                    if (result.ProcessedCount > 0)
                    {
                        _logger.LogInformation(
                            "NoShow background processor marked {ProcessedCount} appointment(s) as NoShow.",
                            result.ProcessedCount);
                    }

                    var licenseRepo = scope.ServiceProvider.GetRequiredService<ILicenseRepository>();
                    var clock = scope.ServiceProvider.GetRequiredService<IJordanClock>();
                    var expiredCount = await licenseRepo.DeactivateExpiredLicensesAsync(clock.Now.Date);
                    if (expiredCount > 0)
                    {
                        _logger.LogInformation(
                            "Background processor deactivated {ExpiredCount} expired license(s).",
                            expiredCount);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during NoShow background processing loop.");
            }

            var intervalSeconds = Math.Max(1, _options.Value.PollingIntervalSeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("NoShow background processor service stopped.");
    }
}
