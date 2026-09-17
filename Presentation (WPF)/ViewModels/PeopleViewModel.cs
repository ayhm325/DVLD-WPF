using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Person;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class PeopleViewModel : ObservableObject
{
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IUserNotificationService _userNotifications;
    private readonly IApiNotificationService _notifications;
    private List<PersonListResponse> _allPeople = [];

    [ObservableProperty] private ObservableCollection<PersonListResponse> _filteredPeople = [];
    [ObservableProperty] private int _peopleCount;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _searchToolTip = "Search...";
    [ObservableProperty] private PersonListResponse? _selectedPerson;
    [ObservableProperty] private bool _isSearchTextVisible;
    [ObservableProperty] private bool _isGenderComboVisible;

    public List<string> FilterTypes { get; } = ["None", "National No", "Name", "Gender"];
    public List<string> GenderOptions { get; } = ["All", "Male", "Female"];

    private string _selectedFilterType = "None";
    public string SelectedFilterType
    {
        get => _selectedFilterType;
        set
        {
            if (SetProperty(ref _selectedFilterType, value))
                OnFilterTypeChanged();
        }
    }

    private string _selectedGender = "All";
    public string SelectedGender
    {
        get => _selectedGender;
        set
        {
            if (SetProperty(ref _selectedGender, value))
                ApplyFilter();
        }
    }

    public PeopleViewModel(
        IPeopleApiClient peopleApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _peopleApiClient = peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
    }

    [RelayCommand]
    public async Task LoadPeopleAsync()
    {
        var result = await _peopleApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Load People Failed");
            return;
        }

        _allPeople = result.Value ?? [];
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void OnFilterTypeChanged()
    {
        SearchText = string.Empty;
        SelectedGender = "All";
        IsSearchTextVisible = SelectedFilterType is "Name" or "National No";
        IsGenderComboVisible = SelectedFilterType == "Gender";
        SearchToolTip = SelectedFilterType switch
        {
            "Name" => "Search by Name...",
            "National No" => "Search by National No...",
            _ => "Search..."
        };
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<PersonListResponse> query = _allPeople;

        if (SelectedFilterType == "National No" && !string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(p => p.NationalNo.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
        else if (SelectedFilterType == "Name" && !string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(p => p.FullName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
        else if (SelectedFilterType == "Gender")
        {
            if (SelectedGender == "Male")
                query = query.Where(p => p.Gender == Gender.Male);
            else if (SelectedGender == "Female")
                query = query.Where(p => p.Gender == Gender.Female);
        }

        var filtered = query.ToList();
        FilteredPeople = new(filtered);
        PeopleCount = filtered.Count;
    }

    [RelayCommand]
    private void ShowDetails(PersonListResponse? person)
    {
        if (person is null)
            return;

        var apiClient = App.ServiceProvider.GetRequiredService<IPeopleApiClient>();
        var window = new PersonDetailsWindow(person.PersonId, apiClient)
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        window.ShowDialog();
    }

    [RelayCommand]
    private async Task AddNewPerson()
    {
        var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
        await vm.InitializeAsync(null);

        var window = new AddEditPersonWin(vm)
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        window.ShowDialog();
        await LoadPeopleAsync();
    }

    [RelayCommand]
    private async Task EditPerson(PersonListResponse? person)
    {
        if (person is null)
            return;

        var vm = App.ServiceProvider.GetRequiredService<AddEditPersonViewModel>();
        await vm.InitializeAsync(person.PersonId);

        var window = new AddEditPersonWin(vm)
        {
            Owner = Application.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        window.ShowDialog();
        await LoadPeopleAsync();
    }

    [RelayCommand]
    private async Task DeletePersonAsync(PersonListResponse? person)
    {
        if (person is null)
            return;

        if (_userNotifications.ShowConfirmation(
         $"Are you sure you want to delete {person.FullName}?",
         "Confirm Delete") != MessageBoxResult.Yes)
            return;

        var result = await _peopleApiClient.DeleteAsync(person.PersonId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Delete Failed");
            return;
        }

        await LoadPeopleAsync();

        _userNotifications.ShowInfo(
            "Person deleted successfully.",
            "Success");
    }

    [RelayCommand]
    private void SendEmail(PersonListResponse? person)
    {
    }

    [RelayCommand]
    private void PhoneCall(PersonListResponse? person)
    {
    }
}