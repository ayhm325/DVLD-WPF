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
using Presentation.Views.Windows.Tests;

namespace Presentation.ViewModels
{
    public partial class TestTypeViewModel : ObservableObject
    {
        private readonly ITestTypeService _testTypeService;

        public ObservableCollection<TestTypeDto> TestTypes { get; } = new ();

        public TestTypeViewModel(ITestTypeService testTypeService)
        {
            _testTypeService = testTypeService;
            LoadTestTyepsAsync();
        }
    
        public async void LoadTestTyepsAsync()
        {
            var data = await _testTypeService.GetAllTestTypesAsync();

            // التحقق من أن data ليست null قبل الاستخدام
            if (data == null)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: Data returned is null.");
                return; // الخروج إذا لم توجد بيانات
            }

            System.Diagnostics.Debug.WriteLine($"DEBUG: Loaded {data.Count} items.");

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

            LoadTestTyepsAsync();
        }


        }
}
