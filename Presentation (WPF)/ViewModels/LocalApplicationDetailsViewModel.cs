using CommunityToolkit.Mvvm.ComponentModel;
using DVLD.Contracts.Application;
using DVLD.Contracts.License;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using Presentation.Services.Api;
using Presentation.Services.UI;

namespace Presentation.ViewModels;

public partial class LocalApplicationDetailsViewModel : ObservableObject
{
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private ApplicationBasicInfoResponse? _applicationInfo;
    [ObservableProperty] private LocalDrivingLicenseApplicationResponse? _ldlAppInfo;
    [ObservableProperty] private LicenseResponse? _licenseInfo;

    public LocalApplicationDetailsViewModel(
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        ILicensesApiClient licensesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _localApplicationsApiClient = localApplicationsApiClient ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));
        _licensesApiClient = licensesApiClient ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    public async Task LoadAsync(int localId)
    {
        ResetState();

        if (localId <= 0)
        {
            _userNotifications.ShowWarning(
                "Invalid local application ID.",
                "Application Details");
            return;
        }

        try
        {
            var localAppResult = await _localApplicationsApiClient.GetByIdAsync(localId);

            if (localAppResult.IsFailure)
            {
                _notifications.ShowFailure(localAppResult, "Application Details");
                return;
            }

            if (localAppResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Local driving license application was not found.",
                    "Application Details");
                return;
            }

            LdlAppInfo = localAppResult.Value;

            var applicationResult =
                await _localApplicationsApiClient.GetApplicationBasicInfoAsync(localId);

            if (applicationResult.IsFailure)
            {
                _notifications.ShowFailure(applicationResult, "Application Details");
                return;
            }

            if (applicationResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Application information was not found.",
                    "Application Details");
                return;
            }

            ApplicationInfo = applicationResult.Value;

            var licensesResult =
                await _licensesApiClient.GetByApplicationIdAsync(
                    ApplicationInfo.ApplicationId);

            if (licensesResult.IsFailure)
            {
                _notifications.ShowFailure(licensesResult, "Application Details");
                return;
            }

            LicenseInfo = licensesResult.Value?
                .FirstOrDefault(x => x.LicenseClassId == LdlAppInfo.LicenseClassId);
        }
        catch (Exception ex)
        {
            ResetState();
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Application Details");
        }
    }

    private void ResetState()
    {
        ApplicationInfo = null;
        LdlAppInfo = null;
        LicenseInfo = null;
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}