using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class ReplacementDamagedLicenseViewModel : ObservableObject
    {
        private readonly ILicenseService _licenseService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;
        private readonly IInternationalService _internationalService;

        public ReplacementDamagedLicenseViewModel(
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

        [RelayCommand]
        private async Task Search()
        {
            if (!int.TryParse(LicenseIdText, out int licenseId))
            {
                MessageBox.Show("Please enter a valid License ID");
                return;
            }

            LicenseInfo = await _licenseService.GetLicenseDetailsByIdAsync(licenseId);

            if (LicenseInfo == null)
            {
                MessageBox.Show("License not found");
                ReplacementInfo = null;
                return;
            }

            if (!LicenseInfo.IsActive)
            {
                MessageBox.Show("This license is not active.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                LicenseInfo = null;
                ReplacementInfo = null;
                return;
            }

            var applicationType = await _applicationTypeService.GetApplicationTypeByIdAsync(ApplicationTypeId);

            if (applicationType == null)
            {
                MessageBox.Show("Replacement Application Type not found");
                return;
            }

            ReplacementInfo = new ApplicationReplacementInfoDto
            {
                OldLicenseID = LicenseInfo.LicenseId,
                ApplicationDate = DateTime.Now,
                ApplicationFees = applicationType.ApplicationTypeFees,
                ReplacementReason = ReplacementReason, // يستخدم الخاصية الديناميكية
                CreatedByUserName = _currentUserService.Username
            };

            IsLicenseIssued = false;
        }

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
                var newLicenseId = await _licenseService.ReplaceLicenseAsync(LicenseInfo.LicenseId, ReplacementReason, ApplicationTypeId);
                var newLicense = await _licenseService.GetByIdAsync(newLicenseId);

                if (newLicense == null) return;

                ReplacementInfo = new ApplicationReplacementInfoDto
                {
                    ReplacementApplicationID = newLicense.ApplicationID,
                    ReplacementLicenseID = newLicense.LicenseID,
                    OldLicenseID = LicenseInfo.LicenseId,
                    ApplicationDate = newLicense.IssueDate,
                    ApplicationFees = ReplacementInfo?.ApplicationFees ?? 0,
                    LicenseFees = newLicense.PaidFees,
                    ReplacementReason = ReplacementReason, // يستخدم الخاصية الديناميكية
                    CreatedByUserName = newLicense.CreatedByUserName ?? "Unknown"
                };

                IsLicenseIssued = true;
                MessageBox.Show($"License replaced successfully.\nNew License ID: {newLicenseId}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void SelectLost()
        {
            ReplacementReason = "Lost License";
            ApplicationTypeId = 3;
        }

        [RelayCommand]
        private void SelectDamaged()
        {
            ReplacementReason = "Damaged License";
            ApplicationTypeId = 4;
        }

        [RelayCommand]
        private void ShowLicensesHistory()
        {
            if (LicenseInfo == null) return;
            var vm = new LicenseHistoryViewModel(_personService, _driverService, _licenseService, _internationalService);
            new LicenseHistoryWin(vm, LicenseInfo.PersonID).ShowDialog();
        }

        [RelayCommand]
        private void ShowLicensesInfo()
        {
            if (!IsLicenseIssued || ReplacementInfo?.ReplacementLicenseID == null)
            {
                MessageBox.Show("License not issued yet");
                return;
            }
            new DriverLicenseInfoWin(ReplacementInfo.ReplacementLicenseID.Value).ShowDialog();
        }
    }
}