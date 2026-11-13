using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.TestAppointment;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ScheduleTestViewModel : ObservableObject
{
    private readonly ITestAppointmentsApiClient _testAppointmentsApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;
    private int _localApplicationId;
    private static readonly TimeZoneInfo FacilityTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Jordan Standard Time");

    [ObservableProperty] private ScheduleTestResponse _schedule = new();
    [ObservableProperty] private bool _isRetake;
    [ObservableProperty] private AppointmentDayAvailabilityResponse? _selectedAvailableDay;
    [ObservableProperty] private AppointmentSlotResponse? _selectedSlot;
    [ObservableProperty] private bool _isLoadingAvailability;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _availabilityMessage = string.Empty;

    public ObservableCollection<AppointmentDayAvailabilityResponse> AvailableDays { get; } = [];
    public ObservableCollection<AppointmentSlotResponse> AvailableSlots { get; } = [];

    public decimal TotalFees => (Schedule?.Fees ?? 0) + (Schedule?.RetakerFees ?? 0);
    public string TestTypeTitle => (TestType)(Schedule?.TestTypeId ?? 1) switch
    {
        TestType.Theory => "Vision Test",
        TestType.Written => "Written Test",
        TestType.Practical => "Practical Street Test",
        _ => "Test"
    };
    public bool CanSave => !IsSaving && !IsLoadingAvailability &&
                           SelectedAvailableDay is not null && SelectedSlot is not null;

    public ScheduleTestViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _testAppointmentsApiClient = testAppointmentsApiClient ?? throw new ArgumentNullException(nameof(testAppointmentsApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    partial void OnScheduleChanged(ScheduleTestResponse value)
    {

        OnPropertyChanged(nameof(TotalFees));
        OnPropertyChanged(nameof(TestTypeTitle));
    }




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
            await LoadAvailabilityAsync(localAppId, (int)type);
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
            AvailabilityMessage = "Changing an existing appointment requires replacement-slot availability, which the current API does not provide for an already scheduled application.";
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Edit Appointment");
        }
    }

    partial void OnSelectedAvailableDayChanged(AppointmentDayAvailabilityResponse? value)
    {
        AvailableSlots.Clear();
        if (value is not null)
        {
            foreach (var slot in value.Slots)
                AvailableSlots.Add(slot);
            SelectedSlot = AvailableSlots.FirstOrDefault();
        }
        else
        {
            SelectedSlot = null;
        }
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSlotChanged(AppointmentSlotResponse? value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadAvailabilityAsync(int localAppId, int testTypeId)
    {
        IsLoadingAvailability = true;
        AvailabilityMessage = string.Empty;
        AvailableDays.Clear();
        AvailableSlots.Clear();
        SelectedAvailableDay = null;
        SelectedSlot = null;
        try
        {
            var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FacilityTimeZone).Date;
            var result = await _testAppointmentsApiClient.GetAvailabilityAsync(
                localAppId,
                testTypeId,
                DateTime.SpecifyKind(today.AddDays(2), DateTimeKind.Unspecified),
                DateTime.SpecifyKind(today.AddDays(90), DateTimeKind.Unspecified));

            if (result.IsFailure)
            {
                AvailabilityMessage = result.Error;
                _notifications.ShowFailure(result, "Appointment Availability");
                return;
            }

            foreach (var day in result.Value?.AvailableDays ?? [])
                AvailableDays.Add(day);

            SelectedAvailableDay = AvailableDays.FirstOrDefault();
            if (AvailableDays.Count == 0)
                AvailabilityMessage = "No eligible appointment slots are available in the next 90 days.";
        }
        catch (Exception ex)
        {
            AvailabilityMessage = GetExceptionMessage(ex);
            _userNotifications.ShowError(AvailabilityMessage, "Appointment Availability");
        }
        finally
        {
            IsLoadingAvailability = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (IsSaving || SelectedAvailableDay is null || SelectedSlot is null)
            return;

        IsSaving = true;
        try
        {
            var appointmentDate = CreateFacilityDateTime(SelectedAvailableDay.Date, SelectedSlot.StartTime);
            var appointmentOffset = ToFacilityDateTimeOffset(appointmentDate);
            if (Schedule.AppointmentId > 0)
            {
                var result = await _testAppointmentsApiClient.UpdateAsync(
                    new UpdateTestAppointmentRequest
                    {
                        TestAppointmentId = Schedule.AppointmentId,
                        AppointmentDate = appointmentOffset
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
                        AppointmentDate = appointmentOffset
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
        finally
        {
            IsSaving = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingAvailabilityChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static DateTime CreateFacilityDateTime(string date, string time)
    {
        var day = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var slot = TimeOnly.ParseExact(time, "HH:mm", CultureInfo.InvariantCulture);
        return DateTime.SpecifyKind(day.ToDateTime(slot), DateTimeKind.Unspecified);
    }

    private static DateTimeOffset ToFacilityDateTimeOffset(DateTime facilityDateTime)
    {
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(facilityDateTime, FacilityTimeZone);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
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
