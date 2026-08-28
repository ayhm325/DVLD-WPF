using Application.DTOs.ApplicationDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.DTOs.LicenseDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class LocalApplicationDetailsViewModel
        : ObservableObject
    {
        private readonly ILocalDrivingLicenseApplicationService _localService;
        private readonly IApplicationService _applicationService;
        private readonly ILicenseService _licenseService;

        // =====================================================
        // APPLICATION INFO
        // =====================================================

        [ObservableProperty]
        private ApplicationBasicInfoDto? applicationInfo;

        // =====================================================
        // LOCAL APPLICATION INFO
        // =====================================================

        [ObservableProperty]
        private LocalDrivingLicenseApplicationListDto? ldlAppInfo;

        // =====================================================
        // LICENSE INFO
        // =====================================================

        [ObservableProperty]
        private LicenseDto? licenseInfo;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public LocalApplicationDetailsViewModel(
            ILocalDrivingLicenseApplicationService localService,
            IApplicationService applicationService,
            ILicenseService licenseService)
        {
            _localService =
                localService
                ?? throw new ArgumentNullException(
                    nameof(localService));

            _applicationService =
                applicationService
                ?? throw new ArgumentNullException(
                    nameof(applicationService));

            _licenseService =
                licenseService
                ?? throw new ArgumentNullException(
                    nameof(licenseService));
        }

        // =====================================================
        // LOAD
        // =====================================================

        public async Task LoadAsync(int localId)
        {
            // =================================================
            // RESET
            // =================================================

            ApplicationInfo = null;
            LdlAppInfo = null;
            LicenseInfo = null;

            // =================================================
            // VALIDATE
            // =================================================

            if (localId <= 0)
            {
                MessageBox.Show(
                    "Invalid local application ID.",
                    "Application Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                // =============================================
                // GET LOCAL APPLICATION
                // =============================================

                var localAppResult =
                    await _localService
                        .GetLocalDrivingLicenseApplicationByIdAsync(
                            localId);

                if (localAppResult.IsFailure)
                {
                    MessageBox.Show(
                        localAppResult.Error,
                        "Application Details",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                LdlAppInfo =
                    localAppResult.Value;

                // =============================================
                // GET APPLICATION ID
                // =============================================

                var appIdResult =
                    await _localService
                        .GetApplicationIdByLocalIdAsync(
                            localId);

                if (appIdResult.IsFailure)
                {
                    MessageBox.Show(
                        appIdResult.Error,
                        "Application Details",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var applicationId =
                    appIdResult.Value;

                // =============================================
                // GET BASIC APPLICATION INFO
                // =============================================

                var applicationResult =
                    await _applicationService
                        .GetBasicInfoAsync(
                            applicationId);

                if (applicationResult.IsFailure)
                {
                    MessageBox.Show(
                        applicationResult.Error,
                        "Application Details",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                ApplicationInfo =
                    applicationResult.Value;

                // =============================================
                // GET LICENSES FOR THIS APPLICATION
                // =============================================

                var licensesResult =
                    await _licenseService
                        .GetByApplicationIdAsync(
                            applicationId);

                if (licensesResult.IsFailure)
                {
                    LicenseInfo = null;

                    return;
                }

                var licenses =
                    licensesResult.Value
                    ?? new List<LicenseDto>();

                // =============================================
                // FIND LICENSE FOR THIS REQUEST'S CLASS
                // =============================================

                LicenseInfo =
                    licenses.FirstOrDefault(x =>
                        x.LicenseClassID ==
                        LdlAppInfo!.LicenseClassID);

                // =============================================
                // NO LICENSE
                // =============================================

                if (LicenseInfo is null)
                {
                    // This is not necessarily an error.
                    // The application may not have a license yet.
                    return;
                }
            }
            catch (Exception ex)
            {
                ApplicationInfo = null;
                LdlAppInfo = null;
                LicenseInfo = null;

                MessageBox.Show(
                    ex.Message,
                    "Application Details",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}