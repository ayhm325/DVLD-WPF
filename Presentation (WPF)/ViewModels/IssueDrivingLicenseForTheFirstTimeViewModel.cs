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
    private readonly int _localAppId;
    private readonly Window _window;

    [ObservableProperty]
    private int drivingLicenseApplicationId;

    [ObservableProperty]
    private string licenseClassName = string.Empty;

    [ObservableProperty]
    private int passedTests;

    [ObservableProperty]
    private int totalTests = 3;

    [ObservableProperty]
    private ApplicationBasicInfoResponse? basicApplicationInfo;

    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private bool isBusy;

    public IssueDrivingLicenseForTheFirstTimeViewModel(
        int localAppId,
        Window window,
        ILicenseIssuanceApiClient licenseIssuanceApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient)
    {
        if (localAppId <= 0)
            throw new ArgumentOutOfRangeException(nameof(localAppId));

        _localAppId = localAppId;

        _window = window
            ?? throw new ArgumentNullException(nameof(window));

        _licenseIssuanceApiClient = licenseIssuanceApiClient
            ?? throw new ArgumentNullException(
                nameof(licenseIssuanceApiClient));

        _localApplicationsApiClient = localApplicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(localApplicationsApiClient));

        DrivingLicenseApplicationId = localAppId;

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var localAppResult =
                await _localApplicationsApiClient
                    .GetByIdAsync(_localAppId);

            if (localAppResult.IsFailure ||
                localAppResult.Value is null)
            {
                ShowError(
                    localAppResult.IsFailure
                        ? localAppResult.Error
                        : "Local driving license application was not found.",
                    "Loading Error");

                return;
            }

            var localApp = localAppResult.Value;

            DrivingLicenseApplicationId =
                localApp.LocalDrivingLicenseApplicationId;

            LicenseClassName =
                localApp.LicenseClassName ?? string.Empty;

            PassedTests =
                localApp.PassedTest;

            var applicationResult =
                await _localApplicationsApiClient
                    .GetApplicationBasicInfoAsync(_localAppId);

            if (applicationResult.IsFailure)
            {
                ShowError(
                    applicationResult.Error,
                    "Loading Error");

                return;
            }

            BasicApplicationInfo =
                applicationResult.Value;
        }
        catch (Exception ex)
        {
            ShowError(
                ex.Message,
                "Loading Error");
        }
    }

    [RelayCommand]
    private void Close() =>
        _window.Close();

    private bool CanIssue() =>
        !IsBusy;

    [RelayCommand(CanExecute = nameof(CanIssue))]
    private async Task Issue()
    {
        try
        {
            IsBusy = true;

            var result =
                await _licenseIssuanceApiClient
                    .IssueFirstLicenseAsync(
                        new IssueFirstLicenseRequest(
                            _localAppId,
                            Notes));

            if (result.IsFailure)
            {
                ShowError(
                    result.Error,
                    "Error");

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
            ShowError(
                ex.Message,
                "Error");
        }
        finally
        {
            IsBusy = false;
            IssueCommand.NotifyCanExecuteChanged();
        }
    }

    private static void ShowError(
        string message,
        string title) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}