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

        public ScheduleTestViewModel(ITestAppointmentService service,
                                     ILocalDrivingLicenseApplicationService lDLAppService,
                                     ITestTypeService testTypeService,
                                     ICurrentUserService currentUserService,
                                     IApplicationTypeService applicationTypeService)
        {
            _service = service;
            _lDLAppService = lDLAppService;
            _testTypeService = testTypeService;
            _currentUserService = currentUserService;
            _applicationTypeService = applicationTypeService;
        }

        [ObservableProperty]
        private ScheduleTestDto schedule = new();

        private int _localAppId;
      

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
           

            var appInfo = await _lDLAppService.GetLocalDrivingLicenseApplicationByIdAsync(localAppId);
            if (appInfo == null) throw new Exception("Application not found");

            var appointments = await _service.GetByApplicationIdAsync(localAppId);
            var filteredAppointments = appointments.Where(x => x.TestTypeID == (int)type).ToList();

            int count = filteredAppointments.Count;
            decimal testFees = await _service.GetTestTypeFeesAsync((int)type);
            var retakeType = await _applicationTypeService.GetApplicationTypeByIdAsync(7);
            decimal retakeFees = retakeType?.ApplicationTypeFees ?? 0;

            // نقوم بإنشاء كائن واحد فقط ونملأه بالكامل
            bool shouldShowRetake = count > 0;

            Schedule = new ScheduleTestDto
            {
                LocalDrivingLicenseApplicationID = localAppId,
                FullName = appInfo.FullName,
                LicenseClassName = appInfo.LicenseClassName,
                Trial = count + 1,
                Date = MinDate,
                Fees = testFees,
                RetakerFees = shouldShowRetake ? retakeFees : 0,
                TestTypeID = (int)type
            };

            // تحديث الحالة لتظهر في الواجهة
            IsRetake = shouldShowRetake;
        }

        public async Task LoadForEditAsync(int appointmentId)
        {
            var data = await _service.GetScheduleInfoAsync(appointmentId);
            if (data == null)
            {
                MessageBox.Show("Appointment record not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            

            data.AppointmentID = appointmentId;

            var allAppointments = await _service.GetByApplicationIdAsync(data.LocalDrivingLicenseApplicationID);
            data.Trial = allAppointments.Count(x => x.TestTypeID == data.TestTypeID);

            Schedule = data;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (Schedule == null) return;
            

            var appointmentToSave = MapScheduleToAppointment(Schedule);

            try
            {
                bool isSuccess = Schedule.AppointmentID > 0
                    ? await _service.UpdateAsync(appointmentToSave)
                    : await _service.AddAsync(appointmentToSave);

                if (isSuccess)
                {
                    MessageBox.Show("Appointment saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            catch (Exception ex)
            {
                string message = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Failed to save: {message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                IsLocked = false
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