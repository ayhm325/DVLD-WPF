using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Test;
using DVLD.Contracts.TestAppointment;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;

namespace Presentation.ViewModels;

public partial class TakeTestViewModel : ObservableObject
{
    private readonly ITestAppointmentsApiClient _testAppointmentsApiClient;
    private readonly ITestsApiClient _testsApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private TestResult _testResult = TestResult.NotTaken;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private ScheduleTestResponse? _schedule;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _licenseClassName = string.Empty;
    [ObservableProperty] private decimal _fees;
    [ObservableProperty] private bool _isSaving;

    public bool CanSave => Schedule is not null &&
                           TestResult != TestResult.NotTaken &&
                           !IsSaving;

    public string TestTypeTitle => (TestType)(Schedule?.TestTypeId ?? 1) switch
    {
        TestType.Theory => "Vision Test",
        TestType.Written => "Written Test",
        TestType.Practical => "Practical Street Test",
        _ => "Test"
    };

    public TakeTestViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        ITestsApiClient testsApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _testAppointmentsApiClient = testAppointmentsApiClient ?? throw new ArgumentNullException(nameof(testAppointmentsApiClient));
        _testsApiClient = testsApiClient ?? throw new ArgumentNullException(nameof(testsApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    partial void OnScheduleChanged(ScheduleTestResponse? value)
    {
        FullName = value?.FullName ?? string.Empty;
        LicenseClassName = value?.LicenseClassName ?? string.Empty;
        Fees = value?.Fees ?? 0;
        OnPropertyChanged(nameof(TestTypeTitle));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    partial void OnTestResultChanged(TestResult value)
    {
        OnPropertyChanged(nameof(IsPassed));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(IsNotTaken));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    public bool IsPassed
    {
        get => TestResult == TestResult.Pass;
        set { if (value) TestResult = TestResult.Pass; }
    }

    public bool IsFailed
    {
        get => TestResult == TestResult.Fail;
        set { if (value) TestResult = TestResult.Fail; }
    }

    public bool IsNotTaken
    {
        get => TestResult == TestResult.NotTaken;
        set { if (value) TestResult = TestResult.NotTaken; }
    }

    [RelayCommand]
    private static void Close()
    {
        System.Windows.Application.Current.Windows
            .OfType<TakeTestWin>()
            .FirstOrDefault()?.Close();
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (!CanSave || Schedule is null)
        {
            _userNotifications.ShowWarning(
                "Test appointment data is not available.",
                "Take Test");
            return;
        }

        if (TestResult == TestResult.NotTaken)
        {
            _userNotifications.ShowWarning(
                "Please select Pass or Fail.",
                "Take Test");
            return;
        }

        IsSaving = true;
        var request = new SaveTestResultRequest(
            Schedule.AppointmentId,
            TestResult == TestResult.Pass,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());

        try
        {
            var result = await _testsApiClient.SaveResultAsync(request);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Take Test");
                return;
            }

            _userNotifications.ShowInfo(
                request.TestResult
                    ? "Test result saved successfully.\n\nResult: Passed."
                    : "Test result saved successfully.\n\nResult: Failed.",
                "Take Test");

            Close();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Take Test");
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task LoadAsync(int appointmentId)
    {
        if (appointmentId <= 0)
        {
            _userNotifications.ShowWarning(
                "Invalid test appointment ID.",
                "Take Test");
            return;
        }

        try
        {
            var result = await _testAppointmentsApiClient
                .GetScheduleInfoAsync(appointmentId);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Take Test");
                return;
            }

            if (result.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Test appointment data was not found.",
                    "Take Test");
                return;
            }

            var data = result.Value;

            Schedule = new ScheduleTestResponse
            {
                AppointmentId = data.AppointmentId,
                RetakeTestApplicationId = data.RetakeTestApplicationId,
                LocalDrivingLicenseApplicationId = data.LocalDrivingLicenseApplicationId,
                LicenseClassName = data.LicenseClassName,
                FullName = data.FullName,
                Trial = data.Trial,
                Date = data.Date,
                Fees = data.Fees,
                TestTypeId = data.TestTypeId,
                RetakerFees = data.RetakerFees,
                TestId = data.TestId,
                Result = data.Result,
                Notes = data.Notes
            };
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Loading Error");
        }
    }

    partial void OnIsSavingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}
