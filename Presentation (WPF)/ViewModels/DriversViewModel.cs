using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels
{
    public partial class DriversViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDriverService _driverService;
        private List<DriverDto> _allDrivers = new();

        public DriversViewModel(IDriverService driverService, IServiceProvider serviceProvider)
        {
            _driverService = driverService;
            _serviceProvider = serviceProvider;           
        }

        [ObservableProperty]
        private DriverDto? selectedDriver;

        public ObservableCollection<DriverDto> Drivers { get; set; } = new();


        private int _driversCount;
        public int DriversCount
        {
            get => _driversCount;
            set => SetProperty(ref _driversCount, value);
        }


        public async Task LoadAsync()
        {
            var result = await _driverService.GetAllAsync();

            if (result.IsFailure)
            {
                _allDrivers = new List<DriverDto>();
                return;
            }

            _allDrivers = result.Value ?? new List<DriverDto>();

            FilterDrivers(string.Empty, "None");
        }

        public void FilterDrivers(string filterValue, string filterBy)
        {
            IEnumerable<DriverDto> filtered = _allDrivers;

            if (!string.IsNullOrWhiteSpace(filterValue))
            {
                string val = filterValue.ToLower(); // لجعل البحث غير حساس لحالة الأحرف

                if (filterBy == "Driver ID")
                    filtered = _allDrivers.Where(d => d.DriverID.ToString().Contains(val));

                else if (filterBy == "Person ID")
                    filtered = _allDrivers.Where(d => d.PersonID.ToString().Contains(val));

                else if (filterBy == "Full Name") // إضافة شرط الاسم
                    filtered = _allDrivers.Where(d => d.FullName.ToLower().Contains(val));
            }

            Drivers.Clear();
            foreach (var item in filtered)
                Drivers.Add(item);

            DriversCount = Drivers.Count;
        }

        [RelayCommand]
        private async Task ShowLicenseHistory()
        {
            if (SelectedDriver == null)
                return;

            int personId = SelectedDriver.PersonID;

            var vm = _serviceProvider
                .GetRequiredService<LicenseHistoryViewModel>();

            await vm.LoadAsync(personId);

            var window = new LicenseHistoryWin(vm, personId)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();
        }

        [RelayCommand]
        private void ShowPersonInfo()
        {
            if (SelectedDriver == null)
                return;

            var window = new PersonDetailsWindow(
                SelectedDriver.PersonID)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };


            window.ShowDialog();
        }
    }
}