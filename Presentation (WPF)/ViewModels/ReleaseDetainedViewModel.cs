using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.DetainedLicense;
using DVLD.Contracts.License;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;

namespace Presentation.ViewModels;

public partial class ReleaseDetainedViewModel : ObservableObject
{
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IDetainedLicensesApiClient _detainedLicensesApiClient;
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private bool _isLicenseIdReadOnly;
    [ObservableProperty] private string? _licenseIdText;
    [ObservableProperty] private DriverLicenseInfoResponse? _licenseInfo;
    [ObservableProperty] private DetainedLicenseResponse? _release;
    [ObservableProperty] private decimal _applicationFees;
    [ObservableProperty] private bool _isLicenseIssued;

    public decimal TotalFees => ApplicationFees + (Release?.FineFees ?? 0);

    public ReleaseDetainedViewModel(
        ILicensesApiClient licensesApiClient,
        IDetainedLicensesApiClient detainedLicensesApiClient,
        IApplicationTypesApiClient applicationTypesApiClient,
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _licensesApiClient = licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _detainedLicensesApiClient = detainedLicensesApiClient
            ?? throw new ArgumentNullException(nameof(detainedLicensesApiClient));
        _applicationTypesApiClient = applicationTypesApiClient
            ?? throw new ArgumentNullException(nameof(applicationTypesApiClient));
        _peopleApiClient = peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _driversApiClient = driversApiClient
            ?? throw new ArgumentNullException(nameof(driversApiClient));
        _internationalLicensesApiClient = internationalLicensesApiClient
            ?? throw new ArgumentNullException(nameof(internationalLicensesApiClient));
        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications
            ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    partial void OnReleaseChanged(DetainedLicenseResponse? value)
    {
        IsLicenseIssued = value is not null;
        OnPropertyChanged(nameof(TotalFees));
    }

    partial void OnApplicationFeesChanged(decimal value) =>
        OnPropertyChanged(nameof(TotalFees));

    public async Task LoadAsync(int licenseId)
    {
        IsLicenseIdReadOnly = true;
        LicenseIdText = licenseId.ToString();
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!int.TryParse(LicenseIdText?.Trim(), out var licenseId) ||
            licenseId <= 0)
            return;

        LicenseInfo = null;
        Release = null;
        ApplicationFees = 0;
        IsLicenseIssued = false;

        var licenseResult =
            await _licensesApiClient.GetDetailsByIdAsync(licenseId);

        if (licenseResult.IsFailure)
        {
            _notifications.ShowFailure(
                licenseResult,
                "Release License");
            return;
        }

        if (licenseResult.Value is null)
        {
            _userNotifications.ShowWarning(
                "License information was not found.",
                "Release License");
            return;
        }

        LicenseInfo = licenseResult.Value;

        var releaseResult =
            await _detainedLicensesApiClient
                .GetActiveByLicenseIdAsync(licenseId);

        if (releaseResult.IsFailure)
        {
            LicenseInfo = null;
            _notifications.ShowFailure(
                releaseResult,
                "Release License");
            return;
        }

        if (releaseResult.Value is null)
        {
            LicenseInfo = null;
            _userNotifications.ShowWarning(
                "This license is not detained.",
                "Release License");
            return;
        }

        Release = releaseResult.Value;

        var applicationTypeResult =
            await _applicationTypesApiClient.GetByIdAsync(5);

        if (applicationTypeResult.IsFailure)
        {
            _notifications.ShowFailure(
                applicationTypeResult,
                "Release License");
            return;
        }

        if (applicationTypeResult.Value is null)
        {
            _userNotifications.ShowError(
                "Application type was not found.",
                "Release License");
            return;
        }

        ApplicationFees =
            applicationTypeResult.Value.ApplicationTypeFees;
    }

    [RelayCommand]
    private async Task ReleaseLicenseAsync()
    {
        if (Release is null || LicenseInfo is null)
        {
            _userNotifications.ShowWarning(
                "Please search for a detained license first.",
                "Release License");
            return;
        }

        try
        {
            var result =
                await _detainedLicensesApiClient.ReleaseAsync(
                    new ReleaseDetainedLicenseRequest
                    {
                        DetainId = Release.DetainId
                    });

            if (result.IsFailure)
            {
                _notifications.ShowFailure(
                    result,
                    "Release License");
                return;
            }

            var refreshedResult =
                await _detainedLicensesApiClient
                    .GetByIdAsync(Release.DetainId);

            if (refreshedResult.IsSuccess &&
                refreshedResult.Value is not null)
            {
                Release = refreshedResult.Value;
            }
            else
            {
                Release = null;
                IsLicenseIssued = false;
            }

            _userNotifications.ShowInfo(
                "License released successfully.",
                "Success");
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Release License");
        }
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
        if (LicenseInfo is null)
            return;

        var window = new DriverLicenseInfoWin(
            LicenseInfo.LicenseId)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}" +
              $"Inner Exception: {ex.InnerException.Message}";
}