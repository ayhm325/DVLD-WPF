using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using DVLD.Contracts.Application;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.TestAppointment;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ScheduleTestViewModel : ObservableObject
{
    private const int RetakeApplicationTypeId = 7;

    private readonly ITestAppointmentsApiClient _testAppointmentsApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly IApplicationsApiClient _applicationsApiClient;
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;
    private readonly ICurrentUserSession _currentUserSession;

    private int _localApplicationId;
    private int _applicationId;

    public ScheduleTestViewModel(
        ITestAppointmentsApiClient testAppointmentsApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IApplicationsApiClient applicationsApiClient,
        IApplicationTypesApiClient applicationTypesApiClient,
        ICurrentUserSession currentUserSession)
    {
        _testAppointmentsApiClient =
            testAppointmentsApiClient
            ?? throw new ArgumentNullException(nameof(testAppointmentsApiClient));

        _localApplicationsApiClient =
            localApplicationsApiClient
            ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));

        _applicationsApiClient =
            applicationsApiClient
            ?? throw new ArgumentNullException(nameof(applicationsApiClient));

        _applicationTypesApiClient =
            applicationTypesApiClient
            ?? throw new ArgumentNullException(nameof(applicationTypesApiClient));

        _currentUserSession =
            currentUserSession
            ?? throw new ArgumentNullException(nameof(currentUserSession));
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

    partial void OnScheduleChanged(ScheduleTestResponse value)
    {
        OnPropertyChanged(nameof(TotalFees));
    }

    public async Task LoadAsync(
        int localAppId,
        TestTypeEnum type)
    {
        if (localAppId <= 0)
            throw new ArgumentOutOfRangeException(nameof(localAppId));

        _localApplicationId = localAppId;

        var applicationIdResult =
            await _localApplicationsApiClient
                .GetApplicationIdAsync(localAppId);

        if (applicationIdResult.IsFailure)
            throw new Exception(applicationIdResult.Error);

        _applicationId = applicationIdResult.Value;

        var localApplicationResult =
            await _localApplicationsApiClient
                .GetByIdAsync(localAppId);

        if (localApplicationResult.IsFailure)
            throw new Exception(localApplicationResult.Error);

        var localApplication =
            localApplicationResult.Value;

        if (localApplication is null)
        {
            throw new Exception(
                "Local driving license application was not found.");
        }

        var testTypeId = (int)type;

        var appointmentsResult =
            await _testAppointmentsApiClient
                .GetByLocalApplicationIdAsync(localAppId);

        if (appointmentsResult.IsFailure)
            throw new Exception(appointmentsResult.Error);

        var appointments =
            appointmentsResult.Value ?? [];

        var trial =
            appointments.Count(
                x => x.TestTypeId == testTypeId) + 1;

        var testFeesResult =
            await _testAppointmentsApiClient
                .GetTestTypeFeesAsync(testTypeId);

        if (testFeesResult.IsFailure)
            throw new Exception(testFeesResult.Error);

        var retakeApplicationTypeResult =
            await _applicationTypesApiClient
                .GetByIdAsync(RetakeApplicationTypeId);

        if (retakeApplicationTypeResult.IsFailure)
            throw new Exception(
                retakeApplicationTypeResult.Error);

        var retakeApplicationType =
            retakeApplicationTypeResult.Value;

        if (retakeApplicationType is null)
        {
            throw new Exception(
                "Retake application type was not found.");
        }

        var shouldShowRetake =
            appointments.Any(
                x => x.TestTypeId == testTypeId);

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
                    shouldShowRetake
                        ? retakeApplicationType.ApplicationTypeFees
                        : 0,

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

    public async Task LoadForEditAsync(
        int appointmentId)
    {
        if (appointmentId <= 0)
            return;

        var result =
            await _testAppointmentsApiClient
                .GetScheduleInfoAsync(appointmentId);

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

        IsRetake =
            data.RetakeTestApplicationId > 0;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Schedule is null)
            return;

        if (!_currentUserSession.IsLoggedIn ||
            _currentUserSession.UserId <= 0)
        {
            ShowError(
                "You must be logged in first.",
                "Validation",
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            if (IsRetake &&
                Schedule.AppointmentId == 0 &&
                !Schedule.RetakeTestApplicationId.HasValue)
            {
                var applicationResult =
                    await _applicationsApiClient
                        .GetByIdAsync(_applicationId);

                if (applicationResult.IsFailure)
                {
                    ShowError(
                        applicationResult.Error,
                        "Error");

                    return;
                }

                var originalApplication =
                    applicationResult.Value;

                if (originalApplication is null)
                {
                    ShowError(
                        "Original application was not found.",
                        "Error");

                    return;
                }

                var createRequest =
                    new CreateApplicationRequest
                    {
                        ApplicantPersonId =
                            originalApplication.ApplicantPersonId,

                        ApplicationTypeId =
                            RetakeApplicationTypeId
                    };

                var retakeResult =
                    await _applicationsApiClient
                        .CreateAsync(createRequest);

                if (retakeResult.IsFailure)
                {
                    ShowError(
                        retakeResult.Error,
                        "Save Failed");

                    return;
                }

                Schedule =
                    new ScheduleTestResponse
                    {
                        AppointmentId =
                            Schedule.AppointmentId,

                        RetakeTestApplicationId =
                            retakeResult.Value,

                        LocalDrivingLicenseApplicationId =
                            Schedule.LocalDrivingLicenseApplicationId,

                        LicenseClassName =
                            Schedule.LicenseClassName,

                        FullName =
                            Schedule.FullName,

                        Trial =
                            Schedule.Trial,

                        Date =
                            Schedule.Date,

                        Fees =
                            Schedule.Fees,

                        TestTypeId =
                            Schedule.TestTypeId,

                        RetakerFees =
                            Schedule.RetakerFees,

                        TestId =
                            Schedule.TestId,

                        Result =
                            Schedule.Result,

                        Notes =
                            Schedule.Notes
                    };
            }

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
                        .UpdateAsync(updateRequest);

                if (updateResult.IsFailure)
                {
                    ShowError(
                        updateResult.Error,
                        "Save Failed");

                    return;
                }
            }
            else
            {
                var createRequest =
                    new CreateTestAppointmentRequest
                    {
                        TestTypeId =
                            Schedule.TestTypeId,

                        LocalDrivingLicenseApplicationId =
                            Schedule.LocalDrivingLicenseApplicationId,

                        AppointmentDate =
                            Schedule.Date,

                        RetakeTestApplicationId =
                            Schedule.RetakeTestApplicationId
                    };

                var createResult =
                    await _testAppointmentsApiClient
                        .CreateAsync(createRequest);

                if (createResult.IsFailure)
                {
                    ShowError(
                        createResult.Error,
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