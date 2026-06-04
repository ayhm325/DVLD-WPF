using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD.Domain.Entities;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Helpers;
using Presentation.ViewModels;
using Presentation.Views;
using Presentation.Views.Pages;
using Presentation.Views.Windows;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

public partial class UsersViewModel : ObservableObject
{
    private readonly IUserService _userService;

    [ObservableProperty] private ObservableCollection<UserDto> _users = new();
    [ObservableProperty] private UserDto? _selectedUser;
    [ObservableProperty] private int _usersCount;
    [ObservableProperty] private string _searchText = string.Empty;

    public UsersViewModel(IUserService userService)
    {
        _userService = userService;
    }

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        var list = await _userService.GetAllUsersAsync();
        Users = new ObservableCollection<UserDto>(list);
        UsersCount = list.Count;
    }

    [RelayCommand]
    private void AddNewUser()
    {
        var addEditVm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();
        _ = addEditVm.InitializeAsync(null); // تهيئة للإضافة
        NavigationHelper.Navigate(new AddEditUserPage(addEditVm));
    }

    [RelayCommand]
    private async Task DeleteUser()
    {
        if (SelectedUser == null) return;
        await _userService.DeleteUserAsync(SelectedUser.UserId);
        await LoadUsersAsync();
    }

    [RelayCommand] 
    private async Task ShowDetails() 
    {
        if (SelectedUser == null) return;

        // 1. جلب الـ ViewModel من الـ ServiceProvider
        var userDetailsVm = App.ServiceProvider.GetRequiredService<AddEditUserViewModel>();

        // 2. تجهيز الـ ViewModel بالبيانات (هذه الخطوة مهمة جداً)
        // نستخدم الـ InitializeAsync لملء بيانات الشخص واليوزر بناءً على الـ ID
        await userDetailsVm.InitializeAsync(SelectedUser.UserId);

        // 3. تمرير الـ ViewModel الجاهز للنافذة
        var detailsWindow = new UserDetailsWindow(userDetailsVm);

        // 4. إظهار النافذة
        detailsWindow.ShowDialog();
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
        NavigationHelper.Navigate(new AddEditUserPage(vm));
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
        NavigationHelper.Navigate(new AddEditUserPage(addEditUserVm));
    }


    [RelayCommand]
    private void ChangePassword()
    {
        // بما أنك تستخدم SelectedUser (بـ S كبيرة)، تأكد أنك تستخدم 
        // الخاصية المولدة من [ObservableProperty] وليس الحقل _selectedUser
        if (SelectedUser == null) return;

        var vm = App.ServiceProvider.GetRequiredService<ChangePasswordViewModel>();

        vm.UserId = SelectedUser.UserId;
        vm.UserName = SelectedUser.UserName;

        var win = new ChangePasswordWindow(vm);
        win.ShowDialog();
    }

    [RelayCommand] private void SendEmail() { /* ... */ }
    [RelayCommand] private void PhoneCall() { /* ... */ }

}