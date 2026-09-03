using Application.DTOs.TestAppointmentDTO;
using Application.DTOs.TestDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class TakeTestViewModel : ObservableObject
    {
        private readonly ITestAppointmentService _service;
        private readonly ITestWorkflowService _testWorkflowService;
        private readonly ICurrentUserService _currentUser;
        private readonly IApplicationService _applicationService;
        private readonly ILocalDrivingLicenseApplicationService _localService;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public TakeTestViewModel(
            ITestAppointmentService service,
            ITestWorkflowService testWorkflowService,
            ICurrentUserService currentUser,
            IApplicationService applicationService,
            ILocalDrivingLicenseApplicationService localService)
        {
            _service =
                service
                ?? throw new ArgumentNullException(nameof(service));

            _testWorkflowService =
                testWorkflowService
                ?? throw new ArgumentNullException(
                    nameof(testWorkflowService));

            _currentUser =
                currentUser
                ?? throw new ArgumentNullException(nameof(currentUser));

            _applicationService =
                applicationService
                ?? throw new ArgumentNullException(
                    nameof(applicationService));

            _localService =
                localService
                ?? throw new ArgumentNullException(nameof(localService));
        }

        // =========================================================
        // STATE
        // =========================================================

        [ObservableProperty]
        private TestResultType testResult =
            TestResultType.Fail;

        [ObservableProperty]
        private string notes =
            string.Empty;

        [ObservableProperty]
        private ScheduleTestDto? schedule;

        // =========================================================
        // DISPLAY DATA
        // =========================================================

        [ObservableProperty]
        private string fullName =
            string.Empty;

        [ObservableProperty]
        private string licenseClassName =
            string.Empty;

        [ObservableProperty]
        private decimal fees;

        // =========================================================
        // PROPERTY CHANGED
        // =========================================================

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
            OnPropertyChanged(
                nameof(IsPassed));

            OnPropertyChanged(
                nameof(IsFailed));

            OnPropertyChanged(
                nameof(IsNotTaken));
        }

        // =========================================================
        // RESULT TOGGLES
        // =========================================================

        public bool IsPassed
        {
            get =>
                TestResult ==
                TestResultType.Pass;

            set
            {
                if (value)
                {
                    TestResult =
                        TestResultType.Pass;
                }
            }
        }

        public bool IsFailed
        {
            get =>
                TestResult ==
                TestResultType.Fail;

            set
            {
                if (value)
                {
                    TestResult =
                        TestResultType.Fail;
                }
            }
        }

        public bool IsNotTaken
        {
            get =>
                TestResult ==
                TestResultType.NotTaken;

            set
            {
                if (value)
                {
                    TestResult =
                        TestResultType.NotTaken;
                }
            }
        }

        // =========================================================
        // CLOSE
        // =========================================================

        [RelayCommand]
        private void Close()
        {
            System.Windows.Application.Current.Windows
                .OfType<TakeTestWin>()
                .FirstOrDefault()?
                .Close();
        }

        // =========================================================
        // SAVE TEST RESULT
        // =========================================================

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

            // -----------------------------------------------------
            // USER VALIDATION
            // -----------------------------------------------------

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

            // -----------------------------------------------------
            // VALIDATE RESULT
            // -----------------------------------------------------

            if (TestResult ==
                TestResultType.NotTaken)
            {
                MessageBox.Show(
                    "Please select Pass or Fail.",
                    "Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // -----------------------------------------------------
            // BUILD DTO
            // -----------------------------------------------------

            var saveTestResultDto =
                new SaveTestResultDto
                {
                    TestAppointmentID =
                        Schedule.AppointmentID,

                    TestResult =
                        TestResult ==
                        TestResultType.Pass,

                    Notes =
                        string.IsNullOrWhiteSpace(Notes)
                            ? null
                            : Notes.Trim()

                    
                };

            try
            {
                // -------------------------------------------------
                // SAVE RESULT
                // -------------------------------------------------

                var saveResult =
                    await _service
                        .SaveTestResultAsync(
                            saveTestResultDto);

                if (saveResult.IsFailure)
                {
                    MessageBox.Show(
                        saveResult.Error,
                        "Take Test",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                // -------------------------------------------------
                // IF FAILED
                // -------------------------------------------------

                if (!saveTestResultDto.TestResult)
                {
                    MessageBox.Show(
                        "Test result saved successfully.\n\nResult: Failed.",
                        "Take Test",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Close();

                    return;
                }

                // -------------------------------------------------
                // IF PASSED
                // -------------------------------------------------

                var localAppId =
                    Schedule
                        .LocalDrivingLicenseApplicationID;

                // -------------------------------------------------
                // CHECK ALL TESTS
                // -------------------------------------------------

                var passedAllTests =
                    await _testWorkflowService
                        .HasPassedAllTestsAsync(
                            localAppId);

                if (passedAllTests)
                {
                    MessageBox.Show(
                        "Test passed successfully.\n\n" +
                        "All three tests have been passed.\n" +
                        "The application is ready for license issuance.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Close();

                    return;
                }

                // -------------------------------------------------
                // PASSED BUT NOT ALL TESTS
                // -------------------------------------------------

                MessageBox.Show(
                    "Test result saved successfully.\n\n" +
                    "Result: Passed.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                var errorMessage =
                    ex.Message;

                if (ex.InnerException is not null)
                {
                    errorMessage +=
                        "\n\nInner Exception:\n" +
                        ex.InnerException.Message;
                }

                MessageBox.Show(
                    errorMessage,
                    "Take Test",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // =========================================================
        // LOAD
        // =========================================================

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
                var errorMessage =
                    ex.Message;

                if (ex.InnerException is not null)
                {
                    errorMessage +=
                        "\n\nInner Exception:\n" +
                        ex.InnerException.Message;
                }

                MessageBox.Show(
                    errorMessage,
                    "Loading Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}