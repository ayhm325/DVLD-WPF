using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using Presentation.Contexts;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels
{
    public partial class AddEditLDLAppViewModel : ObservableObject
    {
       
        private readonly ILicenseClassService _licenseClassService;
        private readonly IApplicationService _applicationService;
        public PersonSearchContext PersonContext { get; }

        public ObservableCollection<LicenseClassDto> LicenseClasses { get; } = new();
        [ObservableProperty] private LicenseClassDto? _selectedLicenseClass;
        [ObservableProperty] private DateTime _applicationDate = DateTime.Now;

        [ObservableProperty] private string _createdBy = "Admin"; 
        [ObservableProperty] private int _createdByUserID = 1;


        public AddEditLDLAppViewModel(PersonSearchContext personContext, ILicenseClassService licenseClassService, IApplicationService applicationService)
        {
            PersonContext = personContext;
            _licenseClassService = licenseClassService;    
            _applicationService = applicationService;
            LoadLicenseClasses();
        }

        private async void LoadLicenseClasses()
        {
            var classes = await _licenseClassService.GetAllLicenseClassesAsync();
            foreach (var c in classes) LicenseClasses.Add(c);

            SelectedLicenseClass = LicenseClasses.FirstOrDefault();
        }




        [RelayCommand]
        private async Task Save()
        {
            // التحقق من أن الشخص تم اختياره
            if (PersonContext.SelectedPerson == null)
            {
                System.Windows.MessageBox.Show("Please select a person first!");
                return;
            }

            

            // إنشاء الـ DTO للإرسال للخدمة
            var newApp = new ApplicationDto
            {
                ApplicantPersonID = PersonContext.SelectedPerson.PersonId,
                ApplicationDate = ApplicationDate,
                ApplicationTypeID = SelectedLicenseClass?.LicenseClassId ?? 0,
                ApplicationStatus = Domain.Enums.AppStatus.New, // الحالة الافتراضية
                PaidFees = SelectedLicenseClass?.LicenseClassFees ?? 0,
                CreatedByUserID = CreatedByUserID,
                LastStatusDate = DateTime.Now
            };

            // استدعاء الخدمة للحفظ
            int newId = await _applicationService.AddNewApplicationAsync(newApp);

            System.Windows.MessageBox.Show($"Application Saved Successfully! ID: {newId}");
        }
    }
}