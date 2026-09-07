using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using DVLD.Contracts.Application;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.TestAppointment;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class TestAppointmentViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITestAppointmentsApiClient _testAppointmentsApiClient;
    private readonly ITestWorkflowApiClient _testWorkflowApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly IApplicationsApiClient _applicationsApiClient;

    private int _localApplicationId;

    public TestAppointmentViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        ITestWorkflowApiClient testWorkflowApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IApplicationsApiClient applicationsApiClient,
        IServiceProvider serviceProvider)
    {
        _testAppointmentsApiClient =
            testAppointmentsApiClient
            ?? throw new ArgumentNullException(nameof(testAppointmentsApiClient));

        _testWorkflowApiClient =
            testWorkflowApiClient
            ?? throw new ArgumentNullException(nameof(testWorkflowApiClient));

        _localApplicationsApiClient =
            localApplicationsApiClient
            ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));

        _applicationsApiClient =
            applicationsApiClient
            ?? throw new ArgumentNullException(nameof(applicationsApiClient));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    // ===== STATE =====

    [ObservableProperty]
    private TestTypeEnum testType;

    [ObservableProperty]
    private LocalDrivingLicenseApplicationResponse? ldlAppInfo;

    [ObservableProperty]
    private ApplicationBasicInfoResponse? applicationInfo;

    [ObservableProperty]
    private TestAppointmentResponse? selectedAppointment;

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

    public ObservableCollection<TestAppointmentResponse> AppointmentsList { get; } = new();

    // ===== UI TEXT =====

    public string PageTitle => TestType switch
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

    public string PageDescription => TestType switch
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

    // ===== LOAD =====

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

            if (localApplicationId <= 0)
            {
                WorkflowMessage =
                    "Invalid local driving license application ID.";

                Show(
                    WorkflowMessage,
                    "Invalid Data",
                    MessageBoxImage.Warning);

                return;
            }

            var ldlResult =
                await _localApplicationsApiClient
                    .GetByIdAsync(localApplicationId);

            if (ldlResult.IsFailure ||
                ldlResult.Value is null)
            {
                WorkflowMessage =
                    ldlResult.IsFailure
                        ? ldlResult.Error
                        : "Local driving license application was not found.";

                Show(
                    WorkflowMessage,
                    "Application Not Found",
                    MessageBoxImage.Warning);

                return;
            }

            LdlAppInfo =
                ldlResult.Value;

            var applicationIdResult =
                await _localApplicationsApiClient
                    .GetApplicationIdAsync(localApplicationId);

            if (applicationIdResult.IsFailure)
            {
                WorkflowMessage =
                    applicationIdResult.Error;

                Show(
                    WorkflowMessage,
                    "Application Error",
                    MessageBoxImage.Warning);

                return;
            }

            var applicationResult =
                await _applicationsApiClient
                    .GetBasicInfoAsync(
                        applicationIdResult.Value);

            if (applicationResult.IsSuccess)
            {
                ApplicationInfo =
                    applicationResult.Value;
            }

            var workflowResult =
                await _testWorkflowApiClient
                    .CanScheduleAsync(
                        localApplicationId,
                        ToContractTestType(TestType));

            if (workflowResult.IsFailure ||
                workflowResult.Value is null ||
                !workflowResult.Value.Allowed)
            {
                IsWorkflowAllowed = false;

                WorkflowMessage =
                    workflowResult.IsFailure
                        ? workflowResult.Error
                        : workflowResult.Value?.Error
                          ?? "Test cannot be scheduled at this stage.";

                await LoadAppointmentsAsync();

                RefreshCommands();

                return;
            }

            IsWorkflowAllowed = true;
            WorkflowMessage = string.Empty;

            await LoadAppointmentsAsync();
            await RefreshAppointmentStateAsync();

            RefreshCommands();
        }
        catch (Exception ex)
        {
            ResetState();

            Show(
                ex.Message,
                "Loading Error",
                MessageBoxImage.Error);
        }
    }

    private async Task LoadAppointmentsAsync()
    {
        AppointmentsList.Clear();

        var result =
            await _testAppointmentsApiClient
                .GetByLocalApplicationIdAsync(
                    _localApplicationId);

        if (result.IsFailure)
        {
            Show(
                result.Error,
                "Appointments Error",
                MessageBoxImage.Warning);

            return;
        }

        var testTypeId =
            (int)TestType;

        var appointments =
            result.Value?
                .Where(x => x.TestTypeId == testTypeId)
                .OrderByDescending(x => x.AppointmentDate)
                .ToList()
            ?? [];

        foreach (var appointment in appointments)
            AppointmentsList.Add(appointment);
    }

    private async Task RefreshAppointmentStateAsync()
    {
        CanAddAppointment = false;

        if (!IsWorkflowAllowed)
            return;

        var result =
            await _testAppointmentsApiClient
                .IsAppointmentAlreadyScheduledAsync(
                    _localApplicationId,
                    (int)TestType);

        if (result.IsFailure)
        {
            CanAddAppointment = false;
            return;
        }

        CanAddAppointment = !result.Value;
    }

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

    // ===== ADD APPOINTMENT =====

    [RelayCommand(CanExecute = nameof(CanAddAppointment))]
    private async Task AddAppointmentAsync()
    {
        if (LdlAppInfo is null)
            return;

        var workflowResult =
            await _testWorkflowApiClient
                .CanScheduleAsync(
                    LdlAppInfo.LocalDrivingLicenseApplicationId,
                    ToContractTestType(TestType));

        if (workflowResult.IsFailure ||
            workflowResult.Value is null ||
            !workflowResult.Value.Allowed)
        {
            var error =
                workflowResult.IsFailure
                    ? workflowResult.Error
                    : workflowResult.Value?.Error
                      ?? "Test cannot be scheduled at this stage.";

            Show(
                error,
                "Cannot Schedule Test",
                MessageBoxImage.Warning);

            return;
        }

        var vm =
            _serviceProvider
                .GetRequiredService<ScheduleTestViewModel>();

        await vm.LoadAsync(
            LdlAppInfo.LocalDrivingLicenseApplicationId,
            TestType);

        OpenDialog(
            new ScheduleTestWin(vm));

        await LoadAsync(
            LdlAppInfo.LocalDrivingLicenseApplicationId,
            TestType);
    }

    // ===== EDIT APPOINTMENT =====

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
            Show(
                "This appointment is locked and cannot be modified.",
                "Edit Appointment",
                MessageBoxImage.Warning);

            return;
        }

        var vm =
            _serviceProvider
                .GetRequiredService<ScheduleTestViewModel>();

        await vm.LoadForEditAsync(
            SelectedAppointment.TestAppointmentId);

        OpenDialog(
            new ScheduleTestWin(vm));

        await LoadAsync(
            LdlAppInfo.LocalDrivingLicenseApplicationId,
            TestType);
    }

    // ===== TAKE TEST =====

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
            Show(
                "This appointment is already locked.",
                "Take Test",
                MessageBoxImage.Warning);

            return;
        }

        var workflowResult =
            await _testWorkflowApiClient
                .CanTakeAsync(
                    SelectedAppointment.TestAppointmentId);

        if (workflowResult.IsFailure ||
            workflowResult.Value is null ||
            !workflowResult.Value.Allowed)
        {
            var error =
                workflowResult.IsFailure
                    ? workflowResult.Error
                    : workflowResult.Value?.Error
                      ?? "This test cannot be taken yet.";

            Show(
                error,
                "Cannot Take Test",
                MessageBoxImage.Warning);

            return;
        }

        var vm =
            _serviceProvider
                .GetRequiredService<TakeTestViewModel>();

        await vm.LoadAsync(
            SelectedAppointment.TestAppointmentId);

        OpenDialog(
            new TakeTestWin(vm));

        await LoadAsync(
            LdlAppInfo.LocalDrivingLicenseApplicationId,
            TestType);
    }

    // ===== SELECTION =====

    partial void OnSelectedAppointmentChanged(
        TestAppointmentResponse? value)
    {
        UpdateSelectedAppointmentState();
    }

    private void UpdateSelectedAppointmentState()
    {
        if (SelectedAppointment is null)
        {
            CanEditAppointment = false;
            CanTakeTest = false;
        }
        else
        {
            CanEditAppointment =
                !SelectedAppointment.IsLocked;

            CanTakeTest =
                !SelectedAppointment.IsLocked;
        }

        RefreshCommands();
    }

    // ===== HELPERS =====

    private static DVLD.Contracts.TestAppointment.TestType
        ToContractTestType(
            TestTypeEnum testType) =>
        testType switch
        {
            TestTypeEnum.Theory =>
                DVLD.Contracts.TestAppointment.TestType.Theory,

            TestTypeEnum.Written =>
                DVLD.Contracts.TestAppointment.TestType.Written,

            TestTypeEnum.Practical =>
                DVLD.Contracts.TestAppointment.TestType.Practical,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(testType))
        };

    private static void Show(
        string message,
        string title,
        MessageBoxImage image)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            image);
    }

    private static void OpenDialog(Window window)
    {
        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();
    }

    private void RefreshCommands()
    {
        AddAppointmentCommand.NotifyCanExecuteChanged();
        EditAppointmentCommand.NotifyCanExecuteChanged();
        TakeTestCommand.NotifyCanExecuteChanged();
    }
}