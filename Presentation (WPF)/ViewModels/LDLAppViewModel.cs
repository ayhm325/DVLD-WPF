using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.TestAppointment;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class LDLAppViewModel : ObservableObject
{
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly IApplicationsApiClient _applicationsApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPeopleApiClient _peopleApiClient;

    private List<LocalDrivingLicenseApplicationResponse> _allApplications = new();

    public ObservableCollection<LocalDrivingLicenseApplicationResponse> Applications { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedFilter = "Full Name";

    public List<string> StatusFilterOptions { get; } =
        new() { "All", "New", "Cancelled", "Completed" };

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    partial void OnSelectedStatusFilterChanged(string value) =>
        FilterApplications();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScheduleTests))]
    [NotifyCanExecuteChangedFor(
        nameof(EditCommand),
        nameof(DeleteCommand),
        nameof(ShowDetailsCommand),
        nameof(CancelCommand),
        nameof(ScheduleVisionCommand),
        nameof(ScheduleWrittenCommand),
        nameof(ScheduleStreetCommand),
        nameof(IssueLicenseCommand),
        nameof(ShowLicenseCommand))]
    private LocalDrivingLicenseApplicationResponse? _selectedApplication;

    partial void OnSelectedApplicationChanged(
        LocalDrivingLicenseApplicationResponse? value) =>
        RefreshCommands();

    partial void OnSearchTextChanged(string value) =>
        FilterApplications();

    public LDLAppViewModel(
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        IApplicationsApiClient applicationsApiClient,
        ILicensesApiClient licensesApiClient,
        IServiceProvider serviceProvider,
        IPeopleApiClient peopleApiClient)
    {
        _localApplicationsApiClient =
            localApplicationsApiClient
            ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));

        _applicationsApiClient =
            applicationsApiClient
            ?? throw new ArgumentNullException(nameof(applicationsApiClient));

        _licensesApiClient =
            licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));

        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        _ = LoadApplicationsAsync();
    }

    private void RefreshCommands()
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        ShowDetailsCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        ScheduleVisionCommand.NotifyCanExecuteChanged();
        ScheduleWrittenCommand.NotifyCanExecuteChanged();
        ScheduleStreetCommand.NotifyCanExecuteChanged();
        IssueLicenseCommand.NotifyCanExecuteChanged();
        ShowLicenseCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public async Task LoadApplicationsAsync()
    {
        var result =
            await _localApplicationsApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            _allApplications.Clear();
            Applications.Clear();
            return;
        }

        _allApplications =
            result.Value
            ?? new List<LocalDrivingLicenseApplicationResponse>();

        FilterApplications();
        RefreshCommands();
    }

    public void FilterApplications()
    {
        var filtered = _allApplications.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(x =>
                (x.FullName?.Contains(
                    SearchText,
                    StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.NationalNo?.Contains(
                    SearchText,
                    StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (SelectedStatusFilter != "All")
        {
            filtered = filtered.Where(x =>
                string.Equals(
                    x.ApplicationStatus,
                    SelectedStatusFilter,
                    StringComparison.OrdinalIgnoreCase));
        }

        Applications.Clear();

        foreach (var item in filtered)
        {
            Applications.Add(item);
        }
    }

    // Add New

    [RelayCommand]
    private void AddNew()
    {
        var addEditVm =
            App.ServiceProvider
                .GetRequiredService<AddEditLDLAppViewModel>();

        var window =
            new NewLocalLicnnse(addEditVm)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();

        _ = LoadApplicationsAsync();
    }

    // Delete

    private bool CanDelete() =>
        SelectedApplication != null &&
        !string.Equals(
            SelectedApplication.StatusText,
            "Completed",
            StringComparison.OrdinalIgnoreCase);

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete(int localApplicationId)
    {
        try
        {
            var result =
                await _localApplicationsApiClient
                    .DeleteAsync(localApplicationId);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error);
                return;
            }

            await LoadApplicationsAsync();

            SelectedApplication = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    // Details

    [RelayCommand]
    private async Task ShowDetails()
    {
        if (SelectedApplication == null)
            return;

        var vm =
            _serviceProvider
                .GetRequiredService<LocalApplicationDetailsViewModel>();

        await vm.LoadAsync(
            SelectedApplication.LocalDrivingLicenseApplicationId);

        var window =
            new LocalApplicationDetailsWin(
                vm,
                _peopleApiClient)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();
    }

    // Edit

    private bool CanEdit() =>
        SelectedApplication != null &&
        string.Equals(
            SelectedApplication.StatusText,
            "New",
            StringComparison.OrdinalIgnoreCase);

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit()
    {
    }

    // Cancel

    private bool CanCancel() =>
        SelectedApplication != null &&
        !string.Equals(
            SelectedApplication.StatusText,
            "Completed",
            StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(
            SelectedApplication.StatusText,
            "Cancelled",
            StringComparison.OrdinalIgnoreCase);

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task Cancel(int localApplicationId)
    {
        try
        {
            var appIdResult =
                await _localApplicationsApiClient
                    .GetApplicationIdAsync(localApplicationId);

            if (appIdResult.IsFailure)
            {
                MessageBox.Show(appIdResult.Error);
                return;
            }

            var result =
                await _applicationsApiClient
                    .CancelAsync(appIdResult.Value);

            if (result.IsFailure)
            {
                MessageBox.Show(result.Error);
                return;
            }

            await LoadApplicationsAsync();

            SelectedApplication = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // Test Scheduling

    public bool CanScheduleTests =>
        SelectedApplication != null &&
        string.Equals(
            SelectedApplication.StatusText,
            "New",
            StringComparison.OrdinalIgnoreCase) &&
        SelectedApplication.PassedTest < 3;

    private bool CanScheduleVision() =>
        SelectedApplication != null &&
        string.Equals(
            SelectedApplication.StatusText,
            "New",
            StringComparison.OrdinalIgnoreCase) &&
        SelectedApplication.PassedTest == 0;

    [RelayCommand(CanExecute = nameof(CanScheduleVision))]
    private async Task ScheduleVision() =>
        await OpenTestAppointment(TestType.Theory);

    private bool CanScheduleWritten() =>
        SelectedApplication != null &&
        string.Equals(
            SelectedApplication.StatusText,
            "New",
            StringComparison.OrdinalIgnoreCase) &&
        SelectedApplication.PassedTest == 1;

    [RelayCommand(CanExecute = nameof(CanScheduleWritten))]
    private async Task ScheduleWritten() =>
        await OpenTestAppointment(TestType.Written);

    private bool CanScheduleStreet() =>
        SelectedApplication != null &&
        string.Equals(
            SelectedApplication.StatusText,
            "New",
            StringComparison.OrdinalIgnoreCase) &&
        SelectedApplication.PassedTest == 2;

    [RelayCommand(CanExecute = nameof(CanScheduleStreet))]
    private async Task ScheduleStreet() =>
        await OpenTestAppointment(TestType.Practical);

    private async Task OpenTestAppointment(
        TestType testType)
    {
        if (SelectedApplication == null)
            return;

        int currentApplicationId =
            SelectedApplication.LocalDrivingLicenseApplicationId;

        var vm =
            _serviceProvider
                .GetRequiredService<TestAppointmentViewModel>();

        await vm.LoadAsync(
            currentApplicationId,
            testType);

        var window =
            new TestAppointmentWin(
                vm,
                _peopleApiClient)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();

        await LoadApplicationsAsync();

        SelectedApplication =
            Applications.FirstOrDefault(x =>
                x.LocalDrivingLicenseApplicationId ==
                currentApplicationId);

        RefreshCommands();

        OnPropertyChanged(
            nameof(CanScheduleTests));
    }

    // Issue License

    private bool CanIssueLicense() =>
        SelectedApplication != null &&
        SelectedApplication.PassedTest == 3 &&
        !SelectedApplication.HasLicense;

    [RelayCommand(CanExecute = nameof(CanIssueLicense))]
    private async Task IssueLicense()
    {
        var window =
            new IssueDrivingLicenseForTheFirstTimeWin(
                null!,
                _peopleApiClient);

        var vm =
            ActivatorUtilities.CreateInstance<
                IssueDrivingLicenseForTheFirstTimeViewModel>(
                _serviceProvider,
                SelectedApplication!.LocalDrivingLicenseApplicationId,
                window);

        window.DataContext = vm;

        window.Owner =
            System.Windows.Application.Current.MainWindow;

        window.ShowDialog();

        int id =
            SelectedApplication.LocalDrivingLicenseApplicationId;

        await LoadApplicationsAsync();

        SelectedApplication =
            Applications.FirstOrDefault(x =>
                x.LocalDrivingLicenseApplicationId == id);

        RefreshCommands();
    }

    // Show License

    private bool CanShowLicense() =>
        SelectedApplication != null &&
        SelectedApplication.HasLicense;

    [RelayCommand(CanExecute = nameof(CanShowLicense))]
    private async Task ShowLicense()
    {
        if (SelectedApplication is null)
            return;

        try
        {
            int localApplicationId =
                SelectedApplication.LocalDrivingLicenseApplicationId;

            int licenseClassId =
                SelectedApplication.LicenseClassId;

            var applicationIdResult =
                await _localApplicationsApiClient
                    .GetApplicationIdAsync(localApplicationId);

            if (applicationIdResult.IsFailure)
            {
                MessageBox.Show(
                    applicationIdResult.Error,
                    "License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var licensesResult =
                await _licensesApiClient
                    .GetByApplicationIdAsync(
                        applicationIdResult.Value);

            if (licensesResult.IsFailure)
            {
                MessageBox.Show(
                    licensesResult.Error,
                    "License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var license =
                licensesResult.Value?
                    .FirstOrDefault(x =>
                        x.LicenseClassId == licenseClassId);

            if (license is null)
            {
                MessageBox.Show(
                    $"License for class " +
                    $"{SelectedApplication.LicenseClassName} " +
                    "was not found.",
                    "License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window =
                new DriverLicenseInfoWin(
                    license.LicenseId)
                {
                    Owner =
                        System.Windows.Application.Current.MainWindow
                };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "License Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // License History

    [RelayCommand]
    private async Task ShowHistory()
    {
        if (SelectedApplication == null)
            return;

        var vm =
            _serviceProvider
                .GetRequiredService<LicenseHistoryViewModel>();

        int personId =
            SelectedApplication.ApplicantPersonId;

        await vm.LoadAsync(personId);

        var window =
            new LicenseHistoryWin(
                vm,
                personId)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();
    }
}