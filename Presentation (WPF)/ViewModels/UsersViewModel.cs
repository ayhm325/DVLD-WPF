using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Contracts.User;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services.Api;
using Presentation.ViewModels;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Windows;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUsersApiClient _usersApiClient;

    private List<UserResponse> _allUsers = new();

    [ObservableProperty]
    private ObservableCollection<UserResponse> _users = new();

    [ObservableProperty]
    private UserResponse? _selectedUser;

    [ObservableProperty]
    private int _usersCount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedFilterType = "None";

    [ObservableProperty]
    private string _selectedStatus = "All";

    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private bool _isStatusVisible;

    public List<string> FilterOptions { get; } =
        new() { "None", "UserID", "UserName", "Status" };

    public List<string> StatusOptions { get; } =
        new() { "All", "Active", "Inactive" };

    public UsersViewModel(IUsersApiClient usersApiClient)
    {
        _usersApiClient = usersApiClient;
    }

    private async void OnUserSaveCompleted(bool success)
    {
        if (!success)
            return;

        await LoadUsersAsync();
    }

    partial void OnSelectedFilterTypeChanged(string value)
    {
        SearchText = string.Empty;
        SelectedStatus = "All";

        IsSearchVisible = value == "UserID" || value == "UserName";
        IsStatusVisible = value == "Status";

        ApplyFilter();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<UserResponse> filtered = _allUsers;

        if (SelectedFilterType == "UserID" &&
            int.TryParse(SearchText, out int id))
        {
            filtered = filtered.Where(u => u.UserId == id);
        }
        else if (SelectedFilterType == "UserName" &&
                 !string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(u =>
                u.UserName.Contains(
                    SearchText,
                    StringComparison.OrdinalIgnoreCase));
        }
        else if (SelectedFilterType == "Status")
        {
            if (SelectedStatus == "Active")
            {
                filtered = filtered.Where(u => u.IsActive);
            }
            else if (SelectedStatus == "Inactive")
            {
                filtered = filtered.Where(u => !u.IsActive);
            }
        }

        Users = new ObservableCollection<UserResponse>(filtered);
        UsersCount = Users.Count;
    }

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        var result = await _usersApiClient.GetAllAsync();

        if (result.IsFailure)
        {
            _allUsers = new List<UserResponse>();

            Users = new ObservableCollection<UserResponse>();
            UsersCount = 0;

            MessageBox.Show(
                result.Error,
                "Load Users Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        _allUsers = result.Value?.ToList() ?? new List<UserResponse>();

        ApplyFilter();
    }

    [RelayCommand]
    private void AddNewUser()
    {
        var addEditVm =
            App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();

        _ = addEditVm.InitializeAsync(null);

        var win = new AddEditUserWin(addEditVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        win.ShowDialog();
    }

    [RelayCommand]
    private async Task ShowDetails()
    {
        if (SelectedUser == null)
            return;

        var userDetailsVm =
            App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();

        await userDetailsVm.InitializeAsync(SelectedUser.UserId);

        var detailsWindow =
            App.ServiceProvider.GetRequiredService<UserDetailsWindow>();

        detailsWindow.DataContext = userDetailsVm;
        detailsWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task AddUser()
    {
        var vm =
            App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();

        await vm.InitializeAsync(null);

        vm.SaveCompleted += OnUserSaveCompleted;

        var win = new AddEditUserWin(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        win.ShowDialog();

        vm.SaveCompleted -= OnUserSaveCompleted;
    }

    [RelayCommand]
    private async Task EditUser()
    {
        if (SelectedUser == null)
            return;

        var vm =
            App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();

        await vm.InitializeAsync(SelectedUser.UserId);

        vm.SaveCompleted += OnUserSaveCompleted;

        var win = new AddEditUserWin(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        win.ShowDialog();

        vm.SaveCompleted -= OnUserSaveCompleted;
    }

    [RelayCommand]
    private async Task DeleteUser()
    {
        if (SelectedUser == null)
            return;

        var confirmation = MessageBox.Show(
            $"Are you sure you want to delete {SelectedUser.UserName}?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
            return;

        var result =
            await _usersApiClient.DeleteAsync(SelectedUser.UserId);

        if (result.IsFailure)
        {
            MessageBox.Show(
                result.Error,
                "Delete Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        await LoadUsersAsync();

        MessageBox.Show(
            "User deleted successfully.",
            "Success",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void ChangePassword()
    {
        if (SelectedUser == null)
            return;

        var vm =
            App.ServiceProvider.GetRequiredService<ChangePasswordViewModel>();

        vm.UserId = SelectedUser.UserId;
        vm.UserName = SelectedUser.UserName;

        var win = new ChangePasswordWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void SendEmail()
    {
        // Not implemented yet.
    }

    [RelayCommand]
    private void PhoneCall()
    {
        // Not implemented yet.
    }
}