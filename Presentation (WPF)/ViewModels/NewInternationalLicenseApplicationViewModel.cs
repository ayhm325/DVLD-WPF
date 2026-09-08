using Application.DTOs.ApplicationDTO;
using Application.DTOs.InternationalLicenseDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class NewInternationalLicenseApplicationViewModel
        : ObservableObject
    {
        private readonly IInternationalService _internationalService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ILicenseService _licenseService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;

        private readonly IPeopleApiClient _peopleApiClient;
        private readonly IDriversApiClient _driversApiClient;
        private readonly ILicensesApiClient _licensesApiClient;
        private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;

        [ObservableProperty]
        private DriverLicenseInfoDto? licenseInfo;

        [ObservableProperty]
        private int localLicenseId;

        [ObservableProperty]
        private string licenseIdText = string.Empty;

        [ObservableProperty]
        private InternationalLicenseApplicationInfoDto? applicationInfo;

        [ObservableProperty]
        private bool isLicenseIssued = false;

        public NewInternationalLicenseApplicationViewModel(
            IInternationalService internationalService,
            ICurrentUserService currentUserService,
            IApplicationTypeService applicationTypeService,
            ILicenseService licenseService,
            IPersonService personService,
            IDriverService driverService,
            IPeopleApiClient peopleApiClient,
            IDriversApiClient driversApiClient,
            ILicensesApiClient licensesApiClient,
            IInternationalLicensesApiClient internationalLicensesApiClient)
        {
            _internationalService =
                internationalService
                ?? throw new ArgumentNullException(
                    nameof(internationalService));

            _currentUserService =
                currentUserService
                ?? throw new ArgumentNullException(
                    nameof(currentUserService));

            _applicationTypeService =
                applicationTypeService
                ?? throw new ArgumentNullException(
                    nameof(applicationTypeService));

            _licenseService =
                licenseService
                ?? throw new ArgumentNullException(
                    nameof(licenseService));

            _personService =
                personService
                ?? throw new ArgumentNullException(
                    nameof(personService));

            _driverService =
                driverService
                ?? throw new ArgumentNullException(
                    nameof(driverService));

            _peopleApiClient =
                peopleApiClient
                ?? throw new ArgumentNullException(
                    nameof(peopleApiClient));

            _driversApiClient =
                driversApiClient
                ?? throw new ArgumentNullException(
                    nameof(driversApiClient));

            _licensesApiClient =
                licensesApiClient
                ?? throw new ArgumentNullException(
                    nameof(licensesApiClient));

            _internationalLicensesApiClient =
                internationalLicensesApiClient
                ?? throw new ArgumentNullException(
                    nameof(internationalLicensesApiClient));
        }

        [RelayCommand]
        private async Task Search()
        {
            if (!int.TryParse(
                    LicenseIdText,
                    out int licenseId))
            {
                MessageBox.Show(
                    "Please enter a valid License ID");

                return;
            }

            LocalLicenseId = 0;
            LicenseInfo = null;

            var licenseResult =
                await _internationalService
                    .GetLocalLicenseInfoAsync(
                        licenseId);

            if (licenseResult.IsFailure)
            {
                LicenseInfo = null;

                MessageBox.Show(
                    licenseResult.Error);

                return;
            }

            LocalLicenseId = licenseId;
            LicenseInfo = licenseResult.Value;

            var applicationTypeResult =
                await _applicationTypeService
                    .GetApplicationTypeByIdAsync(6);

            if (applicationTypeResult.IsFailure)
            {
                MessageBox.Show(
                    applicationTypeResult.Error);

                return;
            }

            var applicationType =
                applicationTypeResult.Value!;

            var paidFees =
                applicationType.ApplicationTypeFees;

            ApplicationInfo =
                new InternationalLicenseApplicationInfoDto
                {
                    LocalLicenseID =
                        licenseId,

                    ApplicationDate =
                        DateTime.Now,

                    IssueDate =
                        DateTime.Now,

                    ExpirationDate =
                        DateTime.Now.AddYears(1),

                    ApplicationStatus =
                        AppStatus.New,

                    LastStatusDate =
                        DateTime.Now,

                    PaidFees =
                        paidFees,

                    CreatedByUserID =
                        _currentUserService.UserId,

                    CreatedByUserName =
                        _currentUserService.Username
                };

            MessageBox.Show(
                "Local License Found Successfully");
        }

        [RelayCommand]
        private async Task Issue()
        {
            if (LicenseInfo == null)
            {
                MessageBox.Show(
                    "Please search for a local license first");

                return;
            }

            var result =
                await _internationalService
                    .IssueInternationalLicenseAsync(
                        LocalLicenseId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error);

                return;
            }

            var internationalResult =
                await _internationalService
                    .GetByLocalLicenseIdAsync(
                        LocalLicenseId);

            if (internationalResult.IsFailure)
            {
                MessageBox.Show(
                    internationalResult.Error);

                return;
            }

            var international =
                internationalResult.Value!
                    .FirstOrDefault();

            if (international == null)
            {
                MessageBox.Show(
                    "International license not found");

                return;
            }

            ApplicationInfo =
                new InternationalLicenseApplicationInfoDto
                {
                    ApplicationID =
                        international.ApplicationID,

                    InternationalLicenseID =
                        international.InternationalLicenseID,

                    LocalLicenseID =
                        international.IssuedUsingLocalLicenseID,

                    ApplicationDate =
                        international.IssueDate,

                    IssueDate =
                        international.IssueDate,

                    ExpirationDate =
                        international.ExpirationDate,

                    ApplicationStatus =
                        AppStatus.Completed,

                    LastStatusDate =
                        DateTime.Now,

                    PaidFees =
                        international.Fees,

                    CreatedByUserID =
                        international.CreatedByUserID,

                    CreatedByUserName =
                        international.CreatedByUserName
                };

            IsLicenseIssued = true;

            MessageBox.Show(
                "International License Issued Successfully");
        }

        [RelayCommand]
        private void ShowLicensesHistory()
        {
            if (LicenseInfo == null)
                return;

            var vm =
                new LicenseHistoryViewModel(
                    _peopleApiClient,
                    _driversApiClient,
                    _licensesApiClient,
                    _internationalLicensesApiClient);

            var win =
                new LicenseHistoryWin(
                    vm,
                    LicenseInfo.PersonID);

            win.ShowDialog();
        }

        [RelayCommand]
        private void ShowLicensesInfo()
        {
            if (ApplicationInfo == null ||
                !IsLicenseIssued ||
                ApplicationInfo.InternationalLicenseID <= 0)
            {
                return;
            }

            var win =
                new DriverInterNationalLicenseInfoWin(
                    ApplicationInfo.InternationalLicenseID);

            win.ShowDialog();
        }
    }
}