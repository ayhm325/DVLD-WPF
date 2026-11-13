using CommunityToolkit.Mvvm.ComponentModel;
using DVLD.Contracts.Country;
using Presentation.Services.Api;
using Presentation.Services.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Presentation.ViewModels;

public partial class CountriesViewModel : ObservableObject
{
    private readonly ICountriesApiClient _countriesApiClient;
    private readonly IApiNotificationService _notifications;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasLoaded;

    public ObservableCollection<CountryResponse> Countries { get; } = [];
    public ICollectionView FilteredCountries { get; }
    public bool HasNoResults => HasLoaded && FilteredCountries.IsEmpty;

    public CountriesViewModel(
        ICountriesApiClient countriesApiClient,
        IApiNotificationService notifications)
    {
        _countriesApiClient = countriesApiClient ?? throw new ArgumentNullException(nameof(countriesApiClient));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        FilteredCountries = System.Windows.Data.CollectionViewSource.GetDefaultView(Countries);
        FilteredCountries.Filter = MatchesSearch;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilteredCountries.Refresh();
        OnPropertyChanged(nameof(HasNoResults));
    }

    partial void OnHasLoadedChanged(bool value) => OnPropertyChanged(nameof(HasNoResults));

    private bool MatchesSearch(object item) =>
        item is CountryResponse country &&
        (string.IsNullOrWhiteSpace(SearchText) ||
         country.CountryName.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        HasLoaded = false;
        try
        {
            var result = await _countriesApiClient.GetAllAsync(cancellationToken);
            if (_notifications.ShowFailure(result, "Load Countries Failed"))
                return;

            Countries.Clear();
            if (result.Value is not null)
                foreach (var country in result.Value)
                    Countries.Add(country);

            FilteredCountries.Refresh();
            HasLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
