using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LicenseClass;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.Person;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.Results;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class AddEditLDLAppViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILicenseClassesApiClient _licenseClassesApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly LDLAppViewModel _gridViewModel;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private PersonResponse? _person;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private LicenseClassResponse? _selectedLicenseClass;

    [ObservableProperty] private int _localApplicationId;
    [ObservableProperty] private DateTime _applicationDate = DateTime.Now;
    [ObservableProperty] private string _filterText = string.Empty;
    [ObservableProperty] private int _selectedFilterIndex;
    [ObservableProperty] private decimal _applicationFees;

    public ObservableCollection<LicenseClassResponse> LicenseClasses { get; } = [];

    public int SelectedLicenseClassId =>
        SelectedLicenseClass?.LicenseClassId ?? 0;

    public AddEditLDLAppViewModel(
        ILicenseClassesApiClient licenseClassesApiClient,
        IPeopleApiClient peopleApiClient,
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        LDLAppViewModel gridViewModel,
        IServiceProvider serviceProvider,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _licenseClassesApiClient = licenseClassesApiClient
            ?? throw new ArgumentNullException(nameof(licenseClassesApiClient));

        _peopleApiClient = peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        _localApplicationsApiClient = localApplicationsApiClient
            ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));

        _gridViewModel = gridViewModel
            ?? throw new ArgumentNullException(nameof(gridViewModel));

        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        _notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));

        _userNotifications = userNotifications
            ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    private bool CanSave() =>
        Person is not null && SelectedLicenseClass is not null;

    public async Task InitializeAsync()
    {
        await LoadLicenseClassesAsync();
        await LoadCreateInfoAsync();
    }

    private async Task LoadLicenseClassesAsync()
    {
        try
        {
            var result = await _licenseClassesApiClient.GetAllAsync();

            if (result.IsFailure)
            {
                _notifications.ShowFailure(
                    result,
                    "Load License Classes Failed");
                return;
            }

            LicenseClasses.Clear();

            foreach (var licenseClass in result.Value ?? [])
                LicenseClasses.Add(licenseClass);

            SelectedLicenseClass =
                LicenseClasses.Count > 0
                    ? LicenseClasses[0]
                    : null;
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Load License Classes");
        }
    }

    private async Task LoadCreateInfoAsync()
    {
        try
        {
            var result = await _localApplicationsApiClient.GetCreateInfoAsync();

            if (result.IsFailure)
            {
                _notifications.ShowFailure(
                    result,
                    "Load Application Information Failed");
                return;
            }

            ApplicationFees = result.Value?.ApplicationFees ?? 0;
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Application Information");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (Person is null)
        {
            _userNotifications.ShowWarning(
                "Please select a person first.",
                "Validation");
            return;
        }

        if (SelectedLicenseClass is null)
        {
            _userNotifications.ShowWarning(
                "Please select a license class.",
                "Validation");
            return;
        }

        try
        {
            var request = new CreateLocalDrivingLicenseApplicationRequest
            {
                ApplicantPersonId = Person.PersonId,
                LicenseClassId = SelectedLicenseClass.LicenseClassId
            };

            var result = await _localApplicationsApiClient.CreateAsync(request);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Create Application Failed");
                return;
            }

            LocalApplicationId = result.Value;

            if (LocalApplicationId <= 0)
            {
                _userNotifications.ShowError(
                    "Failed to create the application.",
                    "Create Application");
                return;
            }

            _userNotifications.ShowInfo(
                $"The application has been successfully created and saved to the system.\n\nID: {LocalApplicationId}",
                "Success");

            await _gridViewModel.LoadApplicationsAsync();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Save Application");
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            return;

        try
        {
            var result = SelectedFilterIndex == 0
                ? await SearchByIdAsync()
                : await _peopleApiClient.GetByNationalNoAsync(
                    FilterText.Trim());

            if (result.IsFailure)
            {
                _notifications.ShowFailure(
                    result,
                    "Person Search Failed");
                return;
            }

            if (result.Value is null)
            {
                _userNotifications.ShowWarning(
                    "Person was not found.",
                    "Person Not Found");
                return;
            }

            Person = result.Value;
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(
                GetExceptionMessage(ex),
                "Search Error");
        }
    }

    private async Task<ApiResult<PersonResponse>> SearchByIdAsync()
    {
        if (!int.TryParse(FilterText.Trim(), out var personId) || personId <= 0)
            return ApiResult<PersonResponse>.Failure(
                "Please enter a valid Person ID.");

        return await _peopleApiClient.GetByIdAsync(personId);
    }

    [RelayCommand]
    private void AddPerson()
    {
        var window = _serviceProvider
            .GetRequiredService<AddEditPersonWin>();

        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();
    }

    partial void OnSelectedLicenseClassChanged(
        LicenseClassResponse? value) =>
        OnPropertyChanged(nameof(SelectedLicenseClassId));

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}