using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.License;
using DVLD.Contracts.LicenseReplacement;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;

namespace Presentation.ViewModels;

public partial class ReplacementDamagedLicenseViewModel : ObservableObject
{
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly ILicenseReplacementApiClient _licenseReplacementApiClient;
    private readonly IApplicationsApiClient _applicationsApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private string _licenseIdText = string.Empty;
    [ObservableProperty] private DriverLicenseInfoResponse? _licenseInfo;
    [ObservableProperty] private ReplacementApplicationInfo? _replacementInfo;
    [ObservableProperty] private bool _isLicenseIssued;
    [ObservableProperty] private string _replacementReason = "Damaged License";
    [ObservableProperty] private bool _isBusy;

    public bool CanSearch => !IsBusy &&
        int.TryParse(LicenseIdText?.Trim(), out var id) && id > 0;
    public bool CanIssue => !IsBusy && !IsLicenseIssued && LicenseInfo is not null;

    partial void OnLicenseIdTextChanged(string value)
    {
        OnPropertyChanged(nameof(CanSearch));
        SearchCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSearch));
        OnPropertyChanged(nameof(CanIssue));
        SearchCommand.NotifyCanExecuteChanged();
        IssueCommand.NotifyCanExecuteChanged();
    }

    partial void OnLicenseInfoChanged(DriverLicenseInfoResponse? value)
    {
        OnPropertyChanged(nameof(CanIssue));
        IssueCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLicenseIssuedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanIssue));
        IssueCommand.NotifyCanExecuteChanged();
    }

    public ReplacementDamagedLicenseViewModel(
        ILicensesApiClient licensesApiClient,
        ILicenseReplacementApiClient licenseReplacementApiClient,
        IApplicationsApiClient applicationsApiClient,
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _licensesApiClient = licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _licenseReplacementApiClient = licenseReplacementApiClient
            ?? throw new ArgumentNullException(nameof(licenseReplacementApiClient));
        _applicationsApiClient = applicationsApiClient
            ?? throw new ArgumentNullException(nameof(applicationsApiClient));
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

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task Search()
    {
        if (!CanSearch)
            return;

        IsBusy = true;
        try
        {
            await SearchCoreAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SearchCoreAsync()
    {
        var licenseId = int.Parse(LicenseIdText.Trim());

        ClearReplacementData();

        var result = await _licensesApiClient.GetDetailsByIdAsync(licenseId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Replacement License");
            ClearLicenseData();
            return;
        }

        if (result.Value is null)
        {
            _userNotifications.ShowWarning(
                "License information was not found.",
                "Replacement License");
            ClearLicenseData();
            return;
        }

        LicenseInfo = result.Value;

        if (!LicenseInfo.IsActive)
        {
            _userNotifications.ShowWarning(
                "This license is not active.",
                "Replacement License");
            ClearLicenseData();
            return;
        }

        _userNotifications.ShowInfo(
            "License found successfully.",
            "Replacement License");
    }

    [RelayCommand(CanExecute = nameof(CanIssue))]
    private async Task Issue()
    {
        var currentLicense = LicenseInfo;
        if (!CanIssue || currentLicense is null)
            return;

        if (string.IsNullOrWhiteSpace(ReplacementReason))
        {
            _userNotifications.ShowWarning(
                "Please select a replacement reason.",
                "Replacement License");
            return;
        }

        try
        {
            IsBusy = true;
            var request = new ReplaceLicenseRequest(
                currentLicense.LicenseId,
                ReplacementReason);

            var replaceResult =
                await _licenseReplacementApiClient.ReplaceAsync(request);

            if (replaceResult.IsFailure)
            {
                _notifications.ShowFailure(
                    replaceResult,
                    "Replacement License");
                return;
            }

            if (replaceResult.Value is null ||
                replaceResult.Value.LicenseId <= 0)
            {
                _userNotifications.ShowError(
                    "The new license ID was not returned.",
                    "Replacement License");
                return;
            }

            var newLicenseId = replaceResult.Value.LicenseId;
            ReplacementInfo = new ReplacementApplicationInfo
            {
                OldLicenseId = currentLicense.LicenseId,
                ReplacementLicenseId = newLicenseId,
                ReplacementReason = ReplacementReason
            };
            IsLicenseIssued = true;

            var newLicenseResult =
                await _licensesApiClient.GetByIdAsync(newLicenseId);

            if (newLicenseResult.IsFailure)
            {
                _userNotifications.ShowWarning(
                    $"The replacement succeeded and created license #{newLicenseId}, but its details could not be refreshed: {newLicenseResult.Error}",
                    "Replacement Completed");
                return;
            }

            if (newLicenseResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    $"The replacement succeeded and created license #{newLicenseId}, but the license details were not returned.",
                    "Replacement Completed");
                return;
            }

            var newLicense = newLicenseResult.Value;

            var applicationResult =
                await _applicationsApiClient.GetByIdAsync(
                    newLicense.ApplicationId);

            if (applicationResult.IsFailure)
            {
                _userNotifications.ShowWarning(
                    $"The replacement succeeded and created license #{newLicenseId}, but application details could not be refreshed: {applicationResult.Error}",
                    "Replacement Completed");
                return;
            }

            if (applicationResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    $"The replacement succeeded and created license #{newLicenseId}, but application details were not returned.",
                    "Replacement Completed");
                return;
            }

            var application = applicationResult.Value;

            ReplacementInfo = new ReplacementApplicationInfo
            {
                ReplacementApplicationId = application.ApplicationId,
                ReplacementLicenseId = newLicense.LicenseId,
                OldLicenseId = currentLicense.LicenseId,
                ApplicationDate = application.ApplicationDate,
                ApplicationFees = application.PaidFees,
                LicenseFees = newLicense.PaidFees,
                ReplacementReason = ReplacementReason,
                CreatedByUserName =
                    newLicense.CreatedByUserName ??
                    application.CreatedByUserName
            };

            _userNotifications.ShowInfo(
                $"License replaced successfully.\nNew License ID: {newLicenseId}",
                "Success");
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Replacement License");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectLost()
    {
        ReplacementReason = "Lost License";
        ClearReplacementData();
    }

    [RelayCommand]
    private void SelectDamaged()
    {
        ReplacementReason = "Damaged License";
        ClearReplacementData();
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
            ReplacementInfo?.ReplacementLicenseId is null)
        {
            _userNotifications.ShowWarning(
                "License not issued yet.",
                "Replacement License");
            return;
        }

        var window = new DriverLicenseInfoWin(
            ReplacementInfo.ReplacementLicenseId.Value,
            _licensesApiClient,
            _notifications)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    private void ClearLicenseData()
    {
        LicenseInfo = null;
        ClearReplacementData();
    }

    private void ClearReplacementData()
    {
        ReplacementInfo = null;
        IsLicenseIssued = false;
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}" +
              $"Inner Exception: {ex.InnerException.Message}";
}

public sealed class ReplacementApplicationInfo
{
    public int? ReplacementApplicationId { get; init; }
    public int OldLicenseId { get; init; }
    public int? ReplacementLicenseId { get; init; }
    public DateTime ApplicationDate { get; init; }
    public decimal ApplicationFees { get; init; }
    public decimal LicenseFees { get; init; }
    public string ReplacementReason { get; init; } = string.Empty;
    public string CreatedByUserName { get; init; } = string.Empty;
}
