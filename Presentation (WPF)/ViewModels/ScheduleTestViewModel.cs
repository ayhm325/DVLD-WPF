using Application.Common.Results;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.TestAppointmentDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using Presentation.Views.Windows;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class ScheduleTestViewModel : ObservableObject
    {
        private readonly ITestAppointmentService _service;
        private readonly ILocalDrivingLicenseApplicationService _lDLAppService;
        private readonly ITestTypeService _testTypeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly IApplicationService _appService;
        private readonly IUnitOfWork _unitOfWork;

        public ScheduleTestViewModel(
            ITestAppointmentService service,
            ILocalDrivingLicenseApplicationService lDLAppService,
            ITestTypeService testTypeService,
            ICurrentUserService currentUserService,
            IApplicationTypeService applicationTypeService,
            IApplicationService appService,
            IUnitOfWork unitOfWork)
        {
            _service = service
                ?? throw new ArgumentNullException(nameof(service));

            _lDLAppService = lDLAppService
                ?? throw new ArgumentNullException(nameof(lDLAppService));

            _testTypeService = testTypeService
                ?? throw new ArgumentNullException(nameof(testTypeService));

            _currentUserService = currentUserService
                ?? throw new ArgumentNullException(nameof(currentUserService));

            _applicationTypeService = applicationTypeService
                ?? throw new ArgumentNullException(nameof(applicationTypeService));

            _appService = appService
                ?? throw new ArgumentNullException(nameof(appService));

            _unitOfWork = unitOfWork
                ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // =========================================================
        // PROPERTIES
        // =========================================================

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

        // =========================================================
        // PROPERTY CHANGED
        // =========================================================

        partial void OnScheduleChanged(ScheduleTestDto value)
        {
            OnPropertyChanged(nameof(IsRetake));
            OnPropertyChanged(nameof(TotalFees));
        }

        // =========================================================
        // LOAD - NEW APPOINTMENT
        // =========================================================

        public async Task LoadAsync(
            int localAppId,
            TestTypeEnum type)
        {
            _localAppId = localAppId;

            // -----------------------------------------------------
            // Get Application ID from Local Driving License App
            // -----------------------------------------------------

            var appIdResult =
                await _lDLAppService
                    .GetApplicationIdByLocalIdAsync(localAppId);

            if (appIdResult.IsFailure)
                throw new Exception(appIdResult.Error);

            _applicationId = appIdResult.Value;

            // -----------------------------------------------------
            // Get Local Driving License Application info
            // -----------------------------------------------------

            var appInfoResult =
                await _lDLAppService
                    .GetLocalDrivingLicenseApplicationByIdAsync(localAppId);

            if (appInfoResult.IsFailure)
                throw new Exception(appInfoResult.Error);

            var appInfo = appInfoResult.Value;

            if (appInfo is null)
                throw new Exception(
                    "Local driving license application was not found.");

            // -----------------------------------------------------
            // Get previous appointments
            //
            // IMPORTANT:
            // This method expects LocalDrivingLicenseApplicationID.
            // -----------------------------------------------------

            var appointmentsResult =
                await _service
                    .GetByLocalDrivingLicenseApplicationIdAsync(
                        _localAppId);

            if (appointmentsResult.IsFailure)
                throw new Exception(appointmentsResult.Error);

            var appointments =
                appointmentsResult.Value ?? [];

            // -----------------------------------------------------
            // Only appointments for the selected test type
            // -----------------------------------------------------

            var filteredAppointments =
                appointments
                    .Where(x => x.TestTypeID == (int)type)
                    .ToList();

            int count = filteredAppointments.Count;

            // -----------------------------------------------------
            // Test fees
            // -----------------------------------------------------

            decimal testFees =
                await _service.GetTestTypeFeesAsync((int)type);

            // -----------------------------------------------------
            // Retake Application Type
            // 7 = Retake Test
            // -----------------------------------------------------

            const int retakeApplicationTypeId = 7;

            var retakeTypeResult =
                await _applicationTypeService
                    .GetApplicationTypeByIdAsync(
                        retakeApplicationTypeId);

            if (retakeTypeResult.IsFailure)
                throw new Exception(retakeTypeResult.Error);

            var retakeType = retakeTypeResult.Value;

            if (retakeType is null)
                throw new Exception(
                    "Retake application type was not found.");

            decimal retakeFees =
                retakeType.ApplicationTypeFees;

            // -----------------------------------------------------
            // If there are previous attempts,
            // this appointment is a Retake.
            // -----------------------------------------------------

            bool shouldShowRetake = count > 0;

            // -----------------------------------------------------
            // Prepare Schedule DTO for UI
            // -----------------------------------------------------

            Schedule = new ScheduleTestDto
            {
                LocalDrivingLicenseApplicationID = localAppId,

                FullName = appInfo.FullName,

                LicenseClassName =
                    appInfo.LicenseClassName,

                Trial = count + 1,

                Date = MinDate,

                Fees = testFees,

                RetakerFees =
                    shouldShowRetake
                        ? retakeFees
                        : 0,

                TestTypeID = (int)type,

                AppointmentID = 0,

                RetakeTestApplicationID = 0
            };

            IsRetake = shouldShowRetake;
        }

        // =========================================================
        // LOAD - EDIT APPOINTMENT
        // =========================================================

        public async Task LoadForEditAsync(
            int appointmentId)
        {
            var result =
                await _service
                    .GetScheduleInfoAsync(appointmentId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var data = result.Value;

            if (data is null)
            {
                MessageBox.Show(
                    "Appointment information was not found.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            data.AppointmentID = appointmentId;

            // -----------------------------------------------------
            // Get all appointments for the Local Driving License
            // Application.
            //
            // IMPORTANT:
            // We already have LocalDrivingLicenseApplicationID.
            // There is no reason to convert it to ApplicationID.
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // Trial number
            // -----------------------------------------------------

            data.Trial =
                allAppointments.Count(
                    x => x.TestTypeID == data.TestTypeID);

            // -----------------------------------------------------
            // Determine Retake state
            // -----------------------------------------------------

            IsRetake =
                data.RetakeTestApplicationID > 0;

            // -----------------------------------------------------
            // Load schedule
            // -----------------------------------------------------

            Schedule = data;
        }

        // =========================================================
        // SAVE
        // =========================================================

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (Schedule is null)
                return;

            await using var transaction =
                await _unitOfWork.BeginTransactionAsync();

            try
            {
                // =================================================
                // RETAKE APPLICATION
                // =================================================

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
                        await transaction.RollbackAsync();

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
                        await transaction.RollbackAsync();

                        MessageBox.Show(
                            "Original application was not found.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        return;
                    }

                    // -------------------------------------------------
                    // Create Retake Application DTO
                    // -------------------------------------------------

                    var createApplication =
                        new CreateApplicationDto
                        {
                            ApplicantPersonID =
                                originalApplication.ApplicantPersonID,

                            ApplicationDate =
                                DateTime.Now,

                            ApplicationTypeID = 7,

                            ApplicationStatus =
                                AppStatus.New,

                            LastStatusDate =
                                DateTime.Now,

                            PaidFees =
                                TotalFees,

                            CreatedByUserID =
                                _currentUserService.UserId
                        };

                    // -------------------------------------------------
                    // Create Retake Application
                    // -------------------------------------------------

                    var retakeResult =
                        await _appService
                            .AddNewApplicationAsync(
                                createApplication);

                    if (retakeResult.IsFailure)
                    {
                        await transaction.RollbackAsync();

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

                // =================================================
                // CREATE / UPDATE APPOINTMENT
                // =================================================

                Result saveResult;

                if (Schedule.AppointmentID > 0)
                {
                    // ---------------------------------------------
                    // UPDATE
                    // ---------------------------------------------

                    var updateDto =
                        new UpdateTestAppointmentDto
                        {
                            TestAppointmentID =
                                Schedule.AppointmentID,

                            AppointmentDate =
                                Schedule.Date
                        };

                    saveResult =
                        await _service.UpdateAsync(
                            updateDto);
                }
                else
                {
                    // ---------------------------------------------
                    // CREATE
                    // ---------------------------------------------

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
                        await _service.AddAsync(
                            createDto);
                }

                // =================================================
                // APPOINTMENT FAILED
                // =================================================

                if (saveResult.IsFailure)
                {
                    await transaction.RollbackAsync();

                    MessageBox.Show(
                        saveResult.Error,
                        "Save Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                // =================================================
                // COMMIT
                // =================================================

                await transaction.CommitAsync();

                // =================================================
                // SUCCESS
                // =================================================

                MessageBox.Show(
                    "Appointment saved successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Preserve the original exception.
                }

                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // CLOSE
        // =========================================================

        [RelayCommand]
        private void Close()
        {
            var window =
                System.Windows.Application.Current.Windows
                    .OfType<ScheduleTestWin>()
                    .FirstOrDefault();

            window?.Close();
        }
    }
}