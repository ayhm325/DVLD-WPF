using Application.DTOs.ApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class RenewLicenseViewModel : ObservableObject
    {
        private readonly ILicenseService _licenseService;
        private readonly ILicenseRenewalService _licenseRenewalService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;
        private readonly IInternationalService _internationalService;

        private const int RenewLicenseApplicationTypeId = 2;

        public RenewLicenseViewModel(
            ILicenseService licenseService,
            ILicenseRenewalService licenseRenewalService,
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

            _licenseRenewalService =
                licenseRenewalService
                ?? throw new ArgumentNullException(
                    nameof(licenseRenewalService));

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
        private ApplicationNewLicenseInfoDto? newLicenseInfo;

        [ObservableProperty]
        private ApplicationDto? applicationInfo;

        [ObservableProperty]
        private bool isLicenseIssued;

        [ObservableProperty]
        private int? renewedLicenseId;

        public bool CanSearch =>
            int.TryParse(
                LicenseIdText,
                out _);

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
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var licenseResult =
                await _licenseService
                    .GetLicenseDetailsByIdAsync(
                        licenseId);

            if (licenseResult.IsFailure)
            {
                MessageBox.Show(
                    licenseResult.Error,
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                ClearLicenseData();
                return;
            }

            LicenseInfo =
                licenseResult.Value!;

            // =====================================================
            // MUST BE EXPIRED
            // =====================================================

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

            // =====================================================
            // MUST BE ACTIVE
            // =====================================================

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

            // =====================================================
            // GET APPLICATION TYPE
            // =====================================================

            var applicationTypeResult =
                await _applicationTypeService
                    .GetApplicationTypeByIdAsync(
                        RenewLicenseApplicationTypeId);

            if (applicationTypeResult.IsFailure)
            {
                MessageBox.Show(
                    applicationTypeResult.Error,
                    "Renew License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var applicationType =
                applicationTypeResult.Value!;

            // =====================================================
            // BUILD DISPLAY INFO
            // =====================================================

            NewLicenseInfo =
                new ApplicationNewLicenseInfoDto
                {
                    OldLicenseID =
                        LicenseInfo.LicenseId,

                    ApplicationDate =
                        DateTime.Now,

                    IssueDate =
                        LicenseInfo.IssueDate,

                    ExpirationDate =
                        LicenseInfo.ExpirationDate,

                    ApplicationFees =
                        applicationType.ApplicationTypeFees,

                    LicenseFees =
                        LicenseInfo.LicenseClassFees,

                    IssueReason =
                        (byte)Domain.Enums.IssueReason.Renew,

                    CreatedByUserName =
                        _currentUserService.Username
                };

            // =====================================================
            // BUILD APPLICATION INFO
            // =====================================================

            ApplicationInfo =
                new ApplicationDto
                {
                    ApplicantPersonID =
                        LicenseInfo.PersonID,

                    ApplicationTypeID =
                        RenewLicenseApplicationTypeId,

                    ApplicationDate =
                        DateTime.Now,

                    ApplicationStatus =
                        Domain.Enums.AppStatus.New,

                    LastStatusDate =
                        DateTime.Now,

                    PaidFees =
                        applicationType.ApplicationTypeFees,

                    CreatedByUserID =
                        _currentUserService.UserId,

                    CreatedByUserName =
                        _currentUserService.Username
                };

            // =====================================================
            // RESET PREVIOUS STATE
            // =====================================================

            RenewedLicenseId = null;
            IsLicenseIssued = false;

            MessageBox.Show(
                "License found successfully",
                "Renew License",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // =========================================================
        // ISSUE / RENEW LICENSE
        // =========================================================

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
                // =================================================
                // RENEWAL SERVICE
                // =================================================

                var renewResult =
                    await _licenseRenewalService
                        .RenewLicenseAsync(
                            LicenseInfo.LicenseId,
                            NewLicenseInfo?.Notes);

                if (renewResult.IsFailure)
                {
                    MessageBox.Show(
                        renewResult.Error,
                        "Renew License",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                int newLicenseId =
                    renewResult.Value;

                RenewedLicenseId =
                    newLicenseId;

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
                // BUILD FINAL DISPLAY INFO
                // =================================================

                NewLicenseInfo =
                    new ApplicationNewLicenseInfoDto
                    {
                        RenewedLicenseApplicationID =
                            newLicense.ApplicationID,

                        RenewedLicenseID =
                            newLicense.LicenseID,

                        OldLicenseID =
                            LicenseInfo.LicenseId,

                        ApplicationDate =
                            newLicense.IssueDate,

                        IssueDate =
                            newLicense.IssueDate,

                        ExpirationDate =
                            newLicense.ExpirationDate,

                        ApplicationFees =
                            ApplicationInfo?.PaidFees ?? 0,

                        LicenseFees =
                            newLicense.PaidFees,

                        CreatedByUserName =
                            newLicense.CreatedByUserName
                            ?? "Unknown"
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

            var win =
                new LicenseHistoryWin(
                    vm,
                    LicenseInfo.PersonID);

            win.ShowDialog();
        }

        // =========================================================
        // NEW LICENSE INFO
        // =========================================================

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

        // =========================================================
        // CLEAR
        // =========================================================

        private void ClearLicenseData()
        {
            LicenseInfo = null;
            NewLicenseInfo = null;
            ApplicationInfo = null;
            RenewedLicenseId = null;
            IsLicenseIssued = false;
        }
    }
}
