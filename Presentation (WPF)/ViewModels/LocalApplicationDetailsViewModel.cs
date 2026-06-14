using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation.ViewModels
{
    public partial class LocalApplicationDetailsViewModel : ObservableObject
    {
        private readonly ILocalDrivingLicenseApplicationService _localService;
        private readonly IApplicationService _applicationService;

        [ObservableProperty]
        private ApplicationBasicInfoDto? applicationInfo;
        [ObservableProperty]
        private LocalDrivingLicenseApplicationListDto? ldlAppInfo;

        public LocalApplicationDetailsViewModel(
            ILocalDrivingLicenseApplicationService localService,
            IApplicationService applicationService)
        {
            _localService = localService;
            _applicationService = applicationService;
        }

        public async Task LoadAsync(int localId)
        {
            var localApp = await _localService
                .GetLocalDrivingLicenseApplicationByIdAsync(localId);

            LdlAppInfo = localApp;

            var appId = await _localService
                .GetApplicationIdByLocalIdAsync(localId);

            if (!appId.HasValue || localApp == null)
                return;

            ApplicationInfo = await _applicationService
                .GetBasicInfoAsync(appId.Value);
        }
    }
}