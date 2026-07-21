using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows.Applications;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Presentation.ControlViewModels
{
    public partial class ApplicationInfoViewModel : ObservableObject
    {
        private  readonly IApplicationService _applicationService;

        [ObservableProperty]
        private ApplicationBasicInfoDto? applicationInfo;

        public ApplicationInfoViewModel(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }


        public async Task LoadAsync(int applicationId)
        {
            var result = await _applicationService.GetBasicInfoAsync(applicationId);

            if (result.IsFailure)
            {
                ApplicationInfo = null;
                return;
            }

            ApplicationInfo = result.Value;
        }


    }
}
