using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.TestAppointment;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class LDLAppViewModel : ObservableObject
{
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
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

    public LDLAppViewModel(
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        ILicensesApiClient licensesApiClient,
        IServiceProvider serviceProvider,
        IPeopleApiClient peopleApiClient)
    {
        _localApplicationsApiClient = localApplicationsApiClient
            ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));

        _licensesApiClient = licensesApiClient
            ?? throw new ArgumentNullException(nameof(licensesApiClient));

        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        _peopleApiClient = peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));

        _ = LoadApplicationsAsync();
    }

    partial void OnSelectedStatusFilterChanged(string value) =>
        FilterApplications();

    partial void OnSearchTextChanged(string value) =>
        FilterApplications();

    partial void OnSelectedApplicationChanged(
        LocalDrivingLicenseApplicationResponse? value) =>
        RefreshCommands();

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

    // Applications

    [RelayCommand]
    public async Task LoadApplicationsAsync()
    {
        var result = await _localApplicationsApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            _allApplications.Clear();
            Applications.Clear();
            return;
        }

        _allApplications = result.Value ?? new();
        FilterApplications();
        RefreshCommands();
    }

    private void FilterApplications()
    {
        var filtered = _allApplications.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(x =>
                x.FullName?.Contains(
                    SearchText,
                    StringComparison.OrdinalIgnoreCase) == true ||
                x.NationalNo?.Contains(
                    SearchText,
                    StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.Equals(SelectedStatusFilter, "All",
                StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(x =>
                string.Equals(
                    x.ApplicationStatus,
                    SelectedStatusFilter,
                    StringComparison.OrdinalIgnoreCase));
        }

        Applications.Clear();

        foreach (var application in filtered)
            Applications.Add(application);
    }

    // Add New

    [RelayCommand]
    private void AddNew()
    {
        var vm = _serviceProvider
            .GetRequiredService<AddEditLDLAppViewModel>();

        var window = new NewLocalLicnnse(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
        _ = LoadApplicationsAsync();
    }

    // Delete

    private bool CanDelete() =>
        SelectedApplication is not null &&
        !IsStatus("Completed");

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
        if (SelectedApplication is null)
            return;

        var vm = _serviceProvider
            .GetRequiredService<LocalApplicationDetailsViewModel>();

        await vm.LoadAsync(
            SelectedApplication.LocalDrivingLicenseApplicationId);

        var window = new LocalApplicationDetailsWin(
            vm,
            _peopleApiClient)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    // Edit

    private bool CanEdit() =>
        SelectedApplication is not null &&
        IsStatus("New");

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit()
    {
    }

    // Cancel

    private bool CanCancel() =>
        SelectedApplication is not null &&
        !IsStatus("Completed") &&
        !IsStatus("Cancelled");

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task Cancel(int localApplicationId)
    {
        try
        {
            var result =
                await _localApplicationsApiClient
                    .CancelAsync(localApplicationId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "Cancel Application",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

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
        SelectedApplication is not null &&
        IsStatus("New") &&
        SelectedApplication.PassedTest < 3;

    private bool CanScheduleVision() =>
        CanScheduleTests &&
        SelectedApplication!.PassedTest == 0;

    [RelayCommand(CanExecute = nameof(CanScheduleVision))]
    private Task ScheduleVision() =>
        OpenTestAppointment(TestType.Theory);

    private bool CanScheduleWritten() =>
        CanScheduleTests &&
        SelectedApplication!.PassedTest == 1;

    [RelayCommand(CanExecute = nameof(CanScheduleWritten))]
    private Task ScheduleWritten() =>
        OpenTestAppointment(TestType.Written);

    private bool CanScheduleStreet() =>
        CanScheduleTests &&
        SelectedApplication!.PassedTest == 2;

    [RelayCommand(CanExecute = nameof(CanScheduleStreet))]
    private Task ScheduleStreet() =>
        OpenTestAppointment(TestType.Practical);

    private async Task OpenTestAppointment(TestType testType)
    {
        if (SelectedApplication is null)
            return;

        int localApplicationId =
            SelectedApplication.LocalDrivingLicenseApplicationId;

        var vm = _serviceProvider
            .GetRequiredService<TestAppointmentViewModel>();

        await vm.LoadAsync(localApplicationId, testType);

        var window = new TestAppointmentWin(
            vm,
            _peopleApiClient)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();

        await LoadApplicationsAsync();

        SelectedApplication = Applications.FirstOrDefault(x =>
            x.LocalDrivingLicenseApplicationId == localApplicationId);

        RefreshCommands();
        OnPropertyChanged(nameof(CanScheduleTests));
    }

    // Issue License

    private bool CanIssueLicense() =>
        SelectedApplication is not null &&
        SelectedApplication.PassedTest == 3 &&
        !SelectedApplication.HasLicense;

    [RelayCommand(CanExecute = nameof(CanIssueLicense))]
    private async Task IssueLicense()
    {
        int localApplicationId =
            SelectedApplication!.LocalDrivingLicenseApplicationId;

        var window = new IssueDrivingLicenseForTheFirstTimeWin(
            null!,
            _peopleApiClient);

        var vm = ActivatorUtilities.CreateInstance<
            IssueDrivingLicenseForTheFirstTimeViewModel>(
            _serviceProvider,
            localApplicationId,
            window);

        window.DataContext = vm;
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.ShowDialog();

        await LoadApplicationsAsync();

        SelectedApplication = Applications.FirstOrDefault(x =>
            x.LocalDrivingLicenseApplicationId == localApplicationId);

        RefreshCommands();
    }

    // Show License

    private bool CanShowLicense() =>
        SelectedApplication?.HasLicense == true;

    [RelayCommand(CanExecute = nameof(CanShowLicense))]
    private async Task ShowLicense()
    {
        if (SelectedApplication is null)
            return;

        try
        {
            int localApplicationId =
                SelectedApplication.LocalDrivingLicenseApplicationId;

            var result =
                await _licensesApiClient
                    .GetDetailsAsync(localApplicationId);

            if (result.IsFailure)
            {
                MessageBox.Show(
                    result.Error,
                    "License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var license = result.Value;

            if (license is null)
            {
                MessageBox.Show(
                    "License details were not found.",
                    "License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var window = new DriverLicenseInfoWin(license.LicenseId)
            {
                Owner = System.Windows.Application.Current.MainWindow
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
        if (SelectedApplication is null)
            return;

        int personId = SelectedApplication.ApplicantPersonId;

        var vm = _serviceProvider
            .GetRequiredService<LicenseHistoryViewModel>();

        await vm.LoadAsync(personId);

        var window = new LicenseHistoryWin(vm, personId)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    // Helpers

    private bool IsStatus(string status) =>
        string.Equals(
            SelectedApplication?.StatusText,
            status,
            StringComparison.OrdinalIgnoreCase);
}