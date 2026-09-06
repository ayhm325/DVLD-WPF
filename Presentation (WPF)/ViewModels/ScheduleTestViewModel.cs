using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using Presentation.Views.Windows;
using System.Linq;
using System.Windows;

namespace Presentation.ViewModels;

public partial class ScheduleTestViewModel : ObservableObject
{
    private readonly ITestAppointmentService _service;
    private readonly ILocalDrivingLicenseApplicationService _lDLAppService;
    private readonly ITestTypeService _testTypeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationTypeService _applicationTypeService;
    private readonly IApplicationService _appService;
    private readonly IUnitOfWork _unitOfWork;

    private const int RetakeApplicationTypeId = 7;

    public ScheduleTestViewModel(
        ITestAppointmentService service,
        ILocalDrivingLicenseApplicationService lDLAppService,
        ITestTypeService testTypeService,
        ICurrentUserService currentUserService,
        IApplicationTypeService applicationTypeService,
        IApplicationService appService,
        IUnitOfWork unitOfWork)
    {
        _service =
            service
            ?? throw new ArgumentNullException(nameof(service));

        _lDLAppService =
            lDLAppService
            ?? throw new ArgumentNullException(nameof(lDLAppService));

        _testTypeService =
            testTypeService
            ?? throw new ArgumentNullException(nameof(testTypeService));

        _currentUserService =
            currentUserService
            ?? throw new ArgumentNullException(
                nameof(currentUserService));

        _applicationTypeService =
            applicationTypeService
            ?? throw new ArgumentNullException(
                nameof(applicationTypeService));

        _appService =
            appService
            ?? throw new ArgumentNullException(nameof(appService));

        _unitOfWork =
            unitOfWork
            ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    [ObservableProperty]
    private ScheduleTestDto schedule = new();

    [ObservableProperty]
    private ApplicationDto appDto = new();

    [ObservableProperty]
    private bool isRetake;

    private int _localAppId;
    private int _applicationId;

    public decimal TotalFees =>
        (Schedule?.Fees ?? 0) +
        (Schedule?.RetakerFees ?? 0);

    public DateTime MinDate =>
        DateTime.Now.Date.AddDays(1);

    partial void OnScheduleChanged(
        ScheduleTestDto value)
    {
        OnPropertyChanged(nameof(IsRetake));
        OnPropertyChanged(nameof(TotalFees));
    }

    public async Task LoadAsync(
        int localAppId,
        TestTypeEnum type)
    {
        if (localAppId <= 0)
            throw new ArgumentOutOfRangeException(nameof(localAppId));

        _localAppId =
            localAppId;

        var appIdResult =
            await _lDLAppService
                .GetApplicationIdByLocalIdAsync(
                    localAppId);

        if (appIdResult.IsFailure)
            throw new Exception(appIdResult.Error);

        _applicationId =
            appIdResult.Value;

        var appInfoResult =
            await _lDLAppService
                .GetLocalDrivingLicenseApplicationByIdAsync(
                    localAppId);

        if (appInfoResult.IsFailure)
            throw new Exception(appInfoResult.Error);

        var appInfo =
            appInfoResult.Value;

        if (appInfo is null)
        {
            throw new Exception(
                "Local driving license application was not found.");
        }

        var appointmentsResult =
            await _service
                .GetByLocalDrivingLicenseApplicationIdAsync(
                    localAppId);

        if (appointmentsResult.IsFailure)
            throw new Exception(appointmentsResult.Error);

        var appointments =
            appointmentsResult.Value ?? [];

        var count =
            appointments.Count(
                x => x.TestTypeID == (int)type);

        var testFees =
            await _service
                .GetTestTypeFeesAsync(
                    (int)type);

        var retakeTypeResult =
            await _applicationTypeService
                .GetApplicationTypeByIdAsync(
                    RetakeApplicationTypeId);

        if (retakeTypeResult.IsFailure)
            throw new Exception(retakeTypeResult.Error);

        var retakeType =
            retakeTypeResult.Value;

        if (retakeType is null)
        {
            throw new Exception(
                "Retake application type was not found.");
        }

        var shouldShowRetake =
            count > 0;

        Schedule =
            new ScheduleTestDto
            {
                LocalDrivingLicenseApplicationID =
                    localAppId,

                FullName =
                    appInfo.FullName,

                LicenseClassName =
                    appInfo.LicenseClassName,

                Trial =
                    count + 1,

                Date =
                    MinDate,

                Fees =
                    testFees,

                RetakerFees =
                    shouldShowRetake
                        ? retakeType.ApplicationTypeFees
                        : 0,

                TestTypeID =
                    (int)type,

                AppointmentID =
                    0,

                RetakeTestApplicationID =
                    0
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
            await _service
                .GetScheduleInfoAsync(
                    appointmentId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        var data =
            result.Value;

        if (data is null)
        {
            MessageBox.Show(
                "Appointment information was not found.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        data.AppointmentID =
            appointmentId;

        var allAppointmentsResult =
            await _service
                .GetByLocalDrivingLicenseApplicationIdAsync(
                    data.LocalDrivingLicenseApplicationID);

        if (allAppointmentsResult.IsFailure)
        {
            MessageBox.Show(
                allAppointmentsResult.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        var allAppointments =
            allAppointmentsResult.Value ?? [];

        data.Trial =
            allAppointments.Count(
                x => x.TestTypeID == data.TestTypeID);

        IsRetake =
            data.RetakeTestApplicationID > 0;

        Schedule =
            data;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Schedule is null)
            return;

        if (!_currentUserService.IsLoggedIn ||
            _currentUserService.UserId <= 0)
        {
            MessageBox.Show(
                "You must be logged in first.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        try
        {
            if (IsRetake &&
                Schedule.AppointmentID == 0 &&
                Schedule.RetakeTestApplicationID == 0)
            {
                var applicationResult =
                    await _appService
                        .GetApplicationByIdAsync(
                            _applicationId);

                if (applicationResult.IsFailure)
                {
                    MessageBox.Show(
                        applicationResult.Error,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                var originalApplication =
                    applicationResult.Value;

                if (originalApplication is null)
                {
                    MessageBox.Show(
                        "Original application was not found.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                var createApplication =
                    new CreateApplicationDto
                    {
                        ApplicantPersonID =
                            originalApplication.ApplicantPersonID,

                        ApplicationTypeID =
                            RetakeApplicationTypeId
                    };

                var retakeResult =
                    await _appService
                        .AddNewApplicationAsync(
                            createApplication);

                if (retakeResult.IsFailure)
                {
                    MessageBox.Show(
                        retakeResult.Error,
                        "Save Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                Schedule.RetakeTestApplicationID =
                    retakeResult.Value;
            }

            Result saveResult;

            if (Schedule.AppointmentID > 0)
            {
                var updateDto =
                    new UpdateTestAppointmentDto
                    {
                        TestAppointmentID =
                            Schedule.AppointmentID,

                        AppointmentDate =
                            Schedule.Date
                    };

                saveResult =
                    await _service
                        .UpdateAsync(
                            updateDto);
            }
            else
            {
                var createDto =
                    new CreateTestAppointmentDto
                    {
                        TestTypeID =
                            Schedule.TestTypeID,

                        LocalDrivingLicenseApplicationID =
                            Schedule.LocalDrivingLicenseApplicationID,

                        AppointmentDate =
                            Schedule.Date,

                        RetakeTestApplicationID =
                            Schedule.RetakeTestApplicationID > 0
                                ? Schedule.RetakeTestApplicationID
                                : null
                    };

                saveResult =
                    await _service
                        .AddAsync(
                            createDto);
            }

            if (saveResult.IsFailure)
            {
                MessageBox.Show(
                    saveResult.Error,
                    "Save Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
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
            MessageBox.Show(
                ex.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
}
