using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.License;
using DVLD.Contracts.LicenseRenewal;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;

namespace Presentation.ViewModels;

public partial class RenewLicenseViewModel : ObservableObject
{
    private readonly IApplicationsApiClient _applicationsApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly ILicenseRenewalApiClient _licenseRenewalApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private string _licenseIdText = string.Empty;
    [ObservableProperty] private DriverLicenseInfoResponse? _licenseInfo;
    [ObservableProperty] private ApplicationNewLicenseInfo? _newLicenseInfo;
    [ObservableProperty] private bool _isLicenseIssued;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int? _renewedLicenseId;

    public bool CanSearch =>
        !IsBusy && int.TryParse(LicenseIdText?.Trim(), out var id) && id > 0;

    public bool CanIssue => !IsBusy && !IsLicenseIssued && LicenseInfo is not null;

    public RenewLicenseViewModel(
        IApplicationsApiClient applicationsApiClient,
        ILicensesApiClient licensesApiClient,
        ILicenseRenewalApiClient licenseRenewalApiClient,
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _applicationsApiClient = applicationsApiClient
            ?? throw new ArgumentNullException(nameof(applicationsApiClient));
        _licensesApiClient = licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _licenseRenewalApiClient = licenseRenewalApiClient
            ?? throw new ArgumentNullException(nameof(licenseRenewalApiClient));
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
        ClearRenewalData();

        var licenseId = int.Parse(LicenseIdText.Trim());
        var result = await _licensesApiClient.GetDetailsByIdAsync(licenseId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Renew License");
            return;
        }

        if (result.Value is null)
        {
            _userNotifications.ShowWarning(
                "License information was not found.",
                "Renew License");
            return;
        }

        LicenseInfo = result.Value;

        if (!LicenseInfo.IsEligibleForRenewal)
        {
            _userNotifications.ShowWarning(
                LicenseInfo.IneligibilityReason ?? "This license is not eligible for renewal.",
                "Renew License");
            ClearLicenseData();
            return;
        }

        _userNotifications.ShowInfo(
            "License found successfully.",
            "Renew License");
    }

    [RelayCommand(CanExecute = nameof(CanIssue))]
    private async Task Issue()
    {
        var currentLicense = LicenseInfo;
        if (!CanIssue || currentLicense is null)
            return;

        try
        {
            IsBusy = true;
            var request = new RenewLicenseRequest(
                currentLicense.LicenseId,
                Notes);

            var renewResult =
                await _licenseRenewalApiClient.RenewAsync(request);

            if (renewResult.IsFailure)
            {
                _notifications.ShowFailure(
                    renewResult,
                    "Renew License");
                return;
            }

            if (renewResult.Value is null ||
                renewResult.Value.LicenseId <= 0)
            {
                _userNotifications.ShowError(
                    "The renewed license ID was not returned.",
                    "Renew License");
                return;
            }

            var newLicenseId = renewResult.Value.LicenseId;
            RenewedLicenseId = newLicenseId;
            IsLicenseIssued = true;

            var newLicenseResult =
                await _licensesApiClient.GetByIdAsync(newLicenseId);

            if (newLicenseResult.IsFailure)
            {
                _userNotifications.ShowWarning(
                    $"The renewal succeeded and created license #{newLicenseId}, but its details could not be refreshed: {newLicenseResult.Error}",
                    "Renewal Completed");
                return;
            }

            if (newLicenseResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    $"The renewal succeeded and created license #{newLicenseId}, but the license details were not returned.",
                    "Renewal Completed");
                return;
            }

            var newLicense = newLicenseResult.Value;

            var applicationResult =
                await _applicationsApiClient.GetByIdAsync(
                    newLicense.ApplicationId);

            if (applicationResult.IsFailure)
            {
                _userNotifications.ShowWarning(
                    $"The renewal succeeded and created license #{newLicenseId}, but application details could not be refreshed: {applicationResult.Error}",
                    "Renewal Completed");
                return;
            }

            if (applicationResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    $"The renewal succeeded and created license #{newLicenseId}, but application details were not returned.",
                    "Renewal Completed");
                return;
            }

            var application = applicationResult.Value;

            NewLicenseInfo = new ApplicationNewLicenseInfo
            {
                RenewedLicenseApplicationId = application.ApplicationId,
                RenewedLicenseId = newLicense.LicenseId,
                OldLicenseId = currentLicense.LicenseId,
                ApplicationDate = application.ApplicationDate,
                IssueDate = newLicense.IssueDate,
                ExpirationDate = newLicense.ExpirationDate,
                ApplicationFees = application.PaidFees,
                LicenseFees = newLicense.PaidFees,
                CreatedByUserName =
                    newLicense.CreatedByUserName ??
                    application.CreatedByUserName,
                Notes = newLicense.Notes
            };

            _userNotifications.ShowInfo(
                $"License renewed successfully.\nNew License ID: {newLicenseId}",
                "Success");
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Renew License");
        }
        finally
        {
            IsBusy = false;
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
        if (!IsLicenseIssued || RenewedLicenseId is null)
        {
            _userNotifications.ShowWarning(
                "License not issued yet.",
                "Renew License");
            return;
        }

        var window = new DriverLicenseInfoWin(
            RenewedLicenseId.Value,
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
        ClearRenewalData();
    }

    private void ClearRenewalData()
    {
        NewLicenseInfo = null;
        Notes = null;
        RenewedLicenseId = null;
        IsLicenseIssued = false;
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}" +
              $"Inner Exception: {ex.InnerException.Message}";
}

public sealed class ApplicationNewLicenseInfo
{
    public int RenewedLicenseApplicationId { get; init; }
    public int RenewedLicenseId { get; init; }
    public DateTime ApplicationDate { get; init; }
    public int OldLicenseId { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime ExpirationDate { get; init; }
    public decimal ApplicationFees { get; init; }
    public decimal LicenseFees { get; init; }
    public decimal TotalFees => ApplicationFees + LicenseFees;
    public string CreatedByUserName { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
