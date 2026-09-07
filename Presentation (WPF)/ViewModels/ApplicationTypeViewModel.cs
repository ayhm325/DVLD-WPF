using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.ApplicationType;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows.Applications;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Presentation.ViewModels;

public partial class ApplicationTypeViewModel : ObservableObject
{
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;

    public ObservableCollection<ApplicationTypeResponse> ApplicationTypes { get; } = new();

    public ApplicationTypeViewModel(
        IApplicationTypesApiClient applicationTypesApiClient)
    {
        _applicationTypesApiClient = applicationTypesApiClient;

        _ = LoadApplicationTypesAsync();
    }

    private async Task LoadApplicationTypesAsync()
    {
        var result = await _applicationTypesApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            System.Diagnostics.Debug.WriteLine(
                $"DEBUG: Failed to load application types: {result.Error}");

            return;
        }

        var data = result.Value ?? new List<ApplicationTypeResponse>();

        System.Diagnostics.Debug.WriteLine(
            $"DEBUG: Loaded {data.Count} items.");

        ApplicationTypes.Clear();

        foreach (var item in data)
        {
            ApplicationTypes.Add(item);
        }
    }

    [RelayCommand]
    private async Task EditApplicationType(
        ApplicationTypeResponse? selectedType)
    {
        if (selectedType == null)
            return;

        var updateVm =
            App.ServiceProvider
                .GetRequiredService<UpdateApplicationTypeViewModel>();

        await updateVm.InitializeAsync(
            selectedType.ApplicationTypeId);

        var editWindow =
            new EditApplicationTypeWindow(updateVm);

        editWindow.ShowDialog();

        await LoadApplicationTypesAsync();
    }
}