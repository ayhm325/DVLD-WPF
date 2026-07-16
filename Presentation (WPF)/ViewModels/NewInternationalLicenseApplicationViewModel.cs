
using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Domain.Enums;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class NewInternationalLicenseApplicationViewModel : ObservableObject
    {       

        private readonly IInternationalService _internationalService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ILicenseService _licenseService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;

        [ObservableProperty]
        private DriverLicenseInfoDto? licenseInfo;

        [ObservableProperty]
        private int localLicenseId;

        [ObservableProperty]
        private string licenseIdText = string.Empty;

        [ObservableProperty]
        private ApplicationDto? applicationInfo;

        [ObservableProperty]
        private bool isLicenseIssued = false;

        public NewInternationalLicenseApplicationViewModel(
            IInternationalService internationalService,
            ICurrentUserService currentUserService,
            IApplicationTypeService applicationTypeService,
            ILicenseService licenseService,
            IPersonService personService,
            IDriverService driverService)
        {
            _internationalService = internationalService;
            _currentUserService = currentUserService;
            _applicationTypeService = applicationTypeService;
            _licenseService = licenseService;
            _personService = personService;
            _driverService = driverService;
        }


        // ===============================
        // Search Local License
        // ===============================
        [RelayCommand]
        private async Task Search()
        {
            if (!int.TryParse(LicenseIdText, out int licenseId))
            {
                MessageBox.Show("Please enter a valid License ID");
                return;
            }

            LocalLicenseId = licenseId;

            LicenseInfo = await _internationalService.GetLocalLicenseInfoAsync(licenseId);

            if (LicenseInfo == null)
            {
                MessageBox.Show("Local License not found");
                return;
            }
            var applicationType = await _applicationTypeService.GetApplicationTypeByIdAsync(6);
            if (applicationType == null)
            {
                MessageBox.Show("Application Type not found");
                return;
            }
            var PaidFees = applicationType.ApplicationTypeFees;
            ApplicationInfo = new ApplicationDto
            {                
                LocalLicenseID = licenseId,

                ApplicationDate = DateTime.Now,
                IssueDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(1),

                ApplicationStatus = AppStatus.New,
                LastStatusDate = DateTime.Now,

                PaidFees = PaidFees,

                CreatedByUserID = _currentUserService.UserId,
                CreatedByUserName = _currentUserService.Username
            };

            // ربط الرخصة المحلية قبل الإصدار
            ApplicationInfo!.LocalLicenseID = licenseId;


            MessageBox.Show("Local License Found Successfully");
        }


        // ===============================
        // Issue International License
        // ===============================
        [RelayCommand]
        private async Task Issue()
        {
            if (LicenseInfo == null)
            {
                MessageBox.Show("Please search for a local license first");

                return;
            }

            var result = await _internationalService.IssueInternationalLicenseAsync(LocalLicenseId);


            if (!result)
            {
                MessageBox.Show("Failed to issue international license");

                return;
            }            

            var application = await _internationalService.GetByLocalLicenseIdAsync(LocalLicenseId);
            var international = System.Linq.Enumerable.FirstOrDefault(application);

            if (international != null)
            {
                ApplicationInfo = new ApplicationDto
                {
                    ApplicationID = international.ApplicationID,
                    LicenseID = international.InternationalLicenseID,
                    LocalLicenseID = international.IssuedUsingLocalLicenseID,
                    ApplicationDate = international.IssueDate,
                    IssueDate = international.IssueDate,
                    ExpirationDate = international.ExpirationDate,
                    ApplicationStatus = AppStatus.Completed,
                    LastStatusDate = DateTime.Now,
                    PaidFees = international.Fees,
                    CreatedByUserID = international.CreatedByUserID,
                    CreatedByUserName = international.CreatedByUserName
                };

                IsLicenseIssued = true;
                MessageBox.Show("International License Issued Successfully");
               
            }
        }

        // ===============================
        // History
        // ===============================
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

        // ===============================
        // License Info
        // ===============================
        [RelayCommand]
        private void ShowLicensesInfo()
        {
            if (ApplicationInfo == null ||
                !IsLicenseIssued ||
                ApplicationInfo.LicenseID == null)
            {
                return;
            }


            var win = new DriverInterNationalLicenseInfoWin(
                ApplicationInfo.LicenseID.Value
            );


            win.ShowDialog();
        }


    }
} 

