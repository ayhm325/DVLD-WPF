using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using System.Threading.Tasks;
using System.Windows;
using Presentation.ViewModels;

namespace Presentation.ViewModels
{
    public partial class UpdateApplicationTypeViewModel : ObservableObject
    {
        private readonly IApplicationTypeService _applicationTypeService;

        [ObservableProperty]
        private ApplicationTypeDto? _currentApplicationType = new();

        public UpdateApplicationTypeViewModel(IApplicationTypeService applicationTypeService)
        {
            _applicationTypeService = applicationTypeService;
        }

        // دالة التهيئة التي ستناديها عند فتح النافذة
        public async Task InitializeAsync(int id)
        {
            CurrentApplicationType = await _applicationTypeService.GetApplicationTypeByIdAsync(id);
        }

        // أمر الحفظ
        [RelayCommand]
        private async Task SaveAsync(Window window)
        {
            if (CurrentApplicationType == null) return;

            var appDto = new ApplicationTypeDto
            {
                ApplicationTypeId = CurrentApplicationType.ApplicationTypeId,
                ApplicationTypeTitle = CurrentApplicationType.ApplicationTypeTitle,
                ApplicationTypeFees = CurrentApplicationType.ApplicationTypeFees
            };

            bool result = await _applicationTypeService.UpdateApplicationTypeAsync(CurrentApplicationType.ApplicationTypeId, appDto);

            if (result)
            {
                MessageBox.Show("Application Type updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                window?.Close();
            }
        }



        [RelayCommand]
        private void Close(Window window)
        {
            window?.Close();
        }


    }
}