using Application.DTOs.ApplicationDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class IssueDrivingLicenseForTheFirstTimeViewModel : ObservableObject
    {
        private readonly ILicenseService _licenseService;
        private readonly ILocalDrivingLicenseApplicationService _localAppService;
        private readonly IApplicationService _applicationService;
        private readonly int _localAppId;
        private readonly Window _window;

        // App Info
        [ObservableProperty]
        private int drivingLicenseApplicationId;

        [ObservableProperty]
        private string licenseClassName = string.Empty;

        [ObservableProperty]
        private int passedTests;

        [ObservableProperty]
        private int totalTests = 3;

        // Basic Info
        [ObservableProperty]
        private ApplicationBasicInfoDto? basicApplicationInfo;

        // Form
        [ObservableProperty]
        private string? notes;

        [ObservableProperty]
        private bool isBusy;

        // Constructor
        public IssueDrivingLicenseForTheFirstTimeViewModel(
            int localAppId,
            Window window,
            ILicenseService licenseService,
            ILocalDrivingLicenseApplicationService localAppService,
            IApplicationService applicationService)
        {
            _localAppId = localAppId;
            _window = window;
            _licenseService = licenseService;
            _localAppService = localAppService;
            _applicationService = applicationService;
            DrivingLicenseApplicationId = localAppId;
            _ = LoadAsync();
        }

        // Load
        private async Task LoadAsync()
        {
            try
            {
                var localAppResult = await _localAppService.GetLocalDrivingLicenseApplicationByIdAsync(_localAppId);
                if (localAppResult.IsFailure) return;

                var localApp = localAppResult.Value!;
                DrivingLicenseApplicationId = localApp.LocalDrivingLicenseApplicationID;
                LicenseClassName = localApp.LicenseClassName ?? string.Empty;
                PassedTests = localApp.PassedTest;

                // Get Application ID
                var applicationIdResult = await _localAppService.GetApplicationIdByLocalIdAsync(_localAppId);
                if (applicationIdResult.IsFailure)
                {
                    BasicApplicationInfo = null;
                    return;
                }
                int applicationId = applicationIdResult.Value;

                // Get basic info
                var result = await _applicationService.GetBasicInfoAsync(applicationId);
                if (result.IsFailure)
                {
                    BasicApplicationInfo = null;
                    return;
                }
                BasicApplicationInfo = result.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Close
        [RelayCommand]
        private void Close() => _window.Close();

        // Issue
        private bool CanIssue() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanIssue))]
        private async Task Issue()
        {
            try
            {
                IsBusy = true;

                var result = await _licenseService.IssueFirstLicenseAsync(_localAppId, Notes);

                if (result.IsFailure)
                {
                    MessageBox.Show(result.Error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("License issued successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _window.Close();
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                    errorMessage += "\n\nInner Exception: " + ex.InnerException.Message;
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                IssueCommand.NotifyCanExecuteChanged();
            }
        }
    }
}