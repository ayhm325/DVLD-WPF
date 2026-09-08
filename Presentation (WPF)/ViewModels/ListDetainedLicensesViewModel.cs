using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.DetainedLicense;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ListDetainedLicensesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDetainedLicensesApiClient _detainedLicensesApiClient;
    private readonly IPeopleApiClient _peopleApiClient;

    private List<DetainedLicenseResponse> _allDetainedLicenses = new();

    public ObservableCollection<DetainedLicenseResponse> DetainedLicenses { get; }
        = new();

    [ObservableProperty]
    private DetainedLicenseResponse? selectedDetainedLicense;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedFilter = "None";

    [ObservableProperty]
    private string selectedReleaseFilter = "All";

    public ObservableCollection<string> FilterOptions { get; } =
    [
        "None",
        "Detain ID",
        "License ID",
        "National No",
        "Full Name",
        "Released"
    ];

    public ObservableCollection<string> ReleaseFilterOptions { get; } =
    [
        "All",
        "Released",
        "Not Released"
    ];

    public bool IsSearchVisible =>
        SelectedFilter != "None" &&
        SelectedFilter != "Released";

    public bool IsReleaseFilterVisible =>
        SelectedFilter == "Released";

    public ListDetainedLicensesViewModel(
        IDetainedLicensesApiClient detainedLicensesApiClient,
        IServiceProvider serviceProvider,
        IPeopleApiClient peopleApiClient)
    {
        _detainedLicensesApiClient =
            detainedLicensesApiClient
            ?? throw new ArgumentNullException(
                nameof(detainedLicensesApiClient));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(
                nameof(serviceProvider));

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(
                nameof(peopleApiClient));
    }

    // =========================================================
    // LOAD
    // =========================================================

    public async Task LoadAsync()
    {
        var result =
            await _detainedLicensesApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        _allDetainedLicenses =
            result.Value ?? [];

        ApplyFilter();
    }

    // =========================================================
    // PROPERTY CHANGES
    // =========================================================

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        OnPropertyChanged(nameof(IsSearchVisible));
        OnPropertyChanged(nameof(IsReleaseFilterVisible));

        ApplyFilter();
    }

    partial void OnSelectedReleaseFilterChanged(string value)
    {
        ApplyFilter();
    }

    // =========================================================
    // FILTER
    // =========================================================

    private void ApplyFilter()
    {
        IEnumerable<DetainedLicenseResponse> query =
            _allDetainedLicenses;

        if (SelectedFilter == "Released")
        {
            query = SelectedReleaseFilter switch
            {
                "Released" =>
                    query.Where(x => x.IsReleased),

                "Not Released" =>
                    query.Where(x => !x.IsReleased),

                _ =>
                    query
            };
        }
        else if (
            !string.IsNullOrWhiteSpace(SearchText) &&
            SelectedFilter != "None")
        {
            string text =
                SearchText.Trim();

            query = SelectedFilter switch
            {
                "Detain ID" =>
                    query.Where(x =>
                        x.DetainId
                            .ToString()
                            .Contains(text)),

                "License ID" =>
                    query.Where(x =>
                        x.LicenseId
                            .ToString()
                            .Contains(text)),

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

                _ =>
                    query
            };
        }

        DetainedLicenses.Clear();

        foreach (var item in query)
        {
            DetainedLicenses.Add(item);
        }
    }

    // =========================================================
    // REFRESH
    // =========================================================

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    // =========================================================
    // PERSON DETAILS
    // =========================================================

    [RelayCommand]
    private void ShowPersonDetails()
    {
        if (SelectedDetainedLicense is null)
            return;

        var window =
            new PersonDetailsWindow(
                SelectedDetainedLicense.PersonId,
                _peopleApiClient)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();
    }

    // =========================================================
    // LICENSE DETAILS
    // =========================================================

    [RelayCommand]
    private void ShowLicenseDetails()
    {
        if (SelectedDetainedLicense is null)
            return;

        var window =
            new DriverLicenseInfoWin(
                SelectedDetainedLicense.LicenseId)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();
    }

    // =========================================================
    // LICENSE HISTORY
    // =========================================================

    [RelayCommand]
    private async Task ShowPersonLicenseHistory()
    {
        if (SelectedDetainedLicense is null)
            return;

        int personId =
            SelectedDetainedLicense.PersonId;

        var vm =
            _serviceProvider
                .GetRequiredService<LicenseHistoryViewModel>();

        await vm.LoadAsync(personId);

        var window =
            new LicenseHistoryWin(
                vm,
                personId)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();
    }

    // =========================================================
    // RELEASE SELECTED DETAINED LICENSE
    // =========================================================

    [RelayCommand]
    private async Task ReleaseDetainedLicenseAsync()
    {
        if (SelectedDetainedLicense is null)
            return;

        int licenseId =
            SelectedDetainedLicense.LicenseId;

        var detainedResult =
            await _detainedLicensesApiClient
                .GetActiveByLicenseIdAsync(
                    licenseId);

        if (detainedResult.IsFailure ||
            detainedResult.Value is null)
        {
            MessageBox.Show(
                "This license is not currently detained.",
                "Release Detained License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var window =
            _serviceProvider
                .GetRequiredService<ReleaseDetainedLicenseWin>();

        await window.LoadAsync(licenseId);

        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();

        await LoadAsync();
    }

    // =========================================================
    // DETAIN NEW LICENSE
    // =========================================================

    [RelayCommand]
    private async Task Detain()
    {
        var window =
            _serviceProvider
                .GetRequiredService<DetainLicenseWin>();

        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();

        await LoadAsync();
    }

    // =========================================================
    // RELEASE DETAINED LICENSE
    // =========================================================

    [RelayCommand]
    private async Task ReleaseDetain()
    {
        var window =
            _serviceProvider
                .GetRequiredService<ReleaseDetainedLicenseWin>();

        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();

        await LoadAsync();
    }
}