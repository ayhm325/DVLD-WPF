using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.ViewModels;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class UpdateTestTypeViewModel : ObservableObject
    {
        private readonly ITestTypeService _testTypeService;

        [ObservableProperty]
        private TestTypeDto? _currentTestType = new();

        public UpdateTestTypeViewModel(ITestTypeService testTypeService)
        {
            _testTypeService = testTypeService;
        }

        public async Task InitializeAsync(int id)
        {
            var result = await _testTypeService.GetTestTypeByIdAsync(id);

            if (result.IsFailure)
            {
                CurrentTestType = null;

                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            CurrentTestType = result.Value!;
        }

        [RelayCommand]
        private async Task SaveAsync(Window window)
        {
            if (CurrentTestType == null)
                return;

            var testDto = new TestTypeDto
            {
                TestTypeId = CurrentTestType.TestTypeId,
                TestTypeTitle = CurrentTestType.TestTypeTitle,
                TestTypeDescription = CurrentTestType.TestTypeDescription,
                TestTypeFees = CurrentTestType.TestTypeFees
            };


            var result = await _testTypeService
                .UpdateTestTypeAsync(
                    CurrentTestType.TestTypeId,
                    testDto);


            if (result.IsSuccess)
            {
                MessageBox.Show(
                    "Test Type updated successfully!",
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