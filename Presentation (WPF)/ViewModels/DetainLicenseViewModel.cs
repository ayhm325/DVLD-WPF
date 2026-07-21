using Application.DTOs;
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
            _licenseService = licenseService;
            _detainedLicenseService = detainedLicenseService;
            _currentUserService = currentUserService;
            _personService = personService;
            _driverService = driverService;
            _internationalService = internationalService;
        }


        [RelayCommand]
        private async Task SearchAsync()
        {
            if (!int.TryParse(LicenseIdText, out int licenseId))
                return;


            var result = await _licenseService.GetLicenseDetailsByIdAsync(licenseId);


            if (result.IsFailure)
            {
                LicenseInfo = null;
                DetainInfo = null;
                IsLicenseIssued = false;

                MessageBox.Show(result.Error);
                return;
            }


            LicenseInfo = result.Value;


            if (LicenseInfo == null)
            {
                DetainInfo = null;
                IsLicenseIssued = false;
                return;
            }


            IsLicenseIssued = true;


            DetainInfo = new DetainedLicenseDto
            {
                LicenseID = LicenseInfo.LicenseId,
                DetainDate = DateTime.Now,
                CreatedByUserName = _currentUserService.Username
            };
        }


        [RelayCommand]
        private async Task IssueAsync()
        {
            if (LicenseInfo == null)
                return;

            if (await _detainedLicenseService.IsLicenseDetainedAsync(LicenseInfo.LicenseId))
            {
                MessageBox.Show(
                    "This license is already detained.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            var dto = new DetainedLicenseDto
            {
                LicenseID = LicenseInfo.LicenseId,
                DetainDate = DateTime.Now,
                FineFees = FineFees,
                CreatedByUserID = _currentUserService.UserId
            };


            var result = await _detainedLicenseService.AddAsync(dto);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (result.Value != null)
            {
                DetainInfo = result.Value;

                MessageBox.Show(
                    "License detained successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        [RelayCommand]
        private void ShowLicensesHistory()
        {
            if (LicenseInfo == null)
                return;


            var vm = new LicenseHistoryViewModel(
                _personService,
                _driverService,
                _licenseService,
                _internationalService);


            var window = new LicenseHistoryWin(
                vm,
                LicenseInfo.PersonID);


            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }


        [RelayCommand]
        private void ShowLicensesInfo()
        {
            if (LicenseInfo == null)
                return;


            var window = new DriverLicenseInfoWin(
                LicenseInfo.LicenseId);


            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }
    }
}