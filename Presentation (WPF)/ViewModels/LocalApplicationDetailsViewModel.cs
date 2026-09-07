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
    private readonly IApplicationsApiClient _applicationsApiClient;
    private readonly ILicensesApiClient _licensesApiClient;

    [ObservableProperty]
    private ApplicationBasicInfoResponse? applicationInfo;

    [ObservableProperty]
    private LocalDrivingLicenseApplicationResponse? ldlAppInfo;

    [ObservableProperty]
    private LicenseResponse? licenseInfo;

    public LocalApplicationDetailsViewModel(
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IApplicationsApiClient applicationsApiClient,
        ILicensesApiClient licensesApiClient)
    {
        _localApplicationsApiClient =
            localApplicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(localApplicationsApiClient));

        _applicationsApiClient =
            applicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(applicationsApiClient));

        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(
                nameof(licensesApiClient));
    }

    public async Task LoadAsync(int localId)
    {
        ApplicationInfo = null;
        LdlAppInfo = null;
        LicenseInfo = null;

        if (localId <= 0)
        {
            MessageBox.Show(
                "Invalid local application ID.",
                "Application Details",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            var localAppResult =
                await _localApplicationsApiClient
                    .GetByIdAsync(localId);

            if (localAppResult.IsFailure)
            {
                MessageBox.Show(
                    localAppResult.Error,
                    "Application Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            LdlAppInfo = localAppResult.Value;

            var appIdResult =
                await _localApplicationsApiClient
                    .GetApplicationIdAsync(localId);

            if (appIdResult.IsFailure)
            {
                MessageBox.Show(
                    appIdResult.Error,
                    "Application Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var applicationId = appIdResult.Value;

            var applicationResult =
                await _applicationsApiClient
                    .GetBasicInfoAsync(applicationId);

            if (applicationResult.IsFailure)
            {
                MessageBox.Show(
                    applicationResult.Error,
                    "Application Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            ApplicationInfo = applicationResult.Value;

            var licensesResult =
                await _licensesApiClient
                    .GetByApplicationIdAsync(applicationId);

            if (licensesResult.IsFailure)
            {
                LicenseInfo = null;
                return;
            }

            var licenses =
                licensesResult.Value
                ?? new List<LicenseResponse>();

            LicenseInfo =
                licenses.FirstOrDefault(x =>
                    x.LicenseClassId ==
                    LdlAppInfo!.LicenseClassId);
        }
        catch (Exception ex)
        {
            ApplicationInfo = null;
            LdlAppInfo = null;
            LicenseInfo = null;

            MessageBox.Show(
                ex.Message,
                "Application Details",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}