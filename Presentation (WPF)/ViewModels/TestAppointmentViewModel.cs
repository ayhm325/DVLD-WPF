using Application.DTOs;
using Application.Interfaces;
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
    public partial class TestAppointmentViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestAppointmentService _testAppointmentService;
        private readonly ILocalDrivingLicenseApplicationService _lDLAppService;
        private readonly IApplicationService _appService;

        public TestTypeEnum TestType { get; set; }

        public TestAppointmentViewModel(
            ITestAppointmentService testAppointmentService,
            ILocalDrivingLicenseApplicationService lDLAppService,
            IApplicationService appService,
            IServiceProvider serviceProvider)
        {
            _testAppointmentService = testAppointmentService;
            _lDLAppService = lDLAppService;
            _appService = appService;
            _serviceProvider = serviceProvider;
        }

        // =========================
        // HEADER DATA
        // =========================

        [ObservableProperty]
        private LocalDrivingLicenseApplicationListDto? ldlAppInfo;

        [ObservableProperty]
        private ApplicationBasicInfoDto? applicationInfo;

        // =========================
        // GRID DATA
        // =========================

        public ObservableCollection<TestAppointmentDto> AppointmentsList { get; set; }
            = new();

        [ObservableProperty]
        private TestAppointmentDto? selectedAppointment;

        [ObservableProperty]
        private bool canAddAppointment;

        // =========================
        // LOAD
        // =========================
        public async Task LoadAsync(int localApplicationId, TestTypeEnum type)
        {
            TestType = type;

            var ldlResult = await _lDLAppService
                .GetLocalDrivingLicenseApplicationByIdAsync(localApplicationId);


            if (ldlResult.IsFailure)
            {
                LdlAppInfo = null;
                MessageBox.Show(ldlResult.Error);
                return;
            }


            LdlAppInfo = ldlResult.Value;


            var appIdResult = await _lDLAppService
                .GetApplicationIdByLocalIdAsync(localApplicationId);


            if (appIdResult.IsFailure)
            {
                ApplicationInfo = null;
                MessageBox.Show(appIdResult.Error);
                return;
            }


            var result = await _appService
                .GetBasicInfoAsync(appIdResult.Value);


            ApplicationInfo = result.IsSuccess
                ? result.Value
                : null;


            var appointmentsResult = await _testAppointmentService
    .GetByApplicationIdAsync(localApplicationId);

            if (appointmentsResult.IsFailure)
            {
                AppointmentsList.Clear();
                MessageBox.Show(appointmentsResult.Error);
                return;
            }

            var data = appointmentsResult.Value!;

            var filtered = data
                .Where(x => x.TestTypeID == (int)type)
                .ToList();


            AppointmentsList.Clear();

            foreach (var item in filtered)
            {
                AppointmentsList.Add(item);
            }


            AppointmentsList.Clear();


            foreach (var item in filtered)
            {
                AppointmentsList.Add(item);
            }


            CanAddAppointment =
                !await _testAppointmentService
                .IsAppointmentAlreadyScheduledAsync(
                    localApplicationId,
                    (int)type);


            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageDescription));
        }

        // =========================
        // UI TEXT
        // =========================

        public string PageTitle => TestType switch
        {
            TestTypeEnum.Theory => "Vision Test Appointments",
            TestTypeEnum.Written => "Written Test Appointments",
            TestTypeEnum.Practical => "Practical Test Appointments",
            _ => "Test Appointments"
        };

        public string PageDescription => TestType switch
        {
            TestTypeEnum.Theory => "Manage vision test appointments for this application.",
            TestTypeEnum.Written => "Manage written test appointments for this application.",
            TestTypeEnum.Practical => "Manage practical test appointments for this application.",
            _ => "Manage test appointments for this application."
        };

        // =========================
        // COMMANDS
        // =========================

        [RelayCommand]
        private async Task AddAppointment()
        {
            var vm = _serviceProvider.GetRequiredService<ScheduleTestViewModel>();

            await vm.LoadAsync(LdlAppInfo!.LocalDrivingLicenseApplicationID, TestType);

            new ScheduleTestWin(vm)
            {
                Owner = App.Current.MainWindow
            }.ShowDialog();

            await LoadAsync(LdlAppInfo!.LocalDrivingLicenseApplicationID, TestType);
        }

        private bool CanEditAppointment()
        {
            return SelectedAppointment != null && !SelectedAppointment.IsLocked;
        }

        private bool CanTakeTest()
        {
            return SelectedAppointment != null && !SelectedAppointment.IsLocked;
        }

        [RelayCommand(CanExecute = nameof(CanEditAppointment))]
        private async Task EditAppointment()
        {
            if (SelectedAppointment is null)
                return;

            var vm = _serviceProvider.GetRequiredService<ScheduleTestViewModel>();

            await vm.LoadForEditAsync(SelectedAppointment.TestAppointmentID);

            new ScheduleTestWin(vm)
            {
                Owner = App.Current.MainWindow
            }.ShowDialog();

            await LoadAsync(LdlAppInfo!.LocalDrivingLicenseApplicationID, TestType);
        }

        [RelayCommand(CanExecute = nameof(CanTakeTest))]
        private async Task TakeTest()
        {
            if (SelectedAppointment is null)
                return;

            var vm = _serviceProvider.GetRequiredService<TakeTestViewModel>();

            await vm.LoadAsync(SelectedAppointment.TestAppointmentID);

            new TakeTestWin(vm)
            {
                Owner = App.Current.MainWindow
            }.ShowDialog();

            await LoadAsync(LdlAppInfo!.LocalDrivingLicenseApplicationID, TestType);
        }

        partial void OnSelectedAppointmentChanged(TestAppointmentDto? value)
        {
            EditAppointmentCommand.NotifyCanExecuteChanged();
            TakeTestCommand.NotifyCanExecuteChanged();
        }
    }
}