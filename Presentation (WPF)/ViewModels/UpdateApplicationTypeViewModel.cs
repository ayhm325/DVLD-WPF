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

        public async Task InitializeAsync(int id)
        {
            var result = await _applicationTypeService
                .GetApplicationTypeByIdAsync(id);

            if (result.IsFailure)
            {
                CurrentApplicationType = null;

                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            CurrentApplicationType = result.Value!;
        }

        // أمر الحفظ
        [RelayCommand]
        private async Task SaveAsync(Window window)
        {
            if (CurrentApplicationType == null)
                return;

            var appDto = new ApplicationTypeDto
            {
                ApplicationTypeId = CurrentApplicationType.ApplicationTypeId,
                ApplicationTypeTitle = CurrentApplicationType.ApplicationTypeTitle,
                ApplicationTypeFees = CurrentApplicationType.ApplicationTypeFees
            };


            var result = await _applicationTypeService
                .UpdateApplicationTypeAsync(
                    CurrentApplicationType.ApplicationTypeId,
                    appDto);


            if (result.IsSuccess)
            {
                MessageBox.Show(
                    "Application Type updated successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                window?.Close();
            }
            else
            {
                MessageBox.Show(
                    result.Error,
                    "Update Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }



        [RelayCommand]
        private void Close(Window window)
        {
            window?.Close();
        }


    }
}