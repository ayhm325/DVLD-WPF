using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.ApplicationType;
using DVLD.Contracts.LicenseClass;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.Person;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class AddEditLDLAppViewModel : ObservableObject
{
    private const int FirstTimeLicenseApplicationTypeId = 1;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILicenseClassesApiClient _licenseClassesApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IApplicationTypesApiClient _applicationTypesApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient
        _localApplicationsApiClient;
    private readonly ICurrentUserSession _currentUserSession;
    private readonly LDLAppViewModel _gridViewModel;

    private ApplicationTypeResponse? _ldlApplicationType;

    public AddEditLDLAppViewModel(
        ILicenseClassesApiClient licenseClassesApiClient,
        IPeopleApiClient peopleApiClient,
        IApplicationTypesApiClient applicationTypesApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        ICurrentUserSession currentUserSession,
        LDLAppViewModel gridViewModel,
        IServiceProvider serviceProvider)
    {
        _licenseClassesApiClient =
            licenseClassesApiClient
            ?? throw new ArgumentNullException(
                nameof(licenseClassesApiClient));

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(
                nameof(peopleApiClient));

        _applicationTypesApiClient =
            applicationTypesApiClient
            ?? throw new ArgumentNullException(
                nameof(applicationTypesApiClient));

        _localApplicationsApiClient =
            localApplicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(localApplicationsApiClient));

        _currentUserSession =
            currentUserSession
            ?? throw new ArgumentNullException(
                nameof(currentUserSession));

        _gridViewModel =
            gridViewModel
            ?? throw new ArgumentNullException(
                nameof(gridViewModel));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(
                nameof(serviceProvider));

        CreatedByUserID = _currentUserSession.UserId;
        CreatedBy = _currentUserSession.Username;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private PersonResponse? person;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private LicenseClassResponse? selectedLicenseClass;

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

    public ObservableCollection<LicenseClassResponse> LicenseClasses { get; }
        = [];

    public int SelectedLicenseClassId =>
        SelectedLicenseClass?.LicenseClassId ?? 0;

    private bool CanSave()
    {
        return Person != null
               && SelectedLicenseClass != null
               && _ldlApplicationType != null;
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
                await _licenseClassesApiClient
                    .GetAllAsync();

            if (result.IsFailure)
            {
                Show(
                    result.Error,
                    "Error",
                    MessageBoxImage.Error);

                return;
            }

            LicenseClasses.Clear();

            if (result.Value is not null)
            {
                foreach (var licenseClass in result.Value)
                    LicenseClasses.Add(licenseClass);
            }

            SelectedLicenseClass =
                LicenseClasses.Count > 0
                    ? LicenseClasses[0]
                    : null;
        }
        catch (Exception ex)
        {
            Show(
                $"Failed to load license classes.\n\n{ex.Message}",
                "Error",
                MessageBoxImage.Error);
        }
    }

    private async Task LoadApplicationTypeAsync()
    {
        try
        {
            var result =
                await _applicationTypesApiClient
                    .GetByIdAsync(
                        FirstTimeLicenseApplicationTypeId);

            if (result.IsFailure)
            {
                Show(
                    result.Error,
                    "Error",
                    MessageBoxImage.Error);

                return;
            }

            _ldlApplicationType = result.Value;

            if (_ldlApplicationType is not null)
            {
                ApplicationTypeFees =
                    _ldlApplicationType.ApplicationTypeFees;
            }

            SaveCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            Show(
                $"Failed to load application type.\n\n{ex.Message}",
                "Error",
                MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (Person is null)
        {
            Show(
                "Please select a person first.",
                "Validation",
                MessageBoxImage.Warning);

            return;
        }

        if (SelectedLicenseClass is null)
        {
            Show(
                "Please select a license class.",
                "Validation",
                MessageBoxImage.Warning);

            return;
        }

        if (_ldlApplicationType is null)
        {
            Show(
                "Application type is not loaded.",
                "Error",
                MessageBoxImage.Error);

            return;
        }

        try
        {
            var request =
                new CreateLocalDrivingLicenseApplicationRequest
                {
                    ApplicantPersonId =
                        Person.PersonId,

                    ApplicationTypeId =
                        _ldlApplicationType.ApplicationTypeId,

                    LicenseClassId =
                        SelectedLicenseClass.LicenseClassId
                };

            var result =
                await _localApplicationsApiClient
                    .CreateAsync(request);

            if (result.IsFailure)
            {
                Show(
                    result.Error,
                    "Error",
                    MessageBoxImage.Error);

                return;
            }

            ApplicationId = result.Value;

            if (ApplicationId <= 0)
            {
                Show(
                    "Failed to create the application.",
                    "Error",
                    MessageBoxImage.Error);

                return;
            }

            Show(
                "The application has been successfully created " +
                "and saved to the system.\n\n" +
                $"ID: {ApplicationId}",
                "Success",
                MessageBoxImage.Information);

            await _gridViewModel.LoadApplicationsAsync();
        }
        catch (Exception ex)
        {
            Show(
                "An unexpected error occurred while saving " +
                $"the application.\n\n{ex.Message}",
                "Error",
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
            var result =
                SelectedFilterIndex == 0
                    ? await SearchByIdAsync()
                    : await _peopleApiClient
                        .GetByNationalNoAsync(
                            FilterText.Trim());

            if (result.IsFailure)
            {
                Show(
                    result.Error,
                    "Person Not Found",
                    MessageBoxImage.Warning);

                return;
            }

            Person = result.Value;
        }
        catch (Exception ex)
        {
            Show(
                $"An error occurred while searching.\n\n{ex.Message}",
                "Search Error",
                MessageBoxImage.Error);
        }
    }

    private async Task<
        Presentation.Services.Results.ApiResult<PersonResponse>>
        SearchByIdAsync()
    {
        if (!int.TryParse(
                FilterText,
                out var personId))
        {
            return Presentation.Services.Results.ApiResult<PersonResponse>
                .Failure("Please enter a valid Person ID.");
        }

        return await _peopleApiClient
            .GetByIdAsync(personId);
    }

    [RelayCommand]
    private void AddPerson()
    {
        var window =
            _serviceProvider
                .GetRequiredService<AddEditPersonWin>();

        window.Owner =
            App.Current.MainWindow;

        window.ShowDialog();
    }

    partial void OnSelectedLicenseClassChanged(
        LicenseClassResponse? value)
    {
        OnPropertyChanged(nameof(SelectedLicenseClassId));
    }

    private static void Show(
        string message,
        string title,
        MessageBoxImage image)
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            image);
    }
}