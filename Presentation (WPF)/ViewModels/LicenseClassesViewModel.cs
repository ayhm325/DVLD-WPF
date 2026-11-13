using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.LicenseClass;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.Views.Windows.ReferenceData;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Presentation.ViewModels;

public partial class LicenseClassesViewModel : ObservableObject
{
    private readonly ILicenseClassesApiClient _licenseClassesApiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IApiNotificationService _notifications;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasLoaded;
    [ObservableProperty] private LicenseClassResponse? _selectedLicenseClass;

    public ObservableCollection<LicenseClassResponse> LicenseClasses { get; } = [];
    public ICollectionView FilteredLicenseClasses { get; }
    public bool HasNoResults => HasLoaded && FilteredLicenseClasses.IsEmpty;
    public bool CanManage => string.Equals(_currentUserSession.Role, "Admin", StringComparison.OrdinalIgnoreCase);

    private readonly ICurrentUserSession _currentUserSession;

    public LicenseClassesViewModel(
        ILicenseClassesApiClient licenseClassesApiClient,
        IServiceProvider serviceProvider,
        IApiNotificationService notifications,
        ICurrentUserSession currentUserSession)
    {
        _licenseClassesApiClient = licenseClassesApiClient ?? throw new ArgumentNullException(nameof(licenseClassesApiClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _currentUserSession = currentUserSession ?? throw new ArgumentNullException(nameof(currentUserSession));
        FilteredLicenseClasses = System.Windows.Data.CollectionViewSource.GetDefaultView(LicenseClasses);
        FilteredLicenseClasses.Filter = MatchesSearch;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilteredLicenseClasses.Refresh();
        OnPropertyChanged(nameof(HasNoResults));
    }

    partial void OnHasLoadedChanged(bool value) => OnPropertyChanged(nameof(HasNoResults));

    partial void OnSelectedLicenseClassChanged(LicenseClassResponse? value) =>
        EditSelectedCommand.NotifyCanExecuteChanged();

    private bool MatchesSearch(object item) =>
        item is LicenseClassResponse licenseClass &&
        (string.IsNullOrWhiteSpace(SearchText) ||
         licenseClass.LicenseClassName.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase) ||
         licenseClass.LicenseClassDescription.Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase));

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        HasLoaded = false;
        try
        {
            var result = await _licenseClassesApiClient.GetAllAsync(cancellationToken);
            if (_notifications.ShowFailure(result, "Load License Classes Failed"))
                return;

            LicenseClasses.Clear();
            if (result.Value is not null)
                foreach (var licenseClass in result.Value)
                    LicenseClasses.Add(licenseClass);

            FilteredLicenseClasses.Refresh();
            HasLoaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanEditSelected() => CanManage && SelectedLicenseClass is not null;

    [RelayCommand(CanExecute = nameof(CanEditSelected))]
    private async Task EditSelectedAsync()
    {
        if (!CanManage || SelectedLicenseClass is null)
            return;

        var editViewModel = _serviceProvider.GetRequiredService<UpdateLicenseClassViewModel>();
        await editViewModel.InitializeAsync(SelectedLicenseClass.LicenseClassId);
        if (editViewModel.CurrentLicenseClass is null)
            return;

        var window = new EditLicenseClassWindow(editViewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (window.ShowDialog() == true)
            await LoadAsync(CancellationToken.None);
    }
}
