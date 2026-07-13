using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows;
using System;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestAppointmentService _testAppointmentService;

        private List<LocalDrivingLicenseApplicationListDto> _allApplications = new();
        public ObservableCollection<LocalDrivingLicenseApplicationListDto> Applications { get; set; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "Full Name";


        // قائمة الخيارات للـ ComboBox (تشمل "All" وجميع حالات الـ Enum)
        public List<string> StatusFilterOptions { get; } = new() { "All", "New", "Cancelled", "Completed" };

        [ObservableProperty]
        private string _selectedStatusFilter = "All"; // القيمة المختارة حالياً

        partial void OnSelectedStatusFilterChanged(string value) => FilterApplications();


        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditCommand),nameof(DeleteCommand), nameof(ShowDetailsCommand), nameof(CancelCommand),
                                    nameof(ScheduleVisionCommand), nameof(ScheduleWrittenCommand), nameof(ScheduleStreetCommand),
                                    nameof(IssueLicenseCommand), nameof(ShowLicenseCommand))]
        private LocalDrivingLicenseApplicationListDto? _selectedApplication;

        partial void OnSelectedApplicationChanged(LocalDrivingLicenseApplicationListDto? value)
        {
            RefreshCommands();
        }

        partial void OnSearchTextChanged(string value) => FilterApplications();

        private void RefreshCommands()
        {
            EditCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            ShowDetailsCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();

            ScheduleVisionCommand.NotifyCanExecuteChanged();
            ScheduleWrittenCommand.NotifyCanExecuteChanged();
            ScheduleStreetCommand.NotifyCanExecuteChanged();

            IssueLicenseCommand.NotifyCanExecuteChanged();
            ShowLicenseCommand.NotifyCanExecuteChanged();
        }

        public LDLAppViewModel(ILocalDrivingLicenseApplicationService service, IApplicationService appService,
                               IServiceProvider serviceProvider, ITestAppointmentService testAppointmentService)
        {
            _service = service;
            _appService = appService;
            _serviceProvider = serviceProvider;
            _testAppointmentService = testAppointmentService;
            _ = LoadApplicationsAsync();
        }

        [RelayCommand]
        public async Task LoadApplicationsAsync()
        {
            _allApplications = await _service.GetAllLocalDrivingLicenseApplicationsAsync();
            FilterApplications();

            RefreshCommands();
        }

        public void FilterApplications()
        {
            var filtered = _allApplications.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(x =>
                    (x.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.NationalNo?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (SelectedStatusFilter != "All" && Enum.TryParse<AppStatus>(SelectedStatusFilter, out var status))
            {
                filtered = filtered.Where(x => x.ApplicationStatus == status);
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

        // --- منطق الحذف ---
        private bool CanDelete() => SelectedApplication != null && SelectedApplication.StatusText != "Completed";
        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task Delete(int localApplicationId)
        {
            try
            {
                bool isDeleted = await _service.DeleteLocalDrivingLicenseApplicationAsync(localApplicationId);
                if (isDeleted) { await LoadApplicationsAsync(); SelectedApplication = null; }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        [RelayCommand]
        private async Task ShowDetails()
        {
            if (SelectedApplication == null) return;
            var vm = _serviceProvider.GetRequiredService<LocalApplicationDetailsViewModel>();
            await vm.LoadAsync(SelectedApplication.LocalDrivingLicenseApplicationID);
            var window = new LocalApplicationDetailsWin(vm) { Owner = System.Windows.Application.Current.MainWindow };
            window.ShowDialog();
        }

        private bool CanEdit() => SelectedApplication != null && SelectedApplication.StatusText == "New";
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void Edit()
        {
            // منطق التعديل الخاص بك هنا
        }

        // --- منطق الإلغاء ---
        private bool CanCancel() => SelectedApplication != null && SelectedApplication.StatusText != "Completed" && SelectedApplication.StatusText != "Cancelled";
        [RelayCommand(CanExecute = nameof(CanCancel))]
        private async Task Cancel(int localApplicationId)
        {
            try
            {
                var appId = await _service.GetApplicationIdByLocalIdAsync(localApplicationId);
                if (appId.HasValue && await _appService.CancelApplicationAsync(appId.Value))
                {
                    await LoadApplicationsAsync();
                    SelectedApplication = null;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // --- منطق الاختبارات (PassedTestCount يُفترض وجوده في DTO) ---
        private bool CanScheduleVision() => SelectedApplication != null && SelectedApplication.StatusText == "New" && SelectedApplication.PassedTest == 0;
        [RelayCommand(CanExecute = nameof(CanScheduleVision))]
        private async Task ScheduleVision() => await OpenTestAppointment(TestTypeEnum.Theory);

        private bool CanScheduleWritten() => SelectedApplication != null && SelectedApplication.StatusText == "New" && SelectedApplication.PassedTest == 1;
        [RelayCommand(CanExecute = nameof(CanScheduleWritten))]
        private async Task ScheduleWritten() => await OpenTestAppointment(TestTypeEnum.Written);

        private bool CanScheduleStreet() => SelectedApplication != null && SelectedApplication.StatusText == "New" && SelectedApplication.PassedTest == 2;
        [RelayCommand(CanExecute = nameof(CanScheduleStreet))]
        private async Task ScheduleStreet() => await OpenTestAppointment(TestTypeEnum.Practical);

        private async Task OpenTestAppointment(TestTypeEnum testType)
        {
            if (SelectedApplication == null)
                return;


            int currentAppId = SelectedApplication.LocalDrivingLicenseApplicationID;


            var vm = _serviceProvider.GetRequiredService<TestAppointmentViewModel>();

            await vm.LoadAsync(currentAppId, testType);


            var window = new TestAppointmentWin(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };


            window.ShowDialog();


            // إعادة تحميل البيانات بعد نجاح أو فشل الفحص
            await LoadApplicationsAsync();


            // إعادة تحديد العنصر مع البيانات الجديدة
            SelectedApplication = Applications
                .FirstOrDefault(x =>
                    x.LocalDrivingLicenseApplicationID == currentAppId);


            // تحديث حالة الأزرار
            RefreshCommands();
        }

        // --- منطق الإصدار والعرض ---
        private bool CanIssueLicense() => SelectedApplication != null && SelectedApplication.PassedTest == 3 && !SelectedApplication.HasLicense;
        [RelayCommand(CanExecute = nameof(CanIssueLicense))]
        private async Task IssueLicense()
        {
            var window = new IssueDrivingLicenseForTheFirstTimeWin(null!);

            var vm = ActivatorUtilities.CreateInstance<IssueDrivingLicenseForTheFirstTimeViewModel>(
                _serviceProvider,
                SelectedApplication!.LocalDrivingLicenseApplicationID,
                window);

            window.DataContext = vm;
            window.Owner = System.Windows.Application.Current.MainWindow;

            window.ShowDialog();


            int id = SelectedApplication.LocalDrivingLicenseApplicationID;

            await LoadApplicationsAsync();

            SelectedApplication = Applications
                .FirstOrDefault(x =>
                    x.LocalDrivingLicenseApplicationID == id);

            RefreshCommands();
        }

        private bool CanShowLicense() => SelectedApplication != null && SelectedApplication.HasLicense;
        [RelayCommand(CanExecute = nameof(CanShowLicense))]
        private void ShowLicense()
        {
            var window = new DriverLicenseInfoWin(SelectedApplication!.LocalDrivingLicenseApplicationID);
            window.ShowDialog();
        }

        [RelayCommand]
        private async Task ShowHistory()
        {
            if (SelectedApplication == null)
                return;


            var vm = _serviceProvider
                .GetRequiredService<LicenseHistoryViewModel>();


            int personId = SelectedApplication.ApplicantPersonID;


            await vm.LoadAsync(personId);


            var window = new LicenseHistoryWin(vm, personId)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };


            window.ShowDialog();
        }
    }
}





//using Application.DTOs;
//using Application.Interfaces;
//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using Domain.Enums;
//using DVLD_WPF;
//using Microsoft.Extensions.DependencyInjection;
//using Presentation.Views.Windows;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows;

//namespace Presentation.ViewModels
//{
//    public partial class LDLAppViewModel : ObservableObject
//    {
//        private readonly ILocalDrivingLicenseApplicationService _service;
//        private readonly IApplicationService _appService;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ITestAppointmentService _testAppointmentService;

//        private List<LocalDrivingLicenseApplicationListDto> _allApplications = new();

//        public ObservableCollection<LocalDrivingLicenseApplicationListDto> Applications { get; set; } = new();




//        [ObservableProperty]
//        private string _searchText = string.Empty;

//        [ObservableProperty]
//        private string _selectedFilter = "Full Name";

//        [ObservableProperty]
//        [NotifyCanExecuteChangedFor(nameof(DeleteCommand), nameof(ShowDetailsCommand), nameof(ScheduleVisionCommand), nameof(CancelCommand))]
//        private LocalDrivingLicenseApplicationListDto? _selectedApplication;



//        partial void OnSearchTextChanged(string value) => FilterApplications();

//        public LDLAppViewModel(ILocalDrivingLicenseApplicationService service, IApplicationService appService, IServiceProvider serviceProvider,
//            ITestAppointmentService testAppointmentService )
//        {
//            _service = service;
//            _appService = appService;
//            _serviceProvider = serviceProvider;
//            _testAppointmentService = testAppointmentService;

//            _ = LoadApplicationsAsync();

//        }

//        [RelayCommand]
//        public async Task LoadApplicationsAsync()
//        {
//            _allApplications = await _service.GetAllLocalDrivingLicenseApplicationsAsync();
//            FilterApplications();
//        }

//        public void FilterApplications()
//        {
//            var filtered = _allApplications.AsEnumerable();
//            if (!string.IsNullOrWhiteSpace(SearchText))
//            {
//                filtered = filtered.Where(x =>
//                    (x.FullName?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
//                    (x.NationalNo?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) ?? false));
//            }

//            Applications.Clear();
//            foreach (var item in filtered) Applications.Add(item);
//        }

//        [RelayCommand]
//        private void AddNew()
//        {
//            var addEditVm = App.ServiceProvider.GetRequiredService<AddEditLDLAppViewModel>();
//            var win = new NewLocalLicnnse(addEditVm) { Owner = System.Windows.Application.Current.MainWindow };
//            win.ShowDialog();
//            _ = LoadApplicationsAsync(); 
//        }


//        // دالة التحقق لعملية الحذف
//        private bool CanDelete() => SelectedApplication != null && SelectedApplication.StatusText != "Completed";
//        [RelayCommand(CanExecute = nameof(CanDelete))]
//        private async Task Delete(int localApplicationId)
//        {
//            //var appId = await _service.GetApplicationIdByLocalIdAsync(applicationId);           
//            try
//            {
//                //if (!appId.HasValue)
//                //    return;
//                bool isDeleted = await _service.DeleteLocalDrivingLicenseApplicationAsync(localApplicationId);
//                //bool isDeletedApp = await _appService.DeleteApplicationAsync(appId.Value);

//                if (isDeleted)//&& isDeletedApp
//                {
//                    await LoadApplicationsAsync();
//                    SelectedApplication = null; // تصفير الاختيار
//                }
//            }
//            catch (System.Exception ex)
//            {
//                //MessageBox.Show(ex.Message, $"Error {appId.Value}", MessageBoxButton.OK, MessageBoxImage.Error);
//                var message = ex.Message;
//                if (ex.InnerException != null)
//                {
//                    message += "\n\nInner Exception: " + ex.InnerException.Message;
//                }

//                MessageBox.Show(message, "Error Details", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        [RelayCommand]
//        private async Task ShowDetails()
//        {
//            if (SelectedApplication == null)
//                return;

//            var localId = SelectedApplication.LocalDrivingLicenseApplicationID;

//            var vm = _serviceProvider.GetRequiredService<LocalApplicationDetailsViewModel>();


//            await vm.LoadAsync(localId);

//            var window = new LocalApplicationDetailsWin(vm)
//            {
//                Owner = System.Windows.Application.Current.MainWindow
//            };

//            window.ShowDialog();
//        }

//        [RelayCommand]
//        private void Edit()
//        {
//            // منطق التعديل
//        }

//        // دالة التحقق لعملية الإلغاء
//        private bool CanCancel()=> SelectedApplication != null && SelectedApplication.StatusText != "Completed" && SelectedApplication.StatusText != "Cancelled";
//        [RelayCommand(CanExecute = nameof(CanCancel))]
//        private async Task Cancel(int localApplicationId)
//        {
//            try
//            {
//                var appId = await _service.GetApplicationIdByLocalIdAsync(localApplicationId);

//                if (!appId.HasValue)
//                    return;

//                bool isCancelled = await _appService.CancelApplicationAsync(appId.Value);

//                if (isCancelled)
//                {
//                    await LoadApplicationsAsync();
//                    SelectedApplication = null;
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }

//        }

//        [RelayCommand]
//        private async Task ScheduleVision()
//        {
//            if (SelectedApplication == null)
//                return;

//            var localId = SelectedApplication.LocalDrivingLicenseApplicationID;

//            var vm = _serviceProvider.GetRequiredService<TestAppointmentViewModel>();

//            await vm.LoadAsync(localId, TestTypeEnum.Theory);

//            var window = new TestAppointmentWin(vm)
//            {
//                Owner = System.Windows.Application.Current.MainWindow
//            };

//            window.ShowDialog();
//            await LoadApplicationsAsync();
//        }

//        [RelayCommand]
//        private async Task ScheduleWritten()
//        {
//            if (SelectedApplication == null)
//                return;

//            var localId = SelectedApplication.LocalDrivingLicenseApplicationID;

//            var vm = _serviceProvider.GetRequiredService<TestAppointmentViewModel>();

//            await vm.LoadAsync(localId, TestTypeEnum.Written);

//            var window = new TestAppointmentWin(vm)
//            {
//                Owner = System.Windows.Application.Current.MainWindow
//            };

//            window.ShowDialog();
//            await LoadApplicationsAsync();
//        }

//        [RelayCommand]
//        private async Task ScheduleStreet()
//        {
//            if (SelectedApplication == null)
//                return;

//            var localId = SelectedApplication.LocalDrivingLicenseApplicationID;

//            var vm = _serviceProvider.GetRequiredService<TestAppointmentViewModel>();

//            await vm.LoadAsync(localId, TestTypeEnum.Practical);

//            var window = new TestAppointmentWin(vm)
//            {
//                Owner = System.Windows.Application.Current.MainWindow
//            };

//            window.ShowDialog();
//            await LoadApplicationsAsync();
//        }

//        [RelayCommand]
//        private void IssueLicense()
//        {
//            if (SelectedApplication == null)
//                return;

//            var window = new IssueDrivingLicenseForTheFirstTimeWin(null!);

//            var vm = ActivatorUtilities.CreateInstance<IssueDrivingLicenseForTheFirstTimeViewModel>(
//                _serviceProvider,
//                SelectedApplication.LocalDrivingLicenseApplicationID,
//                window);

//            window.DataContext = vm;
//            window.Owner = System.Windows.Application.Current.MainWindow;
//            window.ShowDialog();
//        }

//        [RelayCommand]
//        private void ShowLicense()
//        {
//            if (SelectedApplication == null)
//                return;

//            var window = new DriverLicenseInfoWin(SelectedApplication.LocalDrivingLicenseApplicationID);

//            window.ShowDialog();
//        }

//        [RelayCommand]
//        private void ShowHistory()
//        {
//            // منطق عرض السجل
//        }




//    }
//}