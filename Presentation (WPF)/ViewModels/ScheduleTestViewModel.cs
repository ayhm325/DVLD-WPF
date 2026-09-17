using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.TestAppointment;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ScheduleTestViewModel : ObservableObject
{
    private readonly ITestAppointmentsApiClient _testAppointmentsApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;
    private int _localApplicationId;

    [ObservableProperty] private ScheduleTestResponse _schedule = new();
    [ObservableProperty] private bool _isRetake;

    public decimal TotalFees => (Schedule?.Fees ?? 0) + (Schedule?.RetakerFees ?? 0);
    public DateTime MinDate => DateTime.Now.Date.AddDays(1);

    public ScheduleTestViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _testAppointmentsApiClient = testAppointmentsApiClient ?? throw new ArgumentNullException(nameof(testAppointmentsApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    partial void OnScheduleChanged(ScheduleTestResponse value) =>
        OnPropertyChanged(nameof(TotalFees));

    public async Task LoadAsync(int localAppId, TestType type)
    {
        if (localAppId <= 0)
        {
            _userNotifications.ShowWarning(
                "Invalid local driving license application ID.",
                "Schedule Test");
            return;
        }

        _localApplicationId = localAppId;

        try
        {
            var result = await _testAppointmentsApiClient
                .GetSchedulePreparationAsync(localAppId, (int)type);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Schedule Test");
                return;
            }

            if (result.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Schedule information was not found.",
                    "Schedule Test");
                return;
            }

            Schedule = result.Value;
            IsRetake = Schedule.Trial > 1;
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Schedule Test");
        }
    }

    public async Task LoadForEditAsync(int appointmentId)
    {
        if (appointmentId <= 0)
        {
            _userNotifications.ShowWarning(
                "Invalid test appointment ID.",
                "Edit Appointment");
            return;
        }

        try
        {
            var result = await _testAppointmentsApiClient
                .GetScheduleInfoAsync(appointmentId);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Edit Appointment");
                return;
            }

            if (result.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Appointment information was not found.",
                    "Edit Appointment");
                return;
            }

            Schedule = result.Value;
            _localApplicationId = Schedule.LocalDrivingLicenseApplicationId;
            IsRetake = Schedule.RetakeTestApplicationId > 0;
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Edit Appointment");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (Schedule.AppointmentId > 0)
            {
                var result = await _testAppointmentsApiClient.UpdateAsync(
                    new UpdateTestAppointmentRequest
                    {
                        TestAppointmentId = Schedule.AppointmentId,
                        AppointmentDate = Schedule.Date
                    });

                if (result.IsFailure)
                {
                    _notifications.ShowFailure(result, "Save Failed");
                    return;
                }
            }
            else
            {
                var result = await _testAppointmentsApiClient.ScheduleAsync(
                    new ScheduleTestRequest
                    {
                        TestTypeId = Schedule.TestTypeId,
                        LocalDrivingLicenseApplicationId = _localApplicationId,
                        AppointmentDate = Schedule.Date
                    });

                if (result.IsFailure)
                {
                    _notifications.ShowFailure(result, "Save Failed");
                    return;
                }
            }

            _userNotifications.ShowInfo(
                "Appointment saved successfully.",
                "Success");

            Close();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Save Failed");
        }
    }

    [RelayCommand]
    private static void Close()
    {
        System.Windows.Application.Current.Windows
            .OfType<ScheduleTestWin>()
            .FirstOrDefault()?.Close();
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}