using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Application;
using DVLD.Contracts.License;
using DVLD.Contracts.LicenseReplacement;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ReplacementDamagedLicenseViewModel
    : ObservableObject
{
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly ILicenseReplacementApiClient _licenseReplacementApiClient;
    private readonly IApplicationsApiClient _applicationsApiClient;

    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;

    public ReplacementDamagedLicenseViewModel(
        ILicensesApiClient licensesApiClient,
        ILicenseReplacementApiClient licenseReplacementApiClient,
        IApplicationsApiClient applicationsApiClient,
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient)
    {
        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(
                nameof(licensesApiClient));

        _licenseReplacementApiClient =
            licenseReplacementApiClient
            ?? throw new ArgumentNullException(
                nameof(licenseReplacementApiClient));

        _applicationsApiClient =
            applicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(applicationsApiClient));

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
    private ReplacementApplicationInfo? replacementInfo;

    [ObservableProperty]
    private bool isLicenseIssued;

    [ObservableProperty]
    private string replacementReason = "Damaged License";

    [RelayCommand]
    private async Task Search()
    {
        if (!int.TryParse(
                LicenseIdText,
                out int licenseId))
        {
            MessageBox.Show(
                "Please enter a valid License ID",
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        ClearReplacementData();

        var result =
            await _licensesApiClient
                .GetDetailsByIdAsync(licenseId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            ClearLicenseData();

            return;
        }

        if (result.Value is null)
        {
            MessageBox.Show(
                "License information was not found.",
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            ClearLicenseData();

            return;
        }

        LicenseInfo = result.Value;

        if (!LicenseInfo.IsActive)
        {
            MessageBox.Show(
                "This license is not active.",
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            ClearLicenseData();

            return;
        }

        MessageBox.Show(
            "License found successfully",
            "Replacement License",
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
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(ReplacementReason))
        {
            MessageBox.Show(
                "Please select a replacement reason.",
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            var request =
                new ReplaceLicenseRequest(
                    LicenseInfo.LicenseId,
                    ReplacementReason);

            var replaceResult =
                await _licenseReplacementApiClient
                    .ReplaceAsync(request);

            if (replaceResult.IsFailure)
            {
                MessageBox.Show(
                    replaceResult.Error,
                    "Replacement License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (replaceResult.Value is null ||
                replaceResult.Value.LicenseId <= 0)
            {
                MessageBox.Show(
                    "The new license ID was not returned.",
                    "Replacement License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var newLicenseId =
                replaceResult.Value.LicenseId;

            var newLicenseResult =
                await _licensesApiClient
                    .GetByIdAsync(newLicenseId);

            if (newLicenseResult.IsFailure)
            {
                MessageBox.Show(
                    newLicenseResult.Error,
                    "Replacement License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (newLicenseResult.Value is null)
            {
                MessageBox.Show(
                    "The new license could not be found.",
                    "Replacement License",
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
                    "Replacement License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (applicationResult.Value is null)
            {
                MessageBox.Show(
                    "The replacement application could not be found.",
                    "Replacement License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var application =
                applicationResult.Value;

            ReplacementInfo =
                new ReplacementApplicationInfo
                {
                    ReplacementApplicationId =
                        application.ApplicationId,

                    ReplacementLicenseId =
                        newLicense.LicenseId,

                    OldLicenseId =
                        LicenseInfo.LicenseId,

                    ApplicationDate =
                        application.ApplicationDate,

                    ApplicationFees =
                        application.PaidFees,

                    LicenseFees =
                        newLicense.PaidFees,

                    ReplacementReason =
                        ReplacementReason,

                    CreatedByUserName =
                        newLicense.CreatedByUserName
                        ?? application.CreatedByUserName
                };

            IsLicenseIssued = true;

            MessageBox.Show(
                $"License replaced successfully.\n" +
                $"New License ID: {newLicenseId}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SelectLost()
    {
        ReplacementReason =
            "Lost License";

        ClearReplacementData();
    }

    [RelayCommand]
    private void SelectDamaged()
    {
        ReplacementReason =
            "Damaged License";

        ClearReplacementData();
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

        new LicenseHistoryWin(
            vm,
            LicenseInfo.PersonId)
            .ShowDialog();
    }

    [RelayCommand]
    private void ShowLicensesInfo()
    {
        if (!IsLicenseIssued ||
            ReplacementInfo?.ReplacementLicenseId == null)
        {
            MessageBox.Show(
                "License not issued yet",
                "Replacement License",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        new DriverLicenseInfoWin(
            ReplacementInfo
                .ReplacementLicenseId
                .Value)
            .ShowDialog();
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
}

public sealed class ReplacementApplicationInfo
{
    public int? ReplacementApplicationId { get; init; }

    public int OldLicenseId { get; init; }

    public int? ReplacementLicenseId { get; init; }

    public DateTime ApplicationDate { get; init; }

    public decimal ApplicationFees { get; init; }

    public decimal LicenseFees { get; init; }

    public string ReplacementReason { get; init; } =
        string.Empty;

    public string CreatedByUserName { get; init; } =
        string.Empty;
}