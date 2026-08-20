using Application.DTOs;
using Application.DTOs.UserDTO;
using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Presentation.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser;
        private readonly IServiceProvider _serviceProvider;

        public LoginViewModel(
            IUserService userService,
            ICurrentUserService currentUser,
            IServiceProvider serviceProvider)
        {
            _userService = userService
                ?? throw new ArgumentNullException(nameof(userService));

            _currentUser = currentUser
                ?? throw new ArgumentNullException(nameof(currentUser));

            _serviceProvider = serviceProvider
                ?? throw new ArgumentNullException(nameof(serviceProvider));

            RememberMe = Properties.Settings.Default.RememberMe;

            if (RememberMe)
            {
                Username = Properties.Settings.Default.Username;
                Password = Properties.Settings.Default.Password;
            }
        }

        // =========================
        // PROPERTIES
        // =========================

        [ObservableProperty]
        private bool rememberMe;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;


        // =========================
        // LOGIN
        // =========================

        [RelayCommand]
        private async Task LoginAsync()
        {
            // =========================
            // BASIC VALIDATION
            // =========================

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show(
                    "Username and password are required.",
                    "Login Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            try
            {
                // =========================
                // CREATE LOGIN DTO
                // =========================

                var loginDto = new LoginRequestDto
                {
                    UserName = Username,
                    Password = Password
                };


                // =========================
                // LOGIN
                // =========================

                var userResult =
                    await _userService.LoginAsync(loginDto);


                // =========================
                // LOGIN FAILED
                // =========================

                if (userResult.IsFailure)
                {
                    MessageBox.Show(
                        userResult.Error,
                        "Login Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // =========================
                // GET USER
                // =========================

                var user = userResult.Value!;


                // =========================
                // CURRENT USER
                // =========================

                _currentUser.UserId = user.UserId;
                _currentUser.Username = user.UserName;
                _currentUser.FullName = user.FullName;


                // =========================
                // REMEMBER ME
                // =========================

                if (RememberMe)
                {
                    Properties.Settings.Default.Username = Username;

                    // ملاحظة أمنية:
                    // سيتم لاحقًا مناقشة طريقة حفظ Remember Me
                    // لأن تخزين Password بشكل صريح في Settings
                    // ليس مناسبًا لتطبيق production.

                    Properties.Settings.Default.Password = Password;
                    Properties.Settings.Default.RememberMe = true;
                }
                else
                {
                    Properties.Settings.Default.Username = string.Empty;
                    Properties.Settings.Default.Password = string.Empty;
                    Properties.Settings.Default.RememberMe = false;
                }

                Properties.Settings.Default.Save();


                // =========================
                // OPEN MAIN WINDOW
                // =========================

                var mainWindow =
                    _serviceProvider
                        .GetRequiredService<MainWindow>();

                mainWindow.Show();


                // =========================
                // CLOSE LOGIN WINDOW
                // =========================

                System.Windows.Application.Current.Windows
                    .OfType<LoginWindow>()
                    .FirstOrDefault()
                    ?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An error occurred during login: {ex.Message}",
                    "Login Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}