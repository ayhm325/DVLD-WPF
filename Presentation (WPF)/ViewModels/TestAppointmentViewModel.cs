using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    private int _localApplicationId;

    [ObservableProperty]
    private TestType testType;

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

    public ObservableCollection<TestAppointmentResponse> AppointmentsList
    { get; } = new();

    public string PageTitle => TestType switch
    {
        TestType.Theory =>
            "Theory Test Appointments",

        TestType.Written =>
            "Written Test Appointments",

        TestType.Practical =>
            "Practical Test Appointments",

        _ =>
            "Test Appointments"
    };

    public string PageDescription => TestType switch
    {
        TestType.Theory =>
            "Manage theory test appointments for this application.",

        TestType.Written =>
            "Manage written test appointments for this application.",

        TestType.Practical =>
            "Manage practical test appointments for this application.",

        _ =>
            "Manage test appointments for this application."
    };

    public TestAppointmentViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        ITestWorkflowApiClient testWorkflowApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IServiceProvider serviceProvider)
    {
        _testAppointmentsApiClient = testAppointmentsApiClient
            ?? throw new ArgumentNullException(
                nameof(testAppointmentsApiClient));

        _testWorkflowApiClient = testWorkflowApiClient
            ?? throw new ArgumentNullException(
                nameof(testWorkflowApiClient));

        _localApplicationsApiClient = localApplicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(localApplicationsApiClient));

        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(
                nameof(serviceProvider));
    }

    public async Task LoadAsync(
        int localApplicationId,
        TestType type)
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
                ShowWarning(
                    "Invalid local driving license application ID.",
                    "Invalid Data");

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

                ShowWarning(
                    WorkflowMessage,
                    "Application Not Found");

                return;
            }

            LdlAppInfo =
                ldlResult.Value;

            var applicationResult =
                await _localApplicationsApiClient
                    .GetApplicationBasicInfoAsync(
                        localApplicationId);

            if (applicationResult.IsFailure ||
                applicationResult.Value is null)
            {
                WorkflowMessage =
                    applicationResult.IsFailure
                        ? applicationResult.Error
                        : "Application information was not found.";

                ShowWarning(
                    WorkflowMessage,
                    "Application Error");

                return;
            }

            ApplicationInfo =
                applicationResult.Value;

            var workflowResult =
                await _testWorkflowApiClient
                    .CanScheduleAsync(
                        localApplicationId,
                        TestType);

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

            ShowError(
                ex.Message,
                "Loading Error");
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
            ShowWarning(
                result.Error,
                "Appointments Error");

            return;
        }

        var appointments =
            result.Value?
                .Where(x =>
                    x.TestTypeId == (int)TestType)
                .OrderByDescending(x =>
                    x.AppointmentDate)
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

        if (result.IsSuccess)
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

    [RelayCommand(CanExecute = nameof(CanAddAppointment))]
    private async Task AddAppointmentAsync()
    {
        if (LdlAppInfo is null)
            return;

        var localApplicationId =
            LdlAppInfo.LocalDrivingLicenseApplicationId;

        var workflowResult =
            await _testWorkflowApiClient
                .CanScheduleAsync(
                    localApplicationId,
                    TestType);

        if (workflowResult.IsFailure ||
            workflowResult.Value is null ||
            !workflowResult.Value.Allowed)
        {
            ShowWarning(
                workflowResult.IsFailure
                    ? workflowResult.Error
                    : workflowResult.Value?.Error
                      ?? "Test cannot be scheduled at this stage.",
                "Cannot Schedule Test");

            return;
        }

        var vm =
            _serviceProvider
                .GetRequiredService<ScheduleTestViewModel>();

        await vm.LoadAsync(
            localApplicationId,
            TestType);

        OpenDialog(
            new ScheduleTestWin(vm));

        await LoadAsync(
            localApplicationId,
            TestType);
    }

    [RelayCommand(CanExecute = nameof(CanEditAppointment))]
    private async Task EditAppointmentAsync()
    {
        if (SelectedAppointment is null ||
            LdlAppInfo is null)
            return;

        if (SelectedAppointment.IsLocked)
        {
            ShowWarning(
                "This appointment is locked and cannot be modified.",
                "Edit Appointment");

            return;
        }

        var localApplicationId =
            LdlAppInfo.LocalDrivingLicenseApplicationId;

        var vm =
            _serviceProvider
                .GetRequiredService<ScheduleTestViewModel>();

        await vm.LoadForEditAsync(
            SelectedAppointment.TestAppointmentId);

        OpenDialog(
            new ScheduleTestWin(vm));

        await LoadAsync(
            localApplicationId,
            TestType);
    }

    [RelayCommand(CanExecute = nameof(CanTakeTest))]
    private async Task TakeTestAsync()
    {
        if (SelectedAppointment is null ||
            LdlAppInfo is null)
            return;

        if (SelectedAppointment.IsLocked)
        {
            ShowWarning(
                "This appointment is already locked.",
                "Take Test");

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
            ShowWarning(
                workflowResult.IsFailure
                    ? workflowResult.Error
                    : workflowResult.Value?.Error
                      ?? "This test cannot be taken yet.",
                "Cannot Take Test");

            return;
        }

        var localApplicationId =
            LdlAppInfo.LocalDrivingLicenseApplicationId;

        var vm =
            _serviceProvider
                .GetRequiredService<TakeTestViewModel>();

        await vm.LoadAsync(
            SelectedAppointment.TestAppointmentId);

        OpenDialog(
            new TakeTestWin(vm));

        await LoadAsync(
            localApplicationId,
            TestType);
    }

    partial void OnSelectedAppointmentChanged(
        TestAppointmentResponse? value)
    {
        CanEditAppointment =
            value is not null &&
            !value.IsLocked;

        CanTakeTest =
            value is not null &&
            !value.IsLocked;

        RefreshCommands();
    }

    private void RefreshCommands()
    {
        AddAppointmentCommand.NotifyCanExecuteChanged();
        EditAppointmentCommand.NotifyCanExecuteChanged();
        TakeTestCommand.NotifyCanExecuteChanged();
    }

    private static void OpenDialog(Window window)
    {
        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();
    }

    private static void ShowWarning(
        string message,
        string title) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

    private static void ShowError(
        string message,
        string title) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}