using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Application;
using DVLD.Contracts.License;
using DVLD.Contracts.LicenseRenewal;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Windows;
using static Azure.Core.HttpHeader;

namespace Presentation.ViewModels;

public partial class RenewLicenseViewModel : ObservableObject
{
    private readonly IApplicationsApiClient _applicationsApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly ILicenseRenewalApiClient _licenseRenewalApiClient;

    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;

    public RenewLicenseViewModel(
        IApplicationsApiClient applicationsApiClient,
        ILicensesApiClient licensesApiClient,
        ILicenseRenewalApiClient licenseRenewalApiClient,
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient)
    {
        _applicationsApiClient =
            applicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(applicationsApiClient));

        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(
                nameof(licensesApiClient));

        _licenseRenewalApiClient =
            licenseRenewalApiClient
            ?? throw new ArgumentNullException(
                nameof(licenseRenewalApiClient));

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(
                nameof(peopleApiClient));

        _driversApiClient =
            driversApiClient
            ?? throw new ArgumentNullException(
                nameof(driversApiClient));

        _internationalLicensesApiClient =
            internationalLicensesApiClient
            ?? throw new ArgumentNullException(
                nameof(internationalLicensesApiClient));
    }

    [ObservableProperty]
    private string licenseIdText = string.Empty;

    [ObservableProperty]
    private DriverLicenseInfoResponse? licenseInfo;

    [ObservableProperty]
    private ApplicationNewLicenseInfo? newLicenseInfo;

    [ObservableProperty]
    private bool isLicenseIssued;

    [ObservableProperty]
    private string? notes;

    [ObservableProperty]
    private int? renewedLicenseId;

    public bool CanSearch =>
        int.TryParse(
            LicenseIdText,
            out _);

    [RelayCommand]
    private async Task Search()
    {
        if (!int.TryParse(
                LicenseIdText,
                out int licenseId))
        {
            MessageBox.Show(
                "Please enter a valid License ID",
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        ClearRenewalData();

        var result =
            await _licensesApiClient
                .GetDetailsByIdAsync(
                    licenseId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (result.Value is null)
        {
            MessageBox.Show(
                "License information was not found.",
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        LicenseInfo = result.Value;

        if (LicenseInfo.ExpirationDate > DateTime.Now)
        {
            MessageBox.Show(
                "This license has not expired yet. " +
                "Renewal is not allowed.",
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            ClearLicenseData();

            return;
        }

        if (!LicenseInfo.IsActive)
        {
            MessageBox.Show(
                "This license is not active and cannot be renewed.",
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            ClearLicenseData();

            return;
        }

        MessageBox.Show(
            "License found successfully",
            "Renew License",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task Issue()
    {
        if (LicenseInfo == null)
        {
            MessageBox.Show(
                "Please search for a license first",
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            var request =
                new RenewLicenseRequest(
                    LicenseInfo.LicenseId,
                    Notes);

            var renewResult =
                await _licenseRenewalApiClient
                    .RenewAsync(request);

            if (renewResult.IsFailure)
            {
                MessageBox.Show(
                    renewResult.Error,
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (renewResult.Value is null ||
                renewResult.Value.LicenseId <= 0)
            {
                MessageBox.Show(
                    "The renewed license ID was not returned.",
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var newLicenseId =
                renewResult.Value.LicenseId;

            RenewedLicenseId =
                newLicenseId;

            var newLicenseResult =
                await _licensesApiClient
                    .GetByIdAsync(
                        newLicenseId);

            if (newLicenseResult.IsFailure)
            {
                MessageBox.Show(
                    newLicenseResult.Error,
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (newLicenseResult.Value is null)
            {
                MessageBox.Show(
                    "The renewed license could not be found.",
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var newLicense =
                newLicenseResult.Value;

            var applicationResult =
                await _applicationsApiClient
                    .GetByIdAsync(
                        newLicense.ApplicationId);

            if (applicationResult.IsFailure)
            {
                MessageBox.Show(
                    applicationResult.Error,
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (applicationResult.Value is null)
            {
                MessageBox.Show(
                    "The renewal application could not be found.",
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var application =
                applicationResult.Value;

            NewLicenseInfo =
                new ApplicationNewLicenseInfo
                {
                    RenewedLicenseApplicationId =
                        application.ApplicationId,

                    RenewedLicenseId =
                        newLicense.LicenseId,

                    OldLicenseId =
                        LicenseInfo.LicenseId,

                    ApplicationDate =
                        application.ApplicationDate,

                    IssueDate =
                        newLicense.IssueDate,

                    ExpirationDate =
                        newLicense.ExpirationDate,

                    ApplicationFees =
                        application.PaidFees,

                    LicenseFees =
                        newLicense.PaidFees,

                    CreatedByUserName =
                        newLicense.CreatedByUserName
                        ?? application.CreatedByUserName,

                    Notes =
                        newLicense.Notes
                };

            IsLicenseIssued = true;

            MessageBox.Show(
                $"License renewed successfully.\n" +
                $"New License ID: {newLicenseId}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ShowLicensesHistory()
    {
        if (LicenseInfo == null)
            return;

        var vm =
            new LicenseHistoryViewModel(
                _peopleApiClient,
                _driversApiClient,
                _licensesApiClient,
                _internationalLicensesApiClient);

        var win =
            new LicenseHistoryWin(
                vm,
                LicenseInfo.PersonId);

        win.ShowDialog();
    }

    [RelayCommand]
    private void ShowLicensesInfo()
    {
        if (!IsLicenseIssued ||
            RenewedLicenseId == null)
        {
            MessageBox.Show(
                "License not issued yet",
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var win =
            new DriverLicenseInfoWin(
                RenewedLicenseId.Value);

        win.ShowDialog();
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

    public decimal TotalFees =>
        ApplicationFees + LicenseFees;

    public string CreatedByUserName { get; init; } =
        string.Empty;

    public string? Notes { get; init; }
}