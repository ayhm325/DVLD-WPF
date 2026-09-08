using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.DetainedLicense;
using DVLD.Contracts.License;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ReleaseDetainedViewModel : ObservableObject
{
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IDetainedLicensesApiClient _detainedLicensesApiClient;
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;

    private readonly ICurrentUserService _currentUserService;

    private readonly IPersonService _personService;
    private readonly IDriverService _driverService;
    private readonly ILicenseService _licenseService;
    private readonly IInternationalService _internationalService;

    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;

    [ObservableProperty]
    private bool isLicenseIdReadOnly;

    [ObservableProperty]
    private string? licenseIdText;

    [ObservableProperty]
    private DriverLicenseInfoResponse? licenseInfo;

    [ObservableProperty]
    private DetainedLicenseResponse? release;

    [ObservableProperty]
    private decimal applicationFees;

    [ObservableProperty]
    private bool isLicenseIssued;

    public decimal TotalFees =>
        ApplicationFees +
        (Release?.FineFees ?? 0);

    public ReleaseDetainedViewModel(
        ILicensesApiClient licensesApiClient,
        IDetainedLicensesApiClient detainedLicensesApiClient,
        IApplicationTypesApiClient applicationTypesApiClient,
        ICurrentUserService currentUserService,
        IPersonService personService,
        IDriverService driverService,
        ILicenseService licenseService,
        IInternationalService internationalService,
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        IInternationalLicensesApiClient internationalLicensesApiClient)
    {
        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(
                nameof(licensesApiClient));

        _detainedLicensesApiClient =
            detainedLicensesApiClient
            ?? throw new ArgumentNullException(
                nameof(detainedLicensesApiClient));

        _applicationTypesApiClient =
            applicationTypesApiClient
            ?? throw new ArgumentNullException(
                nameof(applicationTypesApiClient));

        _currentUserService =
            currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _personService =
            personService
            ?? throw new ArgumentNullException(
                nameof(personService));

        _driverService =
            driverService
            ?? throw new ArgumentNullException(
                nameof(driverService));

        _licenseService =
            licenseService
            ?? throw new ArgumentNullException(
                nameof(licenseService));

        _internationalService =
            internationalService
            ?? throw new ArgumentNullException(
                nameof(internationalService));

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

    partial void OnReleaseChanged(
        DetainedLicenseResponse? value)
    {
        IsLicenseIssued =
            value != null;

        OnPropertyChanged(
            nameof(TotalFees));
    }

    partial void OnApplicationFeesChanged(
        decimal value)
    {
        OnPropertyChanged(
            nameof(TotalFees));
    }

    public async Task LoadAsync(
        int licenseId)
    {
        IsLicenseIdReadOnly = true;
        LicenseIdText = licenseId.ToString();

        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!int.TryParse(
                LicenseIdText,
                out int licenseId) ||
            licenseId <= 0)
        {
            return;
        }

        var licenseResult =
            await _licensesApiClient
                .GetDetailsByIdAsync(
                    licenseId);

        if (licenseResult.IsFailure)
        {
            LicenseInfo = null;
            Release = null;
            IsLicenseIssued = false;

            CustomMessageBox.Show(
                licenseResult.Error,
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        LicenseInfo =
            licenseResult.Value;

        if (LicenseInfo is null)
        {
            Release = null;
            IsLicenseIssued = false;
            return;
        }

        var releaseResult =
            await _detainedLicensesApiClient
                .GetActiveByLicenseIdAsync(
                    licenseId);

        if (releaseResult.IsFailure)
        {
            Release = null;
            IsLicenseIssued = false;

            CustomMessageBox.Show(
                releaseResult.Error,
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (releaseResult.Value is null)
        {
            Release = null;
            IsLicenseIssued = false;

            CustomMessageBox.Show(
                "This license is not detained.",
                "Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        Release =
            releaseResult.Value;

        IsLicenseIssued = true;

        var applicationTypeResult =
            await _applicationTypesApiClient
                .GetByIdAsync(5);

        if (applicationTypeResult.IsFailure)
        {
            CustomMessageBox.Show(
                applicationTypeResult.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        var applicationType =
            applicationTypeResult.Value;

        if (applicationType is null)
        {
            CustomMessageBox.Show(
                "Application type was not found.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        ApplicationFees =
            applicationType.ApplicationTypeFees;

        OnPropertyChanged(
            nameof(TotalFees));
    }

    [RelayCommand]
    private async Task ReleaseLicenseAsync()
    {
        if (Release is null ||
            LicenseInfo is null)
        {
            CustomMessageBox.Show(
                "Please search for a detained license first.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            CustomMessageBox.Show(
                "You must be logged in first.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            var releaseRequest =
                new ReleaseDetainedLicenseRequest
                {
                    DetainId =
                        Release.DetainId
                };

            var result =
                await _detainedLicensesApiClient
                    .ReleaseAsync(
                        releaseRequest);

            if (result.IsFailure)
            {
                CustomMessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var refreshedResult =
                await _detainedLicensesApiClient
                    .GetByIdAsync(
                        Release.DetainId);

            if (refreshedResult.IsSuccess)
            {
                Release =
                    refreshedResult.Value;
            }
            else
            {
                Release = null;
            }

            CustomMessageBox.Show(
                "License released successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            var errorMessage =
                ex.Message;

            if (ex.InnerException is not null)
            {
                errorMessage +=
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"Inner Exception:{Environment.NewLine}" +
                    ex.InnerException.Message;
            }

            CustomMessageBox.Show(
                errorMessage,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ShowLicensesHistory()
    {
        if (LicenseInfo is null)
            return;

        var vm =
            new LicenseHistoryViewModel(
                _peopleApiClient,
                _driversApiClient,
                _licensesApiClient,
                _internationalLicensesApiClient);

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

public static class CustomMessageBox
{
    public static MessageBoxResult Show(
        string message,
        string title,
        MessageBoxButton button,
        MessageBoxImage icon)
    {
        return MessageBox.Show(
            message,
            title,
            button,
            icon);
    }
}