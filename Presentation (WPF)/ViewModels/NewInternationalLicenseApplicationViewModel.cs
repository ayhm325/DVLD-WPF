using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class NewInternationalLicenseApplicationViewModel
    : ObservableObject
{
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;

    [ObservableProperty]
    private DriverLicenseInfoResponse? licenseInfo;

    [ObservableProperty]
    private int localLicenseId;

    [ObservableProperty]
    private string licenseIdText = string.Empty;

    [ObservableProperty]
    private InternationalLicenseResponse? applicationInfo;

    [ObservableProperty]
    private bool isLicenseIssued;

    public NewInternationalLicenseApplicationViewModel(
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        ILicensesApiClient licensesApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient)
    {
        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(
                nameof(peopleApiClient));

        _driversApiClient =
            driversApiClient
            ?? throw new ArgumentNullException(
                nameof(driversApiClient));

        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(
                nameof(licensesApiClient));

        _internationalLicensesApiClient =
            internationalLicensesApiClient
            ?? throw new ArgumentNullException(
                nameof(internationalLicensesApiClient));
    }

    [RelayCommand]
    private async Task Search()
    {
        if (!int.TryParse(
                LicenseIdText,
                out int licenseId))
        {
            MessageBox.Show(
                "Please enter a valid License ID");

            return;
        }

        LocalLicenseId = 0;
        LicenseInfo = null;
        ApplicationInfo = null;
        IsLicenseIssued = false;

        var result =
            await _internationalLicensesApiClient
                .GetLocalLicenseInfoAsync(
                    licenseId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error);

            return;
        }

        if (result.Value is null)
        {
            MessageBox.Show(
                "Local license information was not returned by the API.");

            return;
        }

        LocalLicenseId = licenseId;
        LicenseInfo = result.Value;

        MessageBox.Show(
            "Local License Found Successfully");
    }

    [RelayCommand]
    private async Task Issue()
    {
        if (LicenseInfo == null ||
            LocalLicenseId <= 0)
        {
            MessageBox.Show(
                "Please search for a local license first");

            return;
        }

        var request =
            new IssueInternationalLicenseRequest(
                LocalLicenseId);

        var result =
            await _internationalLicensesApiClient
                .IssueAsync(request);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error);

            return;
        }

        var internationalLicense =
            result.Value;

        if (internationalLicense == null ||
            internationalLicense.InternationalLicenseId <= 0)
        {
            MessageBox.Show(
                "International license was not returned by the API");

            return;
        }

        ApplicationInfo =
            internationalLicense;

        IsLicenseIssued = true;

        MessageBox.Show(
            "International License Issued Successfully");
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
        if (ApplicationInfo == null ||
            !IsLicenseIssued ||
            ApplicationInfo.InternationalLicenseId <= 0)
        {
            return;
        }

        var win =
            new DriverInterNationalLicenseInfoWin(
                ApplicationInfo.InternationalLicenseId,
                _internationalLicensesApiClient);

        win.ShowDialog();
    }
}