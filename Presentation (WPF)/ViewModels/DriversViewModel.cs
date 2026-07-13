using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels
{
    public partial class DriversViewModel : ObservableObject
    {
        private readonly IDriverService _driverService;
        private List<DriverDto> _allDrivers = new();

        public DriversViewModel(IDriverService driverService)
        {
            _driverService = driverService;
        }


        public ObservableCollection<DriverDto> Drivers { get; set; } = new();


        private int _driversCount;
        public int DriversCount
        {
            get => _driversCount;
            set => SetProperty(ref _driversCount, value);
        }


        public async Task LoadAsync()
        {
            _allDrivers = await _driverService.GetAllAsync();
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
    }
}