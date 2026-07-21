using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows.Applications;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Presentation.ViewModels;

namespace Presentation.ViewModels
{
    public partial class ApplicationTypeViewModel : ObservableObject
    {
        private readonly IApplicationTypeService _applicationTypeService;

        // القائمة يجب أن تكون Public Property لكي يراها الـ DataGrid
        public ObservableCollection<ApplicationTypeDto> ApplicationTypes { get; } = new();

        public ApplicationTypeViewModel(IApplicationTypeService applicationTypeService)
        {
            _applicationTypeService = applicationTypeService;
            LoadApplicationTypesAsync();
        }

        private async void LoadApplicationTypesAsync()
        {
            var result = await _applicationTypeService.GetAllApplicationTypesAsync();

            if (result.IsFailure)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"DEBUG: Failed to load application types: {result.Error}");

                return;
            }

            var data = result.Value!;

            System.Diagnostics.Debug.WriteLine(
                $"DEBUG: Loaded {data.Count} items.");

            ApplicationTypes.Clear();

            foreach (var item in data)
            {
                ApplicationTypes.Add(item);
            }
        }

        [RelayCommand]
        private async Task EditApplicationType(ApplicationTypeDto? selectedType)
        {
            if (selectedType == null) return;


            var updateVm = App.ServiceProvider.GetRequiredService<UpdateApplicationTypeViewModel>();
            await updateVm.InitializeAsync(selectedType.ApplicationTypeId);

            var editWindow = new EditApplicationTypeWindow(updateVm);
            editWindow.ShowDialog();

            // تحديث القائمة بعد إغلاق النافذة
            LoadApplicationTypesAsync();
        }
    }
}