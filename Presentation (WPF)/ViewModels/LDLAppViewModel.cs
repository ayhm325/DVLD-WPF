using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class LDLAppViewModel : ObservableObject
    {
        private readonly ILocalDrivingLicenseApplicationService _service;
        private readonly IApplicationService _appService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestAppointmentService _testAppointmentService;
        private readonly ILicenseService _licenseService;

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
        [NotifyPropertyChangedFor(nameof(CanScheduleTests))]
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
                               IServiceProvider serviceProvider, ITestAppointmentService testAppointmentService, ILicenseService licenseService)
        {
            _service = service;
            _appService = appService;
            _serviceProvider = serviceProvider;
            _testAppointmentService = testAppointmentService;
            _ = LoadApplicationsAsync();
            _licenseService = licenseService;
        }

        [RelayCommand]
        public async Task LoadApplicationsAsync()
        {
            var result = await _service.GetAllLocalDrivingLicenseApplicationsAsync();

            if (result.IsFailure)
            {
                _allApplications.Clear();
                return;
            }

            _allApplications = result.Value ?? new List<LocalDrivingLicenseApplicationListDto>();

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
                var result = await _service
                    .DeleteLocalDrivingLicenseApplicationAsync(localApplicationId);

                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error);
                    return;
                }

                await LoadApplicationsAsync();

                SelectedApplication = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
                var appIdResult = await _service.GetApplicationIdByLocalIdAsync(localApplicationId);
                if (appIdResult.IsFailure)
                {
                    MessageBox.Show(appIdResult.Error);
                    return;
                }
                int appId = appIdResult.Value;
                var result = await _appService.CancelApplicationAsync(appId);
                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error);
                    return;
                }

                await LoadApplicationsAsync();

                SelectedApplication = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"Error", MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        public bool CanScheduleTests => SelectedApplication != null &&
            SelectedApplication.StatusText == "New" && SelectedApplication.PassedTest < 3;

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
            //SelectedApplication = Applications
            //    .FirstOrDefault(x =>x.LocalDrivingLicenseApplicationID == currentAppId);

            // تحديث حالة الأزرار
            RefreshCommands();

            OnPropertyChanged(nameof(CanScheduleTests));
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
        private async Task ShowLicense()
        {
            if (SelectedApplication == null)
                return;


            var applicationIdResult = await _service
                .GetApplicationIdByLocalIdAsync(
                    SelectedApplication.LocalDrivingLicenseApplicationID);


            if (applicationIdResult.IsFailure)
            {
                MessageBox.Show(applicationIdResult.Error);
                return;
            }


            int applicationId = applicationIdResult.Value;


            var result = await _licenseService
                .GetByApplicationIdAsync(applicationId);


            if (result.IsFailure)
            {
                MessageBox.Show(result.Error);
                return;
            }


            var license = result.Value.FirstOrDefault();


            if (license == null)
            {
                MessageBox.Show("License not found");
                return;
            }


            var window = new DriverLicenseInfoWin(license.LicenseID);

            window.ShowDialog();
        }

        [RelayCommand]
        private async Task ShowHistory()
        {
            if (SelectedApplication == null)
                return;
            var vm = _serviceProvider.GetRequiredService<LicenseHistoryViewModel>();
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



