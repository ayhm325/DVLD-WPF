using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
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
        private readonly ILocalDrivingLicenseApplicationService _localDrivingLicenseApplicationService;
        private readonly IApplicationService _applicationService;

        public TestAppointmentViewModel(
            ITestAppointmentService testAppointmentService,
            ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
            IApplicationService applicationService,
            IServiceProvider serviceProvider)
        {
            _testAppointmentService = testAppointmentService ?? throw new ArgumentNullException(nameof(testAppointmentService));
            _localDrivingLicenseApplicationService = localDrivingLicenseApplicationService ?? throw new ArgumentNullException(nameof(localDrivingLicenseApplicationService));
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        // Properties
        [ObservableProperty]
        private TestTypeEnum _testType;

        [ObservableProperty]
        private LocalDrivingLicenseApplicationListDto? _ldlAppInfo;

        [ObservableProperty]
        private ApplicationBasicInfoDto? _applicationInfo;

        public ObservableCollection<TestAppointmentDto> AppointmentsList { get; } = [];

        [ObservableProperty]
        private TestAppointmentDto? _selectedAppointment;

        [ObservableProperty]
        private bool _canAddAppointment;

        // UI Text
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

        // Load
        public async Task LoadAsync(int localApplicationId, TestTypeEnum type)
        {
            TestType = type;
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(PageDescription));

            // Validate
            if (localApplicationId <= 0)
            {
                ResetState();
                MessageBox.Show("Invalid local driving license application ID.", "Invalid Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Load LDL App
            var ldlResult = await _localDrivingLicenseApplicationService.GetLocalDrivingLicenseApplicationByIdAsync(localApplicationId);
            if (ldlResult.IsFailure)
            {
                ResetState();
                MessageBox.Show(ldlResult.Error, "Application Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            LdlAppInfo = ldlResult.Value;

            // Get Application ID
            var applicationIdResult = await _localDrivingLicenseApplicationService.GetApplicationIdByLocalIdAsync(localApplicationId);
            if (applicationIdResult.IsFailure)
            {
                ApplicationInfo = null;
                AppointmentsList.Clear();
                CanAddAppointment = false;
                AddAppointmentCommand.NotifyCanExecuteChanged();
                MessageBox.Show(applicationIdResult.Error, "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Load basic app info
            var applicationResult = await _applicationService.GetBasicInfoAsync(applicationIdResult.Value);
            ApplicationInfo = applicationResult.IsSuccess ? applicationResult.Value : null;

            // Load appointments
            var appointmentsResult = await _testAppointmentService.GetByApplicationIdAsync(localApplicationId);
            AppointmentsList.Clear();
            if (appointmentsResult.IsFailure)
            {
                CanAddAppointment = false;
                AddAppointmentCommand.NotifyCanExecuteChanged();
                MessageBox.Show(appointmentsResult.Error, "Appointments Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var filteredAppointments = appointmentsResult.Value?.Where(x => x.TestTypeID == (int)TestType).ToList() ?? [];
            foreach (var appointment in filteredAppointments)
                AppointmentsList.Add(appointment);

            // Check if can add new appointment
            CanAddAppointment = !await _testAppointmentService.IsAppointmentAlreadyScheduledAsync(localApplicationId, (int)TestType);
            AddAppointmentCommand.NotifyCanExecuteChanged();
            EditAppointmentCommand.NotifyCanExecuteChanged();
            TakeTestCommand.NotifyCanExecuteChanged();
        }

        private void ResetState()
        {
            LdlAppInfo = null;
            ApplicationInfo = null;
            AppointmentsList.Clear();
            CanAddAppointment = false;
            AddAppointmentCommand.NotifyCanExecuteChanged();
        }

        // Add
        [RelayCommand(CanExecute = nameof(CanAddAppointment))]
        private async Task AddAppointment()
        {
            if (LdlAppInfo is null) return;

            var vm = _serviceProvider.GetRequiredService<ScheduleTestViewModel>();
            await vm.LoadAsync(LdlAppInfo.LocalDrivingLicenseApplicationID, TestType);

            var window = new ScheduleTestWin(vm) { Owner = System.Windows.Application.Current.MainWindow };
            window.ShowDialog();

            await LoadAsync(LdlAppInfo.LocalDrivingLicenseApplicationID, TestType);
        }

        // Edit
        private bool CanEditAppointment() => SelectedAppointment is not null && !SelectedAppointment.IsLocked;

        [RelayCommand(CanExecute = nameof(CanEditAppointment))]
        private async Task EditAppointment()
        {
            if (SelectedAppointment is null || LdlAppInfo is null) return;

            var vm = _serviceProvider.GetRequiredService<ScheduleTestViewModel>();
            await vm.LoadForEditAsync(SelectedAppointment.TestAppointmentID);

            var window = new ScheduleTestWin(vm) { Owner = System.Windows.Application.Current.MainWindow };
            window.ShowDialog();

            await LoadAsync(LdlAppInfo.LocalDrivingLicenseApplicationID, TestType);
        }

        // Take Test
        private bool CanTakeTest() => SelectedAppointment is not null && !SelectedAppointment.IsLocked;

        [RelayCommand(CanExecute = nameof(CanTakeTest))]
        private async Task TakeTest()
        {
            if (SelectedAppointment is null || LdlAppInfo is null) return;

            var vm = _serviceProvider.GetRequiredService<TakeTestViewModel>();
            await vm.LoadAsync(SelectedAppointment.TestAppointmentID);

            var window = new TakeTestWin(vm) { Owner = System.Windows.Application.Current.MainWindow };
            window.ShowDialog();

            await LoadAsync(LdlAppInfo.LocalDrivingLicenseApplicationID, TestType);
        }

        // Partial Methods
        partial void OnSelectedAppointmentChanged(TestAppointmentDto? value)
        {
            EditAppointmentCommand.NotifyCanExecuteChanged();
            TakeTestCommand.NotifyCanExecuteChanged();
        }
    }
}