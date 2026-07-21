using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

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
            var localAppResult = await _localService
                .GetLocalDrivingLicenseApplicationByIdAsync(localId);


            if (localAppResult.IsFailure)
            {
                LdlAppInfo = null;
                return;
            }


            LdlAppInfo = localAppResult.Value;


            var appIdResult = await _localService
                .GetApplicationIdByLocalIdAsync(localId);


            if (appIdResult.IsFailure)
            {
                ApplicationInfo = null;
                return;
            }


            int appId = appIdResult.Value;


            var result = await _applicationService
                .GetBasicInfoAsync(appId);


            if (result.IsSuccess)
            {
                ApplicationInfo = result.Value;
            }
            else
            {
                ApplicationInfo = null;
                MessageBox.Show(result.Error);
            }
        }
    }
}