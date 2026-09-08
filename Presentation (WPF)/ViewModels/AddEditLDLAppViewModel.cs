using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LicenseClass;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.Person;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class AddEditLDLAppViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILicenseClassesApiClient _licenseClassesApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient
        _localApplicationsApiClient;
    private readonly LDLAppViewModel _gridViewModel;

    public AddEditLDLAppViewModel(
        ILicenseClassesApiClient licenseClassesApiClient,
        IPeopleApiClient peopleApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
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

        _localApplicationsApiClient =
            localApplicationsApiClient
            ?? throw new ArgumentNullException(
                nameof(localApplicationsApiClient));

        _gridViewModel =
            gridViewModel
            ?? throw new ArgumentNullException(
                nameof(gridViewModel));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(
                nameof(serviceProvider));
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private PersonResponse? person;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private LicenseClassResponse? selectedLicenseClass;

    [ObservableProperty]
    private int localApplicationId;

    [ObservableProperty]
    private DateTime applicationDate = DateTime.Now;

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private int selectedFilterIndex;

    [ObservableProperty]
    private decimal applicationFees;

    public ObservableCollection<LicenseClassResponse> LicenseClasses { get; }
        = [];

    public int SelectedLicenseClassId =>
        SelectedLicenseClass?.LicenseClassId ?? 0;

    private bool CanSave()
        => Person != null && SelectedLicenseClass != null;

    public async Task InitializeAsync()
    {
        await LoadLicenseClassesAsync();
        await LoadCreateInfoAsync();
    }

    private async Task LoadLicenseClassesAsync()
    {
        try
        {
            var result =
                await _licenseClassesApiClient.GetAllAsync();

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

    private async Task LoadCreateInfoAsync()
    {
        try
        {
            var result =
                await _localApplicationsApiClient
                    .GetCreateInfoAsync();

            if (result.IsFailure)
            {
                Show(
                    result.Error,
                    "Error",
                    MessageBoxImage.Error);

                return;
            }

            ApplicationFees =
                result.Value?.ApplicationFees ?? 0;
        }
        catch (Exception ex)
        {
            Show(
                $"Failed to load application information.\n\n{ex.Message}",
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

        try
        {
            var request =
                new CreateLocalDrivingLicenseApplicationRequest
                {
                    ApplicantPersonId =
                        Person.PersonId,

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

            LocalApplicationId = result.Value;

            if (LocalApplicationId <= 0)
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
                $"ID: {LocalApplicationId}",
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
                    : await _peopleApiClient.GetByNationalNoAsync(
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

        return await _peopleApiClient.GetByIdAsync(personId);
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
        => OnPropertyChanged(nameof(SelectedLicenseClassId));

    private static void Show(
        string message,
        string title,
        MessageBoxImage image)
        => MessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            image);
}