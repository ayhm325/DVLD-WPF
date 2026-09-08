using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Driver;
using DVLD.Contracts.InternationalLicense;
using DVLD.Contracts.License;
using DVLD.Contracts.Person;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class LicenseHistoryViewModel : ObservableObject
    {
        private readonly IPeopleApiClient _peopleApiClient;
        private readonly IDriversApiClient _driversApiClient;
        private readonly ILicensesApiClient _licensesApiClient;
        private readonly IInternationalLicensesApiClient _internationalLicensesApiClient;

        [ObservableProperty]
        private PersonResponse? person;

        [ObservableProperty]
        private LicenseResponse? selectedLocalLicense;

        [ObservableProperty]
        private InternationalLicenseResponse? selectedInternationalLicense;

        [ObservableProperty]
        private ObservableCollection<LicenseResponse> localLicenses = [];

        [ObservableProperty]
        private ObservableCollection<InternationalLicenseResponse> internationalLicenses = [];

        public LicenseHistoryViewModel(
            IPeopleApiClient peopleApiClient,
            IDriversApiClient driversApiClient,
            ILicensesApiClient licensesApiClient,
            IInternationalLicensesApiClient internationalLicensesApiClient)
        {
            _peopleApiClient = peopleApiClient;
            _driversApiClient = driversApiClient;
            _licensesApiClient = licensesApiClient;
            _internationalLicensesApiClient = internationalLicensesApiClient;
        }

        public async Task LoadAsync(int personId)
        {
            var personResult =
                await _peopleApiClient.GetByIdAsync(personId);

            if (personResult.IsFailure ||
                personResult.Value is null)
            {
                Person = null;
                LocalLicenses.Clear();
                InternationalLicenses.Clear();
                return;
            }

            Person = personResult.Value;

            var driverResult =
                await _driversApiClient.GetByPersonIdAsync(personId);

            if (driverResult.IsFailure ||
                driverResult.Value is null)
            {
                LocalLicenses.Clear();
                InternationalLicenses.Clear();
                return;
            }

            DriverResponse driver = driverResult.Value;

            var licensesResult =
                await _licensesApiClient.GetByDriverIdAsync(
                    driver.DriverId);

            if (licensesResult.IsFailure ||
                licensesResult.Value is null)
            {
                LocalLicenses.Clear();
            }
            else
            {
                LocalLicenses =
                    new ObservableCollection<LicenseResponse>(
                        licensesResult.Value);
            }

            var internationalResult =
                await _internationalLicensesApiClient.GetByDriverIdAsync(
                    driver.DriverId);

            if (internationalResult.IsFailure ||
                internationalResult.Value is null)
            {
                InternationalLicenses.Clear();
            }
            else
            {
                InternationalLicenses =
                    new ObservableCollection<InternationalLicenseResponse>(
                        internationalResult.Value);
            }
        }

        [RelayCommand]
        private void ShowLicense()
        {
            if (SelectedLocalLicense is null)
            {
                MessageBox.Show("Please select a license first");
                return;
            }

            var win =
                new DriverLicenseInfoWin(
                    SelectedLocalLicense.LicenseId);

            win.ShowDialog();
        }

        [RelayCommand]
        private void ShowInternationalLicense()
        {
            if (SelectedInternationalLicense is null)
            {
                MessageBox.Show(
                    "Please select an international license first");

                return;
            }

            MessageBox.Show(
                "International license details are not available yet.");
        }
    }
}