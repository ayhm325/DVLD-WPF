using Application.DTOs.DetainedLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class DetainLicenseViewModel : ObservableObject
    {
        private readonly ILicenseService _licenseService;
        private readonly IDetainedLicenseService _detainedLicenseService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;
        private readonly IInternationalService _internationalService;


        [ObservableProperty]
        private string? licenseIdText;

        [ObservableProperty]
        private DriverLicenseInfoDto? licenseInfo;

        [ObservableProperty]
        private DetainedLicenseDto? detainInfo;

        [ObservableProperty]
        private decimal fineFees;

        [ObservableProperty]
        private bool isLicenseIssued;


        public DetainLicenseViewModel(
            ILicenseService licenseService,
            IDetainedLicenseService detainedLicenseService,
            ICurrentUserService currentUserService,
            IPersonService personService,
            IDriverService driverService,
            IInternationalService internationalService)
        {
            _licenseService =
                licenseService
                ?? throw new ArgumentNullException(
                    nameof(licenseService));

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
        }


        // =========================================================
        // SEARCH LICENSE
        // =========================================================

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
                await _licenseService
                    .GetLicenseDetailsByIdAsync(
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


            LicenseInfo = result.Value;


            if (LicenseInfo == null)
            {
                MessageBox.Show(
                    "License information was not found.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            IsLicenseIssued = true;


            // =====================================================
            // Check Existing Active Detention
            // =====================================================

            var detentionResult =
                await _detainedLicenseService
                    .GetActiveDetainByLicenseIdAsync(
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


        // =========================================================
        // DETAIN LICENSE
        // =========================================================

        [RelayCommand]
        private async Task IssueAsync()
        {
            if (LicenseInfo == null)
            {
                MessageBox.Show(
                    "Please search for a license first.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            // =====================================================
            // Check Existing Active Detention
            // =====================================================

            var alreadyDetained =
                await _detainedLicenseService
                    .IsLicenseDetainedAsync(
                        LicenseInfo.LicenseId);


            if (alreadyDetained)
            {
                MessageBox.Show(
                    "This license is already detained.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            // =====================================================
            // Validate Fine
            // =====================================================

            if (FineFees < 0)
            {
                MessageBox.Show(
                    "Fine fees cannot be negative.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            // =====================================================
            // DTO FOR CREATE
            // =====================================================

            var dto =
                new CreateDetainedLicenseDto
                {
                    LicenseID =
                        LicenseInfo.LicenseId,

                    DetainDate =
                        DateTime.Now,

                    FineFees =
                        FineFees,

                    CreatedByUserID =
                        _currentUserService.UserId
                };


            // =====================================================
            // Service
            // =====================================================

            var result =
                await _detainedLicenseService
                    .AddAsync(dto);


            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }


            // =====================================================
            // Update UI With Returned DTO
            // =====================================================

            DetainInfo =
                result.Value;


            MessageBox.Show(
                "License detained successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
}