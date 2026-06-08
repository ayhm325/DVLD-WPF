using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Helpers;
using Presentation.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class AddEditLDLAppViewModel : ObservableObject
    {

        private readonly ILicenseClassService _licenseClassService;
        private readonly IApplicationService _applicationService;
        private readonly IPersonService _personService;
        private readonly ICurrentUserService _currentUserService;

        public AddEditLDLAppViewModel(ILicenseClassService licenseClassService, IApplicationService applicationService,
           IPersonService personService, ICurrentUserService currentUserService)
        {

            _licenseClassService = licenseClassService;
            _applicationService = applicationService;
            _personService = personService;
            
            _currentUserService = currentUserService;

            CreatedByUserID = _currentUserService.UserId;
            CreatedBy = _currentUserService.Username;

            LoadLicenseClasses();
        }

        [ObservableProperty] private PersonDto? _person;
        [ObservableProperty] private int _applicationId;
        [ObservableProperty] private LicenseClassDto? _selectedLicenseClass;
        [ObservableProperty] private DateTime _applicationDate = DateTime.Now;
        [ObservableProperty] private string _createdBy;
        [ObservableProperty] private int _createdByUserID;


        [ObservableProperty] private string _filterText = string.Empty;
        [ObservableProperty] private int _selectedFilterIndex = 0;

        public ObservableCollection<LicenseClassDto> LicenseClasses { get; } = new();


        private async void LoadLicenseClasses()
        {
            var classes = await _licenseClassService.GetAllLicenseClassesAsync();
            foreach (var c in classes) LicenseClasses.Add(c);

            SelectedLicenseClass = LicenseClasses.FirstOrDefault();
        }




        [RelayCommand]
        private async Task Save()
        {
            var newApp = new ApplicationDto
            {
                ApplicantPersonID = Person.PersonId, 
                ApplicationDate = ApplicationDate,
                ApplicationTypeID = SelectedLicenseClass?.LicenseClassId ?? 0,
                ApplicationStatus = Domain.Enums.AppStatus.New,
                PaidFees = SelectedLicenseClass?.LicenseClassFees ?? 0,
                CreatedByUserID = CreatedByUserID,
                LastStatusDate = DateTime.Now
            };


            int newId = await _applicationService.AddNewApplicationAsync(newApp);

            if (newId > 0)
            {
                ApplicationId = newId;
            }

            System.Windows.MessageBox.Show(
                $"The application has been successfully created and saved to the system.\n\nID: {newId}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task Search()
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return;

            PersonDto? person = null;

            try
            {
                // 1. البحث عن الشخص
                if (SelectedFilterIndex == 0 && int.TryParse(FilterText, out int id))
                    person = await _personService.GetPersonByIdAsync(id);
                else
                    person = await _personService.GetPersonByNationalNoAsync(FilterText);
                Person = person;

                if (person == null)
                {
                    MessageBox.Show(
                        "No person was found with the specified criteria.",
                        "Person Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    // لا نغير أي شيء في الـ SelectedPerson أو الـ UserName
                    return;
                }

                                                                   
            }
            catch (Exception ex)
            {
                // 1. تسجيل الخطأ في نافذة المخرجات (للمطور)
                System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");

                // 2. إظهار رسالة للمستخدم (لأن الخطأ قد يعني فشل الاتصال بقاعدة البيانات)
                MessageBox.Show(
                    "An error occurred while searching. Please try again later.",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddPerson()
        {
            var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
            NavigationHelper.Navigate(new AddEditPersonPage(vm));
        }

        

    }
}