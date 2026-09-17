using CommunityToolkit.Mvvm.ComponentModel;
using Presentation.Services.Api;
using Presentation.Services.UI;

namespace Presentation.ViewModels;

public partial class DashboardViewModel(
    IDashboardApiClient dashboardApiClient,
    IApiNotificationService notifications,
    IUserNotificationService userNotifications) : ObservableObject
{
    private readonly IDashboardApiClient _dashboardApiClient =
        dashboardApiClient
        ?? throw new ArgumentNullException(nameof(dashboardApiClient));

    private readonly IApiNotificationService _notifications =
        notifications
        ?? throw new ArgumentNullException(nameof(notifications));

    private readonly IUserNotificationService _userNotifications =
        userNotifications
        ?? throw new ArgumentNullException(nameof(userNotifications));

    [ObservableProperty] private int _totalPeople;
    [ObservableProperty] private int _totalDrivers;
    [ObservableProperty] private int _activeLicenses;
    [ObservableProperty] private int _pendingApplications;
    [ObservableProperty] private int _localDrivingLicenseApplications;
    [ObservableProperty] private int _internationalLicenses;
    [ObservableProperty] private int _detainedLicenses;
    [ObservableProperty] private int _upcomingTests;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _dashboardApiClient.GetStatisticsAsync(cancellationToken);

            if (result.IsFailure)
            {
                ErrorMessage = result.Error;
                _notifications.ShowFailure(result, "Dashboard");
                return;
            }

            if (result.Value is null)
            {
                ErrorMessage = "The API returned an empty response.";
                _userNotifications.ShowWarning(
                    ErrorMessage,
                    "Dashboard");
                return;
            }

            var statistics = result.Value;

            TotalPeople = statistics.TotalPeople;
            TotalDrivers = statistics.TotalDrivers;
            ActiveLicenses = statistics.ActiveLicenses;
            PendingApplications = statistics.PendingApplications;
            LocalDrivingLicenseApplications = statistics.LocalDrivingLicenseApplications;
            InternationalLicenses = statistics.InternationalLicenses;
            DetainedLicenses = statistics.DetainedLicenses;
            UpcomingTests = statistics.UpcomingTests;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.InnerException is null
                ? ex.Message
                : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                  $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";

            _userNotifications.ShowError(
                ErrorMessage,
                "Dashboard");
        }
        finally
        {
            IsLoading = false;
        }
    }
}