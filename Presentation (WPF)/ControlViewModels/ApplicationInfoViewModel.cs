using CommunityToolkit.Mvvm.ComponentModel;
using Presentation.Services.Api;
using DVLD.Contracts.Application;

namespace Presentation.ControlViewModels;

public partial class ApplicationInfoViewModel : ObservableObject
{
    private readonly IApplicationsApiClient _applicationsApiClient;

    [ObservableProperty]
    private ApplicationBasicInfoResponse? applicationInfo;

    public ApplicationInfoViewModel(IApplicationsApiClient applicationsApiClient)
    {
        _applicationsApiClient = applicationsApiClient;
    }

    public async Task LoadAsync(
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationsApiClient.GetBasicInfoAsync(
            applicationId,
            cancellationToken);

        ApplicationInfo = result.IsSuccess
            ? result.Value
            : null;
    }
}