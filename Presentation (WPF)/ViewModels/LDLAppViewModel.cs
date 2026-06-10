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
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class LDLAppViewModel : ObservableObject
    {
        private readonly ILocalDrivingLicenseApplicationService _service;
        private readonly IApplicationService _appService;
        private List<LocalDrivingLicenseApplicationDto> _allApplications = new();

        public ObservableCollection<LocalDrivingLicenseApplicationDto> Applications { get; set; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "Full Name";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand), nameof(ShowDetailsCommand), nameof(ScheduleVisionCommand), nameof(CancelCommand))]
        private LocalDrivingLicenseApplicationDto? _selectedApplication;

        partial void OnSearchTextChanged(string value) => FilterApplications();

        public LDLAppViewModel(ILocalDrivingLicenseApplicationService service, IApplicationService appService)
        {
            _service = service;
            _appService = appService;
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
            var win = new NewLocalLicnnse(addEditVm) { Owner = System.Windows.Application.Current.MainWindow };
            win.ShowDialog();
            _ = LoadApplicationsAsync(); 
        }

        // دالة التحقق لعملية الحذف
        private bool CanDelete() => SelectedApplication != null && SelectedApplication.StatusText != "Completed";
        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task Delete(int localApplicationId)
        {
            //var appId = await _service.GetApplicationIdByLocalIdAsync(applicationId);           
            try
            {
                //if (!appId.HasValue)
                //    return;
                bool isDeleted = await _service.DeleteLocalDrivingLicenseApplicationAsync(localApplicationId);
                //bool isDeletedApp = await _appService.DeleteApplicationAsync(appId.Value);

                if (isDeleted)//&& isDeletedApp
                {
                    await LoadApplicationsAsync();
                    SelectedApplication = null; // تصفير الاختيار
                }
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show(ex.Message, $"Error {appId.Value}", MessageBoxButton.OK, MessageBoxImage.Error);
                var message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += "\n\nInner Exception: " + ex.InnerException.Message;
                }

                MessageBox.Show(message, "Error Details", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowDetails()
        {
            // منطق فتح نافذة التفاصيل
        }

        [RelayCommand]
        private void Edit()
        {
            // منطق التعديل
        }

        // دالة التحقق لعملية الإلغاء
        private bool CanCancel()=> SelectedApplication != null && SelectedApplication.StatusText != "Completed" && SelectedApplication.StatusText != "Cancelled";
        [RelayCommand(CanExecute = nameof(CanCancel))]
        private async Task Cancel(int localApplicationId)
        {
            try
            {
                var appId = await _service.GetApplicationIdByLocalIdAsync(localApplicationId);

                if (!appId.HasValue)
                    return;

                bool isCancelled = await _appService.CancelApplicationAsync(appId.Value);

                if (isCancelled)
                {
                    await LoadApplicationsAsync();
                    SelectedApplication = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        [RelayCommand]
        private void ScheduleVision()
        {
            // منطق جدولة اختبار الرؤية
        }

        [RelayCommand]
        private void ScheduleWritten()
        {
            // منطق جدولة الاختبار الكتابي
        }

        [RelayCommand]
        private void ScheduleStreet()
        {
            // منطق جدولة اختبار الشارع
        }

        [RelayCommand]
        private void IssueLicense()
        {
            // منطق إصدار الرخصة
        }

        [RelayCommand]
        private void ShowLicense()
        {
            // منطق عرض الرخصة
        }

        [RelayCommand]
        private void ShowHistory()
        {
            // منطق عرض السجل
        }
    }
}