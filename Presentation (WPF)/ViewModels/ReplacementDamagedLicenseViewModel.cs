using Application.DTOs.ApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class ReplacementDamagedLicenseViewModel
        : ObservableObject
    {
        private readonly ILicenseService _licenseService;
        private readonly ILicenseReplacementService _licenseReplacementService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;
        private readonly IInternationalService _internationalService;

        public ReplacementDamagedLicenseViewModel(
            ILicenseService licenseService,
            ILicenseReplacementService licenseReplacementService,
            IApplicationTypeService applicationTypeService,
            ICurrentUserService currentUserService,
            IPersonService personService,
            IDriverService driverService,
            IInternationalService internationalService)
        {
            _licenseService =
                licenseService
                ?? throw new ArgumentNullException(
                    nameof(licenseService));

            _licenseReplacementService =
                licenseReplacementService
                ?? throw new ArgumentNullException(
                    nameof(licenseReplacementService));

            _applicationTypeService =
                applicationTypeService
                ?? throw new ArgumentNullException(
                    nameof(applicationTypeService));

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
        // PROPERTIES
        // =========================================================

        [ObservableProperty]
        private string licenseIdText = string.Empty;

        [ObservableProperty]
        private DriverLicenseInfoDto? licenseInfo;

        [ObservableProperty]
        private ApplicationReplacementInfoDto? replacementInfo;

        [ObservableProperty]
        private bool isLicenseIssued;

        [ObservableProperty]
        private int applicationTypeId = 4;

        [ObservableProperty]
        private string replacementReason = "Damaged License";

        // =========================================================
        // SEARCH
        // =========================================================

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

            // =====================================================
            // GET LICENSE
            // =====================================================

            var licenseResult =
                await _licenseService
                    .GetLicenseDetailsByIdAsync(
                        licenseId);

            if (licenseResult.IsFailure)
            {
                MessageBox.Show(
                    licenseResult.Error,
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                ClearLicenseData();

                return;
            }

            LicenseInfo =
                licenseResult.Value!;

            // =====================================================
            // MUST BE ACTIVE
            // =====================================================

            if (!LicenseInfo.IsActive)
            {
                MessageBox.Show(
                    "This license is not active.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                ClearLicenseData();

                return;
            }

            // =====================================================
            // GET APPLICATION TYPE
            // =====================================================

            var applicationTypeResult =
                await _applicationTypeService
                    .GetApplicationTypeByIdAsync(
                        ApplicationTypeId);

            if (applicationTypeResult.IsFailure)
            {
                MessageBox.Show(
                    applicationTypeResult.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var applicationType =
                applicationTypeResult.Value!;

            // =====================================================
            // BUILD DISPLAY INFO
            // =====================================================

            ReplacementInfo =
                new ApplicationReplacementInfoDto
                {
                    OldLicenseID =
                        LicenseInfo.LicenseId,

                    ApplicationDate =
                        DateTime.Now,

                    ApplicationFees =
                        applicationType.ApplicationTypeFees,

                    ReplacementReason =
                        ReplacementReason,

                    CreatedByUserName =
                        _currentUserService.Username
                };

            IsLicenseIssued = false;
        }

        // =========================================================
        // ISSUE REPLACEMENT LICENSE
        // =========================================================

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

            try
            {
                // =================================================
                // REPLACEMENT SERVICE
                // =================================================

                var replaceResult =
                    await _licenseReplacementService
                        .ReplaceLicenseAsync(
                            LicenseInfo.LicenseId,
                            ReplacementReason,
                            ApplicationTypeId);

                if (replaceResult.IsFailure)
                {
                    MessageBox.Show(
                        replaceResult.Error,
                        "Replacement License",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                int newLicenseId =
                    replaceResult.Value;

                // =================================================
                // GET NEW LICENSE
                // =================================================

                var licenseResult =
                    await _licenseService
                        .GetByIdAsync(
                            newLicenseId);

                if (licenseResult.IsFailure)
                {
                    MessageBox.Show(
                        licenseResult.Error,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                var newLicense =
                    licenseResult.Value!;

                // =================================================
                // PRESERVE APPLICATION FEES
                // =================================================

                var applicationFees =
                    ReplacementInfo?.ApplicationFees ?? 0;

                // =================================================
                // BUILD FINAL DISPLAY INFO
                // =================================================

                ReplacementInfo =
                    new ApplicationReplacementInfoDto
                    {
                        ReplacementApplicationID =
                            newLicense.ApplicationID,

                        ReplacementLicenseID =
                            newLicense.LicenseID,

                        OldLicenseID =
                            LicenseInfo.LicenseId,

                        ApplicationDate =
                            newLicense.IssueDate,

                        ApplicationFees =
                            applicationFees,

                        LicenseFees =
                            newLicense.PaidFees,

                        ReplacementReason =
                            ReplacementReason,

                        CreatedByUserName =
                            newLicense.CreatedByUserName
                            ?? "Unknown"
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

        // =========================================================
        // SELECT LOST
        // =========================================================

        [RelayCommand]
        private void SelectLost()
        {
            ReplacementReason =
                "Lost License";

            ApplicationTypeId = 3;

            if (ReplacementInfo != null)
            {
                ReplacementInfo.ReplacementReason =
                    ReplacementReason;
            }
        }

        // =========================================================
        // SELECT DAMAGED
        // =========================================================

        [RelayCommand]
        private void SelectDamaged()
        {
            ReplacementReason =
                "Damaged License";

            ApplicationTypeId = 4;

            if (ReplacementInfo != null)
            {
                ReplacementInfo.ReplacementReason =
                    ReplacementReason;
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

            new LicenseHistoryWin(
                vm,
                LicenseInfo.PersonID)
                .ShowDialog();
        }

        // =========================================================
        // NEW LICENSE INFO
        // =========================================================

        [RelayCommand]
        private void ShowLicensesInfo()
        {
            if (!IsLicenseIssued ||
                ReplacementInfo?.ReplacementLicenseID == null)
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
                    .ReplacementLicenseID
                    .Value)
                .ShowDialog();
        }

        // =========================================================
        // CLEAR
        // =========================================================

        private void ClearLicenseData()
        {
            LicenseInfo = null;
            ReplacementInfo = null;
            IsLicenseIssued = false;
        }
    }
}
