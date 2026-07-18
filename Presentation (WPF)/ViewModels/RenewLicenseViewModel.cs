using Application.DTOs;
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
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;
        private readonly IInternationalService _internationalService;


        private const int RenewLicenseApplicationTypeId = 2;
        // غير الرقم حسب جدول ApplicationTypes عندك

        public RenewLicenseViewModel(
            ILicenseService licenseService,
            IApplicationTypeService applicationTypeService,
            ICurrentUserService currentUserService,
            IPersonService personService,
            IDriverService driverService,
            IInternationalService internationalService)
        {
            _licenseService = licenseService;
            _applicationTypeService = applicationTypeService;
            _currentUserService = currentUserService;
            _personService = personService;
            _driverService = driverService;
            _internationalService = internationalService;
        }

        // =========================
        // Properties
        // =========================
        [ObservableProperty]
        private string licenseIdText = string.Empty;

        [ObservableProperty]
        private DriverLicenseInfoDto? licenseInfo;

        // للعرض في ApplicationNewLicenseInfo Control
        [ObservableProperty]
        private ApplicationNewLicenseInfoDto? newLicenseInfo;

        // للـ Database لاحقا
        [ObservableProperty]
        private ApplicationDto? applicationInfo;

        [ObservableProperty]
        private bool isLicenseIssued;

        [ObservableProperty]
        private int? renewedLicenseId;

        public bool CanSearch =>int.TryParse(LicenseIdText, out _);



        // =========================
        // Search
        // =========================


        [RelayCommand]
        private async Task Search()
        {
            if (!int.TryParse(LicenseIdText, out int licenseId))
            {
                MessageBox.Show("Please enter a valid License ID");
                return;
            }

            LicenseInfo =await _licenseService.GetLicenseDetailsByIdAsync(licenseId);

            if (LicenseInfo == null)
            {
                MessageBox.Show("License not found");

                NewLicenseInfo = null;
                ApplicationInfo = null;

                return;
            }

            if (LicenseInfo.ExpirationDate > DateTime.Now)
            {
                MessageBox.Show("This license has not expired yet. Renewal is not allowed.",
                    "Renew License",MessageBoxButton.OK,MessageBoxImage.Warning);

                LicenseInfo = null;
                NewLicenseInfo = null;
                ApplicationInfo = null;
                return;
            }

            if (!LicenseInfo.IsActive)
            {
                MessageBox.Show("This license is not active and cannot be renewed.",
                    "Renew License",MessageBoxButton.OK, MessageBoxImage.Warning);

                LicenseInfo = null;
                NewLicenseInfo = null;
                ApplicationInfo = null;

                return;
            }

            var applicationType =await _applicationTypeService.GetApplicationTypeByIdAsync(RenewLicenseApplicationTypeId);

            if (applicationType == null)
            {
                MessageBox.Show("Renew License Application Type not found");
                return;
            }

            // معلومات العرض
            NewLicenseInfo = new ApplicationNewLicenseInfoDto
            {
                OldLicenseID = LicenseInfo.LicenseId,
                ApplicationDate = DateTime.Now,
                IssueDate = LicenseInfo.IssueDate,
                ExpirationDate = LicenseInfo.ExpirationDate,
                ApplicationFees = applicationType.ApplicationTypeFees,
                LicenseFees = LicenseInfo.LicenseClassFees,
                IssueReason = (byte)Domain.Enums.IssueReason.Renew,
                CreatedByUserName = _currentUserService.Username
            };



            // Application سيتم استخدامها عند الإصدار
            ApplicationInfo = new ApplicationDto
            {
                ApplicantPersonID = LicenseInfo.PersonID,
                ApplicationTypeID =RenewLicenseApplicationTypeId,
                ApplicationDate = DateTime.Now,
                ApplicationStatus =Domain.Enums.AppStatus.New,
                LastStatusDate = DateTime.Now,
                PaidFees =applicationType.ApplicationTypeFees,
                CreatedByUserID = _currentUserService.UserId,
                CreatedByUserName =_currentUserService.Username
            };

            IsLicenseIssued = false;
            MessageBox.Show("License found successfully");
        }





        // =========================
        // Issue Renew License
        // =========================


        [RelayCommand]
        private async Task Issue()
        {
            if (LicenseInfo == null)
            {
                MessageBox.Show("Please search for a license first");
                return;
            }

            try
            {
                var newLicenseId =await _licenseService.RenewLicenseAsync(
                        LicenseInfo.LicenseId,NewLicenseInfo?.Notes);

                var newLicense =await _licenseService.GetByIdAsync(newLicenseId);
                if (newLicense == null)
                    return;

                if (ApplicationInfo != null)
                {
                    ApplicationInfo.LicenseID = newLicense.LicenseID;
                }

                RenewedLicenseId = newLicense.LicenseID;

                NewLicenseInfo = new ApplicationNewLicenseInfoDto
                {
                    RenewedLicenseApplicationID =newLicense.ApplicationID,
                    RenewedLicenseID =newLicense.LicenseID,
                    OldLicenseID =LicenseInfo.LicenseId,
                    ApplicationDate =newLicense.IssueDate,
                    IssueDate =newLicense.IssueDate,
                    ExpirationDate =newLicense.ExpirationDate,
                    ApplicationFees =ApplicationInfo?.PaidFees ?? 0,
                    LicenseFees =newLicense.PaidFees,
                    CreatedByUserName =newLicense.CreatedByUserName ?? "Unknown"
                };
                
                IsLicenseIssued = true;
                MessageBox.Show($"License renewed successfully.\nNew License ID: {newLicenseId}",
                    "Success",MessageBoxButton.OK,MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        // =========================
        // History
        // =========================


        [RelayCommand]
        private void ShowLicensesHistory()
        {
            if (LicenseInfo == null)
                return;

            var vm = new LicenseHistoryViewModel(
                _personService,
                _driverService,
                _licenseService,
                _internationalService
            );

            var win = new LicenseHistoryWin(
                vm,
                LicenseInfo.PersonID
            );

            win.ShowDialog();
        }





        // =========================
        // License Info
        // =========================


        [RelayCommand]
        private void ShowLicensesInfo()
        {
            if (ApplicationInfo == null ||
                !IsLicenseIssued ||
                ApplicationInfo.LicenseID == null)
            {
                MessageBox.Show("License not issued yet");
                return;
            }

            var win = new DriverLicenseInfoWin(ApplicationInfo.LicenseID.Value);

            win.ShowDialog();
        }

    }
}