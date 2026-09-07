using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Application;
using DVLD.Contracts.LicenseIssuance;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using Presentation.Services.Api;
using System.Windows;

namespace Presentation.ViewModels;

public partial class IssueDrivingLicenseForTheFirstTimeViewModel : ObservableObject
{
    private readonly ILicenseIssuanceApiClient _licenseIssuanceApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly IApplicationsApiClient _applicationsApiClient;

    private readonly int _localAppId;
    private readonly Window _window;

    // App Info
    [ObservableProperty]
    private int drivingLicenseApplicationId;

    [ObservableProperty]
    private string licenseClassName = string.Empty;

    [ObservableProperty]
    private int passedTests;

    [ObservableProperty]
    private int totalTests = 3;

    // Basic Info
    [ObservableProperty]
    private ApplicationBasicInfoResponse? basicApplicationInfo;

    // Form
    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private bool isBusy;

    public IssueDrivingLicenseForTheFirstTimeViewModel(
        int localAppId,
        Window window,
        ILicenseIssuanceApiClient licenseIssuanceApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IApplicationsApiClient applicationsApiClient)
    {
        if (localAppId <= 0)
            throw new ArgumentOutOfRangeException(nameof(localAppId));

        _localAppId = localAppId;
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _licenseIssuanceApiClient =
            licenseIssuanceApiClient ?? throw new ArgumentNullException(nameof(licenseIssuanceApiClient));
        _localApplicationsApiClient =
            localApplicationsApiClient ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));
        _applicationsApiClient =
            applicationsApiClient ?? throw new ArgumentNullException(nameof(applicationsApiClient));

        DrivingLicenseApplicationId = localAppId;

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var localAppResult =
                await _localApplicationsApiClient.GetByIdAsync(_localAppId);

            if (localAppResult.IsFailure)
            {
                MessageBox.Show(
                    localAppResult.Error,
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            LocalDrivingLicenseApplicationResponse? localApp =
                localAppResult.Value;

            if (localApp is null)
            {
                MessageBox.Show(
                    "Local driving license application was not found.",
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            DrivingLicenseApplicationId =
                localApp.LocalDrivingLicenseApplicationId;

            LicenseClassName =
                localApp.LicenseClassName ?? string.Empty;

            PassedTests =
                localApp.PassedTest;

            var applicationIdResult =
                await _localApplicationsApiClient.GetApplicationIdAsync(_localAppId);

            if (applicationIdResult.IsFailure)
            {
                BasicApplicationInfo = null;

                MessageBox.Show(
                    applicationIdResult.Error,
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            int applicationId =
                applicationIdResult.Value;

            var result =
                await _applicationsApiClient.GetBasicInfoAsync(applicationId);

            if (result.IsFailure)
            {
                BasicApplicationInfo = null;

                MessageBox.Show(
                    result.Error,
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            BasicApplicationInfo =
                result.Value;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Loading Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Close()
    {
        _window.Close();
    }

    private bool CanIssue() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanIssue))]
    private async Task Issue()
    {
        try
        {
            IsBusy = true;

            var request =
                new IssueFirstLicenseRequest(
                    _localAppId,
                    Notes);

            var result =
                await _licenseIssuanceApiClient.IssueFirstLicenseAsync(request);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            MessageBox.Show(
                "License issued successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _window.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            IssueCommand.NotifyCanExecuteChanged();
        }
    }
}