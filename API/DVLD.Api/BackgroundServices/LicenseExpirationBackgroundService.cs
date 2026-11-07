using Application.Interfaces;
using Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DVLD.Api.BackgroundServices;

public sealed class LicenseExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<LicenseExpirationOptions> _options;
    private readonly ILogger<LicenseExpirationBackgroundService> _logger;

    public LicenseExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<LicenseExpirationOptions> options,
        ILogger<LicenseExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("License expiration background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = _options.Value;
                if (options.IsEnabled)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var licenseRepo = scope.ServiceProvider.GetRequiredService<ILicenseRepository>();
                    var clock = scope.ServiceProvider.GetRequiredService<IJordanClock>();

                    var expiredCount = await licenseRepo.DeactivateExpiredLicensesAsync(clock.Today);
                    if (expiredCount > 0)
                    {
                        _logger.LogInformation(
                            "License expiration background service deactivated {ExpiredCount} expired license(s) for Jordan date {JordanDate}.",
                            expiredCount,
                            clock.Today);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during license expiration background processing.");
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

        _logger.LogInformation("License expiration background service stopped.");
    }
}
