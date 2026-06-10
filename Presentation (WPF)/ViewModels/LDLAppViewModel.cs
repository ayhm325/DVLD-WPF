using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Presentation.ViewModels
{
    public partial class LDLAppViewModel : ObservableObject
    {
        private readonly ILocalDrivingLicenseApplicationService _service;
        private List<LocalDrivingLicenseApplicationDto> _allApplications = new();

        public ObservableCollection<LocalDrivingLicenseApplicationDto> Applications { get; set; } = new();

        // استخدام السمة الصحيحة
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "Full Name";

        // تعريف partial method للتعامل مع التغيير
        partial void OnSearchTextChanged(string value)
        {
            FilterApplications();
        }

        public LDLAppViewModel(ILocalDrivingLicenseApplicationService service)
        {
            _service = service;
            _ = LoadApplicationsAsync();
        }

        [RelayCommand]
        public async Task LoadApplicationsAsync()
        {
            _allApplications = await _service.GetAllLocalDrivingLicenseApplicationsAsync();
            FilterApplications();
        }

        public void FilterApplications()
        {
            var filtered = _allApplications.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // معالجة null لضمان عدم حدوث خطأ
                filtered = filtered.Where(x =>
                    (x.FullName?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.NationalNo?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false));
            }

            Applications.Clear();
            foreach (var item in filtered) Applications.Add(item);
        }

        [RelayCommand]
        private void AddNew()
        {
            var addEditVm = App.ServiceProvider.GetRequiredService<AddEditLDLAppViewModel>();           
            
            var win = new NewLocalLicnnse(addEditVm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            win.ShowDialog();
        }


    }
}