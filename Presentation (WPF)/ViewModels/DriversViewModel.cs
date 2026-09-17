using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Driver;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class DriversViewModel(
    IServiceProvider serviceProvider,
    IDriversApiClient driversApiClient,
    IPeopleApiClient peopleApiClient,
    IApiNotificationService notifications) : ObservableObject
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IDriversApiClient _driversApiClient =
        driversApiClient ?? throw new ArgumentNullException(nameof(driversApiClient));
    private readonly IPeopleApiClient _peopleApiClient =
        peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
    private readonly IApiNotificationService _notifications =
        notifications ?? throw new ArgumentNullException(nameof(notifications));

    private List<DriverListResponse> _allDrivers = [];

    [ObservableProperty] private DriverListResponse? _selectedDriver;

    public ObservableCollection<DriverListResponse> Drivers { get; } = [];

    private int _driversCount;
    public int DriversCount
    {
        get => _driversCount;
        private set => SetProperty(ref _driversCount, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var result = await _driversApiClient.GetAllAsync(cancellationToken);

        if (result.IsFailure)
        {
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