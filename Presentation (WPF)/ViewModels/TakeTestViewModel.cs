using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Test;
using DVLD.Contracts.TestAppointment;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels;

public partial class TakeTestViewModel : ObservableObject
{
    private readonly ITestAppointmentsApiClient _testAppointmentsApiClient;
    private readonly ITestsApiClient _testsApiClient;

    public TakeTestViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        ITestsApiClient testsApiClient)
    {
        _testAppointmentsApiClient =
            testAppointmentsApiClient
            ?? throw new ArgumentNullException(
                nameof(testAppointmentsApiClient));

        _testsApiClient =
            testsApiClient
            ?? throw new ArgumentNullException(
                nameof(testsApiClient));
    }

    [ObservableProperty]
    private TestResult testResult =
        TestResult.Fail;

    [ObservableProperty]
    private string notes =
        string.Empty;

    [ObservableProperty]
    private ScheduleTestResponse? schedule;

    [ObservableProperty]
    private string fullName =
        string.Empty;

    [ObservableProperty]
    private string licenseClassName =
        string.Empty;

    [ObservableProperty]
    private decimal fees;

    partial void OnScheduleChanged(
        ScheduleTestResponse? value)
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
        TestResult value)
    {
        OnPropertyChanged(nameof(IsPassed));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(IsNotTaken));
    }

    public bool IsPassed
    {
        get => TestResult == TestResult.Pass;

        set
        {
            if (value)
                TestResult = TestResult.Pass;
        }
    }

    public bool IsFailed
    {
        get => TestResult == TestResult.Fail;

        set
        {
            if (value)
                TestResult = TestResult.Fail;
        }
    }

    public bool IsNotTaken
    {
        get => TestResult == TestResult.NotTaken;

        set
        {
            if (value)
                TestResult = TestResult.NotTaken;
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

        if (TestResult == TestResult.NotTaken)
        {
            MessageBox.Show(
                "Please select Pass or Fail.",
                "Take Test",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var request =
            new SaveTestResultRequest(
                Schedule.AppointmentId,
                TestResult == TestResult.Pass,
                string.IsNullOrWhiteSpace(Notes)
                    ? null
                    : Notes.Trim());

        try
        {
            var result =
                await _testsApiClient
                    .SaveResultAsync(request);

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
                request.TestResult
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
                await _testAppointmentsApiClient
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
                await _testAppointmentsApiClient
                    .GetTrialCountAsync(
                        data.LocalDrivingLicenseApplicationId,
                        data.TestTypeId);

            if (trialCount.IsFailure)
            {
                MessageBox.Show(
                    trialCount.Error,
                    "Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Schedule =
                new ScheduleTestResponse
                {
                    AppointmentId =
                        data.AppointmentId,

                    RetakeTestApplicationId =
                        data.RetakeTestApplicationId,

                    LocalDrivingLicenseApplicationId =
                        data.LocalDrivingLicenseApplicationId,

                    LicenseClassName =
                        data.LicenseClassName,

                    FullName =
                        data.FullName,

                    Trial =
                        trialCount.Value,

                    Date =
                        data.Date,

                    Fees =
                        data.Fees,

                    TestTypeId =
                        data.TestTypeId,

                    RetakerFees =
                        data.RetakerFees,

                    TestId =
                        data.TestId,

                    Result =
                        data.Result,

                    Notes =
                        data.Notes
                };
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