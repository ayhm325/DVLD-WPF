using Application.DTOs.PersonDTO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Enums;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class PeopleViewModel : ObservableObject
{
    private readonly IPeopleApiClient _peopleApiClient;

    private List<PersonDto> _allPeople = new();

    [ObservableProperty]
    private ObservableCollection<PersonDto> _filteredPeople = new();

    [ObservableProperty]
    private int _peopleCount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _searchToolTip = "Search...";

    [ObservableProperty]
    private PersonDto? _selectedPerson;

    [ObservableProperty]
    private bool _isSearchTextVisible;

    [ObservableProperty]
    private bool _isGenderComboVisible;

    public List<string> FilterTypes { get; } =
        new()
        {
            "None",
            "National No",
            "Name",
            "Gender"
        };

    public List<string> GenderOptions { get; } =
        new()
        {
            "All",
            "Male",
            "Female"
        };

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

    public PeopleViewModel(IPeopleApiClient peopleApiClient)
    {
        _peopleApiClient =
            peopleApiClient
            ?? throw new ArgumentNullException(nameof(peopleApiClient));
    }

    [RelayCommand]
    public async Task LoadPeopleAsync()
    {
        var result =
            await _peopleApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        _allPeople =
            result.Value ?? new List<PersonDto>();

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void OnFilterTypeChanged()
    {
        SearchText = string.Empty;
        SelectedGender = "All";

        IsSearchTextVisible =
            SelectedFilterType is "Name" or "National No";

        IsGenderComboVisible =
            SelectedFilterType == "Gender";

        SearchToolTip =
            SelectedFilterType switch
            {
                "Name" => "Search by Name...",
                "National No" => "Search by National No...",
                _ => "Search..."
            };

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<PersonDto> query = _allPeople;

        if (SelectedFilterType == "National No" &&
            !string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(
                p =>
                    !string.IsNullOrWhiteSpace(p.NationalNo) &&
                    p.NationalNo.Contains(
                        SearchText,
                        StringComparison.CurrentCultureIgnoreCase));
        }
        else if (SelectedFilterType == "Name" &&
                 !string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(
                p =>
                    !string.IsNullOrWhiteSpace(p.FullName) &&
                    p.FullName.Contains(
                        SearchText,
                        StringComparison.CurrentCultureIgnoreCase));
        }
        else if (SelectedFilterType == "Gender")
        {
            if (SelectedGender == "Male")
            {
                query = query.Where(
                    p => p.Gender == Gender.Male);
            }
            else if (SelectedGender == "Female")
            {
                query = query.Where(
                    p => p.Gender == Gender.Female);
            }
        }

        var filteredList =
            query.ToList();

        FilteredPeople =
            new ObservableCollection<PersonDto>(
                filteredList);

        PeopleCount =
            filteredList.Count;
    }

    [RelayCommand]
    private void ShowDetails(PersonDto? person)
    {
        if (person is null)
            return;

        var apiClient =
            App.ServiceProvider
                .GetRequiredService<IPeopleApiClient>();

        var detailsWindow =
            new PersonDetailsWindow(
                person.PersonId,
                apiClient)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        detailsWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task AddNewPerson()
    {
        var addEditViewModel =
            App.ServiceProvider
                .GetRequiredService<AddEditPersonViewModel>();

        await addEditViewModel.InitializeAsync(null);

        var window =
            new AddEditPersonWin(addEditViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        window.ShowDialog();

        await LoadPeopleAsync();
    }

    [RelayCommand]
    private async Task EditPerson(PersonDto? person)
    {
        if (person is null)
            return;

        var addEditViewModel =
            App.ServiceProvider
                .GetRequiredService<AddEditPersonViewModel>();

        await addEditViewModel.InitializeAsync(
            person.PersonId);

        var window =
            new AddEditPersonWin(addEditViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };

        window.ShowDialog();

        await LoadPeopleAsync();
    }

    [RelayCommand]
    private async Task DeletePersonAsync(PersonDto? person)
    {
        if (person is null)
            return;

        var confirmation =
            MessageBox.Show(
                $"Are you sure you want to delete {person.FullName}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
            return;

        var result =
            await _peopleApiClient.DeleteAsync(
                person.PersonId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Delete Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        await LoadPeopleAsync();

        MessageBox.Show(
            "Person deleted successfully.",
            "Success",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SendEmail(PersonDto? person)
    {
    }

    [RelayCommand]
    private void PhoneCall(PersonDto? person)
    {
    }
}