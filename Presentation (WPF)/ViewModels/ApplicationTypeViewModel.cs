using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.ApplicationType;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows.Applications;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class ApplicationTypeViewModel : ObservableObject
{
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;
    private readonly IApiNotificationService _notifications;

    public ObservableCollection<ApplicationTypeResponse> ApplicationTypes { get; } = [];

    public ApplicationTypeViewModel(
        IApplicationTypesApiClient applicationTypesApiClient,
        IApiNotificationService notifications)
    {
        _applicationTypesApiClient = applicationTypesApiClient
            ?? throw new ArgumentNullException(nameof(applicationTypesApiClient));

        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));

        _ = LoadApplicationTypesAsync();
    }

    private async Task LoadApplicationTypesAsync()
    {
        var result = await _applicationTypesApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            _notifications.ShowFailure(
                result,
                "Load Application Types Failed");
            return;
        }

        ApplicationTypes.Clear();

        if (result.Value is null)
            return;

        foreach (var item in result.Value)
            ApplicationTypes.Add(item);
    }

    [RelayCommand]
    private async Task EditApplicationType(
        ApplicationTypeResponse? selectedType)
    {
        if (selectedType is null)
            return;

        var updateVm = App.ServiceProvider
            .GetRequiredService<UpdateApplicationTypeViewModel>();

        await updateVm.InitializeAsync(
            selectedType.ApplicationTypeId);

        if (updateVm.CurrentApplicationType is null)
            return;

        var editWindow = new EditApplicationTypeWindow(updateVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        editWindow.ShowDialog();

        await LoadApplicationTypesAsync();
    }
}