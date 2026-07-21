using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Presentation.Views.Windows;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class ReleaseDetainedViewModel : ObservableObject
    {
        private readonly ILicenseService _licenseService;
        private readonly IDetainedLicenseService _detainedLicenseService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPersonService _personService;
        private readonly IDriverService _driverService;
        private readonly IInternationalService _internationalService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly IApplicationService _applicationService;

        [ObservableProperty]
        private string? licenseIdText;

        [ObservableProperty]
        private DriverLicenseInfoDto? licenseInfo;

        [ObservableProperty]
        private DetainedLicenseDto? release;

        [ObservableProperty]
        private decimal applicationFees;

        [ObservableProperty]
        private bool isLicenseIssued;

        public decimal TotalFees =>
            ApplicationFees + (Release?.FineFees ?? 0);

        public ReleaseDetainedViewModel(
            ILicenseService licenseService,
            IDetainedLicenseService detainedLicenseService,
            ICurrentUserService currentUserService,
            IPersonService personService,
            IDriverService driverService,
            IInternationalService internationalService,
            IApplicationTypeService applicationTypeService,
            IApplicationService applicationService)
        {
            _licenseService = licenseService;
            _detainedLicenseService = detainedLicenseService;
            _currentUserService = currentUserService;
            _personService = personService;
            _driverService = driverService;
            _internationalService = internationalService;
            _applicationTypeService = applicationTypeService;
            _applicationService = applicationService;
        }

        partial void OnReleaseChanged(DetainedLicenseDto? value)
        {
            IsLicenseIssued = value != null;
            OnPropertyChanged(nameof(TotalFees));
        }

        partial void OnApplicationFeesChanged(decimal value)
        {
            OnPropertyChanged(nameof(TotalFees));
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (!int.TryParse(LicenseIdText, out int licenseId))
                return;


            var licenseResult =
                await _licenseService.GetLicenseDetailsByIdAsync(licenseId);


            if (licenseResult.IsFailure)
            {
                LicenseInfo = null;
                Release = null;
                IsLicenseIssued = false;

                CustomMessageBox.Show(
                    licenseResult.Error,
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            LicenseInfo = licenseResult.Value;


            var releaseResult =
                await _detainedLicenseService
                .GetActiveDetainByLicenseIdAsync(licenseId);


            if (releaseResult == null)
            {
                Release = null;
                IsLicenseIssued = false;

                CustomMessageBox.Show(
                    "This license is not detained.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            Release = releaseResult;


            IsLicenseIssued = true;


            var applicationTypeResult = await _applicationTypeService.GetApplicationTypeByIdAsync(5);

            if (applicationTypeResult.IsFailure)
            {
                CustomMessageBox.Show(
                    applicationTypeResult.Error,"Error",
                    MessageBoxButton.OK,MessageBoxImage.Error);

                return;
            }

            var applicationType = applicationTypeResult.Value!;

            ApplicationFees = applicationType.ApplicationTypeFees;

            OnPropertyChanged(nameof(TotalFees));
        }

        [RelayCommand]
        private async Task ReleaseLicenseAsync()
        {
            if (Release == null || LicenseInfo == null)
            {
                CustomMessageBox.Show(
                    "Please search for a detained license first.",
                    "Error",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }

            try
            {
                var newApplication = new ApplicationDto
                {
                    ApplicantPersonID = LicenseInfo.PersonID,
                    ApplicationTypeID = 5,
                    ApplicationDate = DateTime.Now,
                    ApplicationStatus = Domain.Enums.AppStatus.Completed,
                    LastStatusDate = DateTime.Now,
                    PaidFees = ApplicationFees,
                    CreatedByUserID = _currentUserService.UserId,
                    CreatedByUserName = _currentUserService.Username
                };

                var result = await _applicationService.AddNewApplicationAsync(newApplication);
                int newApplicationId = result.Value;

                await _detainedLicenseService.ReleaseAsync(
                    Release.DetainID,
                    _currentUserService.UserId,
                    newApplicationId);

                Release.IsReleased = true;
                Release.ReleaseDate = DateTime.Now;

                CustomMessageBox.Show(
                    "License released successfully. The application has been created.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += "\n" + ex.InnerException.Message;
                }

                CustomMessageBox.Show(
                    $"An error occurred while processing the operation:\n{errorMessage}",
                    "Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ShowLicensesHistory()
        {
            if (LicenseInfo == null)
                return;

            var vm = new LicenseHistoryViewModel(
                _personService,
                _driverService,
                _licenseService,
                _internationalService);

            var window = new LicenseHistoryWin(vm, LicenseInfo.PersonID);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }

        [RelayCommand]
        private void ShowLicensesInfo()
        {
            if (LicenseInfo == null)
                return;

            var window = new DriverLicenseInfoWin(LicenseInfo.LicenseId);
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.ShowDialog();
        }
    }

    public static class CustomMessageBox
    {
        public static MessageBoxResult Show(
            string message,
            string title,
            MessageBoxButton button,
            MessageBoxImage icon)
        {
            var result = MessageBox.Show(message, title, button, icon);
            return result;
        }
    }
}