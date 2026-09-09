using CommunityToolkit.Mvvm.ComponentModel;
using Presentation.Services.Api;

namespace Presentation.ViewModels;

public partial class DashboardViewModel(
    IDashboardApiClient dashboardApiClient) : ObservableObject
{
    private readonly IDashboardApiClient _dashboardApiClient =
        dashboardApiClient
        ?? throw new ArgumentNullException(nameof(dashboardApiClient));

    [ObservableProperty]
    private int totalPeople;

    [ObservableProperty]
    private int totalDrivers;

    [ObservableProperty]
    private int activeLicenses;

    [ObservableProperty]
    private int pendingApplications;

    [ObservableProperty]
    private int localDrivingLicenseApplications;

    [ObservableProperty]
    private int internationalLicenses;

    [ObservableProperty]
    private int detainedLicenses;

    [ObservableProperty]
    private int upcomingTests;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result =
                await _dashboardApiClient.GetStatisticsAsync(
                    cancellationToken);

            if (result.IsFailure)
            {
                ErrorMessage = result.Error;
                return;
            }

            var statistics = result.Value;

            if (statistics is null)
            {
                ErrorMessage =
                    "The API returned an empty response.";

                return;
            }

            TotalPeople =
                statistics.TotalPeople;

            TotalDrivers =
                statistics.TotalDrivers;

            ActiveLicenses =
                statistics.ActiveLicenses;

            PendingApplications =
                statistics.PendingApplications;

            LocalDrivingLicenseApplications =
                statistics.LocalDrivingLicenseApplications;

            InternationalLicenses =
                statistics.InternationalLicenses;

            DetainedLicenses =
                statistics.DetainedLicenses;

            UpcomingTests =
                statistics.UpcomingTests;
        }
        finally
        {
            IsLoading = false;
        }
    }
}