using Application.Common.Results;
using Application.DTOs;
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

        public ScheduleTestViewModel(ITestAppointmentService service,
                                     ILocalDrivingLicenseApplicationService lDLAppService,
                                     ITestTypeService testTypeService,
                                     ICurrentUserService currentUserService,
                                     IApplicationTypeService applicationTypeService,
                                     IApplicationService appService)
        {
            _service = service;
            _lDLAppService = lDLAppService;
            _testTypeService = testTypeService;
            _currentUserService = currentUserService;
            _applicationTypeService = applicationTypeService;
            _appService = appService;
        }

        [ObservableProperty]
        private ScheduleTestDto schedule = new();
        [ObservableProperty]
        private ApplicationDto appDto = new();

        private int _localAppId;
        private int _applicationId;


        [ObservableProperty]
        private bool _isRetake;

        public decimal TotalFees => (Schedule?.Fees ?? 0) + (Schedule?.RetakerFees ?? 0);
        public DateTime MinDate => DateTime.Now.Date.AddDays(1);

        partial void OnScheduleChanged(ScheduleTestDto value)
        {
            OnPropertyChanged(nameof(IsRetake));
            OnPropertyChanged(nameof(TotalFees));
        }

        public async Task LoadAsync(int localAppId, TestTypeEnum type)
        {
            _localAppId = localAppId;

            var appIdResult = await _lDLAppService
                .GetApplicationIdByLocalIdAsync(localAppId);

            if (appIdResult.IsFailure)
                throw new Exception(appIdResult.Error);

            _applicationId = appIdResult.Value;


            var appInfoResult = await _lDLAppService
                .GetLocalDrivingLicenseApplicationByIdAsync(localAppId);


            if (appInfoResult.IsFailure)
                throw new Exception(appInfoResult.Error);


            var appInfo = appInfoResult.Value;


            var appointmentsResult = await _service.GetByApplicationIdAsync(localAppId);

            if (appointmentsResult.IsFailure)
                throw new Exception(appointmentsResult.Error);

            var appointments = appointmentsResult.Value!;

            var filteredAppointments = appointments
                .Where(x => x.TestTypeID == (int)type)
                .ToList();

            int count = filteredAppointments.Count;


            decimal testFees =
                await _service.GetTestTypeFeesAsync((int)type);


            var retakeTypeResult =
                await _applicationTypeService.GetApplicationTypeByIdAsync(7);

            if (retakeTypeResult.IsFailure)
                throw new Exception(retakeTypeResult.Error);

            var retakeType = retakeTypeResult.Value!;

            decimal retakeFees = retakeType.ApplicationTypeFees;


            bool shouldShowRetake = count > 0;


            Schedule = new ScheduleTestDto
            {
                LocalDrivingLicenseApplicationID = localAppId,

                FullName = appInfo.FullName,

                LicenseClassName = appInfo.LicenseClassName,

                Trial = count + 1,

                Date = MinDate,

                Fees = testFees,

                RetakerFees = shouldShowRetake
                    ? retakeFees
                    : 0,

                TestTypeID = (int)type
            };


            IsRetake = shouldShowRetake;
        }

        public async Task LoadForEditAsync(int appointmentId)
        {
            var result = await _service.GetScheduleInfoAsync(appointmentId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var data = result.Value!;

            data.AppointmentID = appointmentId;


            var allAppointmentsResult =
                await _service.GetByApplicationIdAsync(data.LocalDrivingLicenseApplicationID);

            if (allAppointmentsResult.IsFailure)
            {
                MessageBox.Show(
                    allAppointmentsResult.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var allAppointments = allAppointmentsResult.Value!;


            data.Trial = allAppointments.Count(
                x => x.TestTypeID == data.TestTypeID);


            Schedule = data;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (Schedule == null) return;

            try
            {
                // إذا كان Retake ولم يتم إنشاء طلب إعادة بعد
                if (IsRetake && Schedule.AppointmentID == 0 && Schedule.RetakeTestApplicationID == 0)
                {
                    var applicationResult = await _appService
    .GetApplicationByIdAsync(_applicationId);

                    if (applicationResult.IsFailure)
                        throw new Exception(applicationResult.Error);

                    var originalApplication = applicationResult.Value;

                    AppDto = new ApplicationDto
                    {
                        ApplicantPersonID = originalApplication.ApplicantPersonID,
                        ApplicationDate = DateTime.Now,
                        ApplicationTypeID = 7, // Retake Test
                        ApplicationStatus = AppStatus.New,
                        LastStatusDate = DateTime.Now,
                        PaidFees = TotalFees,
                        CreatedByUserID = _currentUserService.UserId
                    };


                    var retakeResult = await _appService
                        .AddNewApplicationAsync(AppDto);

                    if (retakeResult.IsFailure)
                        throw new Exception(retakeResult.Error);

                    Schedule.RetakeTestApplicationID = retakeResult.Value;
                }

                var appointmentToSave = MapScheduleToAppointment(Schedule);

                Result saveResult;

                if (Schedule.AppointmentID > 0)
                {
                    saveResult = await _service.UpdateAsync(appointmentToSave);
                }
                else
                {
                    saveResult = await _service.AddAsync(appointmentToSave);
                }

                if (saveResult.IsSuccess)
                {
                    MessageBox.Show("Appointment saved successfully.");
                }
                else
                {
                    MessageBox.Show(
                        saveResult.Error,
                        "Save Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private TestAppointmentDto MapScheduleToAppointment(ScheduleTestDto schedule)
        {
            return new TestAppointmentDto
            {
                TestAppointmentID = schedule.AppointmentID,
                LocalDrivingLicenseApplicationID = schedule.LocalDrivingLicenseApplicationID,
                AppointmentID = schedule.AppointmentID,
                TestTypeID = schedule.TestTypeID,
                AppointmentDate = schedule.Date,
                PaidFees = schedule.Fees,
                CreatedByUserID = _currentUserService.UserId,
                IsLocked = false,
                // التعديل هنا: إذا كان الـ ID هو 0، نرسل null ليتم التعامل معه كـ NULL في قاعدة البيانات
                RetakeTestApplicationID = (schedule.RetakeTestApplicationID > 0) ? schedule.RetakeTestApplicationID : null
            };
        }

        [RelayCommand]
        private void Close()
        {
            var window = System.Windows.Application.Current.Windows.OfType<ScheduleTestWin>().FirstOrDefault();
            window?.Close();
        }
    }
}