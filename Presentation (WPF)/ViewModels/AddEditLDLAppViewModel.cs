using Application.Common.Results;
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
using Presentation.Views.Windows;
using System;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class AddEditLDLAppViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILicenseClassService _licenseClassService;
        private readonly IApplicationService _applicationService;
        private readonly IPersonService _personService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationTypeService _applicationTypeService;
        private readonly ILocalDrivingLicenseApplicationService _localDrivingLicenseApplicationService;

        private readonly LDLAppViewModel _gridViewModel;

        private ApplicationTypeDto? _ldlApplicationType;

        public AddEditLDLAppViewModel(ILicenseClassService licenseClassService, IApplicationService applicationService,
           IPersonService personService, ICurrentUserService currentUserService, IApplicationTypeService applicationType,
           ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService, LDLAppViewModel gridViewModel,
           IServiceProvider serviceProvider)
        { 

            _licenseClassService = licenseClassService;
            _applicationService = applicationService;
            _personService = personService;
            _applicationTypeService = applicationType;
            _currentUserService = currentUserService;
            _localDrivingLicenseApplicationService = localDrivingLicenseApplicationService;
            _gridViewModel = gridViewModel;
            _serviceProvider = serviceProvider;

            CreatedByUserID = _currentUserService.UserId;
            CreatedBy = _currentUserService.Username;
            
            LoadLicenseClasses();
            LoadApplicationType();
        }

        // ===================== PROPERTIES =====================

        [ObservableProperty] private PersonDto? person;
        [ObservableProperty] private int applicationId;
        [ObservableProperty] private LicenseClassDto? selectedLicenseClass;
        [ObservableProperty] private DateTime applicationDate = DateTime.Now;

        [ObservableProperty] private string createdBy;
        [ObservableProperty] private int createdByUserID;

        [ObservableProperty] private string filterText = string.Empty;
        [ObservableProperty] private int selectedFilterIndex = 0;

        [ObservableProperty] private decimal applicationTypeFees;

        public ObservableCollection<LicenseClassDto> LicenseClasses { get; } = new();

        // ===================== LOAD DATA =====================
        private async void LoadLicenseClasses()
        {
            var result = await _licenseClassService.GetAllLicenseClassesAsync();

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            LicenseClasses.Clear();

            foreach (var c in result.Value!)
                LicenseClasses.Add(c);

            SelectedLicenseClass = LicenseClasses.FirstOrDefault();
        }

        private async void LoadApplicationType()
        {
            var result =
                await _applicationTypeService.GetApplicationTypeByIdAsync(1);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            _ldlApplicationType = result.Value!;

            ApplicationTypeFees = _ldlApplicationType.ApplicationTypeFees;
        }


        // ===================== SAVE =====================
        [RelayCommand]
        private async Task Save()
        {
            if (Person == null)
            {
                MessageBox.Show("Please select a person first.");
                return;
            }

            if (SelectedLicenseClass == null)
            {
                MessageBox.Show("Please select a license class.");
                return;
            }

            if (_ldlApplicationType == null)
            {
                MessageBox.Show("Application type not loaded.");
                return;
            }

            // التحقق من وجود طلب سابق لنفس الرخصة
            int? exists = await _applicationService.HasDuplicateApplicationAsync(Person.PersonId,SelectedLicenseClass.LicenseClassID);

            if (exists>0)
            {
                MessageBox.Show(
                        $"An active application already exists for this person.\n\n" +
                        $"Application ID: {exists.Value}\n" +
                        $"Status: New or Completed\n\n" +
                        $"You cannot create a duplicate application for the same license class.",
                        "Duplicate Application Detected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                return;
            }

            var newApp = new ApplicationDto
            {
                ApplicantPersonID = Person.PersonId, 
                ApplicationDate = ApplicationDate,
                ApplicationTypeID = _ldlApplicationType.ApplicationTypeId,
                ApplicationStatus = Domain.Enums.AppStatus.New,
                PaidFees = _ldlApplicationType.ApplicationTypeFees,
                CreatedByUserID = CreatedByUserID,
                LastStatusDate = DateTime.Now
            };

            var result = await _applicationService.AddNewApplicationAsync(newApp);

            if (result.IsFailure)
            {
                throw new Exception(result.Error);
            }

            int newAppId = result.Value;

            if (newAppId > 0)
            {
                ApplicationId = newAppId;
            }

            var newLDLApp = new LocalDrivingLicenseApplicationCreateUpdateDto
            {
                ApplicatonId = ApplicationId,
                LicenseClassId = SelectedLicenseClass?.LicenseClassID ?? 0
            };

            var result1 = await _localDrivingLicenseApplicationService.AddLocalDrivingLicenseApplicationAsync(newLDLApp);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error);
                return;
            }

            int newLappId = result1.Value;

            System.Windows.MessageBox.Show(
                $"The application has been successfully created and saved to the system.\n\nID: {newAppId}",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            await _gridViewModel.LoadApplicationsAsync();
        }

        // ===================== SEARCH =====================
        [RelayCommand]
        private async Task Search()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
                return;

            try
            {
                Result<PersonDto> result;

                // البحث عن الشخص
                if (SelectedFilterIndex == 0 && int.TryParse(FilterText, out int id))
                    result = await _personService.GetPersonByIdAsync(id);
                else
                    result = await _personService.GetPersonByNationalNoAsync(FilterText);

                if (result.IsFailure)
                {
                    MessageBox.Show(
                        result.Error,
                        "Person Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var person = result.Value!;

                Person = person;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during search: {ex.Message}");

                MessageBox.Show(
                    "An error occurred while searching. Please try again later.",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ===================== ADD PERSON =====================
        [RelayCommand]
        private void AddPerson()
        {
            //var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
            ////MainWindow.Navigation.Navigate(new AddEditPersonWin(vm));            
            //var win = new AddEditPersonWin(vm)
            //{
            //    Owner = System.Windows.Application.Current.MainWindow
            //};
            //win.ShowDialog();

            var win = _serviceProvider.GetRequiredService<AddEditPersonWin>();

            win.Owner = App.Current.MainWindow;
            win.ShowDialog();
        }

        

    }
}