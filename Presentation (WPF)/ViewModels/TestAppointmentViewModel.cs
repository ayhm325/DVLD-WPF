using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Application;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.TestAppointment;
using DVLD.Contracts.TestWorkflow;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.Results;
using Presentation.Services.UI;
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
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    private int _localApplicationId;

    [ObservableProperty] private TestType _testType;
    [ObservableProperty] private LocalDrivingLicenseApplicationResponse? _ldlAppInfo;
    [ObservableProperty] private ApplicationBasicInfoResponse? _applicationInfo;
    [ObservableProperty] private TestAppointmentResponse? _selectedAppointment;
    [ObservableProperty] private bool _canAddAppointment;
    [ObservableProperty] private bool _canTakeTest;
    [ObservableProperty] private bool _canEditAppointment;
    [ObservableProperty] private bool _isWorkflowAllowed;
    [ObservableProperty] private string _workflowMessage = string.Empty;

    public ObservableCollection<TestAppointmentResponse> AppointmentsList { get; } = [];

    public string PageTitle => TestType switch
    {
        TestType.Theory => "Theory Test Appointments",
        TestType.Written => "Written Test Appointments",
        TestType.Practical => "Practical Test Appointments",
        _ => "Test Appointments"
    };

    public string PageDescription => TestType switch
    {
        TestType.Theory => "Manage theory test appointments for this application.",
        TestType.Written => "Manage written test appointments for this application.",
        TestType.Practical => "Manage practical test appointments for this application.",
        _ => "Manage test appointments for this application."
    };

    public TestAppointmentViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        ITestWorkflowApiClient testWorkflowApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IServiceProvider serviceProvider,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _testAppointmentsApiClient = testAppointmentsApiClient ?? throw new ArgumentNullException(nameof(testAppointmentsApiClient));
        _testWorkflowApiClient = testWorkflowApiClient ?? throw new ArgumentNullException(nameof(testWorkflowApiClient));
        _localApplicationsApiClient = localApplicationsApiClient ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    public async Task LoadAsync(int localApplicationId, TestType type)
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
                _userNotifications.ShowWarning(
                    "Invalid local driving license application ID.",
                    "Invalid Data");
                return;
            }

            var ldlResult = await _localApplicationsApiClient.GetByIdAsync(localApplicationId);

            if (ldlResult.IsFailure)
            {
                _notifications.ShowFailure(ldlResult, "Application Error");
                return;
            }

            if (ldlResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Local driving license application was not found.",
                    "Application Not Found");
                return;
            }

            LdlAppInfo = ldlResult.Value;

            var applicationResult =
                await _localApplicationsApiClient.GetApplicationBasicInfoAsync(localApplicationId);

            if (applicationResult.IsFailure)
            {
                _notifications.ShowFailure(applicationResult, "Application Error");
                return;
            }

            if (applicationResult.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Application information was not found.",
                    "Application Error");
                return;
            }

            ApplicationInfo = applicationResult.Value;

            var workflowResult =
                await _testWorkflowApiClient.CanScheduleAsync(localApplicationId, TestType);

            if (workflowResult.IsFailure)
            {
                IsWorkflowAllowed = false;
                WorkflowMessage = workflowResult.Error;
                await LoadAppointmentsAsync();
                RefreshCommands();
                return;
            }

            if (workflowResult.Value is null || !workflowResult.Value.Allowed)
            {
                IsWorkflowAllowed = false;
                WorkflowMessage = workflowResult.Value?.Error ??
                                  "Test cannot be scheduled at this stage.";
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
            _userNotifications.ShowError(ex.Message, "Loading Error");
        }
    }

    private async Task LoadAppointmentsAsync()
    {
        AppointmentsList.Clear();

        var result = await _testAppointmentsApiClient
            .GetByLocalApplicationIdAsync(_localApplicationId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Appointments Error");
            return;
        }

        var appointments = result.Value?
            .Where(x => x.TestTypeId == (int)TestType)
            .OrderByDescending(x => x.AppointmentDate)
            .ToList() ?? [];

        foreach (var appointment in appointments)
            AppointmentsList.Add(appointment);
    }

    private async Task RefreshAppointmentStateAsync()
    {
        CanAddAppointment = false;

        if (!IsWorkflowAllowed)
            return;

        var result = await _testAppointmentsApiClient
            .IsAppointmentAlreadyScheduledAsync(_localApplicationId, (int)TestType);

        if (result.IsSuccess)
            CanAddAppointment = !result.Value;
        else
            _notifications.ShowFailure(result, "Appointment Status Error");
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

        var localApplicationId = LdlAppInfo.LocalDrivingLicenseApplicationId;
        var workflowResult =
            await _testWorkflowApiClient.CanScheduleAsync(localApplicationId, TestType);

        if (!IsWorkflowAllowedResult(workflowResult, "Cannot Schedule Test"))
            return;

        var vm = _serviceProvider.GetRequiredService<ScheduleTestViewModel>();
        await vm.LoadAsync(localApplicationId, TestType);

        OpenDialog(new ScheduleTestWin(vm));
        await LoadAsync(localApplicationId, TestType);
    }

    [RelayCommand(CanExecute = nameof(CanEditAppointment))]
    private async Task EditAppointmentAsync()
    {
        if (SelectedAppointment is null || LdlAppInfo is null)
            return;

        if (SelectedAppointment.IsLocked)
        {
            _userNotifications.ShowWarning(
                "This appointment is locked and cannot be modified.",
                "Edit Appointment");
            return;
        }

        var localApplicationId = LdlAppInfo.LocalDrivingLicenseApplicationId;
        var vm = _serviceProvider.GetRequiredService<ScheduleTestViewModel>();

        await vm.LoadForEditAsync(SelectedAppointment.TestAppointmentId);
        OpenDialog(new ScheduleTestWin(vm));
        await LoadAsync(localApplicationId, TestType);
    }

    [RelayCommand(CanExecute = nameof(CanTakeTest))]
    private async Task TakeTestAsync()
    {
        if (SelectedAppointment is null || LdlAppInfo is null)
            return;

        if (SelectedAppointment.IsLocked)
        {
            _userNotifications.ShowWarning(
                "This appointment is already locked.",
                "Take Test");
            return;
        }

        var workflowResult =
            await _testWorkflowApiClient.CanTakeAsync(
                SelectedAppointment.TestAppointmentId);

        if (!IsWorkflowAllowedResult(workflowResult, "Cannot Take Test"))
            return;

        var localApplicationId = LdlAppInfo.LocalDrivingLicenseApplicationId;
        var vm = _serviceProvider.GetRequiredService<TakeTestViewModel>();

        await vm.LoadAsync(SelectedAppointment.TestAppointmentId);
        OpenDialog(new TakeTestWin(vm));
        await LoadAsync(localApplicationId, TestType);
    }

    partial void OnSelectedAppointmentChanged(TestAppointmentResponse? value)
    {
        CanEditAppointment = value is not null && !value.IsLocked;
        CanTakeTest = value is not null && !value.IsLocked;
        RefreshCommands();
    }

    private bool IsWorkflowAllowedResult(
        ApiResult<TestWorkflowResponse> result,
        string title)
    {
        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, title);
            return false;
        }

        if (result.Value is null || !result.Value.Allowed)
        {
            _userNotifications.ShowWarning(
                result.Value?.Error ?? "The operation is not allowed at this stage.",
                title);
            return false;
        }

        return true;
    }

    private void RefreshCommands()
    {
        AddAppointmentCommand.NotifyCanExecuteChanged();
        EditAppointmentCommand.NotifyCanExecuteChanged();
        TakeTestCommand.NotifyCanExecuteChanged();
    }

    private static void OpenDialog(Window window)
    {
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }
}