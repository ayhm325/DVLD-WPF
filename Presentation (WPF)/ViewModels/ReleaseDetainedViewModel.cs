using Application.DTOs.DetainedLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ReleaseDetainedViewModel : ObservableObject
{
    private readonly ILicenseService _licenseService;
    private readonly ILicenseQueryService _licenseQueryService;
    private readonly IDetainedLicenseService _detainedLicenseService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPersonService _personService;
    private readonly IDriverService _driverService;
    private readonly IInternationalService _internationalService;
    private readonly IApplicationTypeService _applicationTypeService;

    [ObservableProperty]
    private bool isLicenseIdReadOnly;

    [ObservableProperty]
    private string? licenseIdText;

    [ObservableProperty]
    private DriverLicenseInfoDto? licenseInfo;

    [ObservableProperty]
    private DetainedLicenseDto? release;

    [ObservableProperty]
    private decimal applicationFees;

    [ObservableProperty]
    private bool isLicenseIssued;

    public decimal TotalFees =>
        ApplicationFees +
        (Release?.FineFees ?? 0);

    public ReleaseDetainedViewModel(
        ILicenseService licenseService,
        ILicenseQueryService licenseQueryService,
        IDetainedLicenseService detainedLicenseService,
        ICurrentUserService currentUserService,
        IPersonService personService,
        IDriverService driverService,
        IInternationalService internationalService,
        IApplicationTypeService applicationTypeService)
    {
        _licenseService =
            licenseService
            ?? throw new ArgumentNullException(nameof(licenseService));

        _licenseQueryService =
            licenseQueryService
            ?? throw new ArgumentNullException(nameof(licenseQueryService));

        _detainedLicenseService =
            detainedLicenseService
            ?? throw new ArgumentNullException(
                nameof(detainedLicenseService));

        _currentUserService =
            currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _personService =
            personService
            ?? throw new ArgumentNullException(nameof(personService));

        _driverService =
            driverService
            ?? throw new ArgumentNullException(nameof(driverService));

        _internationalService =
            internationalService
            ?? throw new ArgumentNullException(
                nameof(internationalService));

        _applicationTypeService =
            applicationTypeService
            ?? throw new ArgumentNullException(
                nameof(applicationTypeService));
    }

    partial void OnReleaseChanged(
        DetainedLicenseDto? value)
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
            await _licenseQueryService
                .GetLicenseDetailsByIdAsync(
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
            await _detainedLicenseService
                .GetActiveDetainByLicenseIdAsync(
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
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(5);

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
            var releaseDto =
                new ReleaseDetainedLicenseDto
                {
                    DetainID =
                        Release.DetainID
                };

            var result =
                await _detainedLicenseService
                    .ReleaseAsync(
                        releaseDto);

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
                await _detainedLicenseService
                    .GetByIdAsync(
                        Release.DetainID);

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
                _personService,
                _driverService,
                _licenseService,
                _internationalService);

        var window =
            new LicenseHistoryWin(
                vm,
                LicenseInfo.PersonID);

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
