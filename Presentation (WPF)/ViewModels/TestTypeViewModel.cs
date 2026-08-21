using Application.DTOs.TestTypeDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows.Tests;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels
{
    public partial class TestTypeViewModel : ObservableObject
    {
        private readonly ITestTypeService _testTypeService;

        public ObservableCollection<TestTypeDto> TestTypes { get; } = new();

        public TestTypeViewModel(ITestTypeService testTypeService)
        {
            _testTypeService = testTypeService
                ?? throw new ArgumentNullException(nameof(testTypeService));

            _ = LoadTestTypesAsync();
        }

        public async Task LoadTestTypesAsync()
        {
            try
            {
                var result = await _testTypeService.GetAllTestTypesAsync();

                if (result.IsFailure)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"DEBUG: Failed to load test types: {result.Error}");

                    return;
                }

                var data = result.Value;

                if (data is null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "DEBUG: Test types result contains no data.");

                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"DEBUG: Loaded {data.Count} items.");

                TestTypes.Clear();

                foreach (var item in data)
                {
                    TestTypes.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"DEBUG: Failed to load test types: {ex}");
            }
        }

        [RelayCommand]
        private async Task EditTestType(TestTypeDto? selectedType)
        {
            if (selectedType is null)
                return;

            var updateVm =
                App.ServiceProvider.GetRequiredService<UpdateTestTypeViewModel>();

            await updateVm.InitializeAsync(selectedType.TestTypeId);

            var editWindow = new EditTestTypeWindow(updateVm);

            editWindow.ShowDialog();

            await LoadTestTypesAsync();
        }
    }
}