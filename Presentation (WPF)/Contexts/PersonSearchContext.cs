using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Helpers;
using Presentation.ViewModels;
using Presentation.Views;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Presentation.Contexts
{
    public partial class PersonSearchContext : ObservableObject
    {
        private readonly IPersonService _personService;


        [ObservableProperty] private string _filterText = string.Empty;

        // أضفنا هذه الخاصية التي كانت مفقودة
        [ObservableProperty] private int _selectedFilterIndex;

        [ObservableProperty] private PersonDto? _selectedPerson;

        public PersonSearchContext(IPersonService personService)
        {
            _personService = personService;
        }

        [RelayCommand]
        private void AddPerson()
        {

            var addEditPersonVm = App.ServiceProvider.GetRequiredService<PersonViewModel>();
            NavigationHelper.Navigate(new AddEditPersonPage(addEditPersonVm));


            if (SelectedPerson == null)
            {
                // عرض رسالة: الرجاء البحث عن شخص أولاً
                return;
            }
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return;

            // استخدام الخاصية المضافة الآن
            if (SelectedFilterIndex == 0) // ID
            {
                if (int.TryParse(FilterText, out int id))
                    SelectedPerson = await _personService.GetPersonByIdAsync(id);
            }
            else // National No
            {
                SelectedPerson = await _personService.GetPersonByNationalNoAsync(FilterText);
            }
        }
    }
}