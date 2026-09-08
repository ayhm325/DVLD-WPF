using DVLD.Contracts.InternationalLicense;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class InternationalViewModel : ObservableObject
{
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;
    private readonly IServiceProvider _serviceProvider;

    private readonly ObservableCollection<InternationalLicenseResponse>
        _allApplications = [];

    public ObservableCollection<InternationalLicenseResponse>
        Applications
    { get; } = [];

    [ObservableProperty]
    private InternationalLicenseResponse? selectedApplication;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedFilter = "Int License ID";

    public ObservableCollection<string> Filters { get; } =
    [
        "Int License ID",
        "Application ID",
        "Driver ID",
        "L.License ID"
    ];

    public InternationalViewModel(
        IInternationalLicensesApiClient internationalLicensesApiClient,
        IServiceProvider serviceProvider)
    {
        _internationalLicensesApiClient =
            internationalLicensesApiClient
            ?? throw new ArgumentNullException(
                nameof(internationalLicensesApiClient));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(
                nameof(serviceProvider));

        _ = LoadApplicationsAsync();
    }

    private async Task LoadApplicationsAsync()
    {
        var result =
            await _internationalLicensesApiClient
                .GetAllAsync();

        _allApplications.Clear();
        Applications.Clear();

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        if (result.Value is null)
            return;

        foreach (var item in result.Value)
        {
            _allApplications.Add(item);
        }

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Applications.Clear();

        IEnumerable<InternationalLicenseResponse> filtered =
            _allApplications;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var filter =
                SearchText.Trim();

            filtered =
                SelectedFilter switch
                {
                    "Int License ID" =>
                        _allApplications.Where(x =>
                            x.InternationalLicenseId
                                .ToString()
                                .Contains(
                                    filter,
                                    StringComparison.OrdinalIgnoreCase)),

                    "Application ID" =>
                        _allApplications.Where(x =>
                            x.ApplicationId
                                .ToString()
                                .Contains(
                                    filter,
                                    StringComparison.OrdinalIgnoreCase)),

                    "Driver ID" =>
                        _allApplications.Where(x =>
                            x.DriverId
                                .ToString()
                                .Contains(
                                    filter,
                                    StringComparison.OrdinalIgnoreCase)),

                    "L.License ID" =>
                        _allApplications.Where(x =>
                            x.IssuedUsingLocalLicenseId
                                .ToString()
                                .Contains(
                                    filter,
                                    StringComparison.OrdinalIgnoreCase)),

                    _ => _allApplications
                };
        }

        foreach (var item in filtered)
        {
            Applications.Add(item);
        }
    }

    [RelayCommand]
    private async Task IssueNew()
    {
        var window =
            ActivatorUtilities.CreateInstance<
                NewInternationalLicenseApplicationWin>(
                _serviceProvider);

        window.ShowDialog();

        await LoadApplicationsAsync();
    }

    [RelayCommand]
    private void ShowPersonDetails()
    {
        if (SelectedApplication is null)
            return;

        var personId =
            SelectedApplication.PersonId;

        var window =
            ActivatorUtilities.CreateInstance<PersonDetailsWindow>(
                _serviceProvider,
                personId);

        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowLicenseDetails()
    {
        if (SelectedApplication is null)
            return;

        var licenseId =
            SelectedApplication.InternationalLicenseId;

        var window =
            ActivatorUtilities.CreateInstance<
                DriverInterNationalLicenseInfoWin>(
                _serviceProvider,
                licenseId);

        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowPersonLicenseHistory()
    {
        if (SelectedApplication is null)
            return;

        var personId =
            SelectedApplication.PersonId;

        var window =
            ActivatorUtilities.CreateInstance<LicenseHistoryWin>(
                _serviceProvider,
                personId);

        window.ShowDialog();
    }
}