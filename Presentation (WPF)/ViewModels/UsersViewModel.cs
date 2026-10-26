using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.User;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.Services.UI;
using Presentation.ViewModels;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUsersApiClient _usersApiClient;
    private readonly IPeopleApiClient _peopleApiClient;
    private readonly IApiNotificationService _notifications;
    private readonly IUserNotificationService _userNotifications;
    private List<UserResponse> _allUsers = new();

    [ObservableProperty] private ObservableCollection<UserResponse> _users = new();
    [ObservableProperty] private UserResponse? _selectedUser;
    [ObservableProperty] private int _usersCount;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedFilterType = "None";
    [ObservableProperty] private string _selectedStatus = "All";
    [ObservableProperty] private bool _isSearchVisible;
    [ObservableProperty] private bool _isStatusVisible;

    public List<string> FilterOptions { get; } = new() { "None", "UserID", "UserName", "Status" };
    public List<string> StatusOptions { get; } = new() { "All", "Active", "Inactive" };

    public UsersViewModel(
        IUsersApiClient usersApiClient,
        IPeopleApiClient peopleApiClient,
        IApiNotificationService notifications,
        IUserNotificationService userNotifications)
    {
        _usersApiClient = usersApiClient;
        _peopleApiClient = peopleApiClient;
        _notifications = notifications;
        _userNotifications = userNotifications;
    }

    private async void OnUserSaveCompleted(bool success)
    {
        if (success)
            await LoadUsersAsync();
    }

    partial void OnSelectedFilterTypeChanged(string value)
    {
        SearchText = string.Empty;
        SelectedStatus = "All";
        IsSearchVisible = value is "UserID" or "UserName";
        IsStatusVisible = value == "Status";
        ApplyFilter();
    }

    partial void OnSelectedStatusChanged(string value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<UserResponse> filtered = _allUsers;

        if (SelectedFilterType == "UserID" && int.TryParse(SearchText, out var id))
            filtered = filtered.Where(u => u.UserId == id);
        else if (SelectedFilterType == "UserName" && !string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(u => u.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        else if (SelectedFilterType == "Status")
            filtered = SelectedStatus == "Active"
                ? filtered.Where(u => u.IsActive)
                : SelectedStatus == "Inactive"
                    ? filtered.Where(u => !u.IsActive)
                    : filtered;

        Users = new(filtered);
        UsersCount = Users.Count;
    }

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        var result = await _usersApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            _allUsers.Clear();
            Users.Clear();
            UsersCount = 0;
            _notifications.ShowFailure(result, "Load Users Failed");
            return;
        }

        _allUsers = result.Value?.ToList() ?? new();
        ApplyFilter();
    }

    [RelayCommand]
    private async Task AddNewUser()
    {
        await OpenEditWindowAsync(null);
    }

    [RelayCommand]
    private async Task ShowDetails()
    {
        if (SelectedUser is null)
            return;

        var vm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();
        await vm.InitializeAsync(SelectedUser.UserId);

        var window = App.ServiceProvider.GetRequiredService<UserDetailsWindow>();
        window.DataContext = vm;
        window.ShowDialog();
    }

    [RelayCommand]
    private async Task AddUser()
    {
        await OpenEditWindowAsync(null);
    }

    [RelayCommand]
    private async Task EditUser()
    {
        if (SelectedUser is null)
            return;

        await OpenEditWindowAsync(SelectedUser.UserId);
    }

    private async Task OpenEditWindowAsync(int? userId)
    {
        var vm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();
        await vm.InitializeAsync(userId);

        vm.SaveCompleted += OnUserSaveCompleted;

        try
        {
            var window = new AddEditUserWin(vm) { Owner = System.Windows.Application.Current.MainWindow };
            window.ShowDialog();
        }
        finally
        {
            vm.SaveCompleted -= OnUserSaveCompleted;
        }
    }

    [RelayCommand]
    private async Task DeleteUser()
    {
        if (SelectedUser is null)
            return;

        var confirmation = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete {SelectedUser.UserName}?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (confirmation != System.Windows.MessageBoxResult.Yes)
            return;

        var result = await _usersApiClient.DeleteAsync(SelectedUser.UserId);

        if (result.IsFailure)
        {
            _notifications.ShowFailure(result, "Delete Failed");
            return;
        }

        await LoadUsersAsync();
        _userNotifications.ShowInfo("User deleted successfully.", "Success");
    }

    [RelayCommand]
    private void ChangePassword()
    {
        if (SelectedUser is null)
            return;

        var vm = App.ServiceProvider.GetRequiredService<ChangePasswordViewModel>();
        vm.UserId = SelectedUser.UserId;
        vm.UserName = SelectedUser.UserName;

        new ChangePasswordWindow(vm).ShowDialog();
    }

    [RelayCommand]
    private async Task SendEmail()
    {
        if (SelectedUser is null)
            return;

        var personResult = await _peopleApiClient.GetByIdAsync(SelectedUser.PersonId);
        if (personResult.IsFailure || personResult.Value is null || string.IsNullOrWhiteSpace(personResult.Value.Email))
        {
            _userNotifications.ShowInfo("No email address is registered for this user.", "Email");
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"mailto:{personResult.Value.Email}",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            _userNotifications.ShowError($"Could not open email client: {ex.Message}", "Email");
        }
    }

    [RelayCommand]
    private async Task PhoneCall()
    {
        if (SelectedUser is null)
            return;

        var personResult = await _peopleApiClient.GetByIdAsync(SelectedUser.PersonId);
        if (personResult.IsFailure || personResult.Value is null || string.IsNullOrWhiteSpace(personResult.Value.Phone))
        {
            _userNotifications.ShowInfo("No phone number is registered for this user.", "Phone Call");
            return;
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = $"tel:{personResult.Value.Phone}",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            System.Windows.Clipboard.SetText(personResult.Value.Phone);
            _userNotifications.ShowInfo($"Phone number '{personResult.Value.Phone}' copied to clipboard.", "Phone Call");
        }
    }
}