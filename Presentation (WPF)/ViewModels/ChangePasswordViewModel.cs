using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace Presentation.ViewModels
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly IUserService _userService;

        // خصائص المستخدم (تم تحويلها لـ ObservableProperty لضمان العمل مع الـ Binding)
        [ObservableProperty] private int _userId;
        [ObservableProperty] private string _userName = string.Empty;

        // === 1. خصائص كلمات المرور (الخاصة) ===
        // ملاحظة: [ObservableProperty] سيقوم بإنشاء CurrentPassword, NewPassword, ConfirmNewPassword تلقائياً
        [ObservableProperty] private string _currentPassword = string.Empty;
        [ObservableProperty] private string _newPassword = string.Empty;
        [ObservableProperty] private string _confirmNewPassword = string.Empty;

        // === 2. خصائص العين (الظهور/الإخفاء) ===
        [ObservableProperty] private bool _isCurrentPasswordVisible;
        [ObservableProperty] private bool _isNewPasswordVisible;
        [ObservableProperty] private bool _isConfirmNewPasswordVisible;

        public ChangePasswordViewModel(IUserService userService)
        {
            _userService = userService;
        }

        // === 3. أوامر العين ===
        [RelayCommand]
        private void ToggleCurrentPassword() => IsCurrentPasswordVisible = !IsCurrentPasswordVisible;

        [RelayCommand]
        private void ToggleNewPassword() => IsNewPasswordVisible = !IsNewPasswordVisible;

        [RelayCommand]
        private void ToggleConfirmPassword() => IsConfirmNewPasswordVisible = !IsConfirmNewPasswordVisible;

        // === 4. أمر تغيير كلمة المرور ===
        [RelayCommand]
        private async Task ChangePassword()
        {
            // التحقق من الحقول
            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword) ||
                string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentPassword == NewPassword)
            {
                MessageBox.Show("New password must be different from current.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // تنفيذ التغيير
                bool result = await _userService.ChangePasswordAsync(UserId, CurrentPassword, NewPassword);

                if (result)
                {
                    MessageBox.Show("Password changed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                    // === إصلاح خطأ Application.Current ===
                    // نستخدم System.Windows.Application.Current بشكل كامل لتجنب تضارب الأسماء مع مشروعك
                    var window = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                    window?.Close();
                }
                else
                {
                    MessageBox.Show("Current password is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}