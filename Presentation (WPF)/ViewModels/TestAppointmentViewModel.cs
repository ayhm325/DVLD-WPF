
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
        private readonly ITestWorkflowService _testWorkflowService;
        private readonly ILocalDrivingLicenseApplicationService _localDrivingLicenseApplicationService;
        private readonly IApplicationService _applicationService;

        private int _localApplicationId;

        public TestAppointmentViewModel(
            ITestAppointmentService testAppointmentService,
            ITestWorkflowService testWorkflowService,
            ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
            IApplicationService applicationService,
            IServiceProvider serviceProvider)
        {
            _testAppointmentService =
                testAppointmentService
                ?? throw new ArgumentNullException(nameof(testAppointmentService));

            _testWorkflowService =
                testWorkflowService
                ?? throw new ArgumentNullException(nameof(testWorkflowService));

            _localDrivingLicenseApplicationService =
                localDrivingLicenseApplicationService
                ?? throw new ArgumentNullException(nameof(localDrivingLicenseApplicationService));

            _applicationService =
                applicationService
                ?? throw new ArgumentNullException(nameof(applicationService));

            _serviceProvider =
                serviceProvider
                ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        // =========================================================
        // STATE
        // =========================================================

        [ObservableProperty]
        private TestTypeEnum testType;

        [ObservableProperty]
        private LocalDrivingLicenseApplicationListDto? ldlAppInfo;

        [ObservableProperty]
        private ApplicationBasicInfoDto? applicationInfo;

        [ObservableProperty]
        private TestAppointmentDto? selectedAppointment;

        [ObservableProperty]
        private bool canAddAppointment;

        [ObservableProperty]
        private bool canTakeTest;

        [ObservableProperty]
        private bool canEditAppointment;

        [ObservableProperty]
        private bool isWorkflowAllowed;

        [ObservableProperty]
        private string workflowMessage = string.Empty;

        public ObservableCollection<TestAppointmentDto> AppointmentsList { get; }
            = new();

        // =========================================================
        // UI TEXT
        // =========================================================

        public string PageTitle =>
            TestType switch
            {
                TestTypeEnum.Theory =>
                    "Theory Test Appointments",

                TestTypeEnum.Written =>
                    "Written Test Appointments",

                TestTypeEnum.Practical =>
                    "Practical Test Appointments",

                _ =>
                    "Test Appointments"
            };

        public string PageDescription =>
            TestType switch
            {
                TestTypeEnum.Theory =>
                    "Manage theory test appointments for this application.",

                TestTypeEnum.Written =>
                    "Manage written test appointments for this application.",

                TestTypeEnum.Practical =>
                    "Manage practical test appointments for this application.",

                _ =>
                    "Manage test appointments for this application."
            };

        // =========================================================
        // LOAD
        // =========================================================

        public async Task LoadAsync(
            int localApplicationId,
            TestTypeEnum type)
        {
            try
            {
                _localApplicationId = localApplicationId;

                TestType = type;

                OnPropertyChanged(nameof(PageTitle));
                OnPropertyChanged(nameof(PageDescription));

                ResetState();

                // -------------------------------------------------
                // Validate ID
                // -------------------------------------------------

                if (localApplicationId <= 0)
                {
                    WorkflowMessage =
                        "Invalid local driving license application ID.";

                    MessageBox.Show(
                        WorkflowMessage,
                        "Invalid Data",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // -------------------------------------------------
                // Load Local Driving License Application
                // -------------------------------------------------

                var ldlResult =
                    await _localDrivingLicenseApplicationService
                        .GetLocalDrivingLicenseApplicationByIdAsync(
                            localApplicationId);

                if (ldlResult.IsFailure)
                {
                    WorkflowMessage = ldlResult.Error;

                    MessageBox.Show(
                        ldlResult.Error,
                        "Application Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                LdlAppInfo = ldlResult.Value;

                // -------------------------------------------------
                // Get Application ID
                // -------------------------------------------------

                var applicationIdResult =
                    await _localDrivingLicenseApplicationService
                        .GetApplicationIdByLocalIdAsync(
                            localApplicationId);

                if (applicationIdResult.IsFailure)
                {
                    WorkflowMessage =
                        applicationIdResult.Error;

                    MessageBox.Show(
                        applicationIdResult.Error,
                        "Application Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                int applicationId =
                    applicationIdResult.Value;

                // -------------------------------------------------
                // Load Basic Application Info
                // -------------------------------------------------

                var applicationResult =
                    await _applicationService
                        .GetBasicInfoAsync(applicationId);

                if (applicationResult.IsSuccess)
                {
                    ApplicationInfo =
                        applicationResult.Value;
                }

                // -------------------------------------------------
                // CHECK WORKFLOW
                //
                // Theory -> Written -> Practical
                // -------------------------------------------------

                var workflowResult =
                    await _testWorkflowService
                        .CanScheduleTestAsync(
                            localApplicationId,
                            TestType);

                if (workflowResult.IsFailure)
                {
                    IsWorkflowAllowed = false;

                    WorkflowMessage =
                        workflowResult.Error;

                    await LoadAppointmentsAsync();

                    RefreshCommands();

                    return;
                }

                IsWorkflowAllowed = true;
                WorkflowMessage = string.Empty;

                // -------------------------------------------------
                // Load Appointments
                // -------------------------------------------------

                await LoadAppointmentsAsync();

                // -------------------------------------------------
                // Determine whether new appointment can be added
                // -------------------------------------------------

                await RefreshAppointmentStateAsync();

                RefreshCommands();
            }
            catch (Exception ex)
            {
                ResetState();

                MessageBox.Show(
                    ex.Message,
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // LOAD APPOINTMENTS
        // =========================================================

        private async Task LoadAppointmentsAsync()
        {
            AppointmentsList.Clear();

            var result =
                await _testAppointmentService
                    .GetByApplicationIdAsync(
                        _localApplicationId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Appointments Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var appointments =
                result.Value?
                    .Where(x =>
                        x.TestTypeID ==
                        (int)TestType)
                    .OrderByDescending(x =>
                        x.AppointmentDate)
                    .ToList()
                ?? new List<TestAppointmentDto>();

            foreach (var appointment in appointments)
            {
                AppointmentsList.Add(appointment);
            }
        }

        // =========================================================
        // REFRESH STATE
        // =========================================================

        private async Task RefreshAppointmentStateAsync()
        {
            CanAddAppointment = false;

            if (!IsWorkflowAllowed)
                return;

            var alreadyScheduled =
                await _testAppointmentService
                    .IsAppointmentAlreadyScheduledAsync(
                        _localApplicationId,
                        (int)TestType);

            CanAddAppointment =
                !alreadyScheduled;
        }

        // =========================================================
        // RESET
        // =========================================================

        private void ResetState()
        {
            LdlAppInfo = null;
            ApplicationInfo = null;
            SelectedAppointment = null;

            AppointmentsList.Clear();

            CanAddAppointment = false;
            CanTakeTest = false;
            CanEditAppointment = false;

            IsWorkflowAllowed = false;

            WorkflowMessage = string.Empty;

            RefreshCommands();
        }

        // =========================================================
        // ADD APPOINTMENT
        // =========================================================

        [RelayCommand(CanExecute = nameof(CanAddAppointment))]
        private async Task AddAppointmentAsync()
        {
            if (LdlAppInfo is null)
                return;

            // Safety check.
            // The workflow service is the final authority.
            var workflowResult =
                await _testWorkflowService
                    .CanScheduleTestAsync(
                        LdlAppInfo.LocalDrivingLicenseApplicationID,
                        TestType);

            if (workflowResult.IsFailure)
            {
                MessageBox.Show(
                    workflowResult.Error,
                    "Cannot Schedule Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var vm =
                _serviceProvider
                    .GetRequiredService<ScheduleTestViewModel>();

            await vm.LoadAsync(
                LdlAppInfo.LocalDrivingLicenseApplicationID,
                TestType);

            var window =
                new ScheduleTestWin(vm)
                {
                    Owner =
                        System.Windows.Application.Current.MainWindow
                };

            window.ShowDialog();

            await LoadAsync(
                LdlAppInfo.LocalDrivingLicenseApplicationID,
                TestType);
        }

        // =========================================================
        // EDIT APPOINTMENT
        // =========================================================

        [RelayCommand(CanExecute = nameof(CanEditAppointment))]
        private async Task EditAppointmentAsync()
        {
            if (SelectedAppointment is null ||
                LdlAppInfo is null)
            {
                return;
            }

            if (SelectedAppointment.IsLocked)
            {
                MessageBox.Show(
                    "This appointment is locked and cannot be modified.",
                    "Edit Appointment",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var vm =
                _serviceProvider
                    .GetRequiredService<ScheduleTestViewModel>();

            await vm.LoadForEditAsync(
                SelectedAppointment.TestAppointmentID);

            var window =
                new ScheduleTestWin(vm)
                {
                    Owner =
                        System.Windows.Application.Current.MainWindow
                };

            window.ShowDialog();

            await LoadAsync(
                LdlAppInfo.LocalDrivingLicenseApplicationID,
                TestType);
        }

        // =========================================================
        // TAKE TEST
        // =========================================================

        [RelayCommand(CanExecute = nameof(CanTakeTest))]
        private async Task TakeTestAsync()
        {
            if (SelectedAppointment is null ||
                LdlAppInfo is null)
            {
                return;
            }

            if (SelectedAppointment.IsLocked)
            {
                MessageBox.Show(
                    "This appointment is already locked.",
                    "Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Workflow validation before taking the test.
            var workflowResult =
                await _testWorkflowService
                    .CanTakeTestAsync(SelectedAppointment.TestAppointmentID);

            if (workflowResult.IsFailure)
            {
                MessageBox.Show(
                    workflowResult.Error,
                    "Cannot Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var vm =
                _serviceProvider
                    .GetRequiredService<TakeTestViewModel>();

            await vm.LoadAsync(
                SelectedAppointment.TestAppointmentID);

            var window =
                new TakeTestWin(vm)
                {
                    Owner =
                        System.Windows.Application.Current.MainWindow
                };

            window.ShowDialog();

            await LoadAsync(
                LdlAppInfo.LocalDrivingLicenseApplicationID,
                TestType);
        }

        // =========================================================
        // SELECTED APPOINTMENT CHANGED
        // =========================================================

        partial void OnSelectedAppointmentChanged(
            TestAppointmentDto? value)
        {
            UpdateSelectedAppointmentState();
        }

        // =========================================================
        // SELECTED APPOINTMENT STATE
        // =========================================================

        private void UpdateSelectedAppointmentState()
        {
            if (SelectedAppointment is null)
            {
                CanEditAppointment = false;
                CanTakeTest = false;

                RefreshCommands();

                return;
            }

            CanEditAppointment =
                !SelectedAppointment.IsLocked;

            CanTakeTest =
                !SelectedAppointment.IsLocked;

            RefreshCommands();
        }

        // =========================================================
        // COMMAND REFRESH
        // =========================================================

        private void RefreshCommands()
        {
            AddAppointmentCommand
                .NotifyCanExecuteChanged();

            EditAppointmentCommand
                .NotifyCanExecuteChanged();

            TakeTestCommand
                .NotifyCanExecuteChanged();
        }
    }
}