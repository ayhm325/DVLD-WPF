using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LocalDrivingLicenseApplication;
using DVLD.Contracts.TestAppointment;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class LDLAppViewModel : ObservableObject
{
    private readonly ILocalDrivingLicenseApplicationsApiClient _localApplicationsApiClient;
    private readonly ILicensesApiClient _licensesApiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;
    private List<LocalDrivingLicenseApplicationResponse> _allApplications = [];

    public ObservableCollection<LocalDrivingLicenseApplicationResponse> Applications { get; } = [];
    public List<string> StatusFilterOptions { get; } = ["All", "New", "Cancelled", "Completed"];
    public int PageSize { get; } = 10;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedFilter = "Full Name";
    [ObservableProperty] private string _selectedStatusFilter = "All";
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private bool _hasPreviousPage;
    [ObservableProperty] private bool _hasNextPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanScheduleTests))]
    [NotifyCanExecuteChangedFor(
        nameof(EditCommand), nameof(DeleteCommand), nameof(ShowDetailsCommand),
        nameof(CancelCommand), nameof(ScheduleVisionCommand),
        nameof(ScheduleWrittenCommand), nameof(ScheduleStreetCommand),
        nameof(IssueLicenseCommand), nameof(ShowLicenseCommand))]
    private LocalDrivingLicenseApplicationResponse? _selectedApplication;

    public LDLAppViewModel(
        ILocalDrivingLicenseApplicationsApiClient localApplicationsApiClient,
        ILicensesApiClient licensesApiClient,
        IServiceProvider serviceProvider,
        IPeopleApiClient peopleApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _localApplicationsApiClient = localApplicationsApiClient ?? throw new ArgumentNullException(nameof(localApplicationsApiClient));
        _licensesApiClient = licensesApiClient ?? throw new ArgumentNullException(nameof(licensesApiClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _peopleApiClient = peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
        _ = LoadApplicationsAsync();
    }

    partial void OnSelectedStatusFilterChanged(string value) => FilterApplications();
    partial void OnSearchTextChanged(string value) => FilterApplications();
    partial void OnSelectedApplicationChanged(LocalDrivingLicenseApplicationResponse? value) => RefreshCommands();

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

    public async Task LoadApplicationsAsync()
    {
        var result = await _localApplicationsApiClient.GetAllAsync(CurrentPage, PageSize);

        if (result.IsFailure)
        {
            _allApplications.Clear();
            Applications.Clear();
            SelectedApplication = null;
            TotalPages = 0;
            HasPreviousPage = HasNextPage = false;
            _notifications.ShowFailure(result, "Load Applications Failed");
            RefreshCommands();
            return;
        }

        var page = result.Value!;
        _allApplications = page.Items.ToList();
        TotalPages = page.TotalPages;
        HasPreviousPage = page.HasPreviousPage;
        HasNextPage = page.HasNextPage;

        FilterApplications();
        RefreshCommands();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (!HasNextPage) return;

        CurrentPage++;
        SelectedApplication = null;
        await LoadApplicationsAsync();
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (!HasPreviousPage) return;

        CurrentPage--;
        SelectedApplication = null;
        await LoadApplicationsAsync();
    }

    private void FilterApplications()
    {
        var filtered = _allApplications.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var text = SearchText.Trim();
            filtered = filtered.Where(x =>
                x.FullName?.Contains(text, StringComparison.OrdinalIgnoreCase) == true ||
                x.NationalNo?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.Equals(SelectedStatusFilter, "All", StringComparison.OrdinalIgnoreCase))
            filtered = filtered.Where(x =>
                string.Equals(x.ApplicationStatus, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));

        Applications.Clear();
        foreach (var application in filtered)
            Applications.Add(application);
    }

    [RelayCommand]
    private void AddNew()
    {
        var vm = _serviceProvider.GetRequiredService<AddEditLDLAppViewModel>();
        var window = new NewLocalLicnnse(vm) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        _ = LoadApplicationsAsync();
    }

    private bool CanDelete() => SelectedApplication is not null && !IsStatus("Completed");

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete(int localApplicationId)
    {
        try
        {
            var result = await _localApplicationsApiClient.DeleteAsync(localApplicationId);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Delete Application Failed");
                return;
            }

            await LoadApplicationsAsync();
            SelectedApplication = null;
            _userNotifications.ShowInfo("Application deleted successfully.", "Success");
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(GetExceptionMessage(ex), "Delete Application");
        }
    }

    [RelayCommand]
    private async Task ShowDetails()
    {
        if (SelectedApplication is null) return;

        var vm = _serviceProvider.GetRequiredService<LocalApplicationDetailsViewModel>();
        await vm.LoadAsync(SelectedApplication.LocalDrivingLicenseApplicationId);

        var window = new LocalApplicationDetailsWin(
            vm, _peopleApiClient, _licensesApiClient, _notifications)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    private bool CanEdit() => SelectedApplication is not null && IsStatus("New");

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit()
    {
    }

    private bool CanCancel() =>
        SelectedApplication is not null && !IsStatus("Completed") && !IsStatus("Cancelled");

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private async Task Cancel(int localApplicationId)
    {
        try
        {
            var result = await _localApplicationsApiClient.CancelAsync(localApplicationId);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "Cancel Application");
                return;
            }

            await LoadApplicationsAsync();
            SelectedApplication = null;
            _userNotifications.ShowInfo("Application cancelled successfully.", "Success");
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(GetExceptionMessage(ex), "Cancel Application");
        }
    }

    public bool CanScheduleTests =>
        SelectedApplication is not null &&
        IsStatus("New") &&
        SelectedApplication.PassedTest < 3;

    private bool CanScheduleVision() => CanScheduleTests && SelectedApplication!.PassedTest == 0;
    private bool CanScheduleWritten() => CanScheduleTests && SelectedApplication!.PassedTest == 1;
    private bool CanScheduleStreet() => CanScheduleTests && SelectedApplication!.PassedTest == 2;

    [RelayCommand(CanExecute = nameof(CanScheduleVision))]
    private Task ScheduleVision() => OpenTestAppointment(TestType.Theory);

    [RelayCommand(CanExecute = nameof(CanScheduleWritten))]
    private Task ScheduleWritten() => OpenTestAppointment(TestType.Written);

    [RelayCommand(CanExecute = nameof(CanScheduleStreet))]
    private Task ScheduleStreet() => OpenTestAppointment(TestType.Practical);

    private async Task OpenTestAppointment(TestType testType)
    {
        if (SelectedApplication is null) return;

        var localApplicationId = SelectedApplication.LocalDrivingLicenseApplicationId;
        var vm = _serviceProvider.GetRequiredService<TestAppointmentViewModel>();

        await vm.LoadAsync(localApplicationId, testType);

        var window = new TestAppointmentWin(
            vm, _peopleApiClient, _licensesApiClient, _notifications)
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

    private bool CanIssueLicense() =>
        SelectedApplication is not null &&
        SelectedApplication.PassedTest == 3 &&
        !SelectedApplication.HasLicense;

    [RelayCommand(CanExecute = nameof(CanIssueLicense))]
    private async Task IssueLicense()
    {
        if (SelectedApplication is null) return;

        var localApplicationId = SelectedApplication.LocalDrivingLicenseApplicationId;
        var window = new IssueDrivingLicenseForTheFirstTimeWin(
            null, _peopleApiClient, _licensesApiClient, _notifications)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        var vm = ActivatorUtilities.CreateInstance<IssueDrivingLicenseForTheFirstTimeViewModel>(
            _serviceProvider, localApplicationId, window);

        window.DataContext = vm;
        window.ShowDialog();

        await LoadApplicationsAsync();

        SelectedApplication = Applications.FirstOrDefault(x =>
            x.LocalDrivingLicenseApplicationId == localApplicationId);

        RefreshCommands();
    }

    private bool CanShowLicense() => SelectedApplication?.HasLicense == true;

    [RelayCommand(CanExecute = nameof(CanShowLicense))]
    private async Task ShowLicense()
    {
        if (SelectedApplication is null) return;

        try
        {
            var result = await _licensesApiClient.GetDetailsAsync(
                SelectedApplication.LocalDrivingLicenseApplicationId);

            if (result.IsFailure)
            {
                _notifications.ShowFailure(result, "License");
                return;
            }

            if (result.Value is null)
            {
                _userNotifications.ShowWarning("License details were not found.", "License");
                return;
            }

            var window = new DriverLicenseInfoWin(
                result.Value.LicenseId, _licensesApiClient, _notifications)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError(GetExceptionMessage(ex), "License Error");
        }
    }

    [RelayCommand]
    private async Task ShowHistory()
    {
        if (SelectedApplication is null) return;

        var personId = SelectedApplication.ApplicantPersonId;
        var vm = _serviceProvider.GetRequiredService<LicenseHistoryViewModel>();

        await vm.LoadAsync(personId);

        var window = new LicenseHistoryWin(vm, personId) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
    }

    private bool IsStatus(string status) =>
        string.Equals(SelectedApplication?.StatusText, status, StringComparison.OrdinalIgnoreCase);

    private static string GetExceptionMessage(Exception ex) =>
        ex.InnerException is null
            ? ex.Message
            : $"{ex.Message}{Environment.NewLine}{Environment.NewLine}" +
              $"Inner Exception:{Environment.NewLine}{ex.InnerException.Message}";
}