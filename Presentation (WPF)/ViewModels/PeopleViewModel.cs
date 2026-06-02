using Application.DTOs;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Domain.Entities;
using DVLD.Domain.Enums;
using DVLD_WPF;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection; // 👈 تأكد من وجود هذا السطر لاستدعاء الـ GetRequiredService
using Presentation.Helpers;
using Presentation.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels
{
    public partial class PeopleViewModel : ObservableObject
    {
        // 🟢 1. المتغيرات المعرفة في الكلاس
        private readonly IPersonService _personService;
        private readonly PersonRepository _personRepository;
        private List<Person> _allPeople = new();

        [ObservableProperty] private ObservableCollection<PersonDto> _filteredPeople = new();
        [ObservableProperty] private int _peopleCount;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _searchToolTip = "Search...";

        [ObservableProperty] private bool _isSearchTextVisible;
        [ObservableProperty] private bool _isGenderComboVisible;

        public List<string> FilterTypes { get; } = new() { "None", "National No", "Name", "Gender" };
        public List<string> GenderOptions { get; } = new() { "All", "Male", "Female" };

        private string _selectedFilterType = "None";
        public string SelectedFilterType
        {
            get => _selectedFilterType;
            set
            {
                if (SetProperty(ref _selectedFilterType, value))
                {
                    OnFilterTypeChanged();
                }
            }
        }

        private string _selectedGender = "All";
        public string SelectedGender
        {
            get => _selectedGender;
            set
            {
                if (SetProperty(ref _selectedGender, value))
                {
                    ApplyFilter();
                }
            }
        }

        // 🟢 2. تحديث الـ Constructor ليقبل ويحقن الـ IPersonService تلقائياً
        public PeopleViewModel(PersonRepository personRepository, IPersonService personService)
        {
            _personRepository = personRepository;
            _personService = personService; // تثبيت الحقن هنا لمنع خطأ الـ Context
        }

        [RelayCommand]
        public async Task LoadPeopleAsync()
        {
            _allPeople = await _personRepository.GetAllPersonsAsync();
            ApplyFilter();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        private void OnFilterTypeChanged()
        {
            SearchText = string.Empty;
            SelectedGender = "All";

            IsSearchTextVisible = (SelectedFilterType == "Name" || SelectedFilterType == "National No");
            IsGenderComboVisible = (SelectedFilterType == "Gender");

            SearchToolTip = SelectedFilterType switch
            {
                "Name" => "Search by Name...",
                "National No" => "Search by National No...",
                _ => "Search..."
            };

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allPeople == null) return;

            var query = _allPeople.AsQueryable();

            if (SelectedFilterType == "National No" && !string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(p => p.NationalNo != null && p.NationalNo.ToLower().Contains(SearchText.ToLower()));
            }
            else if (SelectedFilterType == "Name" && !string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(p => p.FullName != null && p.FullName.ToLower().Contains(SearchText.ToLower()));
            }
            else if (SelectedFilterType == "Gender")
            {
                if (SelectedGender == "Male")
                    query = query.Where(p => p.Gender == Gender.Male);
                else if (SelectedGender == "Female")
                    query = query.Where(p => p.Gender == Gender.Female);
            }

            var filteredList = query.ToList();

            // 🟢 3. هنا تم إضافة حقل الـ ImagePath الضائع لحل مشكلة الفراغ!
            var dtoList = filteredList.Select(p => new PersonDto
            {
                PersonId = p.PersonId,
                NationalNo = p.NationalNo,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender == Gender.Male ? "Male" : "Female",
                Address = p.Address,
                Phone = p.Phone,
                Email = p.Email,
                CountryName = p.Country?.CountryName ?? "N/A",

                // ✅ السطر المفقود الذي تسبب بحجب الصور عن الـ DataGrid والواجهات:
                ImagePath = p.ImagePath
            }).ToList();

            FilteredPeople = new ObservableCollection<PersonDto>(dtoList);
            PeopleCount = dtoList.Count;
        }

        [RelayCommand]
        private void ShowDetails(PersonDto selectedPerson)
        {
            if (selectedPerson == null) return;

            // 🟢 4. الآن الكود سيتعرف على الـ _personService بنجاح تام وبدون أخطاء كومبايلر
            Presentation.Views.Windows.PersonDetailsWindow detailsWindow =
                new Presentation.Views.Windows.PersonDetailsWindow(_personService, selectedPerson.PersonId);

            detailsWindow.Owner = System.Windows.Application.Current.MainWindow;
            detailsWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            detailsWindow.ShowDialog();
        }

        [RelayCommand]
        private async Task AddNewPerson() // لاحظ تحويلها لـ async Task
        {
            if (App.ServiceProvider != null)
            {
                var addEditVm = App.ServiceProvider.GetRequiredService<PersonViewModel>();

                // 🟢 الإضافة هنا: استدعِ التهيئة حتى عند الإضافة (null تعني Add Mode)
                await addEditVm.InitializeAsync(null);

                NavigationHelper.Navigate(new AddEditPersonPage(addEditVm));
            }
        }

        [RelayCommand]
        private async Task EditPerson(PersonDto selectedPerson) // 👈 أضفنا async و Task
        {
            if (selectedPerson == null) return;

            // 1. طلب الـ ViewModel من الحاوية
            var addEditVm = DVLD_WPF.App.ServiceProvider.GetRequiredService<PersonViewModel>();

            // 2. تحميل البيانات في هذه النسخة تحديداً
            await addEditVm.InitializeAsync(selectedPerson.PersonId);

            // 3. الانتقال بالنسخة التي تحمل البيانات
            NavigationHelper.Navigate(new AddEditPersonPage(addEditVm));
        }

        [RelayCommand] private async Task DeletePersonAsync(PersonDto selectedPerson) { }
        [RelayCommand] private void SendEmail(PersonDto selectedPerson) { }
        [RelayCommand] private void PhoneCall(PersonDto selectedPerson) { }
    }
}