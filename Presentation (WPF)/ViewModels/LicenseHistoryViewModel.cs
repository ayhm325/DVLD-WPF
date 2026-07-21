using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Infrastructure.Repositories;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;


namespace Presentation.ViewModels
{
    public partial class LicenseHistoryViewModel : ObservableObject
    {
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;
        private readonly ILicenseService _licenseService;
        private readonly IInternationalService _internationalService;

        [ObservableProperty]
        private PersonDto? person;

        [ObservableProperty]
        private LicenseDto? selectedLocalLicense;

        [ObservableProperty]
        private InternationalDto? selectedInternationalLicense;

        [ObservableProperty]
        private ObservableCollection<LicenseDto> localLicenses = [];

        [ObservableProperty]
        private ObservableCollection<InternationalDto> internationalLicenses = [];

        public LicenseHistoryViewModel(
            IPersonService personService,
            IDriverService driverService,
            ILicenseService licenseService,
            IInternationalService internationalService
            )
        {
            _personService = personService;
            _driverService = driverService;
            _licenseService = licenseService;
            _internationalService = internationalService;
        }

        public async Task LoadAsync(int personId)
        {
            var personResult = await _personService.GetPersonByIdAsync(personId);

            if (personResult.IsFailure)
            {
                Person = null;
                return;
            }

            Person = personResult.Value!;

            var driverResult = await _driverService.GetByPersonIdAsync(personId);

            if (driverResult.IsFailure)
            {
                LocalLicenses.Clear();
                InternationalLicenses.Clear();
                return;
            }

            var driver = driverResult.Value!;

            var licensesResult = await _licenseService.GetByDriverIdAsync(driver.DriverID);

            if (licensesResult.IsFailure)
            {
                LocalLicenses.Clear();
            }
            else
            {
                LocalLicenses = new ObservableCollection<LicenseDto>(licensesResult.Value!);
            }

            var internationalResult = await _internationalService.GetByDriverIdAsync(driver.DriverID);

            if (internationalResult.IsFailure)
            {
                InternationalLicenses.Clear();
            }
            else
            {
                InternationalLicenses = new ObservableCollection<InternationalDto>( internationalResult.Value!);
            }
        }

        [RelayCommand]
        private void ShowLicense()
        {
            if (SelectedLocalLicense == null)
            {
                MessageBox.Show("Please select a license first");
                return;
            }

            var win = new DriverLicenseInfoWin(SelectedLocalLicense.LicenseID);
            win.ShowDialog();
        }

        [RelayCommand]
        private void ShowInternationalLicense()
        {
            if (SelectedInternationalLicense == null)
            {
                MessageBox.Show("Please select an international license first");
                return;
            }

            var win = new DriverLicenseInfoWin(SelectedInternationalLicense.InternationalLicenseID);
            win.ShowDialog();
        }
    }
}
