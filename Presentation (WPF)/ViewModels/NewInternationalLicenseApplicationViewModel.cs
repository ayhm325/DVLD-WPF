using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;

namespace Presentation.ViewModels;

public partial class NewInternationalLicenseApplicationViewModel : ObservableObject
{
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private DriverLicenseInfoResponse? _licenseInfo;
    [ObservableProperty] private int _localLicenseId;
    [ObservableProperty] private string _licenseIdText = string.Empty;
    [ObservableProperty] private InternationalLicenseResponse? _applicationInfo;
    [ObservableProperty] private bool _isLicenseIssued;

    public NewInternationalLicenseApplicationViewModel(
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        ILicensesApiClient licensesApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _peopleApiClient = peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _driversApiClient = driversApiClient
            ?? throw new ArgumentNullException(nameof(driversApiClient));
        _licensesApiClient = licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _internationalLicensesApiClient = internationalLicensesApiClient
            ?? throw new ArgumentNullException(nameof(internationalLicensesApiClient));
        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications
            ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    [RelayCommand]
    private async Task Search()
    {
        if (!int.TryParse(LicenseIdText.Trim(), out var licenseId) ||
            licenseId <= 0)
        {
            _userNotifications.ShowWarning(
                "Please enter a valid License ID.",
                "International License");
            return;
        }

        LocalLicenseId = 0;
        LicenseInfo = null;
        ApplicationInfo = null;
        IsLicenseIssued = false;

        var result = await _internationalLicensesApiClient
            .GetLocalLicenseInfoAsync(licenseId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(
                result,
                "International License");
            return;
        }

        if (result.Value is null)
        {
            _userNotifications.ShowWarning(
                "Local license information was not returned by the API.",
                "International License");
            return;
        }

        LocalLicenseId = licenseId;
        LicenseInfo = result.Value;

        _userNotifications.ShowInfo(
            "Local License found successfully.",
            "International License");
    }

    [RelayCommand]
    private async Task Issue()
    {
        if (LicenseInfo is null || LocalLicenseId <= 0)
        {
            _userNotifications.ShowWarning(
                "Please search for a local license first.",
                "International License");
            return;
        }

        var result = await _internationalLicensesApiClient.IssueAsync(
            new IssueInternationalLicenseRequest(LocalLicenseId));

        if (result.IsFailure)
        {
            _notifications.ShowFailure(
                result,
                "International License");
            return;
        }

        if (result.Value is null ||
            result.Value.InternationalLicenseId <= 0)
        {
            _userNotifications.ShowError(
                "International license was not returned by the API.",
                "International License");
            return;
        }

        ApplicationInfo = result.Value;
        IsLicenseIssued = true;

        _userNotifications.ShowInfo(
            "International License issued successfully.",
            "Success");
    }

    [RelayCommand]
    private void ShowLicensesHistory()
    {
        if (LicenseInfo is null)
            return;

        var vm = new LicenseHistoryViewModel(
            _peopleApiClient,
            _driversApiClient,
            _licensesApiClient,
            _internationalLicensesApiClient,
            _userNotifications,
            _notifications);

        var window = new LicenseHistoryWin(
            vm,
            LicenseInfo.PersonId)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowLicensesInfo()
    {
        if (!IsLicenseIssued ||
            ApplicationInfo?.InternationalLicenseId is not > 0)
            return;

        var window = new DriverInterNationalLicenseInfoWin(
            ApplicationInfo.InternationalLicenseId,
            _internationalLicensesApiClient)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }
}