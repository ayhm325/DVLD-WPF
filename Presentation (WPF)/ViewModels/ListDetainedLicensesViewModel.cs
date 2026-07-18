using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class ListDetainedLicensesViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDetainedLicenseService _detainedLicenseService;

        private List<DetainedLicenseDto> _allDetainedLicenses = new();

        public ObservableCollection<DetainedLicenseDto> DetainedLicenses { get; }= new();

        [ObservableProperty]
        private DetainedLicenseDto? selectedDetainedLicense;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string selectedFilter = "None";

        public ObservableCollection<string> FilterOptions { get; } =
        [
            "None",
            "Detain ID",
            "License ID",
            "National No",
            "Full Name",
            "Released"
        ];

        public ObservableCollection<string> ReleaseFilterOptions { get; } =
        [
            "All",
            "Released",
            "Not Released"
        ];

        [ObservableProperty]
        private string selectedReleaseFilter = "All";

        public bool IsSearchVisible => SelectedFilter != "None" &&SelectedFilter != "Released";

        public bool IsReleaseFilterVisible => SelectedFilter == "Released";

        public ListDetainedLicensesViewModel(
            IDetainedLicenseService detainedLicenseService,
            IServiceProvider serviceProvider)
        {
            _detainedLicenseService = detainedLicenseService;
            _serviceProvider = serviceProvider;
        }

        public async Task LoadAsync()
        {
            var list = await _detainedLicenseService.GetAllAsync();

            _allDetainedLicenses = list;

            ApplyFilter();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        partial void OnSelectedFilterChanged(string value)
        {
            OnPropertyChanged(nameof(IsSearchVisible));
            OnPropertyChanged(nameof(IsReleaseFilterVisible));
            ApplyFilter();
        }

        partial void OnSelectedReleaseFilterChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<DetainedLicenseDto> query = _allDetainedLicenses;

            if (SelectedFilter == "Released")
            {
                query = SelectedReleaseFilter switch
                {
                    "Released" => query.Where(x => x.IsReleased),

                    "Not Released" =>query.Where(x => !x.IsReleased),

                    _ => query
                };
            }

            else if (!string.IsNullOrWhiteSpace(SearchText)&& SelectedFilter != "None")
            {
                string text = SearchText.Trim().ToLower();

                query = SelectedFilter switch
                {
                    "Detain ID" =>
                        query.Where(x =>
                            x.DetainID
                            .ToString()
                            .Contains(text)),

                    "License ID" =>
                        query.Where(x =>
                            x.LicenseID
                            .ToString()
                            .Contains(text)),

                    "National No" =>
                        query.Where(x =>
                            !string.IsNullOrEmpty(x.NationalNo) &&
                            x.NationalNo.ToLower()
                            .Contains(text)),

                    "Full Name" =>
                        query.Where(x =>
                            !string.IsNullOrEmpty(x.FullName) &&
                            x.FullName.ToLower()
                            .Contains(text)),

                    _ => query
                };
            }
            DetainedLicenses.Clear();
            foreach (var item in query)
            {
                DetainedLicenses.Add(item);
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private void ShowPersonDetails()
        {
            if (SelectedDetainedLicense == null)
                return;
            var window = new PersonDetailsWindow(SelectedDetainedLicense.ApplicantPersonID)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private void ShowLicenseDetails()
        {
            if (SelectedDetainedLicense == null)
                return;
            var window = new DriverLicenseInfoWin(SelectedDetainedLicense.LicenseID)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private async Task ShowPersonLicenseHistory()
        {
            if (SelectedDetainedLicense == null)
                return;
            int personId = SelectedDetainedLicense.ApplicantPersonID;
            var vm = _serviceProvider.GetRequiredService<LicenseHistoryViewModel>();
            await vm.LoadAsync(personId);
            var window = new LicenseHistoryWin(vm, personId)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private async Task ReleaseDetainedLicenseAsync()
        {
            var window = App.ServiceProvider.GetRequiredService<ReleaseDetainedLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
            await LoadAsync();
        }

        [RelayCommand]
        private async Task Detain()
        {
            var window = App.ServiceProvider.GetRequiredService<DetainLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
            await LoadAsync();
        }

        [RelayCommand]
        private async Task ReleaseDetain()
        {
            var window = App.ServiceProvider.GetRequiredService<ReleaseDetainedLicenseWin>();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
            await LoadAsync();
        }
    }
}