using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private int _localApplicationId;

    public ScheduleTestViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient)
    {
        _testAppointmentsApiClient =
            testAppointmentsApiClient
            ?? throw new ArgumentNullException(nameof(testAppointmentsApiClient));
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

    partial void OnScheduleChanged(ScheduleTestResponse value) =>
        OnPropertyChanged(nameof(TotalFees));

    public async Task LoadAsync(int localAppId, TestType type)
    {
        if (localAppId <= 0)
            throw new ArgumentOutOfRangeException(nameof(localAppId));

        _localApplicationId = localAppId;

        var result =
            await _testAppointmentsApiClient
                .GetSchedulePreparationAsync(
                    localAppId,
                    (int)type);

        if (result.IsFailure)
            throw new Exception(result.Error);

        var data = result.Value;

        if (data is null)
            throw new Exception(
                "Schedule information was not found.");

        Schedule = data;
        IsRetake = data.Trial > 1;
    }

    public async Task LoadForEditAsync(int appointmentId)
    {
        if (appointmentId <= 0)
            return;

        var result =
            await _testAppointmentsApiClient
                .GetScheduleInfoAsync(appointmentId);

        if (result.IsFailure)
        {
            ShowError(result.Error, "Error");
            return;
        }

        var data = result.Value;

        if (data is null)
        {
            ShowError(
                "Appointment information was not found.",
                "Error");
            return;
        }

        Schedule = data;
        _localApplicationId = data.LocalDrivingLicenseApplicationId;
        IsRetake = data.RetakeTestApplicationId > 0;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Schedule is null)
            return;

        try
        {
            if (Schedule.AppointmentId > 0)
            {
                var result =
                    await _testAppointmentsApiClient.UpdateAsync(
                        new UpdateTestAppointmentRequest
                        {
                            TestAppointmentId = Schedule.AppointmentId,
                            AppointmentDate = Schedule.Date
                        });

                if (result.IsFailure)
                {
                    ShowError(result.Error, "Save Failed");
                    return;
                }
            }
            else
            {
                var result =
                    await _testAppointmentsApiClient.ScheduleAsync(
                        new ScheduleTestRequest
                        {
                            TestTypeId = Schedule.TestTypeId,
                            LocalDrivingLicenseApplicationId = _localApplicationId,
                            AppointmentDate = Schedule.Date
                        });

                if (result.IsFailure)
                {
                    ShowError(result.Error, "Save Failed");
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
            ShowError(ex.Message, "Error");
        }
    }

    [RelayCommand]
    private void Close()
    {
        System.Windows.Application.Current.Windows
            .OfType<ScheduleTestWin>()
            .FirstOrDefault()?
            .Close();
    }

    private static void ShowError(
        string message,
        string title,
        MessageBoxImage image = MessageBoxImage.Error)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            image);
    }
}