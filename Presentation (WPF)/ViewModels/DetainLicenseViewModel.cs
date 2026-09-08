using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.DetainedLicense;
using DVLD.Contracts.License;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.ViewModels;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class DetainLicenseViewModel : ObservableObject
{
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IDetainedLicensesApiClient _detainedLicensesApiClient;

    [ObservableProperty]
    private string? licenseIdText;

    [ObservableProperty]
    private DriverLicenseInfoResponse? licenseInfo;

    [ObservableProperty]
    private DetainedLicenseResponse? detainInfo;

    [ObservableProperty]
    private decimal fineFees;

    [ObservableProperty]
    private bool isLicenseIssued;

    public DetainLicenseViewModel(
        ILicensesApiClient licensesApiClient,
        IDetainedLicensesApiClient detainedLicensesApiClient)
    {
        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(
                nameof(licensesApiClient));

        _detainedLicensesApiClient =
            detainedLicensesApiClient
            ?? throw new ArgumentNullException(
                nameof(detainedLicensesApiClient));
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!int.TryParse(
                LicenseIdText,
                out int licenseId) ||
            licenseId <= 0)
        {
            MessageBox.Show(
                "Please enter a valid License ID.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        LicenseInfo = null;
        DetainInfo = null;
        FineFees = 0;
        IsLicenseIssued = false;

        var result =
            await _licensesApiClient
                .GetDetailsByIdAsync(
                    licenseId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "License Not Found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (result.Value is null)
        {
            MessageBox.Show(
                "License information was not found.",
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        LicenseInfo =
            result.Value;

        IsLicenseIssued = true;

        var detentionResult =
            await _detainedLicensesApiClient
                .GetActiveByLicenseIdAsync(
                    LicenseInfo.LicenseId);

        if (detentionResult.IsSuccess)
        {
            DetainInfo =
                detentionResult.Value;

            FineFees =
                DetainInfo?.FineFees ?? 0;
        }
        else
        {
            DetainInfo = null;
            FineFees = 0;
        }
    }

    [RelayCommand]
    private async Task IssueAsync()
    {
        if (LicenseInfo is null)
        {
            MessageBox.Show(
                "Please search for a license first.",
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var alreadyDetained =
            await _detainedLicensesApiClient
                .IsLicenseDetainedAsync(
                    LicenseInfo.LicenseId);

        if (alreadyDetained.IsFailure)
        {
            MessageBox.Show(
                alreadyDetained.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        if (alreadyDetained.Value)
        {
            MessageBox.Show(
                "This license is already detained.",
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (FineFees < 0)
        {
            MessageBox.Show(
                "Fine fees cannot be negative.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var request =
            new CreateDetainedLicenseRequest
            {
                LicenseId =
                    LicenseInfo.LicenseId,

                FineFees =
                    FineFees
            };

        var result =
            await _detainedLicensesApiClient
                .DetainAsync(request);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        DetainInfo =
            result.Value;

        MessageBox.Show(
            "License detained successfully.",
            "Success",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ShowLicensesHistory()
    {
        if (LicenseInfo is null)
            return;

        var vm =
            App.ServiceProvider
                .GetRequiredService<LicenseHistoryViewModel>();

        var window =
            new LicenseHistoryWin(
                vm,
                LicenseInfo.PersonId);

        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();
    }

    [RelayCommand]
    private void ShowLicensesInfo()
    {
        if (LicenseInfo is null)
            return;

        var window =
            new DriverLicenseInfoWin(
                LicenseInfo.LicenseId);

        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();
    }
}