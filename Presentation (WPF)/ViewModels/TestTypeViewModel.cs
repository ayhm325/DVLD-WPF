using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using Presentation.Views.Windows.Tests;

namespace Presentation.ViewModels
{
    public partial class TestTypeViewModel : ObservableObject
    {
        private readonly ITestTypeService _testTypeService;

        public ObservableCollection<TestTypeDto> TestTypes { get; } = new();

        public TestTypeViewModel(ITestTypeService testTypeService)
        {
            _testTypeService = testTypeService;
            LoadTestTypesAsync();
        }

        public async Task LoadTestTypesAsync()
        {
            var result = await _testTypeService.GetAllTestTypesAsync();

            if (result.IsFailure)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"DEBUG: Failed to load test types: {result.Error}");

                return;
            }

            var data = result.Value!;

            System.Diagnostics.Debug.WriteLine(
                $"DEBUG: Loaded {data.Count} items.");

            TestTypes.Clear();

            foreach (var item in data)
            {
                TestTypes.Add(item);
            }
        }

        [RelayCommand]
        private async Task EditTestType(TestTypeDto? selectedType)
        {
            if (selectedType == null) return;

            var updateVm = App.ServiceProvider.GetRequiredService<UpdateTestTypeViewModel>();
            await updateVm.InitializeAsync(selectedType.TestTypeId);

            var editWindow = new EditTestTypeWindow(updateVm);
            editWindow.ShowDialog();

            LoadTestTypesAsync();
        }


    }
}