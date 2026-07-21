using Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DVLD_WPF;
using Microsoft.Extensions.DependencyInjection;
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
            _userService = userService;
            _currentUser = currentUser;
            _serviceProvider = serviceProvider;

            RememberMe = Properties.Settings.Default.RememberMe;

            if (RememberMe)
            {
                Username = Properties.Settings.Default.Username;
                Password = Properties.Settings.Default.Password;
            }
        }

        [ObservableProperty]
        private bool rememberMe;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [RelayCommand]
        private async Task LoginAsync()
        {
            var userResult = await _userService.LoginAsync(
        Username,
        Password);

            if (userResult.IsFailure)
            {
                MessageBox.Show(
                    userResult.Error,
                    "Login Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var user = userResult.Value!;

            _currentUser.UserId = user.UserId;
            _currentUser.Username = user.UserName;
            _currentUser.FullName = user.FullName;

            if (RememberMe)
            {
                Properties.Settings.Default.Username = Username;
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

            var mainWindow =_serviceProvider.GetRequiredService<MainWindow>();

            mainWindow.Show();

            System.Windows.Application.Current.Windows
                .OfType<LoginWindow>()
                .FirstOrDefault()
                ?.Close();
        }        
    }
}
