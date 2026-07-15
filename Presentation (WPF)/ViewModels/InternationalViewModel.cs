using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace Presentation.ViewModels
{
    public partial class InternationalViewModel : ObservableObject
    {
        private readonly IInternationalService _licenseService;
        private readonly IServiceProvider _serviceProvider;

        // القائمة الأصلية
        private ObservableCollection<InternationalDto> _allApplications = new();

        // القائمة المعروضة في الـ DataGrid
        public ObservableCollection<InternationalDto> Applications { get; } = new();

        [ObservableProperty]
        private InternationalDto? _selectedApplication;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "Int License ID";

        public InternationalViewModel(
            IInternationalService licenseService,
            IServiceProvider serviceProvider)
        {
            _licenseService = licenseService;
            _serviceProvider = serviceProvider;

            _ = LoadApplications();
        }

        private async Task LoadApplications()
        {
            var data = await _licenseService.GetAllAsync();

            _allApplications.Clear();

            foreach (var item in data)
                _allApplications.Add(item);

            ApplyFilter();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedFilterChanged(string value) => ApplyFilter();

        public ObservableCollection<string> Filters { get; } = new()
        {
            "Int License ID",
            "Application ID",
            "Driver ID",
            "L.License ID"
        };

        private void ApplyFilter()
        {
            // 1. إذا كانت القائمة الأصلية فارغة، لا تقم بأي إجراء
            if (_allApplications == null) return;

            Applications.Clear();

            // 2. البحث
            var filtered = _allApplications.Where(x =>
            {
                // إذا كان نص البحث فارغاً، أعرض الكل
                if (string.IsNullOrWhiteSpace(SearchText)) return true;

                // تحويل نص البحث إلى صيغة موحدة للبحث
                string filter = SearchText.Trim();

                return SelectedFilter switch
                {
                    "Int License ID" => x.InternationalLicenseID.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase),
                    "Application ID" => x.ApplicationID.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase),
                    "Driver ID" => x.DriverID.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase),
                    "L.License ID" => x.IssuedUsingLocalLicenseID.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase),
                    _ => true
                };
            });

            // 3. إضافة النتائج المفلترة للقائمة المعروضة
            foreach (var item in filtered)
            {
                Applications.Add(item);
            }
        }

        [RelayCommand]
        private void IssueNew()
        {
            // منطق فتح نافذة إصدار رخصة جديدة
        }

        [RelayCommand]
        private void ShowPersonDetails()
        {
            if (SelectedApplication == null)
                return;

            int personId = SelectedApplication.PersonID;

            var window = ActivatorUtilities.CreateInstance<PersonDetailsWindow>(
                _serviceProvider,
                personId);

            window.ShowDialog();
        }

        [RelayCommand]
        private void ShowLicenseDetails()
        {
            if (SelectedApplication == null)
                return;

            int licenseId = SelectedApplication.InternationalLicenseID;

            var window = ActivatorUtilities.CreateInstance<DriverInterNationalLicenseInfoWin>(
                _serviceProvider,
                licenseId);

            window.ShowDialog();
        }

        [RelayCommand]
        private void ShowPersonLicenseHistory()
        {
            if (SelectedApplication == null)
                return;

            int personId = SelectedApplication.PersonID;

            var window = ActivatorUtilities.CreateInstance<LicenseHistoryWin>(
                _serviceProvider,
                personId);

            window.ShowDialog();
        }
    }
}