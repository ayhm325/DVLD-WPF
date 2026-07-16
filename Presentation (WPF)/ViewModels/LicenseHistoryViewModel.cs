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
            Person = await _personService.GetPersonByIdAsync(personId);

            var driver = await _driverService.GetByPersonIdAsync(personId);

            if (driver is null)
            {
                LocalLicenses.Clear();
                return;
            }

            var licenses = await _licenseService.GetByDriverIdAsync(driver.DriverID);           

            LocalLicenses = new ObservableCollection<LicenseDto>(licenses);

            var international = await _internationalService.GetByDriverIdAsync(driver.DriverID);
            
            InternationalLicenses = new ObservableCollection<InternationalDto>(international);
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
