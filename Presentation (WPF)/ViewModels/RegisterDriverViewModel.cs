using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.Driver;
using DVLD.Contracts.Person;
using Presentation.Services.Api;
using Presentation.Services.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Presentation.ViewModels;

public partial class RegisterDriverViewModel : ObservableObject
{
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IDriversApiClient _driversApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _hasLoaded;
    [ObservableProperty] private PersonListResponse? _selectedPerson;

    public ObservableCollection<PersonListResponse> People { get; } = [];
    public ICollectionView FilteredPeople { get; }
    public bool HasNoResults => HasLoaded && FilteredPeople.IsEmpty;

    public RegisterDriverViewModel(
        IPeopleApiClient peopleApiClient,
        IDriversApiClient driversApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _peopleApiClient = peopleApiClient ?? throw new ArgumentNullException(nameof(peopleApiClient));
        _driversApiClient = driversApiClient ?? throw new ArgumentNullException(nameof(driversApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _userNotifications = userNotifications ?? throw new ArgumentNullException(nameof(userNotifications));
        FilteredPeople = System.Windows.Data.CollectionViewSource.GetDefaultView(People);
        FilteredPeople.Filter = MatchesSearch;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilteredPeople.Refresh();
        OnPropertyChanged(nameof(HasNoResults));
    }

    partial void OnHasLoadedChanged(bool value) => OnPropertyChanged(nameof(HasNoResults));

    partial void OnSelectedPersonChanged(PersonListResponse? value) =>
        RegisterCommand.NotifyCanExecuteChanged();

    partial void OnIsSavingChanged(bool value) => RegisterCommand.NotifyCanExecuteChanged();

    private bool MatchesSearch(object item) =>
        item is PersonListResponse person &&
        (string.IsNullOrWhiteSpace(SearchText) ||
         person.FullName.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) ||
         person.NationalNo.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        HasLoaded = false;
        try
        {
            var result = await _peopleApiClient.GetAllAsync(cancellationToken);
            if (_notifications.ShowFailure(result, "Load People Failed"))
                return;

            People.Clear();
            if (result.Value is not null)
                foreach (var person in result.Value)
                    People.Add(person);

            FilteredPeople.Refresh();
            HasLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRegister() => SelectedPerson is not null && !IsSaving;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync(Window window)
    {
        if (SelectedPerson is null || IsSaving)
            return;

        IsSaving = true;
        try
        {
            var result = await _driversApiClient.CreateAsync(
                new CreateDriverRequest { PersonId = SelectedPerson.PersonId });

            if (_notifications.ShowFailure(result, "Register Driver Failed"))
                return;

            _userNotifications.ShowInfo("The person was registered as a driver.", "Driver Registered");
            window.DialogResult = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private static void Close(Window window) => window?.Close();
}
