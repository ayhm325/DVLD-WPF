using Application.DTOs.ApplicationDTO;
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
    private readonly IApplicationService _applicationService;


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
        ApplicationFees + (Release?.FineFees ?? 0);


    public ReleaseDetainedViewModel(
        ILicenseService licenseService,
        ILicenseQueryService licenseQueryService,
        IDetainedLicenseService detainedLicenseService,
        ICurrentUserService currentUserService,
        IPersonService personService,
        IDriverService driverService,
        IInternationalService internationalService,
        IApplicationTypeService applicationTypeService,
        IApplicationService applicationService)
    {
        _licenseService =
            licenseService
            ?? throw new ArgumentNullException(
                nameof(licenseService));

        _licenseQueryService =
            licenseQueryService
            ?? throw new ArgumentNullException(
                nameof(licenseQueryService));

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
            ?? throw new ArgumentNullException(
                nameof(personService));

        _driverService =
            driverService
            ?? throw new ArgumentNullException(
                nameof(driverService));

        _internationalService =
            internationalService
            ?? throw new ArgumentNullException(
                nameof(internationalService));

        _applicationTypeService =
            applicationTypeService
            ?? throw new ArgumentNullException(
                nameof(applicationTypeService));

        _applicationService =
            applicationService
            ?? throw new ArgumentNullException(
                nameof(applicationService));
    }


    // =========================================================
    // PROPERTY CHANGES
    // =========================================================

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


    // =========================================================
    // LOAD
    // =========================================================

    public async Task LoadAsync(
        int licenseId)
    {
        IsLicenseIdReadOnly = true;

        LicenseIdText =
            licenseId.ToString();

        await SearchAsync();
    }


    // =========================================================
    // SEARCH
    // =========================================================

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (!int.TryParse(
                LicenseIdText,
                out int licenseId))
        {
            return;
        }


        // -----------------------------------------------------
        // Get License
        // -----------------------------------------------------

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


        if (LicenseInfo == null)
        {
            Release = null;
            IsLicenseIssued = false;

            return;
        }


        // -----------------------------------------------------
        // Get Active Detention
        // -----------------------------------------------------

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


        if (releaseResult.Value == null)
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


        // -----------------------------------------------------
        // Get Application Type
        // -----------------------------------------------------

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


        if (applicationType == null)
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


    // =========================================================
    // RELEASE
    // =========================================================

    [RelayCommand]
    private async Task ReleaseLicenseAsync()
    {
        if (Release == null ||
            LicenseInfo == null)
        {
            CustomMessageBox.Show(
                "Please search for a detained license first.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }


        try
        {
            // -------------------------------------------------
            // 1. Create Release Application
            // -------------------------------------------------

            var newApplication =
                new CreateApplicationDto
                {
                    ApplicantPersonID =
                        LicenseInfo.PersonID,

                    ApplicationDate =
                        DateTime.Now,

                    ApplicationTypeID =
                        5,

                    ApplicationStatus =
                        Domain.Enums.AppStatus.Completed,

                    LastStatusDate =
                        DateTime.Now,

                    PaidFees =
                        ApplicationFees

                    
                };


            var applicationResult =
                await _applicationService
                    .AddNewApplicationAsync(
                        newApplication);


            if (applicationResult.IsFailure)
            {
                CustomMessageBox.Show(
                    applicationResult.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }


            int newApplicationId =
                applicationResult.Value;


            if (newApplicationId <= 0)
            {
                CustomMessageBox.Show(
                    "Failed to create the release application.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }


            // -------------------------------------------------
            // 2. Release Detained License
            // -------------------------------------------------

            var releaseDto =
                new ReleaseDetainedLicenseDto
                {
                    DetainID =
                        Release.DetainID,                    

                    ReleaseApplicationID =
                        newApplicationId
                };


            var releaseResult =
                await _detainedLicenseService
                    .ReleaseAsync(
                        releaseDto);


            if (releaseResult.IsFailure)
            {
                CustomMessageBox.Show(
                    releaseResult.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }


            // -------------------------------------------------
            // 3. Refresh UI
            // -------------------------------------------------

            var refreshedResult =
                await _detainedLicenseService
                    .GetByIdAsync(
                        Release.DetainID);


            if (refreshedResult.IsSuccess)
            {
                Release =
                    refreshedResult.Value;
            }


            CustomMessageBox.Show(
                "License released successfully. " +
                "The application has been created.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            string errorMessage =
                ex.Message;


            if (ex.InnerException != null)
            {
                errorMessage +=
                    Environment.NewLine +
                    ex.InnerException.Message;
            }


            CustomMessageBox.Show(
                $"An error occurred while processing the operation:" +
                $"{Environment.NewLine}{errorMessage}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    // =========================================================
    // LICENSE HISTORY
    // =========================================================

    [RelayCommand]
    private void ShowLicensesHistory()
    {
        if (LicenseInfo == null)
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


    // =========================================================
    // LICENSE INFO
    // =========================================================

    [RelayCommand]
    private void ShowLicensesInfo()
    {
        if (LicenseInfo == null)
            return;


        var window =
            new DriverLicenseInfoWin(
                LicenseInfo.LicenseId);


        window.Owner =
            System.Windows.Application.Current.MainWindow;


        window.ShowDialog();
    }
}


// =============================================================
// CUSTOM MESSAGE BOX
// =============================================================

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
