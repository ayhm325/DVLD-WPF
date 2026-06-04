using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows;
using Presentation.Models;

namespace Presentation.ViewModels
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        

        // خدمة المستخدم (تأكد من تمريرها عبر الـ Constructor)
        private readonly IUserService _userService;

        public int UserId { get; set; }
        public string? UserName { get; set; }

        // دالة لجلب القيم من الـ PasswordBox في الـ View
        public Func<PasswordData> GetPasswordValues { get; set; } = () => new PasswordData();

        public ChangePasswordViewModel(IUserService userService)
        {
            _userService = userService;
        }

        [RelayCommand]
        private async Task ChangePassword()
        {
            if (GetPasswordValues == null) return;

            // جلب القيم من الـ PasswordBox عبر الـ Delegate
            var passwords = GetPasswordValues();

            // 1. التحقق من ملء الحقول الأساسية
            if (string.IsNullOrWhiteSpace(passwords.Current) ||
                string.IsNullOrWhiteSpace(passwords.New) ||
                string.IsNullOrWhiteSpace(passwords.Confirm))
            {
                MessageBox.Show(
                    "Please fill in all required fields before continuing.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 2. التأكد من تطابق كلمة السر الجديدة مع التأكيد
            if (passwords.New != passwords.Confirm)
            {
                MessageBox.Show(
                    "The new password and confirmation password do not match.",
                    "Password Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 3. التحقق من أن كلمة السر الجديدة ليست هي نفسها القديمة (اختياري)
            if (passwords.Current == passwords.New)
            {
                MessageBox.Show(
                    "The new password must be different from the current password.",
                    "Password Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 4. تنفيذ العملية عبر الـ Service
            try
            {
                // ستقوم الخدمة بالتحقق من صحة (CurrentPassword) مقابل قاعدة البيانات
                bool result = await _userService.ChangePasswordAsync(UserId, passwords.Current, passwords.New);

                if (result)
                {
                    MessageBox.Show(
                        "Your password has been changed successfully.",
                        "Password Updated",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // إغلاق النافذة بعد النجاح
                    var window = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                    window?.Close();
                }
                else
                {
                    // هذا الجزء يظهر إذا كانت كلمة السر القديمة خاطئة
                    MessageBox.Show(
                        "The current password you entered is incorrect. Please try again.",
                        "Authentication Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error occurred: {ex.Message}");

                MessageBox.Show(
                    "An unexpected error occurred while updating the password. Please try again.",
                    "Password Update Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }



    }
}