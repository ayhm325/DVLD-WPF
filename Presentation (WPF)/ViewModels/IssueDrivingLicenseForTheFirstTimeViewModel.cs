using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Application;
using DVLD.Contracts.LicenseIssuance;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using Presentation.Services.Api;
using Presentation.Services.UI;
using System.Windows;

namespace Presentation.ViewModels;

public partial class IssueDrivingLicenseForTheFirstTimeViewModel : ObservableObject
{
    private readonly ILicenseIssuanceApiClient _licenseIssuanceApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;
    private readonly int _localAppId;
    private readonly Window _window;

    [ObservableProperty] private int _drivingLicenseApplicationId;
    [ObservableProperty] private string _licenseClassName = string.Empty;
    [ObservableProperty] private int _passedTests;
    [ObservableProperty] private int _totalTests = 3;
    [ObservableProperty] private ApplicationBasicInfoResponse? _basicApplicationInfo;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private bool _isBusy;

    public IssueDrivingLicenseForTheFirstTimeViewModel(
        int localAppId,
        Window window,
        ILicenseIssuanceApiClient licenseIssuanceApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        if (localAppId <= 0)
            throw new ArgumentOutOfRangeException(nameof(localAppId));

        _localAppId = localAppId;
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _licenseIssuanceApiClient = licenseIssuanceApiClient ?? throw new ArgumentNullException(nameof(licenseIssuanceApiClient));
        _localApplicationsApiClient = localApplicationsApiClient ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));

        DrivingLicenseApplicationId = localAppId;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var localAppResult = await _localApplicationsApiClient.GetByIdAsync(_localAppId);

            if (localAppResult.IsFailure)
            {
                _notifications.ShowFailure(localAppResult, "Loading Error");
                return;
            }

            if (localAppResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Local driving license application was not found.",
                    "Loading Error");
                return;
            }

            var localApp = localAppResult.Value;

            DrivingLicenseApplicationId = localApp.LocalDrivingLicenseApplicationId;
            LicenseClassName = localApp.LicenseClassName ?? string.Empty;
            PassedTests = localApp.PassedTest;

            var applicationResult =
                await _localApplicationsApiClient.GetApplicationBasicInfoAsync(_localAppId);

            if (applicationResult.IsFailure)
            {
                _notifications.ShowFailure(applicationResult, "Loading Error");
                return;
            }

            if (applicationResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Application information was not found.",
                    "Loading Error");
                return;
            }

            BasicApplicationInfo = applicationResult.Value;
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Loading Error");
        }
    }

    [RelayCommand]
    private void Close() => _window.Close();

    private bool CanIssue() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanIssue))]
    private async Task Issue()
    {
        try
        {
            IsBusy = true;

            var result = await _licenseIssuanceApiClient.IssueFirstLicenseAsync(
                new IssueFirstLicenseRequest(_localAppId, Notes));

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Issue License");
                return;
            }

            _userNotifications.ShowInfo(
                "License issued successfully.",
                "Success");

            _window.Close();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Issue License");
        }
        finally
        {
            IsBusy = false;
            IssueCommand.NotifyCanExecuteChanged();
        }
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}