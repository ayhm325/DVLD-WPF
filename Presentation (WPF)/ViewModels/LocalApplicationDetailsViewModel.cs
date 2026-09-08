using CommunityToolkit.Mvvm.ComponentModel;
using DVLD.Contracts.Application;
using DVLD.Contracts.License;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using Presentation.Services.Api;
using System.Windows;

namespace Presentation.ViewModels;

public partial class LocalApplicationDetailsViewModel : ObservableObject
{
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly ILicensesApiClient _licensesApiClient;

    [ObservableProperty]
    private ApplicationBasicInfoResponse? applicationInfo;

    [ObservableProperty]
    private LocalDrivingLicenseApplicationResponse? ldlAppInfo;

    [ObservableProperty]
    private LicenseResponse? licenseInfo;

    public LocalApplicationDetailsViewModel(
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        ILicensesApiClient licensesApiClient)
    {
        _localApplicationsApiClient = localApplicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(localApplicationsApiClient));

        _licensesApiClient = licensesApiClient
            ?? throw new ArgumentNullException(
                nameof(licensesApiClient));
    }

    public async Task LoadAsync(int localId)
    {
        ResetState();

        if (localId <= 0)
        {
            ShowWarning(
                "Invalid local application ID.",
                "Application Details");

            return;
        }

        try
        {
            var localAppResult =
                await _localApplicationsApiClient
                    .GetByIdAsync(localId);

            if (localAppResult.IsFailure ||
                localAppResult.Value is null)
            {
                ShowWarning(
                    localAppResult.IsFailure
                        ? localAppResult.Error
                        : "Local driving license application was not found.",
                    "Application Details");

                return;
            }

            LdlAppInfo =
                localAppResult.Value;

            var applicationResult =
                await _localApplicationsApiClient
                    .GetApplicationBasicInfoAsync(localId);

            if (applicationResult.IsFailure ||
                applicationResult.Value is null)
            {
                ShowWarning(
                    applicationResult.IsFailure
                        ? applicationResult.Error
                        : "Application information was not found.",
                    "Application Details");

                return;
            }

            ApplicationInfo =
                applicationResult.Value;

            var applicationId =
                ApplicationInfo.ApplicationId;

            var licensesResult =
                await _licensesApiClient
                    .GetByApplicationIdAsync(applicationId);

            if (licensesResult.IsFailure)
                return;

            LicenseInfo =
                licensesResult.Value?
                    .FirstOrDefault(x =>
                        x.LicenseClassId ==
                        LdlAppInfo.LicenseClassId);
        }
        catch (Exception ex)
        {
            ResetState();

            ShowError(
                ex.Message,
                "Application Details");
        }
    }

    private void ResetState()
    {
        ApplicationInfo = null;
        LdlAppInfo = null;
        LicenseInfo = null;
    }

    private static void ShowWarning(
        string message,
        string title) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

    private static void ShowError(
        string message,
        string title) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}