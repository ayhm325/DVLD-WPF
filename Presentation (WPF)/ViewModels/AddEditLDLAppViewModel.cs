using Application.Common.Results;
using Application.DTOs;
using Application.DTOs.ApplicationDTO;
using Application.DTOs.PersonDTO;
using Application.DTOs.LocalDrivingLicenseApplicationDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
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

        public AddEditLDLAppViewModel(
            ILicenseClassService licenseClassService,
            IApplicationService applicationService,
            IPersonService personService,
            ICurrentUserService currentUserService,
            IApplicationTypeService applicationTypeService,
            ILocalDrivingLicenseApplicationService localDrivingLicenseApplicationService,
            LDLAppViewModel gridViewModel,
            IServiceProvider serviceProvider)
        {
            _licenseClassService = licenseClassService;
            _applicationService = applicationService;
            _personService = personService;
            _currentUserService = currentUserService;
            _applicationTypeService = applicationTypeService;
            _localDrivingLicenseApplicationService = localDrivingLicenseApplicationService;
            _gridViewModel = gridViewModel;
            _serviceProvider = serviceProvider;

            CreatedByUserID = _currentUserService.UserId;
            CreatedBy = _currentUserService.Username;
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private PersonDto? person;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private LicenseClassDto? selectedLicenseClass;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool hasDuplicateApplication;

        [ObservableProperty]
        private int applicationId;

        [ObservableProperty]
        private DateTime applicationDate = DateTime.Now;

        [ObservableProperty]
        private string createdBy = string.Empty;

        [ObservableProperty]
        private int createdByUserID;

        [ObservableProperty]
        private string filterText = string.Empty;

        [ObservableProperty]
        private int selectedFilterIndex;

        [ObservableProperty]
        private decimal applicationTypeFees;

        public ObservableCollection<LicenseClassDto> LicenseClasses { get; } = new();

        public int SelectedLicenseClassId =>
            SelectedLicenseClass?.LicenseClassID ?? 0;

        private bool CanSave()
        {
            return Person != null
                   && SelectedLicenseClass != null
                   && _ldlApplicationType != null
                   && !HasDuplicateApplication;
        }

        private async Task CheckDuplicateApplicationAsync()
        {
            HasDuplicateApplication = false;

            if (Person == null || SelectedLicenseClass == null)
            {
                SaveCommand.NotifyCanExecuteChanged();
                return;
            }

            try
            {
                int? existingApplicationId =
                    await _applicationService.HasDuplicateApplicationAsync(
                        Person.PersonId,
                        SelectedLicenseClass.LicenseClassID);

                HasDuplicateApplication =
                    existingApplicationId.HasValue &&
                    existingApplicationId.Value > 0;
            }
            catch
            {
                // لا نسمح بالحفظ إذا فشل التحقق.
                HasDuplicateApplication = true;
            }

            SaveCommand.NotifyCanExecuteChanged();
        }

        partial void OnPersonChanged(PersonDto? value)
        {
            _ = CheckDuplicateApplicationAsync();
        }

        partial void OnSelectedLicenseClassChanged(LicenseClassDto? value)
        {
            _ = CheckDuplicateApplicationAsync();
        }

        public async Task InitializeAsync()
        {
            await LoadLicenseClassesAsync();
            await LoadApplicationTypeAsync();
        }

        private async Task LoadLicenseClassesAsync()
        {
            try
            {
                var result =
                    await _licenseClassService.GetAllLicenseClassesAsync();

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

                if (result.Value != null)
                {
                    foreach (var licenseClass in result.Value)
                    {
                        LicenseClasses.Add(licenseClass);
                    }
                }

                if (LicenseClasses.Count > 0)
                {
                    SelectedLicenseClass = LicenseClasses[0];
                }
                else
                {
                    SelectedLicenseClass = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load license classes.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadApplicationTypeAsync()
        {
            try
            {
                var result =
                    await _applicationTypeService
                        .GetApplicationTypeByIdAsync(1);

                if (result.IsFailure)
                {
                    MessageBox.Show(
                        result.Error,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                _ldlApplicationType = result.Value;

                if (_ldlApplicationType != null)
                {
                    ApplicationTypeFees =
                        _ldlApplicationType.ApplicationTypeFees;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load application type.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task Save()
        {
            if (Person == null)
            {
                MessageBox.Show(
                    "Please select a person first.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (SelectedLicenseClass == null)
            {
                MessageBox.Show(
                    "Please select a license class.",
                    "Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (_ldlApplicationType == null)
            {
                MessageBox.Show(
                    "Application type is not loaded.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            try
            {
                int licenseClassId =
                    SelectedLicenseClass.LicenseClassID;

                int? existingApplicationId =
                    await _applicationService
                        .HasDuplicateApplicationAsync(
                            Person.PersonId,
                            licenseClassId);

                if (existingApplicationId.HasValue &&
                    existingApplicationId.Value > 0)
                {
                    MessageBox.Show(
                        $"An active application already exists for this person.\n\n" +
                        $"Application ID: {existingApplicationId.Value}\n" +
                        $"Status: New or Completed\n\n" +
                        $"You cannot create a duplicate application for the same license class.",
                        "Duplicate Application Detected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var newApplication = new CreateApplicationDto
                {
                    ApplicantPersonID = Person.PersonId,
                    ApplicationDate = ApplicationDate,
                    ApplicationTypeID =
                        _ldlApplicationType.ApplicationTypeId,
                    ApplicationStatus =
                        Domain.Enums.AppStatus.New,
                    PaidFees =
                        _ldlApplicationType.ApplicationTypeFees,
                    CreatedByUserID = CreatedByUserID,
                    LastStatusDate = DateTime.Now
                };

                var applicationResult =
                    await _applicationService
                        .AddNewApplicationAsync(newApplication);

                if (applicationResult.IsFailure)
                {
                    MessageBox.Show(
                        applicationResult.Error,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                ApplicationId = applicationResult.Value;

                if (ApplicationId <= 0)
                {
                    MessageBox.Show(
                        "Failed to create the application.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                var newLDLApplication =
                    new CreateLocalDrivingLicenseApplicationDto
                    {
                        ApplicationID = ApplicationId,
                        LicenseClassID = licenseClassId
                    };

                var ldlResult =
                    await _localDrivingLicenseApplicationService
                        .AddLocalDrivingLicenseApplicationAsync(
                            newLDLApplication);

                if (ldlResult.IsFailure)
                {
                    MessageBox.Show(
                        ldlResult.Error,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }

                MessageBox.Show(
                    $"The application has been successfully created and saved to the system.\n\n" +
                    $"ID: {ApplicationId}",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await _gridViewModel.LoadApplicationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while saving the application.\n\n" +
                    $"{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task Search()
        {
            if (string.IsNullOrWhiteSpace(FilterText))
                return;

            try
            {
                Result<PersonDto> result;

                if (SelectedFilterIndex == 0)
                {
                    if (!int.TryParse(
                            FilterText,
                            out int personId))
                    {
                        MessageBox.Show(
                            "Please enter a valid Person ID.",
                            "Invalid ID",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }

                    result =
                        await _personService
                            .GetPersonByIdAsync(personId);
                }
                else
                {
                    result =
                        await _personService
                            .GetPersonByNationalNoAsync(
                                FilterText.Trim());
                }

                if (result.IsFailure)
                {
                    MessageBox.Show(
                        result.Error,
                        "Person Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                Person = result.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred while searching.\n\n{ex.Message}",
                    "Search Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddPerson()
        {
            var window =
                _serviceProvider
                    .GetRequiredService<AddEditPersonWin>();

            window.Owner = App.Current.MainWindow;
            window.ShowDialog();
        }
    }
}