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
            CurrentTestType = await _testTypeService.GetTestTypeByIdAsync(id);
        }

        [RelayCommand]
        private async Task SaveAsync(Window window)
        {
            if (CurrentTestType == null) return;
            var testDto = new TestTypeDto
            {
                TestTypeId = CurrentTestType.TestTypeId,
                TestTypeTitle = CurrentTestType.TestTypeTitle,
                TestTypeDescription = CurrentTestType.TestTypeDescription,
                TestTypeFees = CurrentTestType.TestTypeFees
            };
            bool result = await _testTypeService.UpdateTestTypeAsync(CurrentTestType.TestTypeId, testDto);
            if (result)
            {
                MessageBox.Show("Test Type updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
