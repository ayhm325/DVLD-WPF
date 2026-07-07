using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using Presentation.Views.Windows;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class TakeTestViewModel : ObservableObject
    {
        private readonly ITestAppointmentService _service;
        private readonly ICurrentUserService _currentUser;
        private readonly IApplicationService _applicationService;
        private readonly ILocalDrivingLicenseApplicationService _localService;

        public TakeTestViewModel(
            ITestAppointmentService service,
            ICurrentUserService currentUser,
            IApplicationService applicationService,
            ILocalDrivingLicenseApplicationService localService)
        {
            _service = service;
            _currentUser = currentUser;
            _applicationService = applicationService;
            _localService = localService;
        }

        // =========================
        // STATE
        // =========================

        [ObservableProperty]
        private TestResultType testResult = TestResultType.Fail;

        [ObservableProperty]
        private string notes = string.Empty;

        [ObservableProperty]
        private ScheduleTestDto? schedule;

        // ✳️ IMPORTANT: make these observable (NOT computed)
        [ObservableProperty]
        private string fullName;

        [ObservableProperty]
        private string licenseClassName;

        [ObservableProperty]
        private decimal fees;

        // =========================
        // PROPERTY CHANGED HOOKS
        // =========================

        partial void OnScheduleChanged(ScheduleTestDto value)
        {
            if (value == null) return;

            FullName = value.FullName;
            LicenseClassName = value.LicenseClassName;
            Fees = value.Fees;
        }

        partial void OnTestResultChanged(TestResultType value)
        {
            OnPropertyChanged(nameof(IsPassed));
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(IsNotTaken));
        }

        // =========================
        // TOGGLES
        // =========================

        public bool IsPassed
        {
            get => TestResult == TestResultType.Pass;
            set { if (value) TestResult = TestResultType.Pass; }
        }

        public bool IsFailed
        {
            get => TestResult == TestResultType.Fail;
            set { if (value) TestResult = TestResultType.Fail; }
        }

        public bool IsNotTaken
        {
            get => TestResult == TestResultType.Fail;
            set { if (value) TestResult = TestResultType.Fail; }
        }

        // =========================
        // COMMANDS
        // =========================

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
            if (Schedule == null)
                return;

            var testDto = new TestDto
            {
                TestAppointmentID = Schedule.AppointmentID,
                TestResult = TestResult == TestResultType.Pass,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                CreatedByUserID = _currentUser.UserId
            };

            var isSaved = await _service.SaveTestResultAsync(testDto);

            if (isSaved)
            {
                if (testDto.TestResult)
                {
                    int? applicationId = await _localService
                        .GetApplicationIdByLocalIdAsync(Schedule.LocalDrivingLicenseApplicationID);

                    if (applicationId.HasValue)
                    {
                        bool passedAll = await _service.HasPassedAllTestsAsync(applicationId.Value);

                        if (passedAll)
                        {
                            await _applicationService.CompleteApplicationAsync(applicationId.Value);
                        }
                    }
                }

                MessageBox.Show("Result saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);               
                Close();
            }
            else
            {
                MessageBox.Show("Save failed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


        }

        // =========================
        // LOAD
        // =========================

        public async Task LoadAsync(int appointmentId)
        {
            var data = await _service.GetScheduleInfoAsync(appointmentId);

            if (data == null)
                return;

            Schedule = data;

            int trialCount = await _service.GetTrialCountAsync(data.LocalDrivingLicenseApplicationID, data.TestTypeID);
            Schedule.Trial = trialCount;
        }
    }
}