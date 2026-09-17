using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.DetainedLicense;
using DVLD.Contracts.License;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;

namespace Presentation.ViewModels;

public partial class DetainLicenseViewModel : ObservableObject
{
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IDetainedLicensesApiClient _detainedLicensesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private string? _licenseIdText;
    [ObservableProperty] private DriverLicenseInfoResponse? _licenseInfo;
    [ObservableProperty] private DetainedLicenseResponse? _detainInfo;
    [ObservableProperty] private decimal _fineFees;
    [ObservableProperty] private bool _isLicenseIssued;

    public DetainLicenseViewModel(
        ILicensesApiClient licensesApiClient,
        IDetainedLicensesApiClient detainedLicensesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _licensesApiClient = licensesApiClient ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _detainedLicensesApiClient = detainedLicensesApiClient ?? throw new ArgumentNullException(nameof(detainedLicensesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!int.TryParse(LicenseIdText, out var licenseId) || licenseId <= 0)
        {
            _userNotifications.ShowWarning(
                "Please enter a valid License ID.",
                "Validation");
            return;
        }

        LicenseInfo = null;
        DetainInfo = null;
        FineFees = 0;
        IsLicenseIssued = false;

        var result = await _licensesApiClient.GetDetailsByIdAsync(licenseId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "License Search");
            return;
        }

        if (result.Value is null)
        {
            _userNotifications.ShowWarning(
                "License information was not found.",
                "License Search");
            return;
        }

        LicenseInfo = result.Value;
        IsLicenseIssued = true;

        var detentionResult =
            await _detainedLicensesApiClient.GetActiveByLicenseIdAsync(
                LicenseInfo.LicenseId);

        if (detentionResult.IsFailure)
        {
            _notifications.ShowFailure(
                detentionResult,
                "Detention Status");
            return;
        }

        DetainInfo = detentionResult.Value;
        FineFees = DetainInfo?.FineFees ?? 0;
    }

    [RelayCommand]
    private async Task IssueAsync()
    {
        if (LicenseInfo is null)
        {
            _userNotifications.ShowWarning(
                "Please search for a license first.",
                "Detain License");
            return;
        }

        var alreadyDetained =
            await _detainedLicensesApiClient.IsLicenseDetainedAsync(
                LicenseInfo.LicenseId);

        if (alreadyDetained.IsFailure)
        {
            _notifications.ShowFailure(
                alreadyDetained,
                "Detain License");
            return;
        }

        if (alreadyDetained.Value)
        {
            _userNotifications.ShowWarning(
                "This license is already detained.",
                "Detain License");
            return;
        }

        if (FineFees < 0)
        {
            _userNotifications.ShowWarning(
                "Fine fees cannot be negative.",
                "Validation");
            return;
        }

        var result = await _detainedLicensesApiClient.DetainAsync(
            new CreateDetainedLicenseRequest
            {
                LicenseId = LicenseInfo.LicenseId,
                FineFees = FineFees
            });

        if (result.IsFailure)
        {
            _notifications.ShowFailure(
                result,
                "Detain License");
            return;
        }

        if (result.Value is null)
        {
            _userNotifications.ShowError(
                "The detained license was not returned by the API.",
                "Detain License");
            return;
        }

        DetainInfo = result.Value;

        _userNotifications.ShowInfo(
            "License detained successfully.",
            "Success");
    }

    [RelayCommand]
    private void ShowLicensesHistory()
    {
        if (LicenseInfo is null)
            return;

        var vm = App.ServiceProvider
            .GetRequiredService<LicenseHistoryViewModel>();

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
}