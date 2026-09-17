using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.DetainedLicense;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class ListDetainedLicensesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDetainedLicensesApiClient _detainedLicensesApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;
    private List<DetainedLicenseResponse> _allDetainedLicenses = [];

    public ObservableCollection<DetainedLicenseResponse> DetainedLicenses { get; } = [];

    [ObservableProperty] private DetainedLicenseResponse? _selectedDetainedLicense;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedFilter = "None";
    [ObservableProperty] private string _selectedReleaseFilter = "All";

    public ObservableCollection<string> FilterOptions { get; } =
    [
        "None", "Detain ID", "License ID", "National No", "Full Name", "Released"
    ];

    public ObservableCollection<string> ReleaseFilterOptions { get; } =
    [
        "All", "Released", "Not Released"
    ];

    public bool IsSearchVisible =>
        SelectedFilter != "None" && SelectedFilter != "Released";

    public bool IsReleaseFilterVisible =>
        SelectedFilter == "Released";

    public ListDetainedLicensesViewModel(
        IDetainedLicensesApiClient detainedLicensesApiClient,
        IServiceProvider serviceProvider,
        IPeopleApiClient peopleApiClient,
        ILicensesApiClient licensesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _detainedLicensesApiClient =
            detainedLicensesApiClient
            ?? throw new ArgumentNullException(nameof(detainedLicensesApiClient));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));

        _notifications =
            notifications
            ?? throw new ArgumentNullException(nameof(notifications));

        _userNotifications =
            userNotifications
            ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    public async Task LoadAsync()
    {
        var result = await _detainedLicensesApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            _notifications.ShowFailure(
                result,
                "Load Detained Licenses Failed");
            return;
        }

        _allDetainedLicenses = result.Value ?? [];
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) =>
        ApplyFilter();

    partial void OnSelectedFilterChanged(string value)
    {
        OnPropertyChanged(nameof(IsSearchVisible));
        OnPropertyChanged(nameof(IsReleaseFilterVisible));
        ApplyFilter();
    }

    partial void OnSelectedReleaseFilterChanged(string value) =>
        ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<DetainedLicenseResponse> query = _allDetainedLicenses;

        if (SelectedFilter == "Released")
        {
            query = SelectedReleaseFilter switch
            {
                "Released" => query.Where(x => x.IsReleased),
                "Not Released" => query.Where(x => !x.IsReleased),
                _ => query
            };
        }
        else if (!string.IsNullOrWhiteSpace(SearchText) &&
                 SelectedFilter != "None")
        {
            var text = SearchText.Trim();

            query = SelectedFilter switch
            {
                "Detain ID" =>
                    query.Where(x =>
                        x.DetainId.ToString().Contains(text)),

                "License ID" =>
                    query.Where(x =>
                        x.LicenseId.ToString().Contains(text)),

                "National No" =>
                    query.Where(x =>
                        !string.IsNullOrWhiteSpace(x.NationalNo) &&
                        x.NationalNo.Contains(
                            text,
                            StringComparison.OrdinalIgnoreCase)),

                "Full Name" =>
                    query.Where(x =>
                        !string.IsNullOrWhiteSpace(x.FullName) &&
                        x.FullName.Contains(
                            text,
                            StringComparison.OrdinalIgnoreCase)),

                _ => query
            };
        }

        DetainedLicenses.Clear();

        foreach (var item in query)
            DetainedLicenses.Add(item);
    }

    [RelayCommand]
    private Task RefreshAsync() =>
        LoadAsync();

    [RelayCommand]
    private void ShowPersonDetails()
    {
        if (SelectedDetainedLicense is null)
            return;

        var window = new PersonDetailsWindow(
            SelectedDetainedLicense.PersonId,
            _peopleApiClient,
            _notifications)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowLicenseDetails()
    {
        if (SelectedDetainedLicense is null)
            return;

        var window = new DriverLicenseInfoWin(
            SelectedDetainedLicense.LicenseId,
            _licensesApiClient,
            _notifications)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private async Task ShowPersonLicenseHistory()
    {
        if (SelectedDetainedLicense is null)
            return;

        var personId = SelectedDetainedLicense.PersonId;
        var vm = _serviceProvider
            .GetRequiredService<LicenseHistoryViewModel>();

        await vm.LoadAsync(personId);

        var window = new LicenseHistoryWin(vm, personId)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private async Task ReleaseDetainedLicenseAsync()
    {
        if (SelectedDetainedLicense is null)
            return;

        var licenseId = SelectedDetainedLicense.LicenseId;

        var detainedResult =
            await _detainedLicensesApiClient
                .GetActiveByLicenseIdAsync(licenseId);

        if (detainedResult.IsFailure)
        {
            _notifications.ShowFailure(
                detainedResult,
                "Release Detained License");
            return;
        }

        if (detainedResult.Value is null)
        {
            _userNotifications.ShowWarning(
                "This license is not currently detained.",
                "Release Detained License");
            return;
        }

        var window = _serviceProvider
            .GetRequiredService<ReleaseDetainedLicenseWin>();

        await window.LoadAsync(licenseId);

        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();

        await LoadAsync();
    }

    [RelayCommand]
    private async Task Detain()
    {
        var window = _serviceProvider
            .GetRequiredService<DetainLicenseWin>();

        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();

        await LoadAsync();
    }

    [RelayCommand]
    private async Task ReleaseDetain()
    {
        var window = _serviceProvider
            .GetRequiredService<ReleaseDetainedLicenseWin>();

        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();

        await LoadAsync();
    }
}