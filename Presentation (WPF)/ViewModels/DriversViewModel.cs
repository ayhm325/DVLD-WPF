using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Driver;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class DriversViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IPeopleApiClient _peopleApiClient;

    private List<DriverResponse> _allDrivers = [];

    public DriversViewModel(
        IServiceProvider serviceProvider,
        IDriversApiClient driversApiClient,
        IPeopleApiClient peopleApiClient)
    {
        _serviceProvider =
            serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        _driversApiClient =
            driversApiClient
            ?? throw new ArgumentNullException(nameof(driversApiClient));

        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));
    }

    [ObservableProperty]
    private DriverResponse? selectedDriver;

    public ObservableCollection<DriverResponse> Drivers { get; } = [];

    private int _driversCount;

    public int DriversCount
    {
        get => _driversCount;
        private set => SetProperty(ref _driversCount, value);
    }

    public async Task LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var result =
            await _driversApiClient.GetAllAsync(
                cancellationToken);

        if (result.IsFailure ||
            result.Value is null)
        {
            _allDrivers = [];
            Drivers.Clear();
            DriversCount = 0;
            return;
        }

        _allDrivers =
            result.Value.ToList();

        FilterDrivers(
            string.Empty,
            "None");
    }

    public void FilterDrivers(
        string filterValue,
        string filterBy)
    {
        IEnumerable<DriverResponse> filtered =
            _allDrivers;

        if (!string.IsNullOrWhiteSpace(filterValue))
        {
            var value =
                filterValue.Trim();

            if (filterBy == "Driver ID")
            {
                filtered =
                    _allDrivers.Where(driver =>
                        driver.DriverId
                            .ToString()
                            .Contains(
                                value,
                                StringComparison.OrdinalIgnoreCase));
            }
            else if (filterBy == "Person ID")
            {
                filtered =
                    _allDrivers.Where(driver =>
                        driver.PersonId
                            .ToString()
                            .Contains(
                                value,
                                StringComparison.OrdinalIgnoreCase));
            }
            else if (filterBy == "Full Name")
            {
                filtered =
                    _allDrivers.Where(driver =>
                        driver.FullName.Contains(
                            value,
                            StringComparison.OrdinalIgnoreCase));
            }
        }

        Drivers.Clear();

        foreach (var driver in filtered)
        {
            Drivers.Add(driver);
        }

        DriversCount =
            Drivers.Count;
    }

    [RelayCommand]
    private async Task ShowLicenseHistory()
    {
        if (SelectedDriver is null)
            return;

        var personId =
            SelectedDriver.PersonId;

        var vm =
            _serviceProvider
                .GetRequiredService<LicenseHistoryViewModel>();

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

    [RelayCommand]
    private void ShowPersonInfo()
    {
        if (SelectedDriver is null)
            return;

        var window =
            new PersonDetailsWindow(
                SelectedDriver.PersonId,
                _peopleApiClient)
            {
                Owner =
                    System.Windows.Application.Current.MainWindow
            };

        window.ShowDialog();
    }
}