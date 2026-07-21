using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Helpers;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUserService _userService;

    private List<UserDto> _allUsers = new();

    [ObservableProperty] private ObservableCollection<UserDto> _users = new();
    [ObservableProperty] private UserDto? _selectedUser;
    [ObservableProperty] private PersonDto? _person;
    [ObservableProperty] private int _usersCount;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedFilterType = "None"; // القائمة المنسدلة للفلتر
    [ObservableProperty] private string _selectedStatus = "All"; // للقائمة المنسدلة الخاصة بالنشط/غير نشط

    // خصائص للتحكم في ظهور العناصر
    [ObservableProperty] private bool _isSearchVisible = false;
    [ObservableProperty] private bool _isStatusVisible = false;

    // القوائم المنسدلة
    public List<string> FilterOptions { get; } = new() { "None", "UserID", "UserName", "Status" };
    public List<string> StatusOptions { get; } = new() { "All", "Active", "Inactive" };

    public UsersViewModel(IUserService userService)
    {
        _userService = userService;
    }


    // تأكد من وجود partial void عند التنفيذ
    partial void OnSelectedFilterTypeChanged(string value)
    {
        SearchText = string.Empty;
        SelectedStatus = "All";

        IsSearchVisible = (value == "UserID" || value == "UserName");
        IsStatusVisible = (value == "Status");

        ApplyFilter();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        ApplyFilter();
    }

    // تأكد أن هذا أيضاً موجود
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }


    private void ApplyFilter()
    {
        // الفلترة تتم بناءً على القائمة الأصلية _allUsers
        var filtered = _allUsers.AsEnumerable();

        if (SelectedFilterType == "UserID" && int.TryParse(SearchText, out int id))
        {
            filtered = filtered.Where(u => u.UserId == id);
        }
        else if (SelectedFilterType == "UserName" && !string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(u => u.UserName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
        else if (SelectedFilterType == "Status")
        {
            if (SelectedStatus == "Active") filtered = filtered.Where(u => u.IsActive);
            else if (SelectedStatus == "Inactive") filtered = filtered.Where(u => !u.IsActive);
        }

        // تحديث القائمة المعروضة
        Users = new ObservableCollection<UserDto>(filtered.ToList());
        UsersCount = Users.Count;
    }




    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        var result = await _userService.GetAllUsersAsync();

        if (result.IsFailure)
        {
            _allUsers = new List<UserDto>();

            MessageBox.Show(
                result.Error,
                "Load Users Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            return;
        }

        _allUsers = result.Value!;

        ApplyFilter();
    }

    [RelayCommand]
    private void AddNewUser()
    {
        var addEditVm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();
        _ = addEditVm.InitializeAsync(null); // تهيئة للإضافة

        //MainWindow.Navigation.Navigate(new AddEditUserPage(addEditVm));
        var win = new AddEditUserWin(addEditVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
    }


    [RelayCommand]
    private async Task ShowDetails()
    {
        if (SelectedUser == null) return;
        
        var userDetailsVm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();
      
        await userDetailsVm.InitializeAsync(SelectedUser.UserId);
       
        var detailsWindow = App.ServiceProvider.GetRequiredService<UserDetailsWindow>();

        detailsWindow.DataContext = userDetailsVm;
        detailsWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task AddUser()
    {
        // 1.جلب الـ ViewModel الخاص بصفحة الإضافة / التعديل من الـ ServiceProvider
        var addEditUserVm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();

        // 2. تهيئة الـ ViewModel بـ null (وهذا يعني للمشروع أننا في وضع الإضافة)
        // لاحظ أن دالتك InitializeAsync مهيأة لتستقبل int? userId
        await addEditUserVm.InitializeAsync(null);

        // 3. التنقل لصفحة الإضافة باستخدام الـ Helper الخاص بك
        //MainWindow.Navigation.Navigate(new AddEditUserPage(addEditUserVm));
        var win = new AddEditUserWin(addEditUserVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
    }

    [RelayCommand]
    private async Task EditUser()
    {
        // 1. استخدام الخاصية المولدة (بدون الشرطة السفلية)
        if (SelectedUser == null) return;

        // 2. جلب الـ ViewModel
        var vm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();

        // 3. إضافة await لانتظار تحميل بيانات المستخدم من قاعدة البيانات
        await vm.InitializeAsync(SelectedUser.UserId);

        // 4. الانتقال للصفحة بعد أن أصبحت البيانات جاهزة
        //MainWindow.Navigation.Navigate(new AddEditUserPage(vm));
        var win = new AddEditUserWin(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        win.ShowDialog();
    }

    [RelayCommand]
    private async Task DeleteUser()
    {
        if (SelectedUser == null) return;

        var result = MessageBox.Show($"Are you sure you want to delete {SelectedUser.UserName}?",
                                        "Confirm Delete",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                object value = await _userService.DeleteUserAsync(SelectedUser.UserId);

                await LoadUsersAsync();

                MessageBox.Show("User deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }

    [RelayCommand]
    private void ChangePassword()
    {
       
        if (SelectedUser == null) return;

        var vm = App.ServiceProvider.GetRequiredService<ChangePasswordViewModel>();

        vm.UserId = SelectedUser.UserId;
        vm.UserName = SelectedUser.UserName;

        var win = new ChangePasswordWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand]
    private void SendEmail() { /* ... */ }

    [RelayCommand]
    private void PhoneCall() { /* ... */ }

}