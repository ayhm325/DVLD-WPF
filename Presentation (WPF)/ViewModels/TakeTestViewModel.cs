using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class TakeTestViewModel : ObservableObject
{
    private readonly ITestAppointmentService _service;
    private readonly ITestService _testService;
    private readonly ICurrentUserService _currentUser;

    public TakeTestViewModel(
    ITestAppointmentService service,
    ITestService testService,
    ICurrentUserService currentUser)
    {
        _service =
            service
            ?? throw new ArgumentNullException(nameof(service));

        _testService =
            testService
            ?? throw new ArgumentNullException(nameof(testService));

        _currentUser =
            currentUser
            ?? throw new ArgumentNullException(nameof(currentUser));
    }

    [ObservableProperty]
    private TestResultType testResult =
        TestResultType.Fail;

    [ObservableProperty]
    private string notes =
        string.Empty;

    [ObservableProperty]
    private ScheduleTestDto? schedule;

    [ObservableProperty]
    private string fullName =
        string.Empty;

    [ObservableProperty]
    private string licenseClassName =
        string.Empty;

    [ObservableProperty]
    private decimal fees;

    partial void OnScheduleChanged(
        ScheduleTestDto? value)
    {
        if (value is null)
        {
            FullName = string.Empty;
            LicenseClassName = string.Empty;
            Fees = 0;
            return;
        }

        FullName =
            value.FullName
            ?? string.Empty;

        LicenseClassName =
            value.LicenseClassName
            ?? string.Empty;

        Fees =
            value.Fees;
    }

    partial void OnTestResultChanged(
        TestResultType value)
    {
        OnPropertyChanged(nameof(IsPassed));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(IsNotTaken));
    }

    public bool IsPassed
    {
        get => TestResult == TestResultType.Pass;

        set
        {
            if (value)
                TestResult = TestResultType.Pass;
        }
    }

    public bool IsFailed
    {
        get => TestResult == TestResultType.Fail;

        set
        {
            if (value)
                TestResult = TestResultType.Fail;
        }
    }

    public bool IsNotTaken
    {
        get => TestResult == TestResultType.NotTaken;

        set
        {
            if (value)
                TestResult = TestResultType.NotTaken;
        }
    }

    [RelayCommand]
    private void Close()
    {
        System.Windows.Application.Current.Windows
            .OfType<TakeTestWin>()
            .FirstOrDefault()?
            .Close();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Schedule is null)
        {
            MessageBox.Show(
                "Test appointment data is not available.",
                "Take Test",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (!_currentUser.IsLoggedIn ||
            _currentUser.UserId <= 0)
        {
            MessageBox.Show(
                "You must be logged in first.",
                "Take Test",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        if (TestResult == TestResultType.NotTaken)
        {
            MessageBox.Show(
                "Please select Pass or Fail.",
                "Take Test",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var dto =
            new SaveTestResultDto
            {
                TestAppointmentID =
                    Schedule.AppointmentID,

                TestResult =
                    TestResult == TestResultType.Pass,

                Notes =
                    string.IsNullOrWhiteSpace(Notes)
                        ? null
                        : Notes.Trim()
            };

        try
        {
            var result =
                await _testService.AddAsync(dto);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            MessageBox.Show(
                dto.TestResult
                    ? "Test result saved successfully.\n\nResult: Passed."
                    : "Test result saved successfully.\n\nResult: Failed.",
                "Take Test",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            var message =
                ex.Message;

            if (ex.InnerException is not null)
            {
                message +=
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"Inner Exception:{Environment.NewLine}" +
                    ex.InnerException.Message;
            }

            MessageBox.Show(
                message,
                "Take Test",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async Task LoadAsync(
        int appointmentId)
    {
        if (appointmentId <= 0)
        {
            MessageBox.Show(
                "Invalid test appointment ID.",
                "Take Test",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            var result =
                await _service
                    .GetScheduleInfoAsync(
                        appointmentId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var data =
                result.Value;

            if (data is null)
            {
                MessageBox.Show(
                    "Test appointment data was not found.",
                    "Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Schedule =
                data;

            var trialCount =
                await _service
                    .GetTrialCountAsync(
                        data.LocalDrivingLicenseApplicationID,
                        data.TestTypeID);

            Schedule.Trial =
                trialCount;
        }
        catch (Exception ex)
        {
            var message =
                ex.Message;

            if (ex.InnerException is not null)
            {
                message +=
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"Inner Exception:{Environment.NewLine}" +
                    ex.InnerException.Message;
            }

            MessageBox.Show(
                message,
                "Loading Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
