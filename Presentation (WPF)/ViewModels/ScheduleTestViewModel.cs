using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.TestAppointment;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ScheduleTestViewModel : ObservableObject
{
    private readonly ITestAppointmentsApiClient _testAppointmentsApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;

    private int _localApplicationId;

    public ScheduleTestViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient)
    {
        _testAppointmentsApiClient =
            testAppointmentsApiClient
            ?? throw new ArgumentNullException(
                nameof(testAppointmentsApiClient));

        _localApplicationsApiClient =
            localApplicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(localApplicationsApiClient));
    }

    [ObservableProperty]
    private ScheduleTestResponse schedule = new();

    [ObservableProperty]
    private bool isRetake;

    public decimal TotalFees =>
        (Schedule?.Fees ?? 0) +
        (Schedule?.RetakerFees ?? 0);

    public DateTime MinDate =>
        DateTime.Now.Date.AddDays(1);

    partial void OnScheduleChanged(
        ScheduleTestResponse value)
    {
        OnPropertyChanged(nameof(TotalFees));
    }

    // ===== LOAD =====

    public async Task LoadAsync(
        int localAppId,
        TestType type)
    {
        if (localAppId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(localAppId));
        }

        _localApplicationId = localAppId;

        var localApplicationResult =
            await _localApplicationsApiClient
                .GetByIdAsync(localAppId);

        if (localApplicationResult.IsFailure)
        {
            throw new Exception(
                localApplicationResult.Error);
        }

        var localApplication =
            localApplicationResult.Value;

        if (localApplication is null)
        {
            throw new Exception(
                "Local driving license application was not found.");
        }

        var testTypeId =
            (int)type;

        var appointmentsResult =
            await _testAppointmentsApiClient
                .GetByLocalApplicationIdAsync(
                    localAppId);

        if (appointmentsResult.IsFailure)
        {
            throw new Exception(
                appointmentsResult.Error);
        }

        var appointments =
            appointmentsResult.Value ?? [];

        var trial =
            appointments.Count(
                x => x.TestTypeId == testTypeId) + 1;

        var testFeesResult =
            await _testAppointmentsApiClient
                .GetTestTypeFeesAsync(
                    testTypeId);

        if (testFeesResult.IsFailure)
        {
            throw new Exception(
                testFeesResult.Error);
        }

        var shouldShowRetake =
            appointments.Any(
                x => x.TestTypeId == testTypeId);

        var retakeFee =
            await GetRetakeFeeAsync(
                shouldShowRetake);

        Schedule =
            new ScheduleTestResponse
            {
                LocalDrivingLicenseApplicationId =
                    localAppId,

                FullName =
                    localApplication.FullName,

                LicenseClassName =
                    localApplication.LicenseClassName,

                Trial =
                    trial,

                Date =
                    MinDate,

                Fees =
                    testFeesResult.Value,

                RetakerFees =
                    retakeFee,

                TestTypeId =
                    testTypeId,

                AppointmentId =
                    0,

                RetakeTestApplicationId =
                    null
            };

        IsRetake =
            shouldShowRetake;
    }

    // ===== LOAD FOR EDIT =====

    public async Task LoadForEditAsync(
        int appointmentId)
    {
        if (appointmentId <= 0)
            return;

        var result =
            await _testAppointmentsApiClient
                .GetScheduleInfoAsync(
                    appointmentId);

        if (result.IsFailure)
        {
            ShowError(
                result.Error,
                "Error");

            return;
        }

        var data =
            result.Value;

        if (data is null)
        {
            ShowError(
                "Appointment information was not found.",
                "Error");

            return;
        }

        var appointmentsResult =
            await _testAppointmentsApiClient
                .GetByLocalApplicationIdAsync(
                    data.LocalDrivingLicenseApplicationId);

        if (appointmentsResult.IsFailure)
        {
            ShowError(
                appointmentsResult.Error,
                "Error");

            return;
        }

        var appointments =
            appointmentsResult.Value ?? [];

        var trial =
            appointments.Count(
                x => x.TestTypeId == data.TestTypeId);

        Schedule =
            new ScheduleTestResponse
            {
                AppointmentId =
                    appointmentId,

                RetakeTestApplicationId =
                    data.RetakeTestApplicationId,

                LocalDrivingLicenseApplicationId =
                    data.LocalDrivingLicenseApplicationId,

                LicenseClassName =
                    data.LicenseClassName,

                FullName =
                    data.FullName,

                Trial =
                    trial,

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

        _localApplicationId =
            data.LocalDrivingLicenseApplicationId;

        IsRetake =
            data.RetakeTestApplicationId > 0;
    }

    // ===== SAVE =====

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Schedule is null)
            return;

        try
        {
            // Edit existing appointment
            if (Schedule.AppointmentId > 0)
            {
                var updateRequest =
                    new UpdateTestAppointmentRequest
                    {
                        TestAppointmentId =
                            Schedule.AppointmentId,

                        AppointmentDate =
                            Schedule.Date
                    };

                var updateResult =
                    await _testAppointmentsApiClient
                        .UpdateAsync(
                            updateRequest);

                if (updateResult.IsFailure)
                {
                    ShowError(
                        updateResult.Error,
                        "Save Failed");

                    return;
                }
            }
            // Create new appointment
            else
            {
                var scheduleRequest =
                    new ScheduleTestRequest
                    {
                        TestTypeId =
                            Schedule.TestTypeId,

                        LocalDrivingLicenseApplicationId =
                            _localApplicationId,

                        AppointmentDate =
                            Schedule.Date
                    };

                var scheduleResult =
                    await _testAppointmentsApiClient
                        .ScheduleAsync(
                            scheduleRequest);

                if (scheduleResult.IsFailure)
                {
                    ShowError(
                        scheduleResult.Error,
                        "Save Failed");

                    return;
                }
            }

            MessageBox.Show(
                "Appointment saved successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            ShowError(
                ex.Message,
                "Error");
        }
    }

    // ===== RETAKE FEE =====

    private async Task<decimal> GetRetakeFeeAsync(
        bool isRetake)
    {
        if (!isRetake)
            return 0;

        /*
         * Application Type 7 represents the retake application.
         *
         * This is still temporary presentation-side knowledge.
         * We will move this completely to the API when we introduce
         * the schedule preparation/info endpoint.
         */
        const int retakeApplicationTypeId = 7;

        var result =
            await _testAppointmentsApiClient
                .GetTestTypeFeesAsync(
                    retakeApplicationTypeId);

        return result.IsSuccess
            ? result.Value
            : 0;
    }

    // ===== CLOSE =====

    [RelayCommand]
    private void Close()
    {
        System.Windows.Application.Current.Windows
            .OfType<ScheduleTestWin>()
            .FirstOrDefault()?
            .Close();
    }

    // ===== ERROR =====

    private static void ShowError(
        string message,
        string title,
        MessageBoxImage image =
            MessageBoxImage.Error)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            image);
    }
}