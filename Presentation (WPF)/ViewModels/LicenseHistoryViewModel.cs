using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Driver;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using DVLD.Contracts.Person;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class LicenseHistoryViewModel(
    IPeopleApiClient peopleApiClient,
    IDriversApiClient driversApiClient,
    ILicensesApiClient licensesApiClient,
    IInternationalLicensesApiClient internationalLicensesApiClient,
    IUserNotificationService userNotifications,
    IApiNotificationService notifications) : ObservableObject
{
    private readonly IPeopleApiClient _peopleApiClient = peopleApiClient;
    private readonly IDriversApiClient _driversApiClient = driversApiClient;
    private readonly ILicensesApiClient _licensesApiClient = licensesApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient = internationalLicensesApiClient;
    private readonly IUserNotificationService _userNotifications = userNotifications;
    private readonly IApiNotificationService _notifications = notifications;

    [ObservableProperty] private PersonResponse? _person;
    [ObservableProperty] private LicenseResponse? _selectedLocalLicense;
    [ObservableProperty] private InternationalLicenseListResponse? _selectedInternationalLicense;
    [ObservableProperty] private ObservableCollection<LicenseResponse> _localLicenses = [];
    [ObservableProperty] private ObservableCollection<InternationalLicenseListResponse> _internationalLicenses = [];

    public async Task LoadAsync(int personId)
    {
        ResetState();

        if (personId <= 0)
        {
            _userNotifications.ShowWarning(
                "Invalid person ID.",
                "License History");
            return;
        }

        var personResult = await _peopleApiClient.GetByIdAsync(personId);

        if (personResult.IsFailure)
        {
            _notifications.ShowFailure(personResult, "License History");
            return;
        }

        if (personResult.Value is null)
        {
            _userNotifications.ShowWarning(
                "Person information was not found.",
                "License History");
            return;
        }

        Person = personResult.Value;

        var driverResult = await _driversApiClient.GetByPersonIdAsync(personId);

        if (driverResult.IsFailure)
        {
            _notifications.ShowFailure(driverResult, "License History");
            return;
        }

        if (driverResult.Value is null)
        {
            _userNotifications.ShowWarning(
                "Driver information was not found.",
                "License History");
            return;
        }

        var driverId = driverResult.Value.DriverId;

        var licensesResult =
            await _licensesApiClient.GetByDriverIdAsync(driverId);

        if (licensesResult.IsFailure)
        {
            _notifications.ShowFailure(
                licensesResult,
                "Local Licenses");
        }
        else if (licensesResult.Value is not null)
        {
            LocalLicenses = new(licensesResult.Value);
        }

        var internationalResult =
            await _internationalLicensesApiClient.GetByDriverIdAsync(driverId);

        if (internationalResult.IsFailure)
        {
            _notifications.ShowFailure(
                internationalResult,
                "International Licenses");
        }
        else if (internationalResult.Value is not null)
        {
            InternationalLicenses = new(internationalResult.Value);
        }
    }

    [RelayCommand]
    private void ShowLicense()
    {
        if (SelectedLocalLicense is null)
        {
            _userNotifications.ShowWarning(
                "Please select a license first.",
                "License History");
            return;
        }

        var window = new DriverLicenseInfoWin(
            SelectedLocalLicense.LicenseId,
            _licensesApiClient,
            _notifications)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowInternationalLicense()
    {
        if (SelectedInternationalLicense is null)
        {
            _userNotifications.ShowWarning(
                "Please select an international license first.",
                "License History");
            return;
        }

        _userNotifications.ShowInfo(
            "International license details are not available yet.",
            "License History");
    }

    private void ResetState()
    {
        Person = null;
        SelectedLocalLicense = null;
        SelectedInternationalLicense = null;
        LocalLicenses.Clear();
        InternationalLicenses.Clear();
    }
}