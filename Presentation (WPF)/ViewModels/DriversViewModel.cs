using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Driver;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows.Drivers;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class DriversViewModel(
    IServiceProvider serviceProvider,
    IDriversApiClient driversApiClient,
    IPeopleApiClient peopleApiClient,
    IApiNotificationService notifications,
    IUserNotificationService userNotifications) : ObservableObject
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IDriversApiClient _driversApiClient =
        driversApiClient ?? throw new ArgumentNullException(nameof(driversApiClient));
    private readonly IPeopleApiClient _peopleApiClient =
        peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
    private readonly IApiNotificationService _notifications =
        notifications ?? throw new ArgumentNullException(nameof(notifications));
    private readonly IUserNotificationService _userNotifications =
        userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));

    private List<DriverListResponse> _allDrivers = [];

    [ObservableProperty] private DriverListResponse? _selectedDriver;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasLoaded;
    [ObservableProperty] private string _loadErrorMessage = string.Empty;

    public ObservableCollection<DriverListResponse> Drivers { get; } = [];
    public bool HasNoDrivers => HasLoaded && !IsLoading && Drivers.Count == 0 && !HasLoadError;
    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(HasNoDrivers));
    partial void OnHasLoadedChanged(bool value) => OnPropertyChanged(nameof(HasNoDrivers));
    partial void OnLoadErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasLoadError));
        OnPropertyChanged(nameof(HasNoDrivers));
    }

    partial void OnSelectedDriverChanged(DriverListResponse? value) =>
        DeleteSelectedCommand.NotifyCanExecuteChanged();

    private int _driversCount;
    public int DriversCount
    {
        get => _driversCount;
        private set => SetProperty(ref _driversCount, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        HasLoaded = false;
        LoadErrorMessage = string.Empty;
        try
        {
            var result = await _driversApiClient.GetAllAsync(cancellationToken);

            if (result.IsFailure)
            {
                LoadErrorMessage = result.Error;
                _allDrivers = [];
                Drivers.Clear();
                DriversCount = 0;
                _notifications.ShowFailure(result, "Load Drivers Failed");
                return;
            }

            if (result.Value is null)
            {
                _allDrivers = [];
                Drivers.Clear();
                DriversCount = 0;
                return;
            }

            _allDrivers = result.Value.ToList();
            FilterDrivers(string.Empty, "None");
        }
        catch (Exception ex)
        {
            LoadErrorMessage = ex.Message;
            _allDrivers = [];
            Drivers.Clear();
            DriversCount = 0;
            _userNotifications.ShowError(ex.Message, "Load Drivers Failed");
        }
        finally
        {
            IsLoading = false;
            HasLoaded = true;
            OnPropertyChanged(nameof(HasNoDrivers));
        }
    }

    public void FilterDrivers(string filterValue, string filterBy)
    {
        IEnumerable<DriverListResponse> filtered = _allDrivers;

        if (!string.IsNullOrWhiteSpace(filterValue))
        {
            var value = filterValue.Trim();

            filtered = filterBy switch
            {
                "Driver ID" => _allDrivers.Where(d =>
                    d.DriverId.ToString().Contains(value, StringComparison.OrdinalIgnoreCase)),
                "Person ID" => _allDrivers.Where(d =>
                    d.PersonId.ToString().Contains(value, StringComparison.OrdinalIgnoreCase)),
                "Full Name" => _allDrivers.Where(d =>
                    d.FullName.Contains(value, StringComparison.OrdinalIgnoreCase)),
                _ => _allDrivers
            };
        }

        Drivers.Clear();

        foreach (var driver in filtered)
            Drivers.Add(driver);

        DriversCount = Drivers.Count;
        OnPropertyChanged(nameof(HasNoDrivers));
    }

    [RelayCommand]
    private async Task ShowLicenseHistory()
    {
        if (SelectedDriver is null)
            return;

        var personId = SelectedDriver.PersonId;
        var vm = _serviceProvider.GetRequiredService<LicenseHistoryViewModel>();

        await vm.LoadAsync(personId);

        var window = new LicenseHistoryWin(vm, personId)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private async Task RegisterDriver()
    {
        var viewModel = _serviceProvider.GetRequiredService<RegisterDriverViewModel>();

        var window = new RegisterDriverWindow(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private bool CanDeleteSelected() => SelectedDriver is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private async Task DeleteSelected()
    {
        if (SelectedDriver is null)
            return;

        var driver = SelectedDriver;
        if (_userNotifications.ShowConfirmation(
                $"Delete driver {driver.FullName} (#{driver.DriverId})? The API will reject deletion if the driver has any license records.",
                "Delete Driver") != System.Windows.MessageBoxResult.Yes)
            return;

        var result = await _driversApiClient.DeleteAsync(driver.DriverId);
        if (_notifications.ShowFailure(result, "Delete Driver Failed"))
            return;

        _userNotifications.ShowInfo("The driver record was deleted.", "Driver Deleted");
        SelectedDriver = null;
        await LoadAsync();
    }

    [RelayCommand]
    private void ShowPersonInfo()
    {
        if (SelectedDriver is null)
            return;

        var window = new PersonDetailsWindow(
            SelectedDriver.PersonId,
            _peopleApiClient,
            _notifications)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }
}
